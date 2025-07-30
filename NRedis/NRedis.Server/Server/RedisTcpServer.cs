using NRedis.Server.EventsArgs;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace NRedis.Server.Server
{
    // Redis TCP Server with Event Loop
    public class RedisTcpServer : IDisposable
    {
        private readonly IPEndPoint _endPoint;
        private readonly IRedisCommandProcessor _commandProcessor;
        private readonly ConcurrentDictionary<string, RedisClient> _clients;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _serverLock;

        private TcpListener _listener;
        private Task _serverTask;
        private volatile bool _isRunning;

        // Configuration
        public int MaxConnections { get; set; } = 1000;
        public TimeSpan ClientTimeout { get; set; } = TimeSpan.FromMinutes(30);
        public int BacklogSize { get; set; } = 100;

        // Events
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<ServerErrorEventArgs> ServerError;

        // Statistics
        public int ConnectedClients => _clients.Count;
        public long TotalCommandsProcessed { get; private set; }
        public DateTime StartTime { get; private set; }

        public RedisTcpServer(IPEndPoint endPoint, IRedisCommandProcessor commandProcessor)
        {
            _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
            _clients = new ConcurrentDictionary<string, RedisClient>();
            _cancellationTokenSource = new CancellationTokenSource();
            _serverLock = new SemaphoreSlim(1, 1);
        }

        public RedisTcpServer(int port, IRedisCommandProcessor commandProcessor)
            : this(new IPEndPoint(IPAddress.Any, port), commandProcessor)
        {
        }

        public async Task StartAsync()
        {
            await _serverLock.WaitAsync();
            try
            {
                if (_isRunning)
                    throw new InvalidOperationException("Server is already running");

                _listener = new TcpListener(_endPoint);
                _listener.Start(BacklogSize);
                _isRunning = true;
                StartTime = DateTime.UtcNow;

                Console.WriteLine($"Redis TCP Server started on {_endPoint}");

                // Start the main server loop
                _serverTask = Task.Run(ServerLoopAsync);

                // Start cleanup task for idle connections
                _ = Task.Run(CleanupLoopAsync);
            }
            finally
            {
                _serverLock.Release();
            }
        }

        public async Task StopAsync()
        {
            await _serverLock.WaitAsync();
            try
            {
                if (!_isRunning) return;

                _isRunning = false;
                _cancellationTokenSource.Cancel();

                _listener?.Stop();

                // Disconnect all clients
                var disconnectTasks = _clients.Values.Select(DisconnectClientAsync);
                await Task.WhenAll(disconnectTasks);

                // Wait for server task to complete
                if (_serverTask != null)
                {
                    await _serverTask;
                }

                Console.WriteLine("Redis TCP Server stopped");
            }
            finally
            {
                _serverLock.Release();
            }
        }

        private async Task ServerLoopAsync()
        {
            try
            {
                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Accept new connections
                        var tcpClient = await _listener.AcceptTcpClientAsync();

                        // Check connection limit
                        if (_clients.Count >= MaxConnections)
                        {
                            tcpClient.Close();
                            OnServerError(new Exception($"Connection limit reached: {MaxConnections}"), "AcceptConnection");
                            continue;
                        }

                        // Handle new client
                        _ = Task.Run(() => HandleClientAsync(tcpClient));
                    }
                    catch (ObjectDisposedException)
                    {
                        // Server is stopping
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning)
                        {
                            OnServerError(ex, "ServerLoop");
                            await Task.Delay(1000); // Brief pause before retrying
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnServerError(ex, "ServerLoopAsync");
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var client = new RedisClient(tcpClient);

            try
            {
                // Add client to collection
                _clients.TryAdd(client.Id, client);
                OnClientConnected(client);

                // Handle client messages
                while (_isRunning && tcpClient.Connected)
                {
                    var command = await client.ReadCommandAsync(_cancellationTokenSource.Token);
                    if (command == null)
                        break; // Client disconnected or error

                    // Process command
                    _ = Task.Run(() => ProcessClientCommandAsync(client, command));
                }
            }
            catch (Exception ex)
            {
                OnServerError(ex, $"HandleClient-{client.Id}");
            }
            finally
            {
                await DisconnectClientAsync(client, "Connection closed");
            }
        }

        private async Task ProcessClientCommandAsync(RedisClient client, string[] command)
        {
            try
            {
                OnMessageReceived(client, command);

                // Process command through the command processor
                var response = await _commandProcessor.ProcessCommandAsync(command, client.Id);

                //Interlocked.Increment(ref TotalCommandsProcessed);

                // Convert response to Redis protocol format
                var redisResponse = FormatRedisResponse(response);

                // Send response back to client
                await client.SendResponseAsync(redisResponse, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                OnServerError(ex, $"ProcessCommand-{client.Id}");

                // Send error response to client
                try
                {
                    var errorResponse = $"-ERR {ex.Message}\r\n";
                    await client.SendResponseAsync(errorResponse, _cancellationTokenSource.Token);
                }
                catch
                {
                    // If we can't send error response, disconnect client
                    await DisconnectClientAsync(client, "Error sending response");
                }
            }
        }

        private string FormatRedisResponse(object response)
        {
            return $"-ERR Unknown error\r\n";
            //if (!response.IsSuccess)
            //{
            //    return $"-ERR {response.ErrorMessage ?? "Unknown error"}\r\n";
            //}

            //return response switch
            //{
            //    RedisSimpleResponse => "+OK\r\n",
            //    RedisStringResponse str => str.Value == null ? "$-1\r\n" : $"${str.Value.Length}\r\n{str.Value}\r\n",
            //    RedisIntegerResponse integer => $":{integer.Value}\r\n",
            //    RedisBooleanResponse boolean => $":{(boolean.Value ? 1 : 0)}\r\n",
            //    RedisDoubleResponse dbl => dbl.Value == null ? "$-1\r\n" : $"${dbl.Value.ToString().Length}\r\n{dbl.Value}\r\n",
            //    RedisArrayResponse array => FormatArrayResponse(array.Value),
            //    RedisSetResponse set => FormatArrayResponse(set.Value.ToList()),
            //    RedisSortedSetResponse sortedSet => FormatSortedSetResponse(sortedSet.Value),
            //    RedisStreamResponse stream => FormatStreamResponse(stream.Value),
            //    RedisTransactionResponse transaction => FormatTransactionResponse(transaction.Value),
            //    RedisBulkResponse bulk => FormatBulkResponse(bulk.Value),
            //    _ => "+OK\r\n"
            //};
        }

        private string FormatArrayResponse(List<string> values)
        {
            if (values == null || values.Count == 0)
                return "*0\r\n";

            var sb = new StringBuilder();
            sb.AppendLine($"*{values.Count}");

            foreach (var value in values)
            {
                if (value == null)
                {
                    sb.AppendLine("$-1");
                }
                else
                {
                    sb.AppendLine($"${value.Length}");
                    sb.AppendLine(value);
                }
            }

            return sb.ToString();
        }

        private string FormatSortedSetResponse(List<SortedSetItem> items)
        {
            if (items == null || items.Count == 0)
                return "*0\r\n";

            var sb = new StringBuilder();
            sb.AppendLine($"*{items.Count * 2}"); // Each item has value and score

            foreach (var item in items)
            {
                sb.AppendLine($"${item.Value.Length}");
                sb.AppendLine(item.Value);
                var scoreStr = item.Score.ToString();
                sb.AppendLine($"${scoreStr.Length}");
                sb.AppendLine(scoreStr);
            }

            return sb.ToString();
        }

        private string FormatStreamResponse(List<StreamEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return "*0\r\n";

            var sb = new StringBuilder();
            sb.AppendLine($"*{entries.Count}");

            foreach (var entry in entries)
            {
                sb.AppendLine($"*{2 + entry.Fields.Count * 2}"); // ID + field count * 2
                sb.AppendLine($"${entry.Id.Length}");
                sb.AppendLine(entry.Id);

                foreach (var field in entry.Fields)
                {
                    sb.AppendLine($"${field.Key.Length}");
                    sb.AppendLine(field.Key);
                    sb.AppendLine($"${field.Value.Length}");
                    sb.AppendLine(field.Value);
                }
            }

            return sb.ToString();
        }

        private string FormatTransactionResponse(List<object> results)
        {
            if (results == null)
                return "*-1\r\n"; // Transaction aborted

            var sb = new StringBuilder();
            sb.AppendLine($"*{results.Count}");

            foreach (var result in results)
            {
                if (result is RedisResponse redisResp)
                {
                    sb.Append(FormatRedisResponse(redisResp));
                }
                else if (result is Exception ex)
                {
                    sb.AppendLine($"-ERR {ex.Message}");
                }
                else
                {
                    var str = result?.ToString() ?? "";
                    sb.AppendLine($"${str.Length}");
                    sb.AppendLine(str);
                }
            }

            return sb.ToString();
        }

        private string FormatBulkResponse(Dictionary<string, string> values)
        {
            if (values == null || values.Count == 0)
                return "*0\r\n";

            var sb = new StringBuilder();
            sb.AppendLine($"*{values.Count * 2}");

            foreach (var kvp in values)
            {
                sb.AppendLine($"${kvp.Key.Length}");
                sb.AppendLine(kvp.Key);

                if (kvp.Value == null)
                {
                    sb.AppendLine("$-1");
                }
                else
                {
                    sb.AppendLine($"${kvp.Value.Length}");
                    sb.AppendLine(kvp.Value);
                }
            }

            return sb.ToString();
        }

        private async Task DisconnectClientAsync(RedisClient client, string reason = "Unknown")
        {
            try
            {
                _clients.TryRemove(client.Id, out _);
                OnClientDisconnected(client, reason);
                client.Dispose();
            }
            catch (Exception ex)
            {
                OnServerError(ex, $"DisconnectClient-{client.Id}");
            }
        }

        private async Task CleanupLoopAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var clientsToRemove = new List<RedisClient>();

                    foreach (var client in _clients.Values)
                    {
                        if (now - client.LastActivity > ClientTimeout)
                        {
                            clientsToRemove.Add(client);
                        }
                    }

                    foreach (var client in clientsToRemove)
                    {
                        await DisconnectClientAsync(client, "Timeout");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnServerError(ex, "CleanupLoop");
                }
            }
        }

        // Event handlers
        protected virtual void OnClientConnected(RedisClient client)
        {
            ClientConnected?.Invoke(this, new ClientConnectedEventArgs
            {
                ClientId = client.Id,
                RemoteEndPoint = client.RemoteEndPoint
            });
        }

        protected virtual void OnClientDisconnected(RedisClient client, string reason)
        {
            ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs
            {
                ClientId = client.Id,
                Reason = reason
            });
        }

        protected virtual void OnMessageReceived(RedisClient client, string[] command)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs
            {
                ClientId = client.Id,
                Command = command,
                RawMessage = string.Join(" ", command)
            });
        }

        protected virtual void OnServerError(Exception exception, string context)
        {
            ServerError?.Invoke(this, new ServerErrorEventArgs
            {
                Exception = exception,
                Context = context
            });
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
            _serverLock?.Dispose();
        }
    }
}