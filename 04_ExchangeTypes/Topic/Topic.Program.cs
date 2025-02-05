using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Topic;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, Topic!");

        using var manager = new RabbitMqManager();
        await manager.Initialize();

        using var logConsumerChannel = await manager.CreateChannel();

        string exchangeName = "log_exchange";
        await logConsumerChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic);

        // error logs consumer
        string errorQueueName = "error_logs";
        await logConsumerChannel.QueueDeclareAsync(errorQueueName, true, false, false);
        await logConsumerChannel.QueueBindAsync(errorQueueName, exchangeName, "log.error.#");
        var errorConsumer = Defaults.AsyncEventingBasicConsumer(logConsumerChannel, errorQueueName, ConsoleColor.DarkCyan);
        await logConsumerChannel.BasicConsumeAsync(errorQueueName, true, errorConsumer);

        // error exceptions logs consumer
        string errorExceptionQueueName = "error_exception_logs";
        await logConsumerChannel.QueueDeclareAsync(errorExceptionQueueName, true, false, false);
        await logConsumerChannel.QueueBindAsync(errorExceptionQueueName, exchangeName, "log.error.exception");
        var errorExceptionConsumer = Defaults.AsyncEventingBasicConsumer(logConsumerChannel, errorExceptionQueueName, ConsoleColor.Red);
        await logConsumerChannel.BasicConsumeAsync(errorExceptionQueueName, true, errorExceptionConsumer);

        // info logs consumer
        string infoQueueName = "info_logs";
        await logConsumerChannel.QueueDeclareAsync(infoQueueName, true, false, false);
        await logConsumerChannel.QueueBindAsync(infoQueueName, exchangeName, "log.info");
        var infoConsumer = Defaults.AsyncEventingBasicConsumer(logConsumerChannel, infoQueueName, ConsoleColor.Green);
        await logConsumerChannel.BasicConsumeAsync(infoQueueName, true, infoConsumer);

        // metric logs consumer
        string metricQueueName = "metric_logs";
        await logConsumerChannel.QueueDeclareAsync(metricQueueName, true, false, false);
        await logConsumerChannel.QueueBindAsync(metricQueueName, exchangeName, "log.#.metric");
        var metricConsumer = Defaults.AsyncEventingBasicConsumer(logConsumerChannel, metricQueueName, ConsoleColor.DarkYellow);
        await logConsumerChannel.BasicConsumeAsync(metricQueueName, true, metricConsumer);

        // all logs consumer
        string allQueueName = "all_logs";
        await logConsumerChannel.QueueDeclareAsync(allQueueName, true, false, false);
        await logConsumerChannel.QueueBindAsync(allQueueName, exchangeName, "log.#");
        var allConsumer = Defaults.AsyncEventingBasicConsumer(logConsumerChannel, allQueueName, ConsoleColor.Magenta
        );
        await logConsumerChannel.BasicConsumeAsync(allQueueName, true, allConsumer);

        using var producerChannel = await manager.CreateChannel();
        var seqNumberEnum = SampleData.GenereateSequentialNumber(1, 1000).GetEnumerator();

        var exmapleRouteKeys = new string[] {
            "log.error",
            "log.error.unknown",
            "log.error.unknown.fatal",
            "log.error.exception", 
            "log.error.404", 
            "log.info",
            "log.info.404", 
            "log.memory.capacity.metric", 
            "log.cpu.usage.metric" };

        foreach (var routeKey in exmapleRouteKeys)
        {
            string message = $"Log message with route key: {routeKey}";
            await producerChannel.BasicPublishAsync(exchangeName, routeKey, message.ToMessage());
            WriteLine($"Sent message: {message}");

            WriteLine("Press any key to send next message...");
            ReadLine();
        }

        while (true)
        {
            WriteLine("Enter route key to send message or [enter] to exit:");
            string? routeKey = ReadLine();
            if (string.IsNullOrWhiteSpace(routeKey))
            {
                break;
            }

            string message = $"Log message with route key: {routeKey}, seq: {seqNumberEnum.Current}";
            await producerChannel.BasicPublishAsync(exchangeName, routeKey, message.ToMessage());

            WriteLine($"Sent message: {message}");
        }
    }
}

