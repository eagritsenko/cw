using System;
namespace LibBtntDtct
{
    /// <summary>
    /// Represents a flow adapter which classifies flows.
    /// </summary>
    public class ClassifyingFlowAdapter : FlowAdapter
    {
        /// <summary>
        /// Gets or sets the flow classifier to use.
        /// </summary>
        public AbstractClassifier FlowClassifier { get; protected set; }

        /// <summary>
        /// Occurs when flow was classified.
        /// </summary>
        public event Action<Flow, FlowClass> FlowClassified;

        public override void KillFlows()
        {
            if(FlowClassified != null)
                Flows.ForEach(f => FlowClassified(f, FlowClassifier.Classify(f)));
            base.KillFlows();
        }

        public ClassifyingFlowAdapter(AbstractClassifier classifier)
        {
            FlowClassifier = classifier;
        }

    }
}
