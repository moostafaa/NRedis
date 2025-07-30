namespace NRedis.Server.Events
{
    public class MessageReceivedEventArgs : System.EventArgs
    {
        public string ClientId { get; set; }
        public string[] Command { get; set; }
        public string RawMessage { get; set; }
    }
}