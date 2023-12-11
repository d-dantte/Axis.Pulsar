using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Validation;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILanguageContext
    {
        /// <summary>
        /// 
        /// </summary>
        IGrammar Grammar { get; }

        /// <summary>
        /// 
        /// </summary>
        ImmutableDictionary<string, IProductionValidator> ProductionValidators { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputTokens"></param>
        /// <returns></returns>
        IRecognitionResult<ICSTNode> Recognize(string inputTokens);
    }
}
