using Microsoft.VisualStudio.TestTools.UnitTesting;
using N2.DataStreams;
using System;
using System.Threading;

namespace DataStreams.UnitTests
{
    [TestClass]
    public class DataStreamShould
    {
        private StreamConfig _streamconfig = new StreamConfig
        {
            Name = "TestStream",
            StreamType = StreamType.Time,
            IntervalInMilliseconds = 100           
        };

        private IDataStream _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new DataStream(_streamconfig);
        }

        [TestMethod]
        public void UsingMaximumQueueLength()
        {
            const int TestMaxLength = 3;
            _streamconfig.MaximumQueueLength = TestMaxLength;
            _streamconfig.IntervalInMilliseconds = 100;
            IDataStream sut = new DataStream(_streamconfig);
            Assert.AreEqual(TestMaxLength, sut.MaximumQueueLength);
            sut.Start();

            Thread.Sleep(6000);
            sut.Stop();

            // if unrestricted, there would be 6 items
            Assert.AreEqual(TestMaxLength, sut.QueueLength);

            while (sut.DataAvailable)
            {
                var data = sut.NextResult();
                Console.WriteLine(data);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _sut?.Dispose();
        }

        [TestMethod]
        public void ConstructStreamClient()
        {
            IDataStream sut = new DataStream(_streamconfig);
            Assert.IsNotNull(sut);
            Assert.IsTrue(sut.Configured);
        }

        [TestMethod]
        public void InitializeWithEmptyConfig()
        {
            IDataStream sut = new DataStream(new StreamConfig());
            Assert.IsNotNull(sut);
            Assert.IsFalse(sut.Configured);
        }

        [TestMethod]
        public void StreamClientIsDisposable()
        {
            Assert.IsNotNull(_sut as IDisposable);
        }

        [TestMethod]
        public void StreamClientHasHandlerMethods()
        {
            _sut.DataAvailableHandler += (object o, StreamDataAvailableEventArgs e) =>
            {
                Assert.IsNotNull(o);
                Assert.IsNotNull(e);
            };
        }

        [TestMethod]
        public void StreamClientCanStartAndStop()
        {
            _sut.Start();
            _sut.Stop();
            Assert.IsTrue(true, "Start and stop executed");
        }

        [TestMethod]
        public void StreamClientRaisedEvents()
        {
            var eventRaised = false;
            _sut.Start();
            _sut.DataAvailableHandler += (object o, StreamDataAvailableEventArgs e) =>
            {
                Assert.IsNotNull(o);
                Assert.IsNotNull(e);
                eventRaised = true;
            };
            Thread.Sleep(200);
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void StreamClientRaisedDataEvents()
        {
            StreamData eventRaised = new StreamData();
            _sut.Start();
            _sut.StreamDataHandler += (object o, StreamDataEventArgs e) =>
            {
                Assert.IsNotNull(o);
                Assert.IsNotNull(e);
                eventRaised = e.StreamData;
            };
            Thread.Sleep(200);
            Assert.AreNotEqual(0.0f, eventRaised.Reliability);
        }

        [TestMethod]
        public void StreamClientProducesResults()
        {
            _sut.Start();
            Thread.Sleep(200);
            Assert.IsTrue(_sut.DataAvailable);
            var data = _sut.NextResult();
            Assert.IsNotNull(data);
        }

        [TestMethod]
        public void StreamClientCanresetQueue()
        {
            _sut.Start();
            Thread.Sleep(200);
            _sut.Stop();
            Assert.IsTrue(_sut.DataAvailable);
            _sut.ResetQueue();
            Assert.IsFalse(_sut.DataAvailable);
        }

    }
}
