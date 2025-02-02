using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace Messages.MessageId;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, MessageId!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var consumerChannel = await connection.CreateChannelAsync();
        await consumerChannel.QueueDeclareAsync(
            queue: "account-transactions",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(consumerChannel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var json = eventArgs.Body.ToMessageString();
            var message = JsonSerializer.Deserialize<ChangeBalanceMessage>(json);

            if (_processedMessages.Contains(eventArgs.BasicProperties.MessageId))
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"Message {eventArgs.BasicProperties.MessageId} already processed");
                ResetColor();
                return;
            }

            _processedMessages.Add(eventArgs.BasicProperties.MessageId);

            if (_accountBalances.ContainsKey(message.AccountId))
            {
                _accountBalances[message.AccountId] += message.Amount;
                ForegroundColor = ConsoleColor.Green;
                WriteLine($"Account {message.AccountId} balance: {_accountBalances[message.AccountId]}");
                ResetColor();
            }
            else
            {
                ForegroundColor = ConsoleColor.DarkYellow;
                WriteLine($"Account {message.AccountId} not found");
                ResetColor();
            }

            WriteLine("To simulate a case to cause resend same message, press 'n' and nack message. Otherwise, press any key to ack message.");
            var key = ReadKey().Key;

            if (key == ConsoleKey.N)
            {
                await consumerChannel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
            else
            {
                await consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
        };
        await consumerChannel.BasicConsumeAsync(
            queue: "account-transactions",
            autoAck: false,
            consumer: consumer);
        
        var producerTask = Task.Run(async () =>
        {
            using var producerChannel = await connection.CreateChannelAsync();
            while (true)
            {
                int accountId = new Random().Next(1, 6);
                decimal amount = new Random().Next(-100, 100);
                string messageId = Guid.NewGuid().ToString();

                var message = new ChangeBalanceMessage(accountId, amount);
                var json = JsonSerializer.Serialize(message);
                var body = json.ToMessage();

                var properties = new BasicProperties
                {
                    MessageId = messageId
                };

                await producerChannel.BasicPublishAsync(
                    exchange: "",
                    mandatory: false,
                    routingKey: "account-transactions",
                    basicProperties: properties,
                    body: body);

                ForegroundColor = ConsoleColor.Blue;
                WriteLine($"Sent: {messageId} - {json}");
                ResetColor();

                int randomDelay = new Random().Next(5000, 10000);
                await Task.Delay(randomDelay);
            }
        });

        await producerTask;

        WriteLine("Press any key to exit");
        ReadKey();
    }

    static Dictionary<int, decimal> _accountBalances = new()
    {
        [1] = 100,
        [2] = 200,
        [3] = 300,
        [4] = 400,
        [5] = 500
    };

    static HashSet<string> _processedMessages = new();
}

public record ChangeBalanceMessage(int AccountId, decimal Amount);
