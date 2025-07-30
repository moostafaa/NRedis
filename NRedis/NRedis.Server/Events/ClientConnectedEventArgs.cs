using System.Net;

namespace NRedis.Server.EventsArgs
{
    // Event arguments for server events
    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
    }
}