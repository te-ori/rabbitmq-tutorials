using RabbitMQ.Client;

namespace Common;

public class RabbitMqManager : IDisposable
{
    private static ConnectionFactory _factory;
    private static IConnection _connection;
    

    public async Task Initialize()
    {
        _factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
        _connection = await _factory.CreateConnectionAsync();
    }

    public async Task<IChannel> CreateChannel(CreateChannelOptions? options = null)
    {
        if (options != null)
        {
            return await _connection.CreateChannelAsync(options);
        }

        return await _connection.CreateChannelAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
