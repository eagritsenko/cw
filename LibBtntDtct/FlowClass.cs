using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace LibBtntDtct
{
    /// <summary>
    /// Represents a flow class.
    /// </summary>
    public class FlowClass : IEquatable<FlowClass>
    {
        /// <summary>
        /// Max class ID of all FlowClasses initialised.
        /// </summary>
        protected static long maxClassID = 0;

        /// <summary>
        /// Represents this flow class name.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Represents this flow class id in the class list.
        /// </summary>
        protected readonly int id;

        /// <summary>
        /// Returns this flow class's id in the current class list.
        /// </summary>
        public virtual int Id => id;

        /// <summary>
        /// Represents this flow class's class list ID.
        /// </summary>
        public virtual long ClassListID { get; protected set; }

        /// <summary>
        /// Represents the class list this flow class belongs to.
        /// </summary>
        /// <value>The class list.</value>
        public virtual IReadOnlyList<FlowClass> ClassList { get; protected set; }

        /// <summary>
        /// Determines whether the specified <see cref="LibBtntDtct.FlowClass"/> is equal to the current <see cref="T:LibBtntDtct.FlowClass"/>.
        /// It uses list class id's and flow class id for comparision.
        /// </summary>
        /// <param name="other">The <see cref="LibBtntDtct.FlowClass"/> to compare with the current <see cref="T:LibBtntDtct.FlowClass"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="LibBtntDtct.FlowClass"/> is equal to the current
        /// <see cref="T:LibBtntDtct.FlowClass"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(FlowClass other) => id == other.id && ClassListID == other.ClassListID;

        /// <summary>
        /// Checks whether this flow is of the same class group with the other by id.
        /// </summary>
        public bool OfSameClassGroup(FlowClass other) => ClassListID == other.ClassListID;

        /// <summary>
        /// Checks whether this flow is of the same class group with the other by name comparision.
        /// </summary>
        public bool DeepOfSameClassGroup(FlowClass other)
        {
            HashSet<string> classNames;
            if (this.ClassList.Count == other.ClassList.Count)
            {
                classNames = new HashSet<string>(ClassList.Select(c => c.Name));
                for (int i = 0; i < other.ClassList.Count; i++)
                    if (!classNames.Contains(other.ClassList[i].Name))
                        return false;
            }
            else
                return false;
            return true;
        }

        /// <summary>
        /// If two flow class groups (of this flow class and other) are equal by names and class id's,
        /// Tries to set other's class group same class group id's
        /// So that they can be operated by id's.
        /// </summary>
        /// <returns><c>true</c>, on success, <c>false</c> otherwise.</returns>
        public bool TryReduce(FlowClass other)
        {
            bool result = true;
            result = other.ClassList.Count == ClassList.Count;
            if (other.ClassList.Count == ClassList.Count)
                return false;
            for (int i = 0; i < ClassList.Count; i++) { 
                if (ClassList[i].Name != other.ClassList[i].Name)
                {
                    return false;
                }
            }
            var list = other.ClassList;
            for (int i = 0; i < list.Count; i++)
                list[i].ClassListID = this.ClassListID;
            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="T:LibBtntDtct.FlowClass"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="T:LibBtntDtct.FlowClass"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to the current
        /// <see cref="T:LibBtntDtct.FlowClass"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is FlowClass && this.Equals((FlowClass)obj);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="T:LibBtntDtct.FlowClass"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        /// hash table.</returns>
        public override int GetHashCode() => id.GetHashCode() + ClassListID.GetHashCode();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LibBtntDtct.FlowClass"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="id">Identifier.</param>
        /// <param name="classListId">Class list identifier.</param>
        protected FlowClass(string name, int id, long classListId)
        {
            this.Name = name;
            this.id = id;
            this.ClassListID = classListId;
        }

        /// <summary>
        /// Create a new class list group using array of class names provided.
        /// </summary>
        public static FlowClass[] Create(params string[] arr)
        {
            long newClassListID = Interlocked.Read(ref maxClassID);
            Interlocked.Increment(ref maxClassID);
            FlowClass[] flowClasses = new FlowClass[arr.Length];
            for(int i = 0; i < arr.Length; i++)
            {
                flowClasses[i] = new FlowClass(arr[i], i, newClassListID);
                flowClasses[i].ClassList = flowClasses;
            }
            return flowClasses;
        }

    }
}
