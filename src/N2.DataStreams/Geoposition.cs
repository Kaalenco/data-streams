﻿namespace N2.DataStreams
{
    /// <summary>
    /// The geographical position of the device where the data was collected.
    /// </summary>
    public struct Geoposition
    {
        public int Altitude { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public override string ToString()
        {
            var ns = Latitude < 0 ? "N" : "S";
            var ew = Longitude < 0 ? "E" : "W";
            return $"{ns}{Latitude:F2}, {ew}{Longitude:F2}, A{Altitude}";
        }
    }
}