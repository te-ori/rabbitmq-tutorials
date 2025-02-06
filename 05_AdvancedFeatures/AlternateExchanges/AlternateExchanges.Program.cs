using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace AdvancedFeatures.AlternateExchanges;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, AlternateExchanges!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();

        #region alternate exchange
        using var alternateExchangeConsumerChannel = await connection.CreateChannelAsync();
        string alternateExchangeName = "alternate-exchange";
        await alternateExchangeConsumerChannel.ExchangeDeclareAsync(
            exchange: alternateExchangeName,
            type: ExchangeType.Fanout,
            durable: false,
            autoDelete: false,
            arguments: null);

        await alternateExchangeConsumerChannel.QueueDeclareAsync(
            queue: "alternate-exchange-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await alternateExchangeConsumerChannel.QueueBindAsync(
            queue: "alternate-exchange-queue",
            exchange: alternateExchangeName,
            routingKey: string.Empty,
            arguments: null);



        var alternateExchangeConsumer = new AsyncEventingBasicConsumer(alternateExchangeConsumerChannel);
        alternateExchangeConsumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            string message = eventArgs.Body.ToMessageString();
            ForegroundColor = ConsoleColor.Red;
            WriteLine($"Undelivered message received received : {message}");
            WriteLine($"Routing key: {eventArgs.RoutingKey}");
            ResetColor();

            await Task.CompletedTask;
        };

        await alternateExchangeConsumerChannel.BasicConsumeAsync(
            queue: "alternate-exchange-queue",
            autoAck: true,
            consumer: alternateExchangeConsumer);
        #endregion alternate exchange

        #region consumer

        var consumerChannel = await connection.CreateChannelAsync();
        string exchangeName = "operations";
        await consumerChannel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                { "alternate-exchange", alternateExchangeName }
            });

        await consumerChannel.QueueDeclareAsync(
            queue: "operations-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await consumerChannel.QueueBindAsync(
            queue: "operations-queue",
            exchange: exchangeName,
            routingKey: "operations.*",
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(alternateExchangeConsumerChannel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            string message = eventArgs.Body.ToMessageString();
            ForegroundColor = ConsoleColor.DarkGreen;
            WriteLine($"Received message: {message}");
            ResetColor();
            await Task.CompletedTask;
        };
        await consumerChannel.BasicConsumeAsync(
            queue: "operations-queue",
            autoAck: false,
            consumer: consumer);

        #endregion

        #region producer
        var producerChannel = await connection.CreateChannelAsync();
        producerChannel.BasicAcksAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Message acked: {eventArgs.DeliveryTag}");
            ResetColor();
            await Task.CompletedTask;
        };

        producerChannel.BasicNacksAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Message nacked: {eventArgs.DeliveryTag}");
            ResetColor();
            await Task.CompletedTask;
        };

        producerChannel.BasicReturnAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Message returned: {eventArgs.Body}");
            ResetColor();
            await Task.CompletedTask;
        };

        while (true)
        {
            Write("Enter message: ");
            string message = ReadLine();
            if (string.IsNullOrWhiteSpace(message)) break;

            WriteLine($"Mod 2 of message is: {message.Length % 2}");

            string routingKey = message.Length % 2 == 0 ? "operations.sum" : "undefined-queue";

            await producerChannel.BasicPublishAsync(
                exchange: "operations",
                routingKey: routingKey,
                body: message.ToMessage());
            await Task.Delay(100);

        }
        #endregion
    }
}

