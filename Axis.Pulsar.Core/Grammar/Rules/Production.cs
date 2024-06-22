using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class Production : IRecognizer<NodeRecognitionResult>
    {
        /// <summary>
        /// The symbol name pattern
        /// </summary>
        public static Regex SymbolPattern { get; } = new(
            "^[a-zA-Z]([a-zA-Z0-9-])*\\z",
            RegexOptions.Compiled);

        /// <summary>
        /// The production symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// The production rule
        /// </summary>
        public IRule Rule { get; }

        public Production(string symbol, IRule rule)
        {
            // restrain this to only AtomicRule and CompositeRule?
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Symbol = symbol.ThrowIfNot(
                SymbolPattern.IsMatch,
                s => new InvalidOperationException($"Invalid {nameof(symbol)}: {s}"));
        }

        public static Production Of(string symbol, IRule rule) => new(symbol, rule);

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
            var position = reader.Position;

            if (!Rule.TryRecognize(reader, symbolPath, context, out result))
                return false;

            if (!context.ProductionValidators.TryGetValue(Symbol, out var validator))
                return true;

            result = result.Map<ISymbolNode, NodeRecognitionResult>(
                node => validator.Validate(symbolPath, context, node) switch
            {
                Validation.Status.Valid => NodeRecognitionResult.Of(node),

                Validation.Status.Invalid => FailedRecognitionError
                    .Of(symbolPath, position)
                    .ApplyTo(NodeRecognitionResult.Of),

                _ => PartialRecognitionError
                    .Of(symbolPath, node.Tokens.Segment)
                    .ApplyTo(NodeRecognitionResult.Of)
            });

            return result.Is(out ISymbolNode _);
        }


        /// <summary>
        /// Production rule
        /// </summary>
        public interface IRule : IRecognizer<NodeRecognitionResult>
        { }
    }
}
