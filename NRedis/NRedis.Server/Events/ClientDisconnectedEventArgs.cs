namespace NRedis.Server.Events
{
    public class ClientDisconnectedEventArgs : System.EventArgs
    {
        public string ClientId { get; set; }
        public string Reason { get; set; }
    }
}