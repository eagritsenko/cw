using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap.LibPcap;
using SharpPcap;

namespace LibBtntDtct
{

    /// <summary>
    /// Used to meter flows from packets of a live device.
    /// </summary>
    public class LiveCaptureFlowAdapter : FlowAdapter
    {
        /// <summary>
        /// Live device to capture packets from.
        /// </summary>
        ICaptureDevice liveDevice;

        /// <summary>
        /// The queu of captured packets to be processed.
        /// </summary>
        ConcurrentQueue<RawCapture> captures = new ConcurrentQueue<RawCapture>();

        bool readCaptures = true;

        Task captureReader;

        /// <summary>
        /// Represents the total number of packets read.
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Represnts the number of packets the library encountered errors parsing.
        /// </summary>
        public long Errors { get; set; }

        /// <summary>
        /// Represents the number of droped packets due to the queue overflow.
        /// </summary>
        public long Dropped { get; set; }

        /// <summary>
        /// Represents the number of queued packets.
        /// </summary>
        public int QueudPackets => captures.Count;

        /// <summary>
        /// Represent the max packets queue size;
        /// </summary>
        public int MaxQueueSize { get; set; } = 1000000;

        /// <summary>
        /// Reads and processes a raw capture.
        /// </summary>
        private void ReadCapture(RawCapture r)
        {
            try
            {
                ReadPacket(new EthernetPacket(new ByteArraySegment(r.Data)), r.Timeval.Date);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Errors++;
            }
        }

        /// <summary>
        /// Reads and processes captures from the queue. This is launched in a separate thread.
        /// If no packets are found in the queue, sleeps for 64 miliseconds.
        /// </summary>
        private async Task ReadCaptures()
        {
            const int stale = 64;
            while (readCaptures)
            {
                if (!captures.IsEmpty)
                {
                    if (captures.TryDequeue(out RawCapture r))
                        ReadCapture(r);
                }
                else
                    await Task.Delay(stale);
            }
        }

        /// <summary>
        /// Finalises reading captures.
        /// </summary>
        private void FinaliseReadingCaptures()
        {
            foreach (var r in captures)
                ReadCapture(r);
        }

        /// <summary>
        /// Adds the capture to the queue. Drops packet if max queue size is exceded.
        /// </summary>
        private void AddCapture(object sender, CaptureEventArgs e)
        {
            Total++;
            if (captures.Count >= MaxQueueSize)
                Dropped++;
            else
                captures.Enqueue(e.Packet);
        }

        /// <summary>
        /// Stops capturing packets from the live device and processes those left in the queue.
        /// </summary>
        public void StopCapture()
        {
            liveDevice.StopCapture();
            readCaptures = false;
            captureReader.Wait();
            FinaliseReadingCaptures();
        }

        /// <summary>
        /// Starts listening device, capturing and processing packets.
        /// </summary>
        public void StartCapture()
        {
            if (!liveDevice.Started)
                liveDevice.Open();
            liveDevice.StartCapture();
            readCaptures = true;
            captureReader = Task.Run(ReadCaptures);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LibBtntDtct.LiveCaptureFlowAdapter"/> class.
        /// </summary>
        /// <param name="device">Device to initialise this adapter on.</param>
        public LiveCaptureFlowAdapter(ICaptureDevice device)
        {
            liveDevice = device;
            liveDevice.OnPacketArrival += AddCapture;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LibBtntDtct.LiveCaptureFlowAdapter"/> class.
        /// </summary>
        /// <param name="deviceName">Name of the device to initialise this adapter on.</param>
        public LiveCaptureFlowAdapter(string deviceName)
        {
            var deviceList = CaptureDeviceList.Instance;
            liveDevice = null;
            for(int i = 0; i < deviceList.Count; i++)
                if(deviceList[i].Name == deviceName)
                {
                    liveDevice = deviceList[i];
                    break;
                }
            if (liveDevice == null)
                throw new ArgumentException($"Device with name {deviceName} have not been found.");
            liveDevice.OnPacketArrival += AddCapture;
        }

    }
}
