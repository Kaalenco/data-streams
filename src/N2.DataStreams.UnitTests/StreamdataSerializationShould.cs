using Microsoft.VisualStudio.TestTools.UnitTesting;
using N2.DataStreams;
using System;

namespace DataStreams.UnitTests
{
    [TestClass]
    public class StreamdataSerializationShould
    {
        [TestMethod]
        public void SerializeStreamdataBinary()
        {
            var v1 = new StreamData(StreamType.Time, 1, 0.1, 0.6f, 12, 0, 0, DateTime.MaxValue);
            var serializedData = v1.GetBytes();
            Assert.IsNotNull(serializedData);
            Console.WriteLine($"Data length : {serializedData.Length}");
        }

        [TestMethod]
        public void DeSerializeStreamdataBinary()
        {
            var testTime = new DateTime(2021, 1, 23);
            var v1 = new StreamData(StreamType.Time, 1, 234, 0.6f, 12, 0, 0, testTime);
            var serializedData = v1.GetBytes();
            var crc = v1.Crc.Value;

            var v2 = serializedData.Deserialize<StreamData>();
            Assert.IsNotNull(v2);
            Assert.AreEqual(StreamType.Time, v2.StreamType);
            Assert.AreEqual(0.6f, v2.Reliability);
            Assert.AreEqual(12, v2.Geoposition.Altitude);
            Assert.AreEqual(testTime, v2.DateTime);
            Assert.AreEqual(crc, v2.Crc.Value);
        }

        [TestMethod]
        public void StreamdataHasCrc()
        {
            var v1 = new StreamData(StreamType.Time | StreamType.Position, 1, 2345, 0.6f, 12, 0, 0, DateTime.MaxValue);
            var v2 = new StreamData(StreamType.Time | StreamType.Position, 1, 2345, 0.6f, 12, 0, 0, DateTime.MaxValue);
            Assert.AreEqual(v1.Crc.Value, v2.Crc.Value);
            Assert.AreNotEqual(0, v1.Crc.Value);
        }
    }
}