using System;
using System.IO;
using LibBtntDtct;

namespace BotnetDetector
{
    /// <summary>
    /// Abstract worker used to process, classify, print flows and classes information and class statistics.
    /// </summary>
    public abstract class AbstractWorker
    {
        /// <summary>
        /// Indicates whether processed flows should be classified.
        /// </summary>
        public virtual bool Classify { get; set; } = true;

        /// <summary>
        /// Indicates whether flows should be printed.
        /// </summary>
        public virtual bool PrintFlows { get; set; } = true;
        /// <summary>
        /// Indicates whether flow classes should be printed.
        /// If classification is not carried out, neither is printing.
        /// </summary>
        public virtual bool PrintClasses { get; set; } = true;
        /// <summary>
        /// Indicates whether printing should occure only for the classes, name of which is not "normal".
        /// Has no effect if classification is not carried out.
        /// </summary>
        public virtual bool PrintAbnormalTraficOnly { get; set; } = true;
        /// <summary>
        /// Indicates whether all classes wih name not equal to "normal" should be named botnet.
        /// Has no effect if classification is not carried out.
        /// </summary>
        /// <remarks>
        /// If set to true, a binary (normal/botnet) classifier would be placed onto of the given.
        /// </remarks>
        public virtual bool NameAbnormalTraficAsBotnet { get; } = false;

        /// <summary>
        /// Inidicates whether to display statistics.
        /// </summary>
        public abstract bool DisplayStatistics { get; }

        /// <summary>
        /// Represents the output to print flows and classes info to.
        /// </summary>
        public virtual TextWriter Output { get; set; } = Console.Out;
        /// <summary>
        /// Indicates whether the classifier used has a class with name "normal".
        /// </summary>
        public virtual bool HasNormalClass { get; }
        /// <summary>
        /// Returns the ID of the first class with name "normal" of the classifier used.
        /// </summary>
        public virtual int NormalClassID { get; }
        /// <summary>
        /// Represents the classifier used.
        /// </summary>
        public virtual AbstractClassifier Classifier { get; }

        /// <summary>
        /// Represents the number of flows in each class.
        /// </summary>
        protected long[] classCount;

        /// <summary>
        /// Occurs when flow is classified.
        /// </summary>
        public event Action<Flow, FlowClass> FlowClassified;

        /// <summary>
        /// Occurs when flow is classified as abnormal.
        /// </summary>
        public event Action<Flow, FlowClass> FlowClassifiedAsAbnormal;

        /// <summary>
        /// Prints class statistics to console output.
        /// </summary>
        protected void DisplayClassStatistics()
        {
            Console.WriteLine("Flows statistics:");
            for (int i = 0; i < classCount.Length; i++)
                Console.WriteLine($"{classCount[i]}\t{Classifier.FlowClasses[i].Name}");
        }

        /// <summary>
        /// Prints the string represntation of a flow given.
        /// </summary>
        protected void PrintFlowOnly(Flow f) => Output.WriteLine(f);

        /// <summary>
        /// Prints the name of a flow class given.
        /// </summary>
        protected void PrintClassOnly(FlowClass cl) => Output.WriteLine(cl.Name);

        /// <summary>
        /// Creates the new counters of flow numbers in each class.
        /// </summary>
        protected void CreateNewClassCounters() => classCount = new long[Classifier.FlowClasses.Count];

        /// <summary>
        /// Prints the string representation of a flow, than ", ", and then class name.
        /// </summary>
        protected void PrintFlowAndClass(Flow f, FlowClass cl)
        {
            Output.Write(f);
            Output.Write(", ");
            Output.WriteLine(cl.Name);
        }

        /// <summary>
        /// Indicates whether a flow or it's flow class should be printed based on options.
        /// </summary>
        protected bool ShouldBePrinted(FlowClass cl) => !PrintAbnormalTraficOnly || cl.Id != NormalClassID;

        /// <summary>
        /// Used to process a flow if the classification is to be occured.
        /// </summary>
        protected void ProcessClassify(Flow f)
        {
            FlowClass cl = Classifier.ValidateClassify(f);
            classCount[cl.Id]++;
            FlowClassified?.Invoke(f, cl);
            if (cl.Id != NormalClassID)
                FlowClassifiedAsAbnormal?.Invoke(f, cl);
            if (ShouldBePrinted(cl))
            {
                if (PrintFlows)
                {
                    if (PrintClasses)
                        PrintFlowAndClass(f, cl);
                    else
                        PrintFlowOnly(f);
                }
                else if (PrintClasses)
                    PrintClassOnly(cl);
            }
        }

        /// <summary>
        /// Process the specified flow.
        /// </summary>
        protected void Process(Flow f)
        {
            if (Classify)
                ProcessClassify(f);
            else if (PrintFlows)
                PrintFlowOnly(f);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.AbstractWorker"/> class.
        /// </summary>
        /// <param name="cf">Classifier to use.</param>
        /// <param name="nameAbnormalTraficAsBotnet">If set to <c>true</c> names abnormal trafic as botnet.</param>
        /// <remarks>To understand abnormal trafic naming process <see cref="T:BotnetDetector.AbstractWorker.PrintAbnormalTraficOnly"/></remarks>
        protected AbstractWorker(AbstractClassifier cf, bool nameAbnormalTraficAsBotnet)
        {
            if (nameAbnormalTraficAsBotnet)
                Classifier = new BinaryBotnetClassifier(cf);
            else if (cf == null)
                throw new ArgumentNullException(nameof(cf));
            else
                Classifier = cf;
            NameAbnormalTraficAsBotnet = nameAbnormalTraficAsBotnet;
            CreateNewClassCounters();
            for(int i = 0; i < Classifier.FlowClasses.Count; i++)
            {
                if (Classifier.FlowClasses[i].Name == "normal")
                {
                    HasNormalClass = true;
                    NormalClassID = i;
                    break;
                }
                else
                    NormalClassID = -1;
            }
        }

    }
}
