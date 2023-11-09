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
    }

    public class Production: IProduction
    {
        private readonly IRule _rule;

        /// <summary>
        /// The symbol name for this production
        /// </summary>
        public string Symbol { get; }

        public Production(string symbol, IRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Symbol = symbol.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                new ArgumentException($"Invalid {nameof(symbol)}: {symbol}"));
        }

        public static Production Of(string symbol, IRule rule) => new(symbol, rule);

        public bool TryProcessRule(
            TokenReader reader,
            ProductionPath? parentPath,
            ILanguageContext context,
            out IResult<ICSTNode> result)
        {
            var productionPath = parentPath?.Next(Symbol) ?? ProductionPath.Of(Symbol);
            return _rule.TryRecognize(reader, productionPath, context, out result);
        }
    }
}
