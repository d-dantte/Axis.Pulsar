using System;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// Terminal rule representing literal string values
    /// </summary>
    public class LiteralRule : ITerminal
    {
        public string Value { get; }

        /// <summary>
        /// Indicates case-sensitivity of this literal rule
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isCaseSensitive"></param>
        public LiteralRule(string value, bool isCaseSensitive = true)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            IsCaseSensitive = isCaseSensitive;
        }
    }
}
