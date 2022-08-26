using Axis.Pulsar.Parser.Utils;
using System;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// Terminal symbol representing regular-expression patterns of strings
    /// </summary>
    public record PatternRule : ITerminal<Regex>
    {
        /// <summary>
        /// The regular expression pattern.
        /// Note: consider converting this to a function that calls <see cref="Regex.ToString"/>
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// The regex pattern that defines this rule. This regex must recognize at least 1 token.
        /// </summary>
        public Regex Value { get; }

        /// <summary>
        /// Defines how the regular expression will be interpreted.
        /// <para>
        /// Match cardinality specifies the boundaries of the number of CHARACTERS this pattern is expecting to match, starting from a count of 1.
        /// </para>
        /// <para>
        /// Essentially, the rule continues to match characters until the upper limit is reached, at which point it stops trying to match characters.
        /// This places a restriction on the <see cref="System.Text.RegularExpressions.Regex"/> being used for the underlying matching operation.
        /// </para>
        /// <para>
        /// Note: Cardinalities for the <see cref="PatternRule"/> further constrains the <see cref="Cardinality.MinOccurence"/> property to only accept values <c> >= 1</c>.
        /// </para>
        /// </summary>
        public Cardinality MatchCardinality { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold => null;

        /// <summary>
        /// Creates a new Pattern rule instance
        /// </summary>
        /// <param name="regex">The regex to use in matching characters</param>
        /// <param name="matchCardinality">The <see cref="PatternRule.MatchCardinality"/> instance</param>
        public PatternRule(
            Regex regex,
            Cardinality matchCardinality)
        {
            Value = regex ?? throw new ArgumentNullException(nameof(regex));
            Pattern = regex.ToString();
            MatchCardinality = matchCardinality.ThrowIf(
                v => v.MinOccurence <= 0,
                new ArgumentException($"cardinality {nameof(Cardinality.MinOccurence)} must be >= 1"));
        }

        public PatternRule(Regex regex) 
            : this(regex, Cardinality.Occurs(1, null))
        { }
    }
}
