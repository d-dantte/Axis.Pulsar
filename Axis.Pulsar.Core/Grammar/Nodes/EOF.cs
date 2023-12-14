using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Nodes
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
                Production.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid atomic rule {nameof(id)}: '{id}'"));
        }

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(symbolPath);

            var position = reader.Position;
            var eofPath = symbolPath.Next(Id);

            if (!reader.TryGetToken(out _))
            {
                result = ICSTNode
                    .Of(eofPath.Symbol, default(Tokens))
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(eofPath, position)
                    .ApplyTo(NodeRecognitionResult.Of);
                return false;
            }
        }
    }
}
