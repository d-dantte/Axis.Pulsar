using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.IO
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
