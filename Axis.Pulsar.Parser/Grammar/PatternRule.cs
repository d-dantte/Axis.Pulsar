using Axis.Pulsar.Parser.Utils;
using System;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// 
    /// </summary>
    public class PatternRule : Rule
    {
        /// <summary>
        /// 
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// 
        /// </summary>
        public Regex Regex { get; }

        /// <summary>
        /// 
        /// </summary>
        public Cardinality MatchCardinality { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="matchCardinality"></param>
        public PatternRule(
            Regex regex,
            Cardinality matchCardinality = default)
            : base(Cardinality.OccursOnlyOnce())
        {
            Regex = regex ?? throw new ArgumentNullException(nameof(regex));
            Pattern = regex.ToString();
            MatchCardinality = matchCardinality;
        }
    }
}
