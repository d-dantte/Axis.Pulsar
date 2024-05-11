using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Atomic
{
    /// <summary>
    /// 
    /// </summary>
    public class EOF : IAtomicRule
    {
        public string Id { get; }

        public EOF(string id)
        {
            Id = id
                .ThrowIfNull(
                    () => new ArgumentNullException(nameof(id)))
                .ThrowIfNot(
                    Production.SymbolPattern.IsMatch,
                    _ => new ArgumentException($"Invalid {nameof(id)} format: '{id}'"));
        }

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            var eofPath = symbolPath.Next(Id);

            if (!reader.TryGetToken(out _))
            {
                result = ISymbolNode
                    .Of(eofPath.Symbol, Tokens.EmptyAt(reader.Source, position))
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
