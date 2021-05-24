using System;

namespace Axis.Pulsar.Parser.Language
{
    /// <summary>
    /// Defines the number of times a group can occur
    /// </summary>
    public struct Cardinality
    {
        /// <summary>
        /// Maximum number of occurences
        /// </summary>
        public uint? MaxOccurence { get; }

        /// <summary>
        /// Minimum number of occurence
        /// </summary>
        public uint MinOccurence { get; }


        public Cardinality(uint? max, uint min)
        {
            MaxOccurence = max;
            MinOccurence = min;

            Validate();
        }

        public override bool Equals(object obj)
        {
            return obj is Cardinality c
                && c.MinOccurence == this.MinOccurence
                && c.MaxOccurence == this.MaxOccurence;
        }

        public override int GetHashCode() => HashCode.Combine(MinOccurence, MaxOccurence);

        /// <summary>
        /// Validates the cardinality
        /// </summary>
        private void Validate()
        {
            if (MinOccurence > MaxOccurence)
                throw new ArgumentException("Minimum Occurence cannot be more than Maximum Occurence");

            else if (MinOccurence == 0 && MaxOccurence == 0)
                throw new ArgumentException("Both Occurence values cannot be 0");
        }


        public static Cardinality OccursOnlyOnce() => OccursOnly(1);

        public static Cardinality OccursOnly(uint occurences) => new(occurences, occurences);

        public static Cardinality OccursAtLeastOnce() => OccursAtLeast(1);

        public static Cardinality OccursAtLeast(uint leastOccurences) => new(null, leastOccurences);

        public static Cardinality OccursOptionally() => OccursNeverOrAtMost(1);

        public static Cardinality OccursAtMost(uint maximumOccurences) => new(maximumOccurences, 1);

        public static Cardinality OccursNeverOrAtMost(uint maximumOccurences) => new(maximumOccurences, 0);
    }
}
