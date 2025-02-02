using static System.Console;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace Messages.ContentType;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, ContentType!");

        WriteLine("Consumer started");
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(
            queue: "server-room-sensor-data",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var contentType = eventArgs.BasicProperties.ContentType;
            var body = eventArgs.Body.ToArray();

            if (messageReaders.ContainsKey(contentType))
            {
                messageReaders[contentType](body);
            }
            else
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"Unknown content type: {contentType}");
                ResetColor();
            }
        };

        await channel.BasicConsumeAsync(
            queue: "server-room-sensor-data",
            autoAck: true,
            consumer: consumer);

        var jsonProducer = Task.Run(async () => { await JsonDataProducer(); });
        var xmlProducer = Task.Run(async () => { await XmlDataProducer(); });

        await Task.WhenAll(jsonProducer, xmlProducer);

        ReadLine();
    }

    static Dictionary<string, Action<byte[]>> messageReaders = new Dictionary<string, Action<byte[]>>
    {
        { "application/xml", XmlMessageReader },
        { "application/json", JsonMessageReader }
    };

    static void XmlMessageReader(byte[] body)
    {
        string xmlString = Encoding.UTF8.GetString(body);
        var xml = new System.Xml.Serialization.XmlSerializer(typeof(ServerRoomSensorData));
        var stringReader = new StringReader(xmlString);
        var data = (ServerRoomSensorData)xml.Deserialize(stringReader);

        ForegroundColor = ConsoleColor.Magenta;
        WriteLine($"Raw Content: {xmlString}");
        WriteLine($"Received: {data}");
        ResetColor();
    }

    static void JsonMessageReader(byte[] body)
    {
        var json = Encoding.UTF8.GetString(body);
        var data = JsonSerializer.Deserialize<ServerRoomSensorData>(json);

        ForegroundColor = ConsoleColor.Green;
        WriteLine($"Raw Content: {json}");
        WriteLine($"Received: {data}");
        ResetColor();
    }

    static async Task JsonDataProducer()
    {
        WriteLine("JSON Producer started");
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        while (true)
        {
            var data = ServerRoomSensorData.CreateRandom();
            var json = JsonSerializer.Serialize(data);
            var body = json.ToMessage();
            var properties = new BasicProperties
            {
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "server-room-sensor-data",
                mandatory: false,
                basicProperties: properties,
                body: body);

            WriteLine($"Sent as JSON: {data}");
            int randomDelay = new Random().Next(5000, 10000);
            await Task.Delay(randomDelay);
        }
    }

    static async Task XmlDataProducer()
    {
        WriteLine("XML Producer started");
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        while (true)
        {
            var data = ServerRoomSensorData.CreateRandom();

            var xml = new System.Xml.Serialization.XmlSerializer(typeof(ServerRoomSensorData));
            var stringWriter = new StringWriter();
            xml.Serialize(stringWriter, data);

            var body = stringWriter.ToString().ToMessage();
            var properties = new BasicProperties
            {
                ContentType = "application/xml"
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "server-room-sensor-data",
                mandatory: false,
                basicProperties: properties,
                body: body);

            WriteLine($"Sent as XML: {data}");
            int randomDelay = new Random().Next(5000, 10000);
            await Task.Delay(randomDelay);
        }
    }
}
