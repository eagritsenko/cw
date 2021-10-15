using System;
using System.Timers;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LibBtntDtct;

namespace BotnetDetector
{
    public class PerfomanceEvaluator
    {
        /// <summary>
        /// Represents a labeled flow.
        /// </summary>
        class LabeledFlow : Flow
        {
            /// <summary>
            /// Flow label reprsenting its class name
            /// </summary>
            public string Label { get; set; } = "unknown";

            /// <summary>
            /// Parses a labeled flow from the string given.
            /// </summary>
            public static new LabeledFlow Parse(string s)
            {
                LabeledFlow flow = new LabeledFlow();
                string[] arr = s.Split(',');
                int labelId = Flow.Parse(arr, 0, flow);
                flow.Label = arr[labelId].Trim();
                return flow;
            }
        }

        private StreamReader reader;

        /// <summary>
        /// Classifier to evaluate accuracy for.
        /// </summary>
        /// <value>The classifier.</value>
        private AbstractClassifier Classifier { get; }

        /// <summary>
        /// Indicates whether a class name of the evaluated classifier exists in the file as well.
        /// </summary>
        private Dictionary<string, bool> foundClassNames;

        /// <summary>
        /// Represents the number of abnormal flows from the file by class,
        /// incorrectly classified as abnormal by the evaluated classifier.
        /// </summary>
        private Dictionary<string, long> falseNegativeBotnetCounter;

        /// <summary>
        /// Represents the number of flows for each class from the file.
        /// </summary>
        private Dictionary<string, long> realClassifiedCounter;

        /// <summary>
        /// Represents the number of flows in each computed class by class ID.
        /// </summary>
        private long[] computedClassifiedCounter;

        /// <summary>
        /// Number of flows classified as abnormal in the file.
        /// All abnormal flows are considered to be botnet.
        /// </summary>
        private long realBotnetCount;

        /// <summary>
        /// Number of flows classified as normal in the file.
        /// </summary>
        private long realNormalCount;

        /// <summary>
        /// Number of flow computed as botnet.
        /// </summary>
        private long computedBotnetCount;

        /// <summary>
        /// Number of flows computed as normal.
        /// </summary>
        private long computedNormalCount;


        private long FP, FN, TP, TN, total;

        /// <summary>
        /// Normal class id in the classifier given.
        /// </summary>
        private int normalClassId = -1;

        /// <summary>
        /// Indicates whether sets of classes in the file and the evaluated classifier are equal.
        /// </summary>
        private bool equalClasses = true;

        private Timer timer;

        /// <summary>
        /// Prints current status: how many flows have been processed.
        /// </summary>
        private void PrintStatus(object sender, ElapsedEventArgs e)
        {
            Console.Clear();
            Console.Write($"{total} flows have been processed.");
        }

        /// <summary>
        /// Prints the stats.
        /// </summary>
        public void PrintStats()
        {
            string P(long val)
            {
                return $"{val} ({(double)val / total * 100:F3}%)";
            }

            string PFN(long val)
            {
                return $"{val} ({(double)val / FN * 100:F3}%)";
            }

            Console.WriteLine($"Processed {total} flows.");
            Console.WriteLine($"Computed: {P(computedNormalCount)} as normal,\t{P(computedBotnetCount)} as botnet.");
            Console.WriteLine($"Real: {P(realNormalCount)} as normal,\t{P(realBotnetCount)} as botnet.");
            Console.WriteLine();
            Console.WriteLine($"Botnet/normal classes validation results: ");
            Console.WriteLine($"{P(TP)} TP,\t{P(TN)} TN,\t{P(TP + TN)} True");
            Console.WriteLine($"{P(FP)} FP,\t{P(FN)} FN,\t{P(FP + FN)} False");
            Console.WriteLine();
            Console.WriteLine($"Flows contributed to FN were follows:");
            foreach (var fcp in falseNegativeBotnetCounter)
                Console.WriteLine($"{fcp.Key}: {PFN(fcp.Value)}");
            Console.WriteLine();
            if (equalClasses)
            {
                Console.WriteLine($"By class comparision:");
                for (int i = 0; i < computedClassifiedCounter.Length; i++)
                {
                    FlowClass current = Classifier.FlowClasses[i];
                    long computed = computedClassifiedCounter[i];
                    long real = realClassifiedCounter[current.Name];
                    Console.WriteLine($"{current.Name}: {P(computed)} computed,\t{P(real)} real");
                }
            }
            else
            {
                Console.WriteLine($"Computed classes statistics:");
                for (int i = 0; i < computedClassifiedCounter.Length; i++)
                    Console.WriteLine($"{Classifier.FlowClasses[i].Name}: {P(computedClassifiedCounter[i])}");
                Console.WriteLine();
                Console.WriteLine($"Read classes statistics:");
                foreach (var fcp in realClassifiedCounter)
                    Console.WriteLine($"{fcp.Key}: {P(fcp.Value)}");
            }
        }

        /// <summary>
        /// Reads and processes the next labeled flow from the file.
        /// </summary>
        private void ReadNextPair()
        {
            LabeledFlow f;
            FlowClass cl;
            bool computedBotnet = false, realBotnet = false;
            try
            {
                f = LabeledFlow.Parse(reader.ReadLine());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading flow #{total} from the file.", ex);
            }
            cl = Classifier.ValidateClassify(f);
            if (equalClasses && foundClassNames.ContainsKey(f.Label))
            {
                foundClassNames[f.Label] = true;
            }
            else
                equalClasses = false;
            if (!realClassifiedCounter.ContainsKey(f.Label))
                realClassifiedCounter.Add(f.Label, 1);
            else
                realClassifiedCounter[f.Label]++;
            computedBotnet = cl.Id != normalClassId;
            realBotnet = f.Label != "normal";
            if (computedBotnet)
            {
                computedBotnetCount++;
                if (realBotnet)
                {
                    realBotnetCount++;
                    TP++;
                }
                else
                {
                    realNormalCount++;
                    FP++;
                }
            }
            else
            {
                computedNormalCount++;
                if (realBotnet)
                {
                    realBotnetCount++;
                    FN++;
                    if (falseNegativeBotnetCounter.ContainsKey(f.Label))
                        falseNegativeBotnetCounter[f.Label]++;
                    else
                        falseNegativeBotnetCounter.Add(f.Label, 1);
                }
                else
                {
                    TN++;
                    realNormalCount++;
                }
            }
            total++;
            computedClassifiedCounter[cl.Id]++;
        }

        /// <summary>
        /// Read and process all frows from the file given, and print results.
        /// </summary>
        public void ReadAll()
        {
            if (Program.Verbosity != Verbosity.Quiet)
            {
                timer = new Timer(1000);
                timer.Elapsed += PrintStatus;
                timer.Start();
            }
            while (!reader.EndOfStream)
                ReadNextPair();
            timer?.Stop();
            timer = null;
            if (Program.Verbosity != Verbosity.Quiet)
                Console.WriteLine();
            equalClasses = equalClasses && foundClassNames.Values.All(v => v);
            PrintStats();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.PerfomanceEvaluator"/> class.
        /// </summary>
        /// <param name="cf">Classifier to use.</param>
        /// <param name="labeldFlowsPath">Path to the labeled flows table file.</param>
        /// <param name="skipFirstRow">If set to <c>true</c> skips the first row.</param>
        public PerfomanceEvaluator(AbstractClassifier cf, string labeldFlowsPath, bool skipFirstRow)
        {
            if (cf == null)
                throw new ArgumentNullException(nameof(cf));
            Classifier = cf;
            try
            {
                reader = new StreamReader(File.OpenRead(labeldFlowsPath));
                if (skipFirstRow)
                    reader.ReadLine();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error initialising flows reader stream at path: {labeldFlowsPath}", ex);
            }
            foundClassNames = new Dictionary<string, bool>();
            for (int i = 0; i < Classifier.FlowClasses.Count; i++)
            {
                foundClassNames.Add(Classifier.FlowClasses[i].Name, false);
                if (Classifier.FlowClasses[i].Name == "normal")
                    normalClassId = i;
            }
            computedClassifiedCounter = new long[foundClassNames.Count];
            falseNegativeBotnetCounter = new Dictionary<string, long>();
            realClassifiedCounter = new Dictionary<string, long>();

        }
    }
}
