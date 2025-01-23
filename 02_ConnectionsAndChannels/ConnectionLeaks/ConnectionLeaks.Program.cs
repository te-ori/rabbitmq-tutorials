using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ConnectionsAndChannels.ConnectionLeaks;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, ConnectionLeaks!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
        };

            int connectionCount = 0;
        try
        {
            System.Collections.Concurrent.ConcurrentBag<IConnection>   connections = new();

            while (true)
            {
                var connection = await factory.CreateConnectionAsync();

                connections.Add(connection);
                using var channel = await connection.CreateChannelAsync();

                await channel.BasicPublishAsync("demo-exchange", "demo-routing-key", body: "Hello, RabbitMQ!".ToMessage());

                Interlocked.Increment(ref connectionCount);

                if (connectionCount % 100 == 0)
                {
                    WriteLine($"Created {connectionCount} connections");
                }
            }
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        {
            WriteLine($"{connectionCount} connections created before exception");
            WriteLine($"Exception: {ex.Message}");
        }

        WriteLine("Press [enter] to exit");
    }
}

