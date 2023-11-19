namespace Axis.Pulsar.Core.Grammar.Groups
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRefType"></typeparam>
    public interface IRuleRef<TRefType>: IGroupElement
    {
        TRefType Ref { get; }
    }
}
