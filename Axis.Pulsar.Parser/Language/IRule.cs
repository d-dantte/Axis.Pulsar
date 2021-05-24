namespace Axis.Pulsar.Parser.Language
{
    public interface IRule
    {
        /// <summary>
        /// Indicates if this is the root rule for it's language
        /// </summary>
        bool IsRoot { get; }

        /// <summary>
        /// The name of this symbol
        /// </summary>
        string Name { get; }
    }
}
