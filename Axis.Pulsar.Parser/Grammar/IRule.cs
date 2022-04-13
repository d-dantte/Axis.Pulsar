using Axis.Pulsar.Parser.Utils;

namespace Axis.Pulsar.Parser.Grammar
{

    public interface IRule
    {
    }

    /// <summary>
    /// Production rule
    /// 
    /// NOTE: flirting with the idea of adding a Func delegate that represents semantic validation of the symbol that a rule parses. 
    /// This will be given the symbol, which should have access to ancestors; the validation logic can then use the entire syntax tree to do
    /// some semantic validations on the symbol. This validation logic can be injected while the rules are built.
    /// </summary>
    public interface IRule<out T>: IRule
    {
        /// <summary>
        /// The value of the Rule
        /// </summary>
        T Value { get; }
    }

    /// <summary>
    /// Represents rules that produce terminals
    /// </summary>
    public interface ITerminal: IRule<string>
    {
    }

    /// <summary>
    /// Represents rules that produce non-terminals
    /// </summary>
    public interface INonTerminal : IRule<ISymbolExpression>
    {
    }
}
