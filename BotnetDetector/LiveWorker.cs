using System;
using System.IO;
using System.Timers;
using LibBtntDtct;
namespace BotnetDetector
{
    /// <summary>
    /// Live worker used to process, classify, print flows and classes information and class statistics from a live device.
    /// </summary>
    public class LiveWorker : AbstractWorker
    {
        /// <summary>
        /// Live trafic flow adapter.
        /// </summary>
        protected LiveCaptureFlowAdapter adapter;

        protected Timer timer;

        /// <summary>
        /// Returns the name of the device this worker was initialised appon.
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Represents the total number of packets read.
        /// </summary>
        public long Total => adapter.Total;

        /// <summary>
        /// Represnts the number of packets the library encountered errors parsing.
        /// </summary>
        public long Errors => adapter.Errors;

        /// <summary>
        /// Represents the number of droped packets due to the queue overflow.
        /// </summary>
        public long Droped => adapter.Dropped;

        /// <summary>
        /// Represents the number of queued packets.
        /// </summary>
        public long Queued => adapter.QueudPackets;

        /// <summary>
        /// Represent the max packets queue size;
        /// </summary>
        public int MaxQueueSize { get => adapter.MaxQueueSize; set => adapter.MaxQueueSize = value; }

        /// <summary>
        /// Indicates whether capture is running.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Occurs when the capture stopped.
        /// </summary>
        public event Action Stopped;

        /// <summary>
        /// Indicates whether to display statistics.
        /// </summary>
        public override bool DisplayStatistics => !(Output == Console.Out || Program.Verbosity == Verbosity.Quiet);

        /// <summary>
        /// Displaies the status.
        /// </summary>
        public void DisplayStatus(object sender, ElapsedEventArgs e)
        {
            Console.Clear();
            Console.WriteLine($"Packets statistics:");
            Console.WriteLine($"Total: {Total}");
            Console.WriteLine($"Queued: {Queued} / {MaxQueueSize} (max)");
            Console.WriteLine($"Errors: {Errors}");
            Console.WriteLine($"Droped: {Droped}");
            if (Classify)
                DisplayClassStatistics();
        }

        /// <summary>
        /// Starts the capture.
        /// </summary>
        public void StartCapture()
        {
            if (DisplayStatistics)
            {
                timer = new Timer(1000);
                timer.Elapsed += DisplayStatus;
                timer.Start();
            }
            try
            {
                adapter.StartCapture();
                Running = true;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to start cupture.", ex);
            }
        }

        /// <summary>
        /// Stops the capture.
        /// </summary>
        public void StopCapture()
        {
            try
            {
                adapter.StopCapture();
                Running = false;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to gracefully stop capture.", ex);
            }
            timer?.Stop();
            timer = null;
            Stopped?.Invoke();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.LiveWorker"/> class.
        /// </summary>
        /// <param name="deviceName">Device name to listen on.</param>
        /// <param name="cf">Classifier to use.</param>
        public LiveWorker(string deviceName, AbstractClassifier cf, bool nameAbnormalTraficAsBotnet)
            : base(cf, nameAbnormalTraficAsBotnet)
        {
            DeviceName = deviceName;
            adapter = new LiveCaptureFlowAdapter(deviceName);
            adapter.FlowDead += Process;
        }
    }
}
