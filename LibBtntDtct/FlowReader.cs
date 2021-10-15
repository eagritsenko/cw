using System;
using System.IO;

namespace LibBtntDtct
{
    /// <summary>
    /// Flow table reader.
    /// </summary>
    public class FlowReader
    {
        protected StreamReader reader;

        /// <summary>
        /// Indicates whether end of stream (end of file) is reached.
        /// </summary>
        public bool EOF => reader.EndOfStream;

        /// <summary>
        /// Reads a flow from the next row of the stream.
        /// </summary>
        /// <returns>The flow.</returns>
        public Flow ReadFlow()
        {
            return Flow.Parse(reader.ReadLine());
        }

        /// <summary>
        /// Reads a flow from the next row of the stream.
        /// </summary>
        /// <param name="at">Where to save flow data.</param>
        public void ReadFlow(Flow at)
        {
            Flow.Parse(reader.ReadLine(), at);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LibBtntDtct.FlowReader"/> class.
        /// </summary>
        /// <param name="s">Stream to initialize this reader on.</param>
        public FlowReader(Stream s)
        {
            reader = new StreamReader(s);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LibBtntDtct.FlowReader"/> class.
        /// </summary>
        /// <param name="s">Stream to initialize this reader on.</param>
        /// <param name="skipFirstLine">If set to <c>true</c> skips first line.</param>
        public FlowReader(Stream s, bool skipFirstLine)
        {
            reader = new StreamReader(s);
            if (skipFirstLine)
                reader.ReadLine();        
        }
    }
}
