namespace N2.DataStreams
{
    public struct StreamConfig
    {
        public string Name { get; set; } 
        public StreamType StreamType { get; set; }
        public int IntervalInMilliseconds { get; set; } 
        public bool AutoStart { get; set; }
        public int MaximumQueueLength { get; set; }
    }
}
