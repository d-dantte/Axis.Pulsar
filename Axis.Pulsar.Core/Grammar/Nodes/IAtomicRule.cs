namespace Axis.Pulsar.Core.Grammar.Nodes
{
    /// <summary>
    /// Rules that are not composed of other sub-rules. These typically represent terminal rules
    /// </summary>
    public interface IAtomicRule : INodeRule
    {
        string Id { get; }
    }
}
