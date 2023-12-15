using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar.Nodes
{
    /// <summary>
    /// 
    /// </summary>
    public class Production: INodeRule
    {
        /// <summary>
        /// The symbol name pattern
        /// </summary>
        public static Regex SymbolPattern { get; } = new(
            "^[a-zA-Z]([a-zA-Z0-9-])*\\z",
            RegexOptions.Compiled);

        /// <summary>
        /// 
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// 
        /// </summary>
        public INodeRule Rule { get; }

        public Production(string symbol, INodeRule rule)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Symbol = symbol.ThrowIfNot(
                SymbolPattern.IsMatch,
                s => new ArgumentException($"Invalid {nameof(symbol)}: {s}"));
        }

        public static Production Of(string symbol, INodeRule rule) => new(symbol, rule);

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
        public bool TryRecognize(
            TokenReader reader,
            SymbolPath parentPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            var symbolPath = parentPath.Next(Symbol);

            if (!Rule.TryRecognize(reader, symbolPath, context, out result))
                return false;

            if (!context.ProductionValidators.TryGetValue(Symbol, out var validator))
                return true;

            result = result.WithMatch(
                node => validator.Validate(symbolPath, context, node),
                _ => { },
                _ => { });

            return result.Is(out ICSTNode _);
        }
    }
}
