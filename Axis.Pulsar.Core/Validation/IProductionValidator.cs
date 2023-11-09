using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.Lang
{
    /// <summary>
    /// Production validators may be designated to different productions, via the <see cref="ILanguageContext"/>'s Validator map.
    /// </summary>
    public interface IProductionValidator
    {
        /// <summary>
        /// Validates the SUCCESSFULLY recognized tokens of the given production symbol, passed in as
        /// a <see cref="IResult{TData}"/>. The idea is, after a production is SUCCESSFULLY recognized,
        /// the <see cref="IResult{TData}"/> instance containing the <see cref="ICSTNode"/> payload is
        /// passed into the designated validator.
        /// <para/>
        /// 
        /// If the <paramref name="nodeResult"/> is erroneous, nothing happens. Validation is only executed
        /// on a successful <paramref name="nodeResult"/> instance.
        /// </summary>
        /// <param name="productionPath"></param>
        /// <param name="context"></param>
        /// <param name="nodeResult"></param>
        /// <returns></returns>
        bool TryValidate(
            ProductionPath productionPath,
            ILanguageContext context,
            ref IResult<ICSTNode> nodeResult);
    }
}
