using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace IntroductionToMessaging.Producer;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, Producer!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        int i = 0;
        while (true)
        {
            Write("Press enter to send next 10 messages or press `q` quit: ");
            var choise = ReadKey();

            if (choise.Key == ConsoleKey.Q)
            {
                break;
            }
            
            for (int j = 0; j < 10; j++, i++)
            {
                var body = i.ToMessageAsString();
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "intro-to-messaging",
                    body: body);

                WriteLine($"Sent: {i}");
            }
        }

    }
}

