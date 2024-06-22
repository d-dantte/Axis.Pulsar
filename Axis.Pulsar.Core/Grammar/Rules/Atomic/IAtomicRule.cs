namespace Axis.Pulsar.Core.Grammar.Rules.Atomic
{
    /// <summary>
    /// Rules that are not composed of other sub-rules. These typically represent terminal rules
    /// </summary>
    public interface IAtomicRule : Production.IRule
    {
        /// <summary>
        /// The Id/symbol for this rule
        /// </summary>
        string Id { get; }
    }
}
