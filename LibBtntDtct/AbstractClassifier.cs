using System;
using System.Collections.Generic;
using System.Reflection;

namespace LibBtntDtct
{
    public abstract class AbstractClassifier
    {
        static Type[] noTypeConstructors = { };

        /// <summary>
        /// Gets the list of flow classes of this classifier.
        /// </summary>
        public abstract IReadOnlyList<FlowClass> FlowClasses { get; }

        /// <summary>
        /// Classify the specified flow.
        /// </summary>
        /// <returns>Flow class evaluated.</returns>
        public abstract FlowClass Classify(Flow f);

        /// <summary>
        /// Classify the specified flow and validate returned result.
        /// Throws exceptions on invalid class values.
        /// </summary>
        /// <returns>Flow class evaluated.</returns>
        public FlowClass ValidateClassify(Flow f)
        {
            bool sameClassGroup;
            bool emptyName;
            FlowClass cl;
            try
            {
                cl = Classify(f);
                sameClassGroup = cl.OfSameClassGroup(FlowClasses[0]);
                emptyName = string.IsNullOrEmpty(cl.Name);
            }
            catch(Exception inner)
            {
                throw new Exception("Classifier has returned invalid class value.", inner);
            }
            if (!sameClassGroup)
                throw new Exception("Classifier has returned a class value of other class group.");
            if (emptyName)
                throw new Exception("Classifier has returned a class value with empty or null name.");
            return cl;
        }

        /// <summary>
        /// Indicates whether a type is a valid basic classifier.
        /// </summary>
        /// <returns><c>true</c>, if a type is a valid basic classifier, <c>false</c> otherwise.</returns>
        public static bool ValidnessOf(Type t)
        {
            return t.IsClass && t.IsSubclassOf(typeof(AbstractClassifier)) &&
                   (t.GetConstructor(noTypeConstructors)?.IsPublic ?? false);
        }

    }
}
