using System;
using Axis.Pulsar.Parser.Utils;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// 
    /// </summary>
    public class LiteralRule : Rule
    {
        /// <summary>
        /// 
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isCaseSensitive"></param>
        public LiteralRule(string value, bool isCaseSensitive = true)
            : base(Cardinality.OccursOnlyOnce())
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            IsCaseSensitive = isCaseSensitive;
        }
    }
}
