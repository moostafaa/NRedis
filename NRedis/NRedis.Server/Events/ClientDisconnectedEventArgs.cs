namespace NRedis.Server.EventsArgs
{
    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string Reason { get; set; }
    }
}