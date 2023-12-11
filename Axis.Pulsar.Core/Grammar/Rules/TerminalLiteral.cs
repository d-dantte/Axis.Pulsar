using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class TerminalLiteral : IAtomicRule
    {
        public string Id { get; }

        public string Tokens { get; }

        public bool IsCaseInsensitive { get; }

        public TerminalLiteral(string id, string tokens, bool isCaseInsensitive)
        {
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            IsCaseInsensitive = isCaseInsensitive;
            Id = id.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid atomic rule {nameof(id)}: '{id}'"));
        }

        public TerminalLiteral(string id, string tokens)
        : this(id, tokens, true)
        {
        }

        public static TerminalLiteral Of(
            string id,
            string tokens,
            bool isCaseInensitive)
            => new(id, tokens, isCaseInensitive);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IRecognitionResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            var literalPath = productionPath.Next(Id);

            if (reader.TryGetTokens(Tokens.Length, true, out var tokens)
                && tokens.Equals(Tokens, !IsCaseInsensitive))
            {
                result = ICSTNode
                    .Of(literalPath.Name, tokens)
                    .ApplyTo(RecognitionResult.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(literalPath, position)
                    .ApplyTo(error => RecognitionResult.Of<ICSTNode>(error));
                return false;
            }
        }
    }
}
