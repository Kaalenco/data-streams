using System;

namespace N2.DataStreams
{
    public interface IDataStream : IDisposable
    {
        bool Configured { get; }
        event DataAvailableEventHandler DataAvailableHandler;
        event StreamDataEventHandler StreamDataHandler;
        int PollingInterval { get; }
        bool DataAvailable { get; }
        int MaximumQueueLength { get; }
        int QueueLength { get; }

        void Start();
        void Stop();
        StreamData NextResult();
        void ResetQueue();
    }
}
