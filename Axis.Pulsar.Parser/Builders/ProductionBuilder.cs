using Axis.Pulsar.Parser.Grammar;
using System;

namespace Axis.Pulsar.Parser.Builders
{
    /// <summary>
    /// Production builder instance
    /// </summary>
    public class ProductionBuilder: IBuilder<Production>
    {
        private IRule _rule;

        /// <summary>
        /// Name given to the underlying production's symbol
        /// </summary>
        public string SymbolName { get; }

        public ProductionBuilder(string symbolName)
        {
            SymbolName = symbolName.ThrowIf(
                string.IsNullOrEmpty,
                new ArgumentException($"Invalid symbol name"));
        }

        /// <summary>
        /// Replaces the currently set rule.
        /// </summary>
        /// <param name="ruleBuilderAction">An action that customizes the given <see cref="RuleBuilder"/>instance</param>
        public ProductionBuilder HavingRule(Action<RuleBuilder> ruleBuilderAction)
        {
            _rule = new RuleBuilder()
                .Use(ruleBuilderAction.Invoke)
                .Build();
            return this;
        }

        /// <inheritdoc/>
        public Production Build() => new Production(SymbolName, _rule);
    }
}
