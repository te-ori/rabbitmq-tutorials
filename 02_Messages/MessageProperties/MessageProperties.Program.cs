using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Messages.MessageProperties;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, MessageProperties!");
        await BasicProperties();
    }

    static async Task BasicProperties()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        var producerChannel = await connection.CreateChannelAsync();
        await producerChannel.QueueDeclareAsync(
            queue: "message-properties",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await producerChannel.BasicPublishAsync(
            exchange: "",
            routingKey: "message-properties",
            body: "Hello, MessageProperties!".ToMessage());

        var consumerChannel = await connection.CreateChannelAsync();
        await consumerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        var consumer = new AsyncEventingBasicConsumer(consumerChannel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToMessageString();
            WriteLine($"Received: {body}");

            var properties = eventArgs.BasicProperties;
            WriteLine($"Message Id: {properties.MessageId}");
            WriteLine($"Timestamp: {properties.Timestamp}");
            WriteLine($"Type: {properties.Type}");
            WriteLine($"Correlation Id: {properties.CorrelationId}");
            WriteLine($"User Id: {properties.UserId}");
            WriteLine($"App Id: {properties.AppId}");
            WriteLine($"Cluster Id: {properties.ClusterId}");
            WriteLine();
            WriteLine($"Content Type: {properties.ContentType}");
            WriteLine($"Content Encoding: {properties.ContentEncoding}");
            WriteLine($"Delivery Mode: {properties.DeliveryMode}");
            WriteLine($"Priority: {properties.Priority}");
            WriteLine($"Reply To: {properties.ReplyTo}");
            WriteLine($"Expiration: {properties.Expiration}");
            WriteLine("Headers:");
            foreach (var header in properties.Headers)
            {
                WriteLine($"  {header.Key}: {header.Value}");
            }

            await Task.Delay(5000);

        };
        await consumerChannel.BasicConsumeAsync(
            queue: "message-properties",
            autoAck: true,
            consumer: consumer);

        ReadLine();

    }
}

