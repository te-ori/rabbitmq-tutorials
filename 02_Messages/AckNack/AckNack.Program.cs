using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using System.Reflection.Metadata.Ecma335;

namespace Messages.AckNack;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, AckNack!");

        // await AcknowledgementAndNegativeAcknowledgement();       
        await ThrowingExceptionBeforeSendAckWhileConsumerProcessing();
    }

    private static async Task AcknowledgementAndNegativeAcknowledgement()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();

        using var producerChannel = await connection.CreateChannelAsync();
        await producerChannel.QueueDeclareAsync(
            queue: "ack-nack",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var producer = Task.Run(async () =>
        {
            for (int i = 0; ; i++)
            {
                var message = $"Message {i}";
                var body = message.ToMessage();

                await producerChannel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "ack-nack",
                    body: body);

                WriteLine($"Sent: {message}");
                await Task.Delay(15000);
            }
        });

        using var consumerChannel = await connection.CreateChannelAsync();
        await consumerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        var consumer = new AsyncEventingBasicConsumer(consumerChannel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToArray().ToMessageString();
            WriteLine($"Received: {message}");

            WriteLine("Press 'A' for Ack, 'N' for Nack");

            var key = ReadKey().Key;
            WriteLine();
            switch (key)
            {
                case ConsoleKey.A:
                    await consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                    WriteLine($"{message} Acked, you never see this message again");
                    break;
                case ConsoleKey.N:
                    await consumerChannel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    WriteLine($"{message} Nacked, you will see this message again");
                    break;
                default:
                    WriteLine("Invalid key");
                    break;
            }
        };

        WriteLine("`consumerChannel.BasicConsumeAsync(queue: \"ack-nack\", autoAck: false, consumer: consumer)`");
        WriteLine("By setting autoAck to false, we are responsible for acknowledging the message");
        WriteLine("After message arrived to any consumer, you won't see the message in the qeueue");
        WriteLine("If consumer sends `Ack`, message is removed from the queue");
        WriteLine("If consumer sends `Nack`, or, execution of consumer stopped -because of throwing exception, eg-");
        WriteLine("Message counts as `Nack`ed, and it will be requeued");
        await consumerChannel.BasicConsumeAsync(queue: "ack-nack", autoAck: false, consumer: consumer);

        producer.Wait();
    }

    private static async Task ThrowingExceptionBeforeSendAckWhileConsumerProcessing()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var producerChannel = await connection.CreateChannelAsync();
        await producerChannel.QueueDeclareAsync(
            queue: "throw-exception-before-ack",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var producer = Task.Run(async () =>
        {
            for (int i = 0; ; i++)
            {
                var message = $"Message {i}";
                var body = message.ToMessage();

                await producerChannel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "throw-exception-before-ack",
                    body: body);
                ForegroundColor = ConsoleColor.Green;
                WriteLine($"Sent: {message}");
                ResetColor();
                await Task.Delay(15000);
            }
        });

        var consumerChannel = await connection.CreateChannelAsync();
        await consumerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        int consumerNo = 1;
        consumerChannel.CallbackExceptionAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Red;
            WriteLine($"Exception occured: {eventArgs.Exception.Message}");
            ResetColor();

            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            var consumerFontColor = consumerNo % 2 == 0 ? ConsoleColor.Magenta : ConsoleColor.Cyan;
            consumer.ReceivedAsync += SpawnConsumer($"Consumer #{consumerNo++}", consumerFontColor);
            await consumerChannel.BasicConsumeAsync(queue: "throw-exception-before-ack", autoAck: false, consumer: consumer);
            await Task.CompletedTask;
        };

        var consumer1 = new AsyncEventingBasicConsumer(consumerChannel);
        consumer1.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToArray().ToMessageString();
            ForegroundColor = ConsoleColor.Magenta;
            WriteLine("FIRST CONSUMER");
            WriteLine($"Received: {message}");
            ResetColor();

            throw new Exception("Exception occured before sending Ack");
        };
        await consumerChannel.BasicConsumeAsync(queue: "throw-exception-before-ack", autoAck: false, consumer: consumer1);

        WriteLine("Press any key to exit");
        ReadKey();
    }

    private static AsyncEventHandler<BasicDeliverEventArgs> SpawnConsumer(string lable, ConsoleColor consumerFontColor)
    {
        return async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToArray().ToMessageString();
            ForegroundColor = consumerFontColor;
            WriteLine(lable.ToUpperInvariant());
            WriteLine($"Received: {message}");

            int Millisecond = DateTime.Now.Millisecond;
            WriteLine($"{Millisecond} % 10 = {Millisecond % 10}");
            ResetColor();
            if (Millisecond % 10 > 4)
            {
                throw new Exception("Exception occured before sending Ack");
            }
            else
            {
                await ((AsyncEventingBasicConsumer)sender).Channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
        };
    }
}

