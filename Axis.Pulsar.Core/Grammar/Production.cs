using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar
{
    /// <summary>
    /// 
    /// </summary>
    public interface IProduction
    {
        /// <summary>
        /// The symbol name pattern
        /// </summary>
        public static Regex SymbolPattern { get; } = new Regex(
            "^[a-zA-Z]([a-zA-Z0-9-])*\\z",
            RegexOptions.Compiled);

        /// <summary>
        /// Processes the production by applying the encapsulated recognition rule, then calling the symbols validator,
        /// if available, to validate the successfully recognized symbols. Exceptions from the validator MUST be captured
        /// in the return result, rather than allowed to propagate upwards
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="parentPath"></param>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryProcessRule(
            TokenReader reader,
            ProductionPath? parentPath,
            ILanguageContext context,
            out IResult<ICSTNode> result);

        /// <summary>
        /// 
        /// </summary>
        string Symbol { get; }

        /// <summary>
        /// 
        /// </summary>
        IRule Rule { get; }
    }
}
