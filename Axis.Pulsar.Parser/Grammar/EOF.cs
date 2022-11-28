using Axis.Pulsar.Parser.Utils;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// A special rule that recognizes the end of a stream.
    /// </summary>
    public record struct EOF : ISymbolExpression
    {
        public Cardinality Cardinality => Cardinality.OccursOnlyOnce();

        public override string ToString() => "EOF";
    }
}
