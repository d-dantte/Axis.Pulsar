namespace Axis.Pulsar.Grammar.Language
{
    /// <summary>
    /// A composite rule encapsulates only one rule
    /// </summary>
    public interface ICompositeRule: IRule
    {
        IRule Rule { get; }
    }
}
