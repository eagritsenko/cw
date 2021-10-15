using System;
using System.IO;
using System.Timers;
using LibBtntDtct;

namespace BotnetDetector
{
    /// <summary>
    /// Pcap worker used to process, classify, print flows and classes information and class statistics from a pcap file.
    /// </summary>
    public class PcapWorker : AbstractWorker
    {
        protected PcapFlowAdapter adapter;

        protected Timer timer;

        /// <summary>
        /// Returns the path to a pcap file this class was initialised with.
        /// </summary>
        string PcapPath { get; }

        /// <summary>
        /// Represents the number of packets read from the file.
        /// </summary>
        public long PacketsRead { get; private set; }

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
            Console.WriteLine($"{PacketsRead} packets have been read.");
            if (Classify)
                DisplayClassStatistics();
        }

        /// <summary>
        /// Reads pcpap file to end, processing its flows by the way.
        /// </summary>
        public void ReadToEnd()
        {
            PacketsRead = 0;
            if (DisplayStatistics)
            {
                timer = new Timer(1000);
                timer.Elapsed += DisplayStatus;
                timer.Start();
            }
            try
            {
                while (adapter.ReadNextPcapPacket() == 1)
                    PacketsRead++;
                timer?.Stop();
                timer = null;
            }
            catch(Exception ex)
            {
                throw new Exception("Pcap reading error.", ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.PcapWorker"/> class.
        /// </summary>
        /// <param name="pcapPath">Path of the pcap file to read from.</param>
        /// <param name="cf">Classifier to use.</param>
        public PcapWorker(string pcapPath, AbstractClassifier cf, bool nameAbnormalTraficAsBotnet)
            : base(cf, nameAbnormalTraficAsBotnet)
        {
            PcapPath = pcapPath;
            adapter = new PcapFlowAdapter(pcapPath);
            adapter.FlowDead += Process;
        }

    }
}
