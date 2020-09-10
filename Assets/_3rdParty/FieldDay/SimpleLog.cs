namespace FieldDay
{
    /// <summary>
    /// Simple Logger
    /// </summary>
    public class SimpleLog
    {
        public SimpleLog(string inAppId, int inAppVersion)
        {
            // TODO: Implement
        }

        /// <summary>
        /// Logs a new event.
        /// </summary>
        public void Log(ILogEvent inData)
        {
            // TODO: Implement
        }

        /// <summary>
        /// Flushes all queued events
        /// </summary>
        public void Flush()
        {
            // TODO: Implement
        }
    }

    public interface ILogEvent
    {
        // TODO: Something to write the information directly
    }
}