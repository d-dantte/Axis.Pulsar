using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Errors
{
    /// <summary>
    /// 
    /// </summary>
    internal interface INodeError
    {
        /// <summary>
        /// 
        /// </summary>
        ProductionPath ProductionPath { get; }

        /// <summary>
        /// 
        /// </summary>
        int Position { get; }
    }


    /// <summary>
    /// Indicates that the first set of tokens read while trying to recognize a symbol did not
    /// match the symbols rules. E.g, trying to recognize an identifier, and a digit is the first
    /// character the reader returns.
    /// <para/>
    /// In other words, where applicable, the <c>RecognitionThreshold</c> was not reached
    /// </summary>
    public class UnrecognizedTokens : Exception, INodeError
    {
        public ProductionPath ProductionPath { get; }

        public int Position { get; }

        public UnrecognizedTokens(ProductionPath productionPath, int position)
        {
            ProductionPath = productionPath ?? throw new ArgumentNullException(nameof(productionPath));
            Position = position.ThrowIf(
                i => i < 0,
                new ArgumentOutOfRangeException($"Invalid {nameof(position)}: {position}"));
        }

        public static UnrecognizedTokens Of(
            ProductionPath productionPath,
            int position)
            => new(productionPath, position);
    }

    /// <summary>
    /// Indicates that enough characters have been recognized to anticipate the correct symbol, but
    /// an unrecognized set of characters were read subsequently. This usually happens while
    /// recognizing/parsing non-terminals.
    /// <para/>
    /// E.g: trying to recognize a c# method signature, if the modifiers, return type, and name have
    /// all been recognized, but a '{' is read instead of a '(' while trying to recognize the parameter
    /// list, then a partial recognition has occured.
    /// </summary>
    public class PartiallyRecognizedTokens : Exception, INodeError
    {
        private readonly Lazy<Tokens> _tokens;

        public Tokens PartialTokens => _tokens.Value;

        public int Position { get; }

        public ProductionPath ProductionPath { get; }

        public PartiallyRecognizedTokens(
            ProductionPath productionPath,
            int position,
            Func<Tokens> tokenProvider)
        {
            ProductionPath = productionPath ?? throw new ArgumentNullException(nameof(productionPath));
            Position = position.ThrowIf(
                p => p < 0,
                new ArgumentException($"Invalid position: {position}"));

            _tokens = new Lazy<Tokens>(tokenProvider);
        }

        public PartiallyRecognizedTokens(
            ProductionPath productionPath,
            int position,
            Tokens partialTokens)
            : this(productionPath, position, () => partialTokens)
        {
        }

        public static PartiallyRecognizedTokens Of(
            ProductionPath productionPath,
            int position,
            Tokens partialTokens)
            => new(productionPath, position, partialTokens);

        public static PartiallyRecognizedTokens Of(
            ProductionPath productionPath,
            int position,
            Func<Tokens> tokenProvider)
            => new(productionPath, position, tokenProvider);
    }
}
