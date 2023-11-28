using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Validation
{
    /// <summary>
    /// Production validators may be designated to different productions, via the <see cref="ILanguageContext"/>'s Validator map.
    /// </summary>
    public interface IProductionValidator
    {
        /// <summary>
        /// Validates the SUCCESSFULLY recognized tokens of the given production symbol, passed in as
        /// a <see cref="ICSTNode"/>. The idea is, after a production is SUCCESSFULLY processed,
        /// the <see cref="ICSTNode"/> instance is passed into the designated validator.
        /// <para/>
        /// Validation fails if any exception is thrown from this method, or passes if the method
        /// returns successfully.
        /// <para/>
        /// </summary>
        /// <param name="productionPath"></param>
        /// <param name="context"></param>
        /// <param name="recogniedNode"></param>
        /// <returns></returns>
        void Validate(
            ProductionPath productionPath,
            ILanguageContext context,
            ICSTNode recogniedNode);
    }
}
