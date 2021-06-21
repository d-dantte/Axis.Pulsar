namespace Axis.Pulsar.Parser.Language
{
    /// <summary>
    /// Parse rule
    /// 
    /// NODE: flirting with the idea of adding a Func delegate that represents semantic validation of the symbol that a rule parses. 
    /// This will be given the symbol, which should have access to ancestors; the validation logic can then use the entire syntax tree to do
    /// some semantic validations on the symbol. This validation logic can be injected while the rules are built.
    /// </summary>
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
