namespace N2.DataStreams
{
    public class StreamDataAvailableEventArgs : EventArgs
    {
        public StreamType StreamType { get; private set; }
        public float Reliability { get; private set; }

        public StreamDataAvailableEventArgs()
        {
        }

        public StreamDataAvailableEventArgs(StreamType streamType, float reliability)
        {
            StreamType = streamType;
            Reliability = reliability;
        }
    }
}