namespace Axis.Pulsar.Grammar.Language
{
    /// <summary>
    /// An aggregate rule is one composed of one or more sub-rules
    /// </summary>
    public interface IAggregateRule : IRule
    {
        /// <summary>
        /// The rules that make up this expression
        /// </summary>
        IRule[] Rules { get; }
    }
}
