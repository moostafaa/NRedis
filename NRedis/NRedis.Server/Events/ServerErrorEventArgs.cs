namespace NRedis.Server.EventsArgs
{
    public class ServerErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public string Context { get; set; }
    }
}