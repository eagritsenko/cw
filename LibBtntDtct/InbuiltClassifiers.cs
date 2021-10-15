using System;
using System.Collections.Generic;

namespace LibBtntDtct
{
    /// <summary>
    /// Class to store inbuilt classifiers in.
    /// </summary>
    public class InbuiltClassifiers
    {
        /// <summary>
        /// Represent the default (and only) classifier.
        /// </summary>
        public class Default : AbstractClassifier
        {
            /// <summary>
            /// Flow duration to use when it's uncknown.
            /// </summary>
            private static TimeSpan uncknownDuration = new TimeSpan(0, 0, 0, 0, 10);
            /// <summary>
            /// The botnet and normal flow classes.
            /// </summary>
            private static FlowClass[] flowClasses = FlowClass.Create("normal", "botnet");
            /// <summary>
            /// Returns the botnet and normal flow classes this classifier classifies flows into.
            /// </summary>
            public override IReadOnlyList<FlowClass> FlowClasses => flowClasses;

            /// <summary>
            /// Gets the flow's average payload length.
            /// </summary>
            public double GetFlowAPL(Flow f)
            {
                return (double)f.PayloadOctetsCount / f.PacketsCount;
            }

            /// <summary>
            /// Gets how many bytes per second are transmited in this flow.
            /// </summary>
            public double GetFlowBS(Flow f)
            {
                if (f.End == f.Start)
                    return f.OctetsCount / uncknownDuration.TotalSeconds;
                else
                    return f.OctetsCount / (f.End - f.Start).TotalSeconds;
            }

            /// <summary>
            /// Gets the flow's outgoing to total packets ratio.
            /// </summary>
            public double GetFlowIOPR(Flow f)
            {
                return (double)f.OutgoingPackets / f.PacketsCount;
            }

            /// <summary>
            /// Returns this flow's duration.
            /// </summary>
            public double GetFlowDuration(Flow f)
            {
                return (f.End == f.Start ? uncknownDuration : f.End - f.Start).TotalSeconds;
            }

            /// <summary>
            /// Classify the specified flow into either normal or botnet class.
            /// </summary>
            /// <returns>Flow class evaluated.</returns>
            public override FlowClass Classify(Flow f)
            {
                double[] arr = new double[4];
                int result;
                arr[0] = GetFlowAPL(f);
                arr[1] = GetFlowBS(f);
                arr[2] = GetFlowIOPR(f);
                arr[3] = GetFlowDuration(f);
                result = DefaultTree.Function(arr);
                if (result == 0 || result == 1)
                    return FlowClasses[result];
                else
                    return null;
            }

        }
    }
}
