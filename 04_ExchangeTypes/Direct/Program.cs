using Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using static System.Console;
internal class Program
{
    private static async Task Main(string[] args)
    {
        await DirectQueueViaDefaultExchange();

        WriteLine("Press [enter] to exit");
        ReadLine();
    }

    private static async Task DirectQueueViaDefaultExchange()
    {
        using var manager = new RabbitMqManager();
        await manager.Initialize();

        using var consumerChannel = await manager.CreateChannel();

        string queue1Name = "direct_queue1";
        await consumerChannel.QueueDeclareAsync(queue1Name, true, false, false);
        var consumer1 = new AsyncEventingBasicConsumer(consumerChannel);
        consumer1.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);
            WriteLine($"{queue1Name.ToUpper()} Received message: {message}");
            await Task.CompletedTask;
        };
        await consumerChannel.BasicConsumeAsync(queue1Name, true, consumer1);

        string queue2Name = "direct_queue2";
        await consumerChannel.QueueDeclareAsync(queue2Name, true, false, false);
        var consumer2 = new AsyncEventingBasicConsumer(consumerChannel);
        consumer2.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);
            WriteLine($"{queue2Name.ToUpper()} Received message: {message}");
            await Task.CompletedTask;
        };
        await consumerChannel.BasicConsumeAsync(queue2Name, true, consumer2);
        

        using var producerChannel = await manager.CreateChannel();
        while (true)
        {
            await producerChannel.BasicPublishAsync("", queue1Name, $"{DateTime.Now:G}Hello, {queue1Name}!".ToMessage());
            await producerChannel.BasicPublishAsync("", queue2Name, $"{DateTime.Now:G}Hello, {queue2Name}!".ToMessage());
            
            WriteLine("Press [enter] to send more messages or [q] to quit");
            if (ReadLine() == "q")
            {
                break;
            }
        }
    }
}