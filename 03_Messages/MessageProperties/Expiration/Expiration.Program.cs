using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Messages.MessageProperties.Expiration;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, Expiration!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();

        using var consumerChannel = await connection.CreateChannelAsync();
        await consumerChannel.QueueDeclareAsync(
            queue: "message-expiration",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(consumerChannel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            string body = eventArgs.Body.ToMessageString();
            int delay = 0;

            if (int.TryParse(body, out delay))
            {
                WriteLine($"Received message: {delay}");
                await Task.Delay(delay);
            }
            else
            {
                WriteLine($"Received message: {body}");
            }

            await consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        };

        await consumerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        await consumerChannel.BasicConsumeAsync(queue: "message-expiration", autoAck: false, consumer: consumer);

        using var producerChannel = await connection.CreateChannelAsync();

        WriteLine("A message waits in the queue until an available consumer is ready to process it or expired.");
        WriteLine("Lets assume it takes 10 second to process this message.");

        await producerChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "message-expiration",
            body: "10000".ToMessage());

        WriteLine("Next three message won't be consumed. They reache to queue while consumer is busy");
        WriteLine("and because expiration time of them are very little they will removed automatically");
        WriteLine("from the queue.");

        await producerChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "message-expiration",
            basicProperties: new BasicProperties()
            {
                Expiration = "100"
            },
            mandatory: false,
            body: "5000".ToMessage());

        await producerChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "message-expiration",
            basicProperties: new BasicProperties()
            {
                Expiration = "1000"
            },
            mandatory: false,
            body: "5000".ToMessage());

        await producerChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "message-expiration",
            basicProperties: new BasicProperties()
            {
                Expiration = "700"
            },
            mandatory: false,
            body: "5000".ToMessage());

        WriteLine("Following message will be consumed because even it reaches to queue while consumer is busy");
        WriteLine("it has enough time to wait for consumer to process it.");
        await producerChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "message-expiration",
            basicProperties: new BasicProperties()
            {
                Expiration = "15000"
            },
            mandatory: false,
            body: "No wait!".ToMessage());

        WriteLine("This message also won't be consumed because it reaches to queue while consumer is busy");
        WriteLine("and it has expiration time of 100 millisecond. It is not important how long previous message will take to process.");
        WriteLine("This message will expire befor it consumed.");
        await producerChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "message-expiration",
            basicProperties: new BasicProperties()
            {
                Expiration = "100"
            },
            mandatory: false,
            body: "10000".ToMessage());

        ReadKey();
    }
}

