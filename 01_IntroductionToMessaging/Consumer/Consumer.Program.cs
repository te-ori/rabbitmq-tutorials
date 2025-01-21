using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace IntroductionToMessaging.Consumer;

class Program
{
    static async Task Main(string[] args)
    {
        string consumerName = args.Length > 0 ? args[0] : "Consumer";
        WriteLine($"Hello, {consumerName}!");
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        await channel.QueueDeclareAsync(
            queue: "intro-to-messaging",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToMessageString();
            WriteLine($"Received: {message}");
        };

        await channel.BasicConsumeAsync(
            queue: "intro-to-messaging",
            autoAck: true,
            consumer: consumer);
        
        ReadLine();
    }
}

