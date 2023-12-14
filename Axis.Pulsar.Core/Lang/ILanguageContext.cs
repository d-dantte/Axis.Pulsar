using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Validation;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Lang
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
        NodeRecognitionResult Recognize(string inputTokens);
    }
}
