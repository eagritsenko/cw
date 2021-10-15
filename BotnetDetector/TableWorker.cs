using System;
using System.IO;
using System.Timers;
using LibBtntDtct;

namespace BotnetDetector
{
    /// <summary>
    /// Table worker used to process, classify, print flows and classes information and class statistics from a live device.
    /// </summary>
    public class TableWorker : AbstractWorker
    {
        protected FlowReader reader;

        protected Timer timer;

        /// <summary>
        /// Returns the path to a table file this class was initialised with.
        /// </summary>
        string TablePath { get; }

        /// <summary>
        /// Represents the number of flows read from the table.
        /// </summary>
        /// <value>The flows read.</value>
        public long FlowsRead { get; private set; }

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
            Console.WriteLine($"{FlowsRead} flows have been read.");
            if (Classify)
                DisplayClassStatistics();
        }

        /// <summary>
        /// Reads the flows from the table to end, processing them by the way.
        /// </summary>
        public void ReadToEnd()
        {
            FlowsRead = 0;
            if (DisplayStatistics)
            {
                timer = new Timer(1000);
                timer.Elapsed += DisplayStatus;
                timer.Start();
            }
            try
            {
                while (!reader.EOF)
                {
                    Flow f = reader.ReadFlow();
                    Process(f);
                    FlowsRead++;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing flow {FlowsRead}.", ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.TableWorker"/> class.
        /// </summary>
        /// <param name="tablePath">Path of the table file to read from.</param>
        /// <param name="skipFirstLine">If set to <c>true</c> skips first line from the table.</param>
        /// <param name="cf">Classifier to use.</param>
        public TableWorker(string tablePath, bool skipFirstLine, AbstractClassifier cf, bool nameAbnormalTraficAsBotnet)
            : base(cf, nameAbnormalTraficAsBotnet)
        {
            TablePath = tablePath;
            reader = new FlowReader(File.OpenRead(tablePath), skipFirstLine);
        }
    }
}
