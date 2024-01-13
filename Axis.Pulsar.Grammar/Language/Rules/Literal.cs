using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Recognizers;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// Represents a rule that matches strings
    /// </summary>
    public struct Literal: IAtomicRule
    {
        public static readonly string LiteralSymbolName = "@Literal";

        /// <summary>
        /// The string value
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Indicates case-sensitivity of this literal rule
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <inheritdoc/>/>
        public string SymbolName => LiteralSymbolName;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isCaseSensitive"></param>
        public Literal(string value, bool isCaseSensitive = true)
        {
            IsCaseSensitive = isCaseSensitive;
            Value = value
                .ThrowIf(Extensions.IsNull, _ => new ArgumentNullException(nameof(value)))
                .ThrowIf(t => t.Length == 0, _ => new ArgumentException($"Invalid value length: {value.Length}"));
        }

        public override int GetHashCode() => HashCode.Combine(Value, IsCaseSensitive);

        public override bool Equals(object obj)
        {
            return obj is Literal other
                && EqualityComparer<string>.Default.Equals(other.Value, Value)
                && other.IsCaseSensitive.Equals(IsCaseSensitive);
        }

        public override string ToString() 
            => Value is not null
                ? $"{Delimiter()}{Value}{Delimiter()}"
                : null;

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new LiteralRecognizer(this, grammar);

        private char Delimiter() => IsCaseSensitive ? '"' : '\'';

        public static bool operator ==(Literal first, Literal second) => first.Equals(second);
        public static bool operator !=(Literal first, Literal second) => !(first == second);
    }
}
