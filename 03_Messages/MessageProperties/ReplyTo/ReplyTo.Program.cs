using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace _Messages.MessageProperties.ReplyTo;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, ReplyTo!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();

        using var consumerChannel = await connection.CreateChannelAsync();
        await consumerChannel.QueueDeclareAsync(
            queue: "replying-consumer-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(consumerChannel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            string message = eventArgs.Body.ToMessageString();
            ForegroundColor = ConsoleColor.Magenta;
            WriteLine($"Received message: {message}");

            string replyTo = eventArgs.BasicProperties.ReplyTo;
            string replyMessage = message.Reverse().Aggregate(string.Empty, (acc, c) => acc + c);
            WriteLine($"Replying to: {replyTo} with message: {replyMessage}");
            ResetColor();
            var channele = ((AsyncEventingBasicConsumer)sender).Channel;
            await channele.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: replyTo,
                body: replyMessage.ToMessage());

            await Task.CompletedTask;
        };
        await consumerChannel.BasicConsumeAsync(
            queue: "replying-consumer-queue",
            autoAck: true,
            consumer: consumer);

        using var producerChannel = await connection.CreateChannelAsync();

        var replyQueueConsumer = new AsyncEventingBasicConsumer(producerChannel);
        replyQueueConsumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            string message = eventArgs.Body.ToMessageString();
            ForegroundColor = ConsoleColor.Cyan;
            WriteLine($"Received reply: {message}");
            WriteLine($"Queue name: {eventArgs.RoutingKey}");
            ResetColor();
            await Task.CompletedTask;
        };

        while (true)
        {
            await Task.Delay(100);
            WriteLine("If `autoDelete` is set to `true`, the queue will be deleted when there is no any consumer listen to it.");
            WriteLine("we create a channel which will dissapear when loop iteated to next iteration.");
            WriteLine("So the only consumer of it will goes then queue also will deleted automatically.");
            Write("Enter reply-queue name (if you do not provide a name, rabbitmq give a random name): ");
            string replyQueueName = ReadLine();

            using var replyQueueChannel = await connection.CreateChannelAsync();

            var replyQueue = await replyQueueChannel.QueueDeclareAsync(
                queue: replyQueueName,
                durable: false,
                exclusive: false,
                autoDelete: true,
                arguments: null);

            if (string.IsNullOrWhiteSpace(replyQueueName))
            {
                replyQueueName = replyQueue.QueueName;
            }
            
            WriteLine($"Now, if you check the queue in RabbitMQ Management UI, you should see the queue {replyQueueName} in the 'Queues and Streams' tab.");

            await replyQueueChannel.BasicConsumeAsync(
                queue: replyQueueName,
                autoAck: true,
                consumer: replyQueueConsumer);

            Write("Enter message: ");
            string message = ReadLine();

            await producerChannel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "replying-consumer-queue",
                basicProperties: new BasicProperties()
                {
                    ReplyTo = replyQueueName
                },
                mandatory: false,
                body: message.ToMessage());

            await Task.Delay(100);
            WriteLine("Message sent. Please check the console for the reply then press [enter] to continue...");
            ReadLine();
        }
    }
}

