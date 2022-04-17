using System;

namespace Axis.Pulsar.Parser.Utils
{
    /// <summary>
    /// Defines the number of times a group can occur
    /// </summary>
    public struct Cardinality
    {
        /// <summary>
        /// Maximum number of occurences
        /// </summary>
        public int? MaxOccurence { get; }

        /// <summary>
        /// Minimum number of occurence
        /// </summary>
        public int MinOccurence { get; }


        public Cardinality(int min, int? max = null)
        {
            MaxOccurence = max.ThrowIf(
                Extensions.IsNegative,
                v => new ArgumentException($"{nameof(max)} cannot be negative"));

            MinOccurence = min.ThrowIf(
                Extensions.IsNegative,
                v => new ArgumentException($"{nameof(min)} cannot be negative"));

            Validate();
        }

        public override bool Equals(object obj)
        {
            return obj is Cardinality c
                && c.MinOccurence == this.MinOccurence
                && c.MaxOccurence == this.MaxOccurence;
        }

        public override int GetHashCode() => HashCode.Combine(MinOccurence, MaxOccurence);

        public override string ToString()
        {
            return this switch
            {
                Cardinality c when c.MinOccurence.Equals(c.MaxOccurence) && c.MinOccurence > 1 => $".{MinOccurence}",
                { MinOccurence: 1, MaxOccurence: 1 } => "",
                { MaxOccurence: null } => $".{MinOccurence},",
                {} => $".{MinOccurence},{MaxOccurence}"
            };
        }

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

        public static Cardinality OccursOnly(int occurences) => new(occurences, occurences);

        public static Cardinality OccursAtLeastOnce() => OccursAtLeast(1);

        public static Cardinality OccursAtLeast(int leastOccurences) => new(leastOccurences);

        public static Cardinality OccursOptionally() => OccursNeverOrAtMost(1);

        public static Cardinality OccursAtMost(int maximumOccurences) => new(1, maximumOccurences);

        public static Cardinality OccursNeverOrAtMost(int maximumOccurences) => new(0, maximumOccurences);

        public static Cardinality OccursNeverOrMore() => new(0);

        public static bool operator ==(Cardinality left, Cardinality right) => left.Equals(right);

        public static bool operator !=(Cardinality left, Cardinality right) => !(left == right);
    }
}
