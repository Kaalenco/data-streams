using System;

namespace N2.DataStreams
{
    public static class Constants
    {
        public static DateTime Epoch = new DateTime(1970, 1, 1);
        public const int DefaultPollingInterval = 1000;
        public const int DefaultMaxiumQueueLength = 2000;
    }
}
