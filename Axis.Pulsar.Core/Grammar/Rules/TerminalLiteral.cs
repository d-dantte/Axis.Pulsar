using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class TerminalLiteral : IAtomicRule
    {
        public string Tokens { get; }

        public TerminalLiteral(string tokens)
        {
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;

            if (reader.TryGetTokens(Tokens.Length, true, out var tokens)
                && tokens.Equals(Tokens))
            {
                result = ICSTNode
                    .Of(tokens)
                    .ApplyTo(Result.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = Errors.UnrecognizedTokens
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);
                return false;
            }
        }
    }
}
