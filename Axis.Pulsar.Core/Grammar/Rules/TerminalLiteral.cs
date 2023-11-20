using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class TerminalLiteral : IAtomicRule
    {
        public string Tokens { get; }

        public bool IsCaseInsensitive { get; }

        public TerminalLiteral(string tokens, bool isCaseInsensitive)
        {
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            IsCaseInsensitive = isCaseInsensitive;
        }

        public TerminalLiteral(string tokens)
        : this(tokens, true)
        {
        }

        public static TerminalLiteral Of(string tokens, bool isCaseInensitive) => new(tokens, isCaseInensitive);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;

            if (reader.TryGetTokens(Tokens.Length, true, out var tokens)
                && tokens.Equals(Tokens, !IsCaseInsensitive))
            {
                result = ICSTNode
                    .Of(productionPath.Name, tokens)
                    .ApplyTo(Result.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);
                return false;
            }
        }
    }
}
