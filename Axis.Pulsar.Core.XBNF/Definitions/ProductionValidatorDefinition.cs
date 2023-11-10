using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Lang;

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
            Symbol = symbol.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                new FormatException($"Invalid symbol format: '{symbol}'"));
        }

        public ProductionValidatorDefinition Of(
            string symbol,
            IProductionValidator validator)
            => new(symbol, validator);
    }
}
