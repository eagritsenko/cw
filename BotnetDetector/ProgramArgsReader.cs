using System;
using System.Reflection;
using System.Security;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using LibBtntDtct;

namespace BotnetDetector
{
    public partial class Program
    {

        /// <summary>
        /// Represents arg parser state bound to an option name;
        /// </summary>
        class State : IComparable<State>
        {
            /// <summary>
            /// Option name this parsing state is bound to.
            /// </summary>
            public string option;
            /// <summary>
            /// Indicates whether this state should be called withought incrementing the counter.
            /// </summary>
            public bool fallthrough;
            /// <summary>
            /// Indicates whether this state does not invoke others.
            /// </summary>
            public bool single;
            /// <summary>
            /// Represent this state's action
            /// </summary>
            public Action action;

            public State() { }

            /// <summary>
            /// Initializes a new instance of the <see cref="T:BotnetDetector.Program.State"/> class.
            /// </summary>
            /// <param name="action">Action.</param>
            /// <param name="attribute">State attribute.</param>
            public State(Action action, StateAttribute attribute)
            {
                this.option = attribute.OptionName;
                this.fallthrough = attribute.Fallthrough;
                this.single = attribute.Single;
                this.action = action;
            }

            /// <summary>
            /// Compares to the other state, using option names.
            /// </summary>
            /// <returns>The to.</returns>
            /// <param name="other">Other.</param>
            public int CompareTo(State other) => string.Compare(this.option, other.option, StringComparison.Ordinal);
        }

        /// <summary>
        /// Array of states bound to an option.
        /// </summary>
        private static State[] states;

        /// <summary>
        /// Array of this program's args.
        /// </summary>
        private static string[] args;
        /// <summary>
        /// Current arg index.
        /// </summary>
        private static int i;
        /// <summary>
        /// Current arg.
        /// </summary>
        /// <value>The argument.</value>
        private static string Arg => args[i];

        /// <summary>
        /// The external classifier path.
        /// </summary>
        private static string externalClassifierPath = null;
        /// <summary>
        /// The name of the external classifier.
        /// </summary>
        private static string externalClassifierName = null;

        /// <summary>
        /// Worker behaviour switches.
        /// </summary>
        private static bool classify = true, printFlows = true, printClasses = true,
                            printAbnormalTraficOnly = false, nameAbnormalTraficAsBotnet = false;

        /// <summary>
        /// Indicates whether first line should be skiped while reading a flow table file.
        /// </summary>
        private static bool skipFirstLine = false;

        /// <summary>
        /// Indicates whether classifier path provided should be treated as absoulte.
        /// </summary>
        private static bool useAbsoluteClassifierPath = false;

        /// <summary>
        /// Indicates whether current state does not invoke others.
        /// </summary>
        private static bool currentStateSingle = false;

        /// <summary>
        /// Dummy state to use for binary search.
        /// </summary>
        private static State dummy = new State();

        /// <summary>
        /// Current bool action to invoke when switch options are used.
        /// </summary>
        private static Action<bool> currentBoolAction = null;

        /// <summary>
        /// List of input args.
        /// </summary>
        public static List<string> Input { get; } = new List<string>();

        /// <summary>
        /// List of output args.
        /// </summary>
        public static List<string> Output { get; } = new List<string>();

        /// <summary>
        /// Represents the parser's state: an action to use to read the current arg.
        /// </summary>
        public static Action state = ReadOption;

        /// <summary>
        /// This program's main job to perform.
        /// </summary>
        public static Action job = PrintHelp;

        /// <summary>
        /// Preprocesses the negateable argument.
        /// </summary>
        public static bool PreprocessNegateableArg(ref string s)
        {
            if (s.Length >= 2)
            {
                if (s[0] == '-')
                {
                    if (s[1] == '-')
                        s = s.Substring(1);
                    else
                        return s[1] == '0' && s.Length == 2;
                }
            }
            return false;
        }

        /// <summary>
        /// Reads the current arg as option and modifies or executes parser state related.
        /// </summary>
        public static void ReadOption()
        {
            dummy.option = Arg;
            int id = Array.BinarySearch(states, dummy);
            if(id < 0)
            {
                throw new Exception($"Unrecognised option {Arg}");
            }
            else
            {
                if (states[id].fallthrough)
                {
                    states[id].action();
                }
                else
                {
                    state = states[id].action;
                    currentStateSingle = states[id].single;
                }
            }
        }

        [State("-h", true), State("--help", true)]
        public static void SetPrintHelp() => job = PrintHelp;

        [State("-i"), State("--input")]
        public static void ReadInput() => Input.Add(Arg);

        [State("-o"), State("--output")]
        public static void ReadOutput() => Output.Add(Arg);

        [State("--classifier", false, false)]
        public static void ReadClassifier()
        {
            string arg = Arg;
            if (PreprocessNegateableArg(ref arg)) {
                externalClassifierPath = null;
                externalClassifierName = null;
                state = ReadOption;
                return;
            }
            externalClassifierPath = arg;
            state = ReadExternalClassifierName;
        }

        public static void ReadExternalClassifierName()
        {
            externalClassifierName = Arg;
            state = ReadOption;
            currentStateSingle = false;
        }

        [State("-l", true), State("--live", true)]
        public static void SetLiveWorkerJob() => job = PrepareAndExecuteLiveWorker;

        [State("-p", true), State("--pcap", true)]
        public static void SetPcapWorkerJob() => job = PrepareAndExecutePcapWorker;

        [State("-t", true), State("--table", true)]
        public static void SetTableWorkerJob() => job = PrepareAndExecuteTableWorker;

        [State("-pr", true), State("--performance", true)]
        public static void SetEvaluatePerformanceJob() => job = EvaluatePerformance;

        [State("-ls", true), State("--listDevicies", true)]
        public static void SetListDeviciesJob() => job = ListDevicies;

        [State("--classify", true)]
        public static void SetClassify()
        {
            classify = true;
            currentBoolAction = (v) => classify = v;
        }

        [State("--printFlows", true)]
        public static void SetPrintFlows()
        {
            printFlows = true;
            currentBoolAction = (v) => printFlows = v;
        }

        [State("--printClasses", true)]
        public static void SetPrintClasses()
        {
            printClasses = true;
            currentBoolAction = (v) => printClasses = v;
        }

        [State("--printAbnormalOnly", true)]
        public static void SetPrintAbnormalOnly()
        {
            printAbnormalTraficOnly = true;
            currentBoolAction = (v) => printAbnormalTraficOnly = v;
        }

        [State("--nameAbnormalAsBotnet", true)]
        public static void SetNameAbnormalAsBotnet()
        {
            nameAbnormalTraficAsBotnet = true;
            currentBoolAction = (v) => nameAbnormalTraficAsBotnet = v;
        }

        [State("--absoluteClassifierPath", true)]
        public static void SetUseAbsoluteClassifierPath()
        {
            useAbsoluteClassifierPath = true;
            currentBoolAction = (v) => useAbsoluteClassifierPath = v;
        }

        [State("--skipFirstLine", true)]
        public static void SetSkipFirstLine()
        {
            skipFirstLine = true;
            currentBoolAction = (v) => skipFirstLine = v;
        }

        [State("-0", true), State("-1", true)]
        public static void SetBoolOption()
        {
            if(currentBoolAction != null)
            {
                currentBoolAction(Arg[1] == '1');
                currentBoolAction = null;
            }
        }

        [State("-v", true), State("--verbose", true)]
        public static void MakeVerbose() => Verbosity = Verbosity.Verbose;

        [State("-q", true), State("--quiet", true)]
        public static void MakeQuiet() => Verbosity = Verbosity.Quiet;

        /// <summary>
        /// Returns the last item in the list or null if none exeists.
        /// </summary>
        public static string LastIn(List<string> list)
        {
            if (list.Count == 0)
                return null;
            else
                return list[list.Count - 1];
        }

        /// <summary>
        /// Reads the arguments to end, executing the parser state for each arg.
        /// </summary>
        public static void ReadArgs()
        {
            for (; i < args.Length; i++)
            {
                if (currentStateSingle)
                {
                    state();
                    state = ReadOption;
                    currentStateSingle = false;
                }
                else
                    state();
            }
            if (state != ReadOption)
                throw new Exception("More arguments expected for the rightmost option.");
        }

        /// <summary>
        /// Initializes the <see cref="T:BotnetDetector.Program"/> class.
        /// Creates option states array using state attributes.
        /// </summary>
        static Program()
        {
            var mArr = typeof(Program)
                .GetMethods()
                .Where((arg) => Attribute.IsDefined(arg, typeof(StateAttribute)));
            SortedSet<State> states = new SortedSet<State>();
            foreach(var mi in mArr)
            {
                Action action = (Action)mi.CreateDelegate(typeof(Action));
                var attributes = mi.GetCustomAttributes<StateAttribute>();
                foreach(var attribute in attributes)
                {
                    states.Add(new State(action, attribute));
                }
            }
            Program.states = new State[states.Count];
            states.CopyTo(Program.states);
        }

    }
}
