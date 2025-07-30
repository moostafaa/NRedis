using System.Net;

namespace NRedis.Server.Events
{
    // Event arguments for server events
    public class ClientConnectedEventArgs : System.EventArgs
    {
        public string ClientId { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
    }
}