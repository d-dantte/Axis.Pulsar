using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar
{
    public interface IProduction
    {
        /// <summary>
        /// The symbol name pattern
        /// </summary>
        public static Regex SymbolPattern { get; } = new Regex(
            "^[a-zA-Z]([a-zA-Z0-9-])*\\z",
            RegexOptions.Compiled);

        bool TryProcessRule(
            TokenReader reader,
            ProductionPath? parentPath,
            ILanguageContext context,
            out IResult<ICSTNode> result);

        string Symbol { get; }

        IRule Rule { get; }
    }
}
