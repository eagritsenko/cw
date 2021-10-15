using System;
namespace BotnetDetector
{
    /// <summary>
    /// Attribute of an option arg parser state function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class StateAttribute : Attribute
    {
        /// <summary>
        /// Option name the function is bound to.
        /// </summary>
        public string OptionName { get; set; }

        /// <summary>
        /// Indicates whether this function should be called withought incrementing the counter.
        /// </summary>
        public bool Fallthrough { get; set; }

        /// <summary>
        /// Indicates whether this parser state function does not invoke other parser states.
        /// </summary>
        public bool Single { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.StateAttribute"/> class.
        /// </summary>
        /// <param name="name">Option name.</param>
        public StateAttribute(string name)
        {
            Fallthrough = false;
            OptionName = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.StateAttribute"/> class.
        /// </summary>
        /// <param name="name">Option name.</param>
        /// <param name="fallthrough">If set to <c>true</c>  sets Fallthrough propety accordingly.</param>
        public StateAttribute(string name, bool fallthrough)
        {
            Fallthrough = fallthrough;
            OptionName = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BotnetDetector.StateAttribute"/> class.
        /// </summary>
        /// <param name="name">Option name.</param>
        /// <param name="fallthrough">If set to <c>true</c> sets Fallthrough propety accordingly.</param>
        /// <param name="single">If set to <c>true</c> sets Single property accordingly.</param>
        public StateAttribute(string name, bool fallthrough, bool single)
        {
            Fallthrough = fallthrough;
            OptionName = name;
            Single = single;
        }
    }
}
