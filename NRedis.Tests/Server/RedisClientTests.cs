using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;
using FluentAssertions;
using NRedis.Server.Server;

namespace NRedis.Tests.Server
{
    /// <summary>
    /// Comprehensive unit tests for the RedisClient class using XUnit testing framework.
    /// Tests cover constructor behavior, command parsing (both simple and RESP protocol),
    /// response sending, error handling, and resource disposal.
    /// </summary>
    public class RedisClientTests : IDisposable
    {
        private TcpListener? _listener;
        private TcpClient? _testClient;
        private RedisClient? _redisClient;
        private readonly int _testPort = 9999;

        public RedisClientTests()
        {
            // Setup test TCP connection
            _listener = new TcpListener(IPAddress.Loopback, _testPort);
            _listener.Start();
        }

        public void Dispose()
        {
            _redisClient?.Dispose();
            _testClient?.Close();
            _listener?.Stop();
        }

        private async Task<RedisClient> CreateConnectedRedisClientAsync()
        {
            _testClient = new TcpClient();
            await _testClient.ConnectAsync(IPAddress.Loopback, _testPort);
            var serverClient = await _listener!.AcceptTcpClientAsync();
            return new RedisClient(serverClient);
        }

        [Fact]
        public async Task Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange & Act
            _redisClient = await CreateConnectedRedisClientAsync();

            // Assert
            _redisClient.Id.Should().NotBeNullOrEmpty();
            _redisClient.Id.Length.Should().Be(8);
            _redisClient.TcpClient.Should().NotBeNull();
            _redisClient.Stream.Should().NotBeNull();
            _redisClient.RemoteEndPoint.Should().NotBeNull();
            _redisClient.ConnectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            _redisClient.LastActivity.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            _redisClient.CommandCount.Should().Be(0);
        }

        [Fact]
        public async Task ReadCommandAsync_WithSimpleCommand_ShouldParseCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var command = "GET key\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("GET", "key");
            _redisClient.CommandCount.Should().Be(1);
            _redisClient.LastActivity.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ReadCommandAsync_WithRESPCommand_ShouldParseCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var command = "*2\r\n$3\r\nGET\r\n$3\r\nkey\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("GET", "key");
            _redisClient.CommandCount.Should().Be(1);
        }

        [Fact]
        public async Task ReadCommandAsync_WithComplexRESPCommand_ShouldParseCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var command = "*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$4\r\nJohn\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("SET", "name", "John");
            _redisClient.CommandCount.Should().Be(1);
        }

        [Fact]
        public async Task ReadCommandAsync_WithEmptyCommand_ShouldHandleGracefully()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var command = "\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ReadCommandAsync_WithIncompleteRESPCommand_ShouldTimeOut()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var incompleteCommand = "*2\r\n$3\r\nGET\r\n$3\r\n"; // Missing the actual key
            var bytes = Encoding.UTF8.GetBytes(incompleteCommand);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _redisClient.ReadCommandAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCommandAsync_WithInvalidRESPArrayLength_ShouldTimeOut()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var invalidCommand = "*abc\r\n$3\r\nGET\r\n";
            var bytes = Encoding.UTF8.GetBytes(invalidCommand);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _redisClient.ReadCommandAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCommandAsync_WithInvalidRESPStringLength_ShouldTimeOut()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var invalidCommand = "*1\r\n$abc\r\nGET\r\n";
            var bytes = Encoding.UTF8.GetBytes(invalidCommand);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _redisClient.ReadCommandAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCommandAsync_WithMismatchedStringLength_ShouldTimeOut()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var invalidCommand = "*1\r\n$5\r\nGET\r\n"; // Says 5 characters but "GET" is only 3
            var bytes = Encoding.UTF8.GetBytes(invalidCommand);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _redisClient.ReadCommandAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCommandAsync_WithClientDisconnection_ShouldReturnNull()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            
            // Act
            _testClient!.Close(); // Simulate client disconnection
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ReadCommandAsync_WithCancellation_ShouldThrowOperationCanceledException()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _redisClient.ReadCommandAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCommandAsync_WithMultipleCommands_ShouldIncrementCommandCount()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var command1 = "GET key1\r\n";
            var command2 = "SET key2 value\r\n";

            // Act
            await _testClient!.GetStream().WriteAsync(Encoding.UTF8.GetBytes(command1));
            var result1 = await _redisClient.ReadCommandAsync();
            
            await _testClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(command2));
            var result2 = await _redisClient.ReadCommandAsync();

            // Assert
            result1.Should().Equal("GET", "key1");
            result2.Should().Equal("SET", "key2", "value");
            _redisClient.CommandCount.Should().Be(2);
        }

        [Fact]
        public async Task SendResponseAsync_ShouldSendDataCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var response = "+OK\r\n";
            var buffer = new byte[1024];

            // Act
            await _redisClient.SendResponseAsync(response);
            var bytesRead = await _testClient!.GetStream().ReadAsync(buffer, 0, buffer.Length);

            // Assert
            var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            receivedData.Should().Be(response);
        }

        [Fact]
        public async Task SendResponseAsync_WithCancellation_ShouldThrowOperationCanceledException()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _redisClient.SendResponseAsync("+OK\r\n", cts.Token));
        }

        [Fact]
        public async Task SendResponseAsync_WithLargeResponse_ShouldSendCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var largeResponse = new string('X', 5000) + "\r\n";
            var buffer = new byte[10000];

            // Act
            await _redisClient.SendResponseAsync(largeResponse);
            var bytesRead = await _testClient!.GetStream().ReadAsync(buffer, 0, buffer.Length);

            // Assert
            var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            receivedData.Should().Be(largeResponse);
        }

        [Fact]
        public async Task SendResponseAsync_ConcurrentCalls_ShouldBeSerialized()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var responses = new[] { "+OK1\r\n", "+OK2\r\n", "+OK3\r\n" };
            var tasks = new List<Task>();

            // Act - Send all responses concurrently
            foreach (var response in responses)
            {
                tasks.Add(_redisClient.SendResponseAsync(response));
            }

            await Task.WhenAll(tasks);

            // Read all responses
            var receivedData = new List<string>();
            var buffer = new byte[1024];
            
            for (int i = 0; i < responses.Length; i++)
            {
                var bytesRead = await _testClient!.GetStream().ReadAsync(buffer, 0, buffer.Length);
                receivedData.Add(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }

            // Assert - All responses should be received (order may vary due to concurrency)
            receivedData.Should().Contain(responses);
            receivedData.Should().HaveCount(responses.Length);
        }

        [Fact]
        public async Task ReadCommandAsync_WithPartialData_ShouldWaitForCompleteCommand()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var partialCommand = "GET ";
            var remainingCommand = "key\r\n";

            // Act
            await _testClient!.GetStream().WriteAsync(Encoding.UTF8.GetBytes(partialCommand));
            await Task.Delay(50); // Small delay to ensure partial data is processed
            await _testClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(remainingCommand));
            
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("GET", "key");
        }

        [Fact]
        public async Task ReadCommandAsync_WithMultipleRESPCommands_ShouldParseSequentially()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var commands = "*2\r\n$3\r\nGET\r\n$4\r\nkey1\r\n*3\r\n$3\r\nSET\r\n$4\r\nkey2\r\n$5\r\nvalue\r\n";
            var bytes = Encoding.UTF8.GetBytes(commands);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result1 = await _redisClient.ReadCommandAsync();
            var result2 = await _redisClient.ReadCommandAsync();

            // Assert
            result1.Should().Equal("GET", "key1");
            result2.Should().Equal("SET", "key2", "value");
            _redisClient.CommandCount.Should().Be(2);
        }

        [Fact]
        public void Dispose_ShouldCleanupResourcesGracefully()
        {
            // Arrange
            using var tcpClient = new TcpClient();
            var redisClient = new RedisClient(tcpClient);

            // Act & Assert
            var disposeAction = () => redisClient.Dispose();
            disposeAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            using var tcpClient = new TcpClient();
            var redisClient = new RedisClient(tcpClient);

            // Act & Assert
            redisClient.Dispose();
            var secondDispose = () => redisClient.Dispose();
            secondDispose.Should().NotThrow();
        }

        [Fact]
        public async Task ReadCommandAsync_WithVeryLongCommand_ShouldHandleCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var longKey = new string('X', 3000);
            var command = $"GET {longKey}\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("GET", longKey);
        }

        [Theory]
        [InlineData("*0\r\n")]
        [InlineData("*1\r\n$0\r\n\r\n")]
        [InlineData("*2\r\n$4\r\nPING\r\n$0\r\n\r\n")]
        public async Task ReadCommandAsync_WithEdgeCaseRESPCommands_ShouldParseCorrectly(string command)
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task LastActivity_ShouldUpdateOnEachRead()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var initialActivity = _redisClient.LastActivity;
            await Task.Delay(50);

            // Act
            await _testClient!.GetStream().WriteAsync(Encoding.UTF8.GetBytes("PING\r\n"));
            await _redisClient.ReadCommandAsync();

            // Assert
            _redisClient.LastActivity.Should().BeAfter(initialActivity);
        }

        [Fact]
        public async Task ReadCommandAsync_WithZeroLengthString_ShouldHandleCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var command = "*2\r\n$3\r\nSET\r\n$0\r\n\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("SET", "");
        }

        [Fact]
        public async Task ReadCommandAsync_WithMixedRESPAndSimpleCommands_ShouldHandleBoth()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var respCommand = "*2\r\n$3\r\nGET\r\n$3\r\nkey\r\n";
            var simpleCommand = "PING\r\n";

            // Act
            await _testClient!.GetStream().WriteAsync(Encoding.UTF8.GetBytes(respCommand));
            var result1 = await _redisClient.ReadCommandAsync();
            
            await _testClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(simpleCommand));
            var result2 = await _redisClient.ReadCommandAsync();

            // Assert
            result1.Should().Equal("GET", "key");
            result2.Should().Equal("PING");
            _redisClient.CommandCount.Should().Be(2);
        }

        [Fact]
        public async Task ReadCommandAsync_WithExceptionInStream_ShouldReturnNull()
        {
            // This test verifies the exception handling in ReadCommandAsync
            // We'll simulate this by disposing the client after creation
            
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            _testClient!.Close(); // Close the client stream
            _redisClient.Stream.Close(); // Close the server stream as well

            // Act
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SendResponseAsync_WithEmptyResponse_ShouldSendCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var response = "";

            // Act & Assert - Should not throw for empty response
            var sendAction = async () => await _redisClient.SendResponseAsync(response);
            await sendAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task CommandCount_ShouldOnlyIncrementOnSuccessfulParse()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var validCommand = "GET key\r\n";
            var partialCommand = "GET"; // No \r\n terminator

            // Act
            await _testClient!.GetStream().WriteAsync(Encoding.UTF8.GetBytes(validCommand));
            await _redisClient.ReadCommandAsync();

            var initialCount = _redisClient.CommandCount;

            await _testClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(partialCommand));
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            try
            {
                await _redisClient.ReadCommandAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected for partial command
            }

            // Assert
            _redisClient.CommandCount.Should().Be(initialCount); // Should not increment for failed parse
        }

        [Fact]
        public async Task ReadCommandAsync_WithBufferOverflow_ShouldHandleGracefully()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            // Create a command that would exceed the 4096 byte buffer
            var largeKey = new string('X', 5000);
            var command = $"GET {largeKey}\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act - Send in chunks to simulate real network behavior
            const int chunkSize = 1024;
            for (int i = 0; i < bytes.Length; i += chunkSize)
            {
                var remainingBytes = Math.Min(chunkSize, bytes.Length - i);
                await _testClient!.GetStream().WriteAsync(bytes, i, remainingBytes);
                await Task.Delay(1); // Small delay between chunks
            }

            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("GET", largeKey);
        }

        [Fact]
        public async Task Id_ShouldBeUnique_ForMultipleInstances()
        {
            // Arrange & Act
            var client1 = await CreateConnectedRedisClientAsync();
            
            // Create second connection
            var testClient2 = new TcpClient();
            await testClient2.ConnectAsync(IPAddress.Loopback, _testPort);
            var serverClient2 = await _listener!.AcceptTcpClientAsync();
            var client2 = new RedisClient(serverClient2);

            // Assert
            client1.Id.Should().NotBe(client2.Id);
            client1.Id.Length.Should().Be(8);
            client2.Id.Length.Should().Be(8);

            // Cleanup
            client2.Dispose();
            testClient2.Close();
        }

        [Fact]
        public async Task ReadCommandAsync_WithNegativeStringLength_ShouldTimeOut()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var invalidCommand = "*1\r\n$-1\r\nGET\r\n"; // Negative string length
            var bytes = Encoding.UTF8.GetBytes(invalidCommand);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _redisClient.ReadCommandAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCommandAsync_WithSpecialCharacters_ShouldParseCorrectly()
        {
            // Arrange
            _redisClient = await CreateConnectedRedisClientAsync();
            var specialValue = "hello\nworld\ttab";
            var command = $"*3\r\n$3\r\nSET\r\n$3\r\nkey\r\n${specialValue.Length}\r\n{specialValue}\r\n";
            var bytes = Encoding.UTF8.GetBytes(command);

            // Act
            await _testClient!.GetStream().WriteAsync(bytes, 0, bytes.Length);
            var result = await _redisClient.ReadCommandAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal("SET", "key", specialValue);
        }
    }
}