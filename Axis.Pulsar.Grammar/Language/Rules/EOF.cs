using Axis.Pulsar.Grammar.Recognizers;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// A special rule that represents the end of the input stream.
    /// </summary>
    public struct EOF : IAtomicRule
    {
        public static readonly string EOFSymbolName = nameof(EOF);

        public Cardinality Cardinality => Cardinality.OccursOnlyOnce();

        /// <inheritdoc/>/>
        public string SymbolName => EOFSymbolName;

        public override string ToString() => SymbolName;

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new EOFRecognizer(this, grammar);

        public override int GetHashCode() => 0;

        public override bool Equals(object obj) => obj is EOF;

        public static bool operator ==(EOF first, EOF second) => first.Equals(second);
        public static bool operator !=(EOF first, EOF second) => !(first == second);
    }
}
