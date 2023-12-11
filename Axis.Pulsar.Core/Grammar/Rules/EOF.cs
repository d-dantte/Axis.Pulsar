using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public class EOF : IAtomicRule
    {
        public string Id { get; }

        public EOF(string id)
        {
            Id = id.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid atomic rule {nameof(id)}: '{id}'"));
        }

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IRecognitionResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(productionPath);

            var position = reader.Position;
            var eofPath = productionPath.Next(Id);

            if (!reader.TryGetToken(out _))
            {
                result = ICSTNode
                    .Of(eofPath.Name, default(Tokens))
                    .ApplyTo(RecognitionResult.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(eofPath, position)
                    .ApplyTo(error => RecognitionResult.Of<ICSTNode>(error));
                return false;
            }
        }
    }
}
