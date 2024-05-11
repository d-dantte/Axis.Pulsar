using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Lang;

namespace Axis.Pulsar.Core.Grammar.Validation
{
    /// <summary>
    /// Production validators may be designated to different productions, via the <see cref="ILanguageContext"/>'s Validator map.
    /// </summary>
    public interface IProductionValidator
    {
        /// <summary>
        /// Validates the SUCCESSFULLY recognized tokens of the given production symbol, passed in as
        /// a <see cref="ISymbolNode"/>. The idea is, after a production is SUCCESSFULLY processed,
        /// the <see cref="ISymbolNode"/> instance is passed into the designated validator.
        /// <para/>
        /// Validation fails if any exception is thrown from this method, or passes otherwise.
        /// <para/>
        /// </summary>
        /// <param name="symbolPath"></param>
        /// <param name="context"></param>
        /// <param name="recogniedNode"></param>
        void Validate(
            SymbolPath symbolPath,
            ILanguageContext context,
            ISymbolNode recogniedNode);
    }
}
