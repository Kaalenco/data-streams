using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace N2.DataStreams
{
    public static class StreamDataExtensions
    {
        public static byte[] GetBytes<T>(this T value)
        {
            var bytes =  JsonSerializer.SerializeToUtf8Bytes(value);
            return Compress(bytes);
        }

        public static T Deserialize<T>(this byte[] data)
        {
            var bytes = Decompress(data);
            var readOnlySpan = new ReadOnlySpan<byte>(bytes);
            return JsonSerializer.Deserialize<T>(readOnlySpan);
        }

        public static Crc Double(this Crc crc, double value)
        {
            var bytes = BitConverter.GetBytes(value);
            for (var i = 0; i < bytes.Length; i++)
            {
                AddValue(crc, bytes[i]);
            }
            return crc;
        }

        public static Crc Float(this Crc crc, float value)
        {
            var bytes = BitConverter.GetBytes(value);
            for (var i = 0; i < bytes.Length; i++)
            {
                AddValue(crc, bytes[i]);
            }
            return crc;
        }

        public static  Crc Integer(this Crc crc, int value) {
            for(var i = 0; i < 2; i++)
            {
                var low = value & 0xFFFF;
                AddValue(crc, (ushort)low);
                value = value >> 16;
            }           
            return crc;
        }

        public static Crc Geo(this Crc crc, Geoposition value)
        {
            return crc
                .Integer(value.Altitude)
                .Long((long)value.Latitude * 1000000)
                .Long((long)value.Longitude * 1000000);
        }

        public static Crc Long(this Crc crc, long value)
        {
            for (var i = 0; i < 4; i++)
            {
                var low = value & 0xFFFF;
                AddValue(crc, (ushort)low);
                value = value >> 16;
            }
            return crc;
        }

        public static Crc DateTime(this Crc crc, DateTime value)
        {
            return crc.Long(value.Ticks);
        }

        private static  void AddValue(Crc crc, ushort value)
        {
            var val = crc.Value;
            val = (val * 2) ^ value;
            if ((val & 0x10000) !=0)
            {
                val = val ^ 0x8001;
            }
            crc.Value = val;
        }

        private static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}
