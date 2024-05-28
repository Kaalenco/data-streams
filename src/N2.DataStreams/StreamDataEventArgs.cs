namespace N2.DataStreams
{
    public class StreamDataEventArgs : EventArgs
    {
        public StreamData StreamData { get; private set; }

        public StreamDataEventArgs()
        {
        }

        public StreamDataEventArgs(StreamData streamData)
        {
            StreamData = streamData;
        }
    }
}