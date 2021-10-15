using System;
using System.Collections.Generic;
using LibBtntDtct;

namespace BotnetDetector
{
    /// <summary>
    /// Used to perform binary (normal/botnet) classification ontop of a (non)-binary classifier.
    /// </summary>
    public class BinaryBotnetClassifier : AbstractClassifier
    {
        /// <summary>
        /// The classifier result of which are converted to either normal or botnet classes.
        /// </summary>
        AbstractClassifier inner;

        protected int normalClassId;
        protected bool hasNormalClass;

        /// <summary>
        /// Returns the lis of inner classifier's classes.
        /// </summary>
        protected IReadOnlyList<FlowClass> InnerClasses => inner.FlowClasses;

        /// <summary>
        /// Gets the normal class identifier of the inner classifier.
        /// </summary>
        public int NormalClassID { get; }

        /// <summary>
        /// Returns the list of this classifier classes: normal and botnet class.
        /// </summary>
        public override IReadOnlyList<FlowClass> FlowClasses { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.BinaryBotnetClassifier"/> class.
        /// </summary>
        /// <param name="inner">Inner classifier to create this ontop of.</param>
        public BinaryBotnetClassifier(AbstractClassifier inner)
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));
            this.inner = inner;
            for(int i = 0; i < InnerClasses.Count; i++)
            {
                if (InnerClasses[i].Name == "normal")
                {
                    normalClassId = i;
                    hasNormalClass = true;
                }
            }
            FlowClasses = FlowClass.Create("normal", "botnet");
        }

        /// <summary>
        /// Classify the specified flow into either normal or botnet class.
        /// Class is considered to be botnet,
        /// If the inner classifier returns class with id unequal to that of the normal class.
        /// </summary>
        /// <returns>Flow class evaluated.</returns>
        public override FlowClass Classify(Flow f)
        {
            if (hasNormalClass)
                return FlowClasses[inner.Classify(f).Id == normalClassId ? 0 : 1];
            else
                return FlowClasses[1];
        }
    }
}
