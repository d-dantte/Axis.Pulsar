namespace Axis.Pulsar.Core.Grammar.Atomic
{
    /// <summary>
    /// Rules that are not composed of other sub-rules. These typically represent terminal rules
    /// </summary>
    public interface IAtomicRule : IRule
    {
        string Id { get; }
    }
}
