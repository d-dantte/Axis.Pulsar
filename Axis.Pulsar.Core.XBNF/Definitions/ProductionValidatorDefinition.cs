using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar.Validation;

namespace Axis.Pulsar.Core.XBNF.Definitions
{
    public class ProductionValidatorDefinition
    {
        /// <summary>
        /// 
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// 
        /// </summary>
        public IProductionValidator Validator { get; }

        public ProductionValidatorDefinition(
            string symbol,
            IProductionValidator validator)
        {
            Validator = validator ?? throw new ArgumentNullException(nameof(validator));
            Symbol = symbol
                .ThrowIfNull(() => new ArgumentNullException(nameof(symbol)))
                .ThrowIfNot(
                    Production.SymbolPattern.IsMatch,
                    _ => new FormatException($"Invalid symbol format: '{symbol}'"));
        }

        public static ProductionValidatorDefinition Of(
            string symbol,
            IProductionValidator validator)
            => new(symbol, validator);
    }
}
