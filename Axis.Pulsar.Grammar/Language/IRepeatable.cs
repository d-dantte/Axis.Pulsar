namespace Axis.Pulsar.Grammar.Language
{
    /// <summary>
    /// Represents an entity (rule) that can be repeated, based on it's <see cref="Cardinality"/>.
    /// </summary>
    public interface IRepeatable
    {
        /// <summary>
        /// Symbol Name given to the containing node of repeated symbols
        /// </summary>
        public static string SymbolName => "@Repeatable";

        /// <summary>
        /// The cardinality of the rule
        /// </summary>
        Cardinality Cardinality { get; }
    }
}
