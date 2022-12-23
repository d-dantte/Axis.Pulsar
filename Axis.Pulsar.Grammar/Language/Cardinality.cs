using Axis.Luna.Extensions;
using System;

namespace Axis.Pulsar.Grammar.Language
{
    /// <summary>
    /// Defines the number of times a group can occur.
    /// <para>
    /// Note: Within Pulsar, rules with cardinalities having a <see cref="Cardinality.MinOccurence"/> of zero, are greedy recognizers,
    /// meaning if recognition fails, the recognizer will report it as a success - i.e, the rule was absent, and create a 
    /// <see cref="CST.CSTNode.BranchNode"/> instance with zero child-nodes.
    /// </para>
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

        private Cardinality(int min, int? max = null)
        {
            MaxOccurence = max.ThrowIf(
                Extensions.IsNegative,
                new ArgumentException($"{nameof(max)} cannot be negative"));

            MinOccurence = min.ThrowIf(
                Extensions.IsNegative,
                new ArgumentException($"{nameof(min)} cannot be negative"));

            Validate();
        }

        public override bool Equals(object obj)
        {
            return obj is Cardinality other
                && other.MinOccurence == MinOccurence
                && other.MaxOccurence == MaxOccurence;
        }

        public override int GetHashCode() => HashCode.Combine(MinOccurence, MaxOccurence);

        public override string ToString()
        {
            return this switch
            {
                Cardinality c when c.MinOccurence.Equals(c.MaxOccurence) && c.MinOccurence > 1 => $".{MinOccurence}",
                { MinOccurence: 1, MaxOccurence: 1 } => "",
                { MinOccurence: 0, MaxOccurence: 1 } => ".?",
                { MinOccurence: 0, MaxOccurence: null } => ".*",
                { MinOccurence: 1, MaxOccurence: null } => ".+",
                { MaxOccurence: null } => $".{MinOccurence},",
                { } => $".{MinOccurence},{MaxOccurence}"
            };
        }

        /// <summary>
        /// Check that the <paramref name="occurenceCount"/> is within range of the cardinality
        /// </summary>
        /// <param name="occurenceCount">The occurence count</param>
        public bool IsValidRange(int occurenceCount)
        {
            return occurenceCount >= MinOccurence
                && (MaxOccurence == null || occurenceCount <= MaxOccurence);
        }

        /// <summary>
        /// Check if, given the completed repetitions, it is legal to repeat the parse cycle based on the cardinality
        /// </summary>
        /// <param name="completedRepetitions"></param>
        /// <returns>Value indicating if a repetition is legal</returns>
        public bool CanRepeat(int completedRepetitions)
        {
            return MaxOccurence == null || completedRepetitions < MaxOccurence;
        }

        /// <summary>
        /// Validates the cardinality
        /// </summary>
        private void Validate()
        {
            if (MinOccurence > MaxOccurence)
                throw new InvalidOperationException("Minimum Occurence cannot be more than Maximum Occurence");

            else if (MinOccurence == 0 && MaxOccurence == 0)
                throw new InvalidOperationException("Both Occurence values cannot be 0");
        }


        public static Cardinality OccursOnlyOnce() => OccursOnly(1);

        public static Cardinality OccursOnly(int occurences) => new(occurences, occurences);

        public static Cardinality OccursAtLeastOnce() => OccursAtLeast(1);

        public static Cardinality OccursAtLeast(int leastOccurences) => new(leastOccurences);

        public static Cardinality OccursAtMost(int maximumOccurences) => new(1, maximumOccurences);

        public static Cardinality Occurs(int minOccurences, int? maxOccurences)
        {
            return new(
                minOccurences,
                maxOccurences);
        }

        public static Cardinality OccursNeverOrAtMost(int maximumOccurences) => new(0, maximumOccurences);

        public static Cardinality OccursNeverOrMore() => new(0);

        public static Cardinality OccursOptionally() => OccursNeverOrAtMost(1);

        public static bool operator ==(Cardinality left, Cardinality right) => left.Equals(right);

        public static bool operator !=(Cardinality left, Cardinality right) => !(left == right);
    }
}
