using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace ConnectionsAndChannels.AboutConnections;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, AboutConnections!");

        if (await TestThereIsConnection())
        {
            WriteLine("There is at leas one valid RabbitMq server.");
        }
        else
        {
            WriteLine("Connection is closed");
            return;
        }

        // await ConnectionsThrowsExceptionIfServerInfoIsInvalid();
        // await ConnectionsThrowsExceptionIfCredentailsAreInvalid();
        // await ConnectionMayCloseAfterSuccessfullyConnected();
        // await RabbitMqClientMayRecoveryBrokenConnections();

        WriteLine("Press [enter] to exit");
        ReadLine();
    }

    private static async Task<bool> TestThereIsConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        try
        {
            using var connection = await factory.CreateConnectionAsync();

            foreach (var prop in connection.ServerProperties!)
            {
                string? value = "";
                if (prop.Value is byte[] bytes)
                {
                    value = System.Text.Encoding.UTF8.GetString(bytes);
                }
                else if (prop.Value != null)
                {
                    value = prop.Value.ToString();
                }
                else
                {
                    value = "null";
                }

                WriteLine($"{prop.Key}: {value}");
            }

            return connection.IsOpen;
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
        {
            return false;
        }
    }

    private static async Task ConnectionsThrowsExceptionIfServerInfoIsInvalid()
    {
        var factory = new ConnectionFactory
        {
            HostName = "unknown-rabbit-sever",
            UserName = "guest",
            Password = "guest"
        };

        try
        {
            using var connection = await factory.CreateConnectionAsync();
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        {
            WriteLine($"Exception: {ex.Message}");
        }
    }

    private static async Task ConnectionsThrowsExceptionIfCredentailsAreInvalid()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "unknown-user",
            Password = "guest"
        };

        try
        {
            using var connection = await factory.CreateConnectionAsync();
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        {
            WriteLine($"Exception: {ex.Message}");
        }
    }

    static async Task ConnectionMayCloseAfterSuccessfullyConnected()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        connection.ConnectionShutdownAsync += async (sender, eventArgs) =>
        {
            WriteLine($"Connection closed: {eventArgs.ReplyText}");
            await Task.CompletedTask;
        };

        WriteLine("Please, shutdown the RabbitMq server to test the connection closing then press [enter]. ");
        ReadLine();

        WriteLine($"Is connection open?: {connection.IsOpen}");

        ReadLine();
    }

    static async Task RabbitMqClientMayRecoveryBrokenConnections()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        };

        using var connection = await factory.CreateConnectionAsync();
        connection.ConnectionShutdownAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Red;
            WriteLine($"Connection closed: {eventArgs.ReplyText}");
            ResetColor();
            await Task.CompletedTask;
        };

        connection.RecoverySucceededAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Connection recovered");
            ResetColor();
            await Task.CompletedTask;
        };

        WriteLine("Please, shutdown the RabbitMq server to test the connection closing then press [enter]. ");
        ReadLine();

        WriteLine($"Is connection open?: {connection.IsOpen}");

        WriteLine("Please, start the RabbitMq server to test the connection recovery then press [enter]. ");
        ReadLine();
    }

}

