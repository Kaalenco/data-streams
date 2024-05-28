using System.Text;

namespace N2.DataStreams
{
    /// <summary>
    /// The stream data struct is used to store the data that is sent from the device to the server.
    /// It contains the stream type, the unit code, the value, the reliability, the geoposition and the time.
    /// The CRC is calculated based on all the data in the struct. It should be used to verify the data integrity.
    /// </summary>
    public struct StreamData
    {
        public StreamData(StreamType streamType, int unitCode, double value, float reliability = 0.0f, int alt = 0, float lat = 0.0f, float lng = 0.0f, DateTime? time = null)
        {
            StreamType = streamType;
            Reliability = reliability;
            DateTime = time != null ? time.Value : Constants.Epoch;
            Geoposition = new Geoposition { Altitude = alt, Latitude = lat, Longitude = lng };
            UnitCode = unitCode;
            Value = value;
            Crc = new Crc()
                .Integer((int)streamType)
                .Integer(unitCode)
                .Double(value)
                .DateTime(DateTime)
                .Float(Reliability)
                .Geo(Geoposition);
        }

        public StreamType StreamType { get; set; }
        public Geoposition Geoposition { get; set; }
        public DateTime DateTime { get; set; }
        public float Reliability { get; set; }
        public int UnitCode { get; set; }
        public double Value { get; set; }

        public Crc Crc { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Reliability : {Reliability:F2}");
            if ((StreamType & StreamType.Time) == StreamType.Time) sb.AppendLine($"Time : {DateTime:o}");
            if ((StreamType & StreamType.Position) == StreamType.Position) sb.AppendLine($"Geo : {Geoposition}");
            sb.AppendLine($"UnitCode : {UnitCode}");
            sb.AppendLine($"Value : {Value}");
            return sb.ToString();
        }
    }
}