namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRefType"></typeparam>
    public interface INodeRef<TRefType> : IAggregationElementRule
    {
        TRefType Ref { get; }
    }
}
