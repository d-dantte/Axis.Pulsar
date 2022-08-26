using Axis.Pulsar.Parser.Utils;

namespace Axis.Pulsar.Parser.Grammar
{
    public interface IRule
    {
        /// <summary>
        /// <para>
        /// Represents the least number of elements (characters or symnols, depending on what the rule represents) that 
        /// need to match for this Rule to be uniquely identified. In other words, if the list number is reached, and 
        /// recognition still fails, other available options are skipped.
        /// </para>
        /// <list type="number">
        /// <item>Despite being an integer value, values &lt;= 0 are considered invalid.</item>
        /// <item>If the value is null, then no threshold is assumed, and recognition failures are not recorded.</item>
        /// </list>
        /// </summary>
        int? RecognitionThreshold { get; }
    }

    /// <summary>
    /// Production rule
    /// <para>
    /// NOTE: flirting with the idea of adding a Func delegate that represents semantic validation of the symbol that a rule parses. 
    /// This will be given the symbol, which should have access to ancestors; the validation logic can then use the entire syntax tree to do
    /// some semantic validations on the symbol. This validation logic can be injected while the rules are built.
    /// </para>
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
    public interface ITerminal<T>: IRule<T>
    {
    }

    /// <summary>
    /// Represents rules that produce non-terminals
    /// </summary>
    public interface INonTerminal : IRule<ISymbolExpression>
    {
    }
}
