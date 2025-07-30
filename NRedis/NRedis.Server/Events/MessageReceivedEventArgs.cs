namespace NRedis.Server.EventsArgs
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string[] Command { get; set; }
        public string RawMessage { get; set; }
    }
}