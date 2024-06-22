using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules.Atomic
{
    public class TerminalLiteral : IAtomicRule
    {
        public string Id { get; }

        public string Tokens { get; }

        public bool IsCaseSensitive { get; }

        public TerminalLiteral(string id, string tokens, bool isCaseSensitive)
        {
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            IsCaseSensitive = isCaseSensitive;
            Id = id.ThrowIfNot(
                Production.SymbolPattern.IsMatch,
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
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            var literalPath = symbolPath.Next(Id);

            if (reader.TryGetTokens(Tokens.Length, true, out var tokens)
                && tokens.Equals(Tokens, !IsCaseSensitive))
            {
                result = ISymbolNode
                    .Of(literalPath.Symbol, tokens)
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(literalPath, position)
                    .ApplyTo(NodeRecognitionResult.Of);
                return false;
            }
        }
    }
}
