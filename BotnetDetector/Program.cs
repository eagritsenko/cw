using System;
using System.Reflection;
using System.Security;
using System.Threading;
using System.IO;
using LibBtntDtct;

namespace BotnetDetector
{
    partial class Program
    {

        /// <summary>
        /// Indicates how verbose the status output should be.
        /// </summary>
        public static Verbosity Verbosity { get; private set; } = Verbosity.Normal;

        /// <summary>
        /// Prints the help mesage.
        /// </summary>
        public static void PrintHelp()
        {
            Console.WriteLine("Use:");
            Console.WriteLine("-h, --help\t\t\t\t\tto print this message.");
            Console.WriteLine("-l, --live\t\t\t\t\tto process live device packets.");
            Console.WriteLine("-p, --pcap\t\t\t\t\tto process pcap file.");
            Console.WriteLine("-t, --table\t\t\t\t\tto process flows from a table given.");
            Console.WriteLine("-pr, --performance\t\t\t\tto evaluate performance on a labeled flow table.");
            Console.WriteLine("-ls, --listDevicies\t\t\t\tto list devicies available for live capture.");
            Console.WriteLine("-i, --input\t\t\t\t\tto specify input parameters like file or device name.");
            Console.WriteLine("-o, --output\t\t\t\t\tto specify output parameters like file name. If no file provided stdout is used.");
            Console.WriteLine("--classifier pathToAssembly fullTypeName\tto use a classifier in the assembly.");
            Console.WriteLine("--absoluteClassifierPath\t\t\tto consider path to classifier assembly to be absolute.");
            Console.WriteLine("--classify\t\t\t\t\tto specify that trafic should be clasified. Enabled by default.");
            Console.WriteLine("--printFlows\t\t\t\t\tto specify that flows should be printed. Enabled by default.");
            Console.WriteLine("--printClasses\t\t\t\t\tto specify that flows should be printed. Enabled by default.");
            Console.WriteLine("--printAbnormalOnly\t\t\t\tto specify that only abnormal trafic should be printed.");
            Console.WriteLine("--nameAbnormalAsBotnet\t\t\t\tto name trafic classified as not normal as botnet.");
            Console.WriteLine("--skipFirstLine\t\t\t\t\tto skip first line when reading a flow table file.");
            Console.WriteLine("-0\t\t\t\t\t\tto disable a bool option or restore some to default.");
            Console.WriteLine("-1\t\t\t\t\t\tto enable a bool option.");
            Console.WriteLine("-v, --verbose\t\t\t\t\tto be verbose. This includes showing full error trace.");
            Console.WriteLine("-q, --quiet\t\t\t\t\tto supress all state noticies to stdout except errors.");
        }

        /// <summary>
        /// Opens the write stream on a path given.
        /// </summary>
        public static FileStream OpenWriteStream(string on)
        {
            return new FileStream(on, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        /// <summary>
        /// Loads the classifier using either Default one or the one parsed from the args.
        /// </summary>
        public static AbstractClassifier LoadClassifier()
        {
            AbstractClassifier cf;
            if (externalClassifierPath == null)
                cf = new InbuiltClassifiers.Default();
            else
            {
                Assembly dll;
                try
                {
                    if (useAbsoluteClassifierPath)
                        dll = Assembly.LoadFile(externalClassifierPath);
                    else
                        dll = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, externalClassifierPath));
                }
                catch (Exception inner)
                {
                    throw new Exception("Error loading assembly file.", inner);
                }
                Type classifierType;
                try
                {
                    classifierType = dll.GetType(externalClassifierName, true);
                }
                catch (Exception inner)
                {
                    throw new Exception("Error extracting classifier type.", inner);
                }
                if (!AbstractClassifier.ValidnessOf(classifierType))
                    throw new Exception("Error classifier class provided does not appear to be valid.");

                try
                {
                    cf = (AbstractClassifier)Activator.CreateInstance(classifierType);
                }
                catch (Exception inner)
                {
                    throw new Exception("Error creating classifier instance.", inner);
                }
            }
            return cf;
        }

        /// <summary>
        /// Initialises the worker.
        /// </summary>
        public static void InitialiseWorker(AbstractWorker w)
        {
            w.Classify = classify;
            w.PrintFlows = printFlows;
            w.PrintClasses = printClasses;
            w.PrintAbnormalTraficOnly = printAbnormalTraficOnly;
            if (!(w.PrintFlows || w.PrintClasses))
                w.Output = null;
            string output = LastIn(Output);
            if (output != null)
            {
                try
                {
                    w.Output = new StreamWriter(OpenWriteStream(output));
                }
                catch (IOException ex)
                {
                    throw new Exception("IO error while trying to create output.", ex);
                }
                catch (SecurityException ex)
                {
                    throw new Exception("Security error while trying to create output.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error creating output ({ex.Message})", ex);
                }
            }
        }

        /// <summary>
        /// Finalises the worker.
        /// </summary>
        public static void FinaliseWorker(AbstractWorker w)
        {
            if (w.Output != Console.Out && w.Output != null)
            {
                try
                {
                    w.Output.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error closing output thread.", ex);
                }
            }
        }

        /// <summary>
        /// Prepares and executes live worker using args parsed.
        /// </summary>
        public static void PrepareAndExecuteLiveWorker()
        {
            int unfinalised = 1;
            string interfaceName = LastIn(Input);
            if (interfaceName == null)
                throw new Exception("Interface name is not provided in input.");
            AbstractClassifier cf = LoadClassifier();
            LiveWorker worker;
            try
            {
                worker = new LiveWorker(interfaceName, cf, nameAbnormalTraficAsBotnet);
                Console.CancelKeyPress += (sender, e) => {
                    worker.StopCapture();
                    if (Interlocked.CompareExchange(ref unfinalised, 1, 0) == 1)
                        FinaliseWorker(worker);
                };
                InitialiseWorker(worker);
                worker.StartCapture();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to initialise worker: {ex.Message}", ex);
            }
            while (worker.Running)
                Thread.Sleep(1000);
            if (Interlocked.CompareExchange(ref unfinalised, 1, 0) == 1)
                FinaliseWorker(worker);
        }

        /// <summary>
        /// Prepares and executes pcap worker using args parsed.
        /// </summary>
        public static void PrepareAndExecutePcapWorker()
        {
            string pcapName = LastIn(Input);
            if (pcapName == null)
                throw new Exception("No pcap file specified in input.");
            if (!File.Exists(pcapName))
                throw new Exception("Unable to access file path given.");
            AbstractClassifier cf = LoadClassifier();
            PcapWorker worker;
            try
            {
                worker = new PcapWorker(pcapName, cf, nameAbnormalTraficAsBotnet);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to initialise worker: {ex.Message}", ex);
            }
            InitialiseWorker(worker);
            try
            {
                worker.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error analysing pcap file: {ex.Message}", ex);
            }
            FinaliseWorker(worker);
        }

        /// <summary>
        /// Prepares and executes table worker using args parsed.
        /// </summary>
        public static void PrepareAndExecuteTableWorker()
        {
            string tableName = LastIn(Input);
            if (tableName == null)
                throw new Exception("No table file specified in input.");
            if (!File.Exists(tableName))
                throw new Exception("Unable to access file path given.");
            AbstractClassifier cf = LoadClassifier();
            TableWorker worker;
            try
            {
                worker = new TableWorker(tableName, skipFirstLine, cf, nameAbnormalTraficAsBotnet);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to initialise worker: {ex.Message}", ex);
            }
            InitialiseWorker(worker);
            try
            {
                worker.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error analysing table file: {ex.Message}", ex);
            }
            FinaliseWorker(worker);
        }

        /// <summary>
        /// Evaluates the performance using table file path from the args parsed.
        /// </summary>
        public static void EvaluatePerformance()
        {
            string tableName = LastIn(Input);
            if (tableName == null)
                throw new Exception("No table file specified in input.");
            if (!File.Exists(tableName))
                throw new Exception("Unable to access file path given.");
            AbstractClassifier cf = LoadClassifier();
            PerfomanceEvaluator pf;
            try
            {
                pf = new PerfomanceEvaluator(cf, tableName, skipFirstLine);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to initialise performance evaluation: {ex.Message}", ex);
            }
            pf.ReadAll();
        }

        /// <summary>
        /// Prints SharpPcap provided device list to console out.
        /// </summary>
        public static void ListDevicies()
        {
            try
            {
                var devicies = SharpPcap.CaptureDeviceList.Instance;
                foreach (var dev in devicies)
                    Console.WriteLine($"{dev.Name} ---> {dev.Description}");
            }
            catch(Exception ex)
            {
                throw new Exception("Error reading device list", ex);
            }
        }

        public static int Main(string[] args)
        {
            Program.args = args;
            i = 0;
            try
            {
                ReadArgs();
                job();
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                if (Verbosity == Verbosity.Verbose)
                {
                    Console.Error.WriteLine("Full stack trace:");
                    Console.Error.WriteLine(ex);
                }
                return -1;
            }
            return 0;
        }
    }
}
