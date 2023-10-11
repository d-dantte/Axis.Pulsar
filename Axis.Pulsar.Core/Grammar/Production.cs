using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar
{
    public class Production
    {
        /// <summary>
        /// The symbol name pattern
        /// </summary>
        public static Regex SymbolPattern { get; } = new Regex(
            "^[a-zA-Z]([a-zA-Z0-9-])*\\z",
            RegexOptions.Compiled);


        private IRule _rule;

        /// <summary>
        /// The symbol name for this production
        /// </summary>
        public string Symbol { get; }

        public Production(string symbol, IRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Symbol = symbol.ThrowIfNot(
                SymbolPattern.IsMatch,
                new ArgumentException($"Invalid {nameof(symbol)}: {symbol}"));
        }

        public static Production Of(string symbol, IRule rule) => new(symbol, rule);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath? parentPath,
            out IResult<ICSTNode> result)
        {
            var productionPath = parentPath?.Next(Symbol) ?? ProductionPath.Of(Symbol);
            return _rule.TryRecognize(reader, productionPath, out result);
        }
    }
}
