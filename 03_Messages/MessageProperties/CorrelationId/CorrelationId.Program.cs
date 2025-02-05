using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Messages.CorrelationId;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, CorrelationId!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var producerChannel = await connection.CreateChannelAsync();

        await producerChannel.QueueDeclareAsync(
            queue: "seat-booking",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await producerChannel.QueueDeclareAsync(
            queue: "payment-processor",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await producerChannel.QueueDeclareAsync(
            queue: "sms-gateway",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await producerChannel.QueueDeclareAsync(
            queue: "email-gateway",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        using IChannel seatBookingConsumerChannel = await ConfigureSeatBookingConsumer(connection);
        using IChannel paymentProcessorConsumerChannel = await ConfigurePaymentConsumer(connection);

        while (true)
        {
            var transaction = new BusTicketTransaction().CreateRandomTransaction();
            var json = JsonSerializer.Serialize(transaction);
            var body = json.ToMessage();
            var properties = new BasicProperties
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            _transactions.Add(properties.CorrelationId, BusTicketTransactionState.Started);

            await producerChannel.BasicPublishAsync(
                exchange: "",
                routingKey: "seat-booking",
                mandatory: false,
                basicProperties: properties,
                body: body);

            ForegroundColor = ConsoleColor.Green;
            WriteLine($"Transaction {properties.CorrelationId} started");
            ResetColor();
            await Task.Delay(5000);
        }

        ReadKey();
    }

    private static async Task<IChannel> ConfigurePaymentConsumer(IConnection connection)
    {
        var paymentProcessorConsumerChannel = await connection.CreateChannelAsync();
        var paymentProcessorConsumer = new AsyncEventingBasicConsumer(paymentProcessorConsumerChannel);
        paymentProcessorConsumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var json = eventArgs.Body.ToMessageString();
            var transaction = JsonSerializer.Deserialize<BusTicketTransaction>(json);

            if (!_transactions.ContainsKey(eventArgs.BasicProperties.CorrelationId))
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"Transaction {eventArgs.BasicProperties.CorrelationId} not found");
                ResetColor();
                return;
            }

            if (_transactions[eventArgs.BasicProperties.CorrelationId] != BusTicketTransactionState.SeatSelected)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"Transaction {eventArgs.BasicProperties.CorrelationId} is not in the correct state");
                ResetColor();
                _transactions.Remove(eventArgs.BasicProperties.CorrelationId);
                return;
            }

            _transactions[eventArgs.BasicProperties.CorrelationId] = BusTicketTransactionState.PaymentReceived;
            ForegroundColor = ConsoleColor.Yellow;
            WriteLine($"Payment received for transaction {eventArgs.BasicProperties.CorrelationId}");
            ResetColor();

            var body = JsonSerializer.Serialize(transaction).ToMessage();
            var properties = new BasicProperties()
            {
                CorrelationId = eventArgs.BasicProperties.CorrelationId
            };

            // await ((AsyncEventingBasicConsumer)sender).Channel.BasicPublishAsync(
            //     exchange: "",
            //     mandatory: false,
            //     routingKey: "sms-gateway",
            //     basicProperties: properties,
            //     body: body);

            // await ((AsyncEventingBasicConsumer)sender).Channel.BasicPublishAsync(
            //     exchange: "",
            //     mandatory: false,
            //     routingKey: "email-gateway",
            //     basicProperties: properties,
            //     body: body);
        };
        await paymentProcessorConsumerChannel.BasicConsumeAsync(
            queue: "payment-processor",
            autoAck: true,
            consumer: paymentProcessorConsumer);
        return paymentProcessorConsumerChannel;
    }

    private static async Task<IChannel> ConfigureSeatBookingConsumer(IConnection connection)
    {
        var seatBookingConsumerChannel = await connection.CreateChannelAsync();
        var seatBookingConsumer = new AsyncEventingBasicConsumer(seatBookingConsumerChannel);
        seatBookingConsumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var json = eventArgs.Body.ToMessageString();
            var transaction = JsonSerializer.Deserialize<BusTicketTransaction>(json);

            if (!_transactions.ContainsKey(eventArgs.BasicProperties.CorrelationId))
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"Transaction {eventArgs.BasicProperties.CorrelationId} not found");
                ResetColor();
                return;
            }

            if (_transactions[eventArgs.BasicProperties.CorrelationId] != BusTicketTransactionState.Started)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"Transaction {eventArgs.BasicProperties.CorrelationId} is not in the correct state");
                ResetColor();
                _transactions.Remove(eventArgs.BasicProperties.CorrelationId);
                return;
            }

            transaction.SeatNumber = new Random().Next(1, 100);

            if (transaction.SeatNumber % 10 > 3)
            {
                _transactions[eventArgs.BasicProperties.CorrelationId] = BusTicketTransactionState.SeatSelected;
            }
            else
            {
                _transactions[eventArgs.BasicProperties.CorrelationId] = 0;
            }
            ForegroundColor = ConsoleColor.Green;
            WriteLine($"Seat {transaction.SeatNumber} selected for transaction {eventArgs.BasicProperties.CorrelationId}");
            ResetColor();

            var body = JsonSerializer.Serialize(transaction).ToMessage();
            var properties = new BasicProperties()
            {
                CorrelationId = eventArgs.BasicProperties.CorrelationId
            };

            await ((AsyncEventingBasicConsumer)sender).Channel.BasicPublishAsync(
                exchange: "",
                mandatory: false,
                routingKey: "payment-processor",
                basicProperties: properties,
                body: body);
        };
        await seatBookingConsumerChannel.BasicConsumeAsync(
            queue: "seat-booking",
            autoAck: true,
            consumer: seatBookingConsumer);
        return seatBookingConsumerChannel;
    }

    private static Dictionary<string, BusTicketTransactionState> _transactions = new();
}

public class BusTicketTransaction
{
    public int SeatNumber { get; set; }
    public string PaymentReference { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }

    override public string ToString()
    {
        return $"SeatNumber: {SeatNumber}, PaymentReference: {PaymentReference}, PhoneNumber: {PhoneNumber}, Email: {Email}";
    }

    public BusTicketTransaction CreateRandomTransaction()
    {
        var transaction = new BusTicketTransaction
        {
            SeatNumber = new Random().Next(1, 100),
            PaymentReference = Guid.NewGuid().ToString(),
            PhoneNumber = new Random().Next(100000000, 999999999).ToString(),
            Email = $"{Guid.NewGuid().ToString()}@gmail.com"
        };

        return transaction;
    }
}

enum BusTicketTransactionState
{
    Started = 1,
    SeatSelected = 2,
    PaymentReceived = 4,
    SmsSent = 8,
    EmailSent = 16,
}

