using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Lang;

namespace Axis.Pulsar.Core.Lang
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILanguageImporter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputTokens"></param>
        /// <returns></returns>
        ILanguageContext ImportLanguage(string inputTokens);
    }
}
