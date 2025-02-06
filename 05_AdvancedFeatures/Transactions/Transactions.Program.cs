using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;

namespace AdvancedFeatures.Transactions;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, Transactions!");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await factory.CreateConnectionAsync();
        

        
    }
}

