using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public class EOF : IAtomicRule
    {
        public static EOF Instance { get; } = new EOF();

        private EOF()
        {}

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(productionPath);

            var position = reader.Position;

            if (!reader.TryGetToken(out _))
            {
                result = ICSTNode
                    .Of(productionPath.Name, Tokens.Empty)
                    .ApplyTo(Result.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = UnrecognizedTokens
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);
                return false;
            }
        }
    }
}
