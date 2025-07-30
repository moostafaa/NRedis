using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NRedis.Server.Server
{
    // Represents a connected client
    public class RedisClient
    {
        public string Id { get; }
        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; }
        public IPEndPoint RemoteEndPoint { get; }
        public DateTime ConnectedAt { get; }
        public long CommandCount { get; set; }
        public DateTime LastActivity { get; set; }

        private readonly byte[] _buffer = new byte[4096];
        private readonly StringBuilder _messageBuffer = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public RedisClient(TcpClient tcpClient)
        {
            Id = Guid.NewGuid().ToString("N")[..8];
            TcpClient = tcpClient;
            Stream = tcpClient.GetStream();
            RemoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            ConnectedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }

        public async Task<string[]> ReadCommandAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                while (true)
                {
                    var bytesRead = await Stream.ReadAsync(_buffer, 0, _buffer.Length, cancellationToken);
                    if (bytesRead == 0)
                        return null; // Client disconnected

                    LastActivity = DateTime.UtcNow;
                    var data = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                    _messageBuffer.Append(data);

                    // Try to parse complete Redis command(s)
                    var command = TryParseRedisCommand();
                    if (command != null)
                    {
                        CommandCount++;
                        return command;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task SendResponseAsync(string response, CancellationToken cancellationToken = default)
        {
            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                var bytes = Encoding.UTF8.GetBytes(response);
                await Stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Stream.FlushAsync(cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private string[] TryParseRedisCommand()
        {
            var buffer = _messageBuffer.ToString();

            // Simple Redis protocol parser (RESP)
            if (buffer.StartsWith("*"))
            {
                return ParseRESP(buffer);
            }

            // Fallback: simple space-separated command
            if (buffer.Contains("\r\n"))
            {
                var line = buffer.Substring(0, buffer.IndexOf("\r\n"));
                _messageBuffer.Clear();
                _messageBuffer.Append(buffer.Substring(line.Length + 2));

                return line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }

            return null;
        }

        private string[] ParseRESP(string buffer)
        {
            try
            {
                var lines = buffer.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length < 2) return null;

                // Parse array length
                if (!lines[0].StartsWith("*") || !int.TryParse(lines[0][1..], out var arrayLength))
                    return null;

                var command = new List<string>();
                var lineIndex = 1;

                for (int i = 0; i < arrayLength; i++)
                {
                    if (lineIndex >= lines.Length) return null;

                    // Parse bulk string length
                    if (!lines[lineIndex].StartsWith("$") || !int.TryParse(lines[lineIndex][1..], out var stringLength))
                        return null;

                    lineIndex++;
                    if (lineIndex >= lines.Length) return null;

                    // Get the actual string
                    var str = lines[lineIndex];
                    if (str.Length != stringLength) return null;

                    command.Add(str);
                    lineIndex++;
                }

                // Check if we have complete command
                if (lineIndex <= lines.Length)
                {
                    // Remove parsed command from buffer
                    var remainingBuffer = string.Join("\r\n", lines.Skip(lineIndex));
                    _messageBuffer.Clear();
                    _messageBuffer.Append(remainingBuffer);

                    return command.ToArray();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            try
            {
                Stream?.Dispose();
                TcpClient?.Close();
                _sendLock?.Dispose();
            }
            catch { }
        }
    }
}