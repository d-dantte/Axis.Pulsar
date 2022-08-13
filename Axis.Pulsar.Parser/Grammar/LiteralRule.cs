using System;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// Terminal rule representing literal string values. Literals MUST recognize at least 1 token/character
    /// <para>Why is this not a struct?</para>
    /// </summary>
    public class LiteralRule : ITerminal
    {
        public string Value { get; }

        /// <summary>
        /// Indicates case-sensitivity of this literal rule
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold => Value.Length;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isCaseSensitive"></param>
        public LiteralRule(string value, bool isCaseSensitive = true)
        {
            IsCaseSensitive = isCaseSensitive;
            Value = value
                .ThrowIf(Extensions.IsNull, new ArgumentNullException(nameof(value)))
                .ThrowIf(t => t.Length == 0, new ArgumentException($"Invalid value length: {value.Length}"));
        }
    }
}
