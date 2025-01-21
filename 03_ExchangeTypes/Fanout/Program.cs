using System.Threading.Tasks;
using Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using static System.Console;

namespace Fanout;

class Program
{
    static async Task Main(string[] args)
    {
        using var manager = new RabbitMqManager();
        await manager.Initialize();

        using var consumerChannel = await manager.CreateChannel();

        string exchangeName = "fanout_exchange";
        await consumerChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout);

        // consumer 1
        string queue1Name = "fanout_queue1";
        await consumerChannel.QueueDeclareAsync(queue1Name, true, false, false);
        await consumerChannel.QueueBindAsync(queue1Name, exchangeName, "");
        var consumer1 = new AsyncEventingBasicConsumer(consumerChannel);
        consumer1.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToMessageString();
            WriteLine($"{queue1Name}Received message: {message}");
            await Task.CompletedTask;
        };
        await consumerChannel.BasicConsumeAsync(queue1Name, true, consumer1);

        // consumer 2
        string queue2Name = "fanout_queue2";
        await consumerChannel.QueueDeclareAsync(queue2Name, true, false, false);
        await consumerChannel.QueueBindAsync(queue2Name, exchangeName, "");
        var consumer2 = new AsyncEventingBasicConsumer(consumerChannel);
        consumer2.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToMessageString();
            WriteLine($"{queue2Name}Received message: {message}");
            await Task.CompletedTask;
        };
        await consumerChannel.BasicConsumeAsync(queue2Name, true, consumer2);

        // consumer 3
        string queue3Name = "fanout_queue3";
        await consumerChannel.QueueDeclareAsync(queue3Name, true, false, false);
        await consumerChannel.QueueBindAsync(queue3Name, exchangeName, "");
        var consumer3 = new AsyncEventingBasicConsumer(consumerChannel);
        consumer3.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = eventArgs.Body.ToMessageString();
            WriteLine($"{queue3Name}Received message: {message}");
            await Task.CompletedTask;
        };
        await consumerChannel.BasicConsumeAsync(queue3Name, true, consumer3);

        // producer
        using var producerChannel = await manager.CreateChannel();
        while (true)
        {
            string message = $"{DateTime.Now:G}Hello, fanout_exchange!";
            await producerChannel.BasicPublishAsync(exchangeName, "", message.ToMessage());
            WriteLine($"Sent message: {message}");

            WriteLine("Press [enter] to send more messages or [q] to quit");
            if (ReadLine() == "q")
            {
                break;
            }
        }

    }
}
