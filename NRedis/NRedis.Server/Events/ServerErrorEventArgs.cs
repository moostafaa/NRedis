namespace NRedis.Server.Events
{
    public class ServerErrorEventArgs : System.EventArgs
    {
        public Exception Exception { get; set; }
        public string Context { get; set; }
    }
}