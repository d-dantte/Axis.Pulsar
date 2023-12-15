using Axis.Luna.Common;
using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    /// <summary>
    /// Defines the number of times a group can occur.
    /// <para>
    /// Note: Within Pulsar, rules with cardinalities having a <see cref="MinOccurence"/> of zero, are greedy recognizers,
    /// meaning if recognition fails, the recognizer will report it as a success - i.e, the rule was absent, and create a 
    /// <see cref="CST.ICSTNode.NonTerminal"/> instance with zero child-nodes.
    /// </para>
    /// </summary>
    public readonly struct Cardinality:
        IEquatable<Cardinality>,
        IDefaultValueProvider<Cardinality>
    {
        #region properties
        /// <summary>
        /// Maximum number of occurences. Null indicates open-ended maximum occurence
        /// </summary>
        public int? MaxOccurence { get; }

        /// <summary>
        /// Minimum number of occurence
        /// </summary>
        public int MinOccurence { get; }

        public bool IsOpen => MaxOccurence is null;

        public bool IsClosed => !IsOpen;
        #endregion

        #region DefaultValueProvider

        public bool IsDefault => MaxOccurence is null && MinOccurence == 0;

        public static Cardinality Default => default;

        #endregion

        private Cardinality(int min, int? max = null)
        {
            MaxOccurence = max.ThrowIf(
                i => i < 0,
                _ => new ArgumentException($"{nameof(max)} cannot be negative"));

            MinOccurence = min.ThrowIf(
                i => i < 0,
                _ => new ArgumentException($"{nameof(min)} cannot be negative"));

            Validate();
        }

        #region overrides
        public override bool Equals(object? obj)
        {
            return obj is Cardinality other && Equals(other);
        }

        public bool Equals(Cardinality other)
        {
            return other.MinOccurence == MinOccurence
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
        #endregion

        #region API
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
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="symbolPath">The path of the production that owns the <paramref name="element"/></param>
        /// <param name="element">The element to apply cardinality recognition to</param>
        /// <param name="result">The result of applying cardinality recognition</param>
        /// <returns></returns>
        public bool TryRepeat(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            IGroupElement element,
            out GroupRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(symbolPath);

            var occurence = 0;
            var position = reader.Position;
            var nodeSequence = INodeSequence.Empty;
            GroupRecognitionResult elementResult = GroupRecognitionResult.Of(nodeSequence);

            while (CanRepeat(occurence))
            {
                var stepPosition = reader.Position;
                if (element.TryRecognize(
                    reader,
                    symbolPath,
                    context,
                    out elementResult))
                {
                    if (!elementResult.Is(out INodeSequence elementSequence))
                        throw new InvalidOperationException(
                            $"Invalid result: Expected sequence, found - {elementResult}");

                    nodeSequence = nodeSequence.Append(elementSequence);
                    occurence++;
                }
                else
                {
                    reader.Reset(stepPosition);
                    break;
                }
            }

            if (IsValidRange(occurence))
            {
                result = GroupRecognitionResult.Of(nodeSequence);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = elementResult.MapMatch(

                    // data, but since the range wasn't valid...
                    _ => FailedRecognitionError
                        .Of(symbolPath, position)
                        .ApplyTo(error => GroupRecognitionError.Of(error, nodeSequence.Count))
                        .ApplyTo(GroupRecognitionResult.Of),

                    // GroupRecognitionError
                    gre => GroupRecognitionError
                        .Of(gre.Cause, nodeSequence.Count + gre.ElementCount)
                        .ApplyTo(GroupRecognitionResult.Of));

                return false;
            }
        }
        #endregion

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

        #region Standard Cardinality values
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
        #endregion

        public static bool operator ==(Cardinality left, Cardinality right) => left.Equals(right);

        public static bool operator !=(Cardinality left, Cardinality right) => !(left == right);
    }
}
