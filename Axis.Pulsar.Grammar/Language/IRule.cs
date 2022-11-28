using Axis.Pulsar.Grammar.Recognizers;

namespace Axis.Pulsar.Grammar.Language
{
    /// <summary>
    /// Represents information needed to recognize a group of one or more tokens as a single entity known as a symbol.
    /// </summary>
    public interface IRule
    {
        /// <summary>
        /// Gets the <see cref="IRecognizer"/> for this rule
        /// </summary>
        /// <param name="grammar">The grammar to which the rule belongs</param>
        IRecognizer ToRecognizer(Grammar grammar);

        /// <summary>
        /// The name given to the recognized symbol in the Concrete Syntax Tree node. (<see  cref="CST.CSTNode"/>)
        /// </summary>
        string SymbolName { get; }
    }
}
