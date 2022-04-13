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

        public string Value => Pattern;

        /// <summary>
        /// Defines how the regular expression will be interpreted.
        /// 
        /// TODO: expand on this description
        /// </summary>
        public Cardinality MatchCardinality { get; }

        public PatternRule(
            Regex regex,
            Cardinality matchCardinality = default)
        {
            Regex = regex ?? throw new ArgumentNullException(nameof(regex));
            Pattern = regex.ToString();
            MatchCardinality = matchCardinality;
        }
    }
}
