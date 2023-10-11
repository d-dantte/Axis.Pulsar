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
        /// <param name="inputStream"></param>
        /// <returns></returns>
        Grammar.IGrammar ImportGrammar(string inputStream);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customRuleName"></param>
        /// <param name="customRuleFactory"></param>
        /// <returns></returns>
        ILanguageImporter RegisterCustomRuleFactory(
            string customRuleName,
            Func<IDictionary<string, string>, ICustomRule> customRuleFactory);
    }
}
