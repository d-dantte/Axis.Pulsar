using Axis.Pulsar.Parser.Utils;
using System;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// Terminal symbol representing regular-expression patterns of strings
    /// </summary>
    public class PatternRule : ITerminal
    {
        /// <summary>
        /// The regular expression pattern.
        /// Note: consider converting this to a function that calls <see cref="Regex.ToString"/>
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// The <see cref="Regex"/> instance built using the <see cref="PatternRule.Pattern"/> string
        /// </summary>
        public Regex Regex { get; }

        /// <summary>
        /// The regex pattern that defines this rule. This regex must recognize at least 1 token.
        /// </summary>
        public string Value => Pattern;

        /// <summary>
        /// Defines how the regular expression will be interpreted.
        /// <para>
        /// Note: Cardinalities for the <see cref="PatternRule"/> further constrain the <see cref="Cardinality.MinOccurence"/> property to only accept values <c> >= 1</c>.
        /// </para>
        /// </summary>
        public Cardinality MatchCardinality { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold { get; }

        public PatternRule(
            Regex regex,
            int? recognitionThreshold,
            Cardinality matchCardinality = default)
        {
            Regex = regex ?? throw new ArgumentNullException(nameof(regex));
            Pattern = regex.ToString();
            MatchCardinality = matchCardinality.ThrowIf(
                v => v.MinOccurence <= 0,
                new ArgumentException($"cardinality {nameof(Cardinality.MinOccurence)} must be >= 1"));
            RecognitionThreshold = recognitionThreshold.ThrowIf(
                Extensions.IsZeroOrLess,
                new ArgumentException($"{nameof(recognitionThreshold)} cannot be <= 0"));
        }
    }
}
