using Axis.Pulsar.Core.Grammar;

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
        Grammar.IGrammar ImportGrammar(string inputTokens);
    }
}
