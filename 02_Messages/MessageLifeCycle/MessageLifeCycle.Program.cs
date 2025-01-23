using static System.Console;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messages.MessageLifeCycle;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, MessageLifeCycle!");

        // await MessagesDroppedFromQueueWhenAReceiverConsumedThem();

        await ByDefaultMessagesCountConsumedIfReachAnyConsumer();
    }

    static async Task MessagesDroppedFromQueueWhenAReceiverConsumedThem()
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
            queue: "message-lifecycle",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        await producerChannel.BasicPublishAsync(
            exchange: "",
            routingKey: "message-lifecycle",
            body: "Hello, RabbitMQ!".ToMessage());

        WriteLine("Message sent. Pleace check the queue in RabbitMQ Management UI.");
        WriteLine("You should see the message in the queue.");

        WriteLine("Press [enter] when ready to consume the message...");
        ReadLine();

        using var consumerChannel = await connection.CreateChannelAsync();
        var consumer = new AsyncEventingBasicConsumer(consumerChannel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToMessageString();
            WriteLine($"Received: {message}");
            WriteLine("Please check the queue in RabbitMQ Management UI.");
            WriteLine("You should see the message is gone from the queue and queue should be empty.");
            await Task.CompletedTask;
        };

        await consumerChannel.BasicConsumeAsync(
            queue: "message-lifecycle",
            autoAck: true,
            consumer: consumer);

        ReadLine();
    }

    static async Task ByDefaultMessagesCountConsumedIfReachAnyConsumer()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var producerChannel = await connection.CreateChannelAsync();

        string queueName = "message-return-queue";
        await producerChannel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        await producerChannel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            body: "Hello, RabbitMQ!".ToMessage());

        WriteLine("Message sent. Pleace check the queue in RabbitMQ Management UI.");
        WriteLine("Press [enter] when ready to consume the message...");
        ReadLine();

        using var consumerChannel = await connection.CreateChannelAsync();
        var consumer = new AsyncEventingBasicConsumer(consumerChannel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            throw new Exception("Simulating a failure");
        };

        await consumerChannel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        WriteLine("To drop a message from the queue, the consumer which message is delivered to");
        WriteLine("should send `Basic.Ack` response to RabbitMqServer. By default, RabbitMq client");
        WriteLine("sends `Basic.Ack` response to RabbitMqServer immediately, when the message is reached.");
        WriteLine("So, you won't see message in queue even though an exception thorwed inside handler.");

        ReadLine();
    }
}
