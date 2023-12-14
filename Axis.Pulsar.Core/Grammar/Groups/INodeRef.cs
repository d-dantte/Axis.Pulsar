namespace Axis.Pulsar.Core.Grammar.Groups
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRefType"></typeparam>
    public interface INodeRef<TRefType>: IGroupElement
    {
        TRefType Ref { get; }
    }
}
