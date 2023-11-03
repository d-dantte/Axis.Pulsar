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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customRuleName"></param>
        /// <param name="customRuleFactory"></param>
        /// <returns></returns>
        ILanguageImporter RegisterCustomRuleFactory(
            string customRuleName,
            Func<IDictionary<string, string>, IAtomicRule> customRuleFactory);
    }
}
