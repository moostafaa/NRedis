using NRedis.Server.Server;

namespace NRedis.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var commandProcessor = new RedisCommandProcessor(null);
            var server = new RedisTcpServer(6379, commandProcessor);

            // Subscribe to events
            server.ClientConnected += (sender, e) =>
                Console.WriteLine($"Client connected: {e.ClientId} from {e.RemoteEndPoint}");

            server.ClientDisconnected += (sender, e) =>
                Console.WriteLine($"Client disconnected: {e.ClientId}, Reason: {e.Reason}");

            server.MessageReceived += (sender, e) =>
                Console.WriteLine($"Command from {e.ClientId}: {e.RawMessage}");

            server.ServerError += (sender, e) =>
                Console.WriteLine($"Server error in {e.Context}: {e.Exception.Message}");

            try
            {
                await server.StartAsync();
                Console.WriteLine("Redis server is running. Press any key to stop...");
                Console.ReadKey();
            }
            finally
            {
                await server.StopAsync();
            }
        }
    }
}
