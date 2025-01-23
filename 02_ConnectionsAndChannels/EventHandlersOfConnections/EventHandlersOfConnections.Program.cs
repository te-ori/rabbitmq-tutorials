using static System.Console;
using RabbitMQ.Client;
using System.Diagnostics;

namespace ConnectionsAndChannels.EventHandlersOfConnections;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, EventHandlersOfConnections!");

        // await DemonstrateConnectionBlockedEvent();
        // await DemonstrateRecoveryEvents();
        await DemonstrateCallbackExceptionEvent();
    }

    static async Task DemonstrateConnectionBlockedEvent()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        connection.ConnectionBlockedAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Magenta;
            WriteLine($"Connection blocked: {eventArgs.Reason}");
            ResetColor();
            await Task.CompletedTask;
        };

        connection.ConnectionUnblockedAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Connection unblocked");
            ResetColor();
            await Task.CompletedTask;
        };

        string queueName = "blocking-test-queue";
        await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        WriteLine("Before testing, run these commands in RabbitMQ terminal:");
        WriteLine("rabbitmqctl set_vm_memory_high_watermark absolute 10MB");
        WriteLine("\nPress [enter] when ready to flood messages...");
        ReadLine();

        var message = new byte[1024 * 1024]; // 1MB message
        int messageCount = 0;

        while (connection.IsOpen)
        {
            try
            {
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    body: message);

                messageCount++;
                Write($"\rPublished {messageCount} messages");
            }
            catch (Exception ex)
            {
                WriteLine($"\nException: {ex.Message}");
                break;
            }
        }

        WriteLine("To unblock the connection, slightly increace memory capacity of server by running this command in RabbitMQ terminal:");
        WriteLine("rabbitmqctl set_vm_memory_high_watermark absolute 15MB");

        WriteLine("\nPress [enter] to exit. Don't forget to reset memory watermark:");
        WriteLine("rabbitmqctl set_vm_memory_high_watermark 0.4");
        ReadLine();
    }

    static async Task DemonstrateRecoveryEvents()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        };

        Stopwatch stopwatch = new();

        using var connection = await factory.CreateConnectionAsync();

        connection.ConnectionShutdownAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Blue;
            WriteLine($"Connection closed: {eventArgs.ReplyText}");
            ResetColor();
            stopwatch.Start();
            await Task.CompletedTask;
        };

        connection.RecoverySucceededAsync += async (sender, eventArgs) =>
        {
            var elapsed = stopwatch.Elapsed;
            ForegroundColor = ConsoleColor.DarkYellow;
            WriteLine($"It try to recover the connection after {elapsed.TotalSeconds} seconds after connection shutdown.");
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Recovery succeeded");
            ResetColor();
            await Task.CompletedTask;
        };

        connection.ConnectionRecoveryErrorAsync += async (sender, eventArgs) =>
        {
            var elapsed = stopwatch.Elapsed;

            ForegroundColor = ConsoleColor.DarkYellow;
            WriteLine($"It try to recover the connection after {elapsed.TotalSeconds} seconds after connection shutdown.");
            ForegroundColor = ConsoleColor.Red;
            WriteLine($"Recovery error: {eventArgs.Exception.Message}");
            ResetColor();
            await Task.CompletedTask;
        };

        WriteLine("Please, shutdown the RabbitMq server to test the connection closing then press [enter]. ");
        ReadLine();
    }

    static async Task DemonstrateCallbackExceptionEvent()
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

        // The callback exception event is raised when an exception is 
        // thrown in the event handler of connection.
        connection.CallbackExceptionAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Red;
            WriteLine($"Callback exception:");
            WriteLine($"\tMessage: {eventArgs.Exception.Message}\n");
            WriteLine($"\tSource: {eventArgs.Exception.Source}\n");
            WriteLine($"\tStack Trace: {eventArgs.Exception.StackTrace}\n");

            ResetColor();
            await Task.CompletedTask;
        };

        connection.ConnectionShutdownAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Magenta;
            WriteLine($"Connection shutdown: {eventArgs.ReplyText}");
            ResetColor();
            throw new Exception("Connection shutdown");
        };

        connection.RecoverySucceededAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Recovery succeeded");
            ResetColor();
            throw new Exception("Exception when Recovery succeeded");
        };

        connection.ConnectionRecoveryErrorAsync += async (sender, eventArgs) =>
        {
            ForegroundColor = ConsoleColor.DarkYellow;
            WriteLine("Recovery error");
            ResetColor();
            throw new Exception("Exception when Recovery failed");
        };

        WriteLine("Please, shutdown the RabbitMq server to test the connection closing then press [enter]. ");
        ReadLine();
    }
}
