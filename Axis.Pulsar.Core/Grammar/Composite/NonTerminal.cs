using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Composite
{
    /// <summary>
    /// 
    /// </summary>
    public class NonTerminal : ICompositeRule
    {
        public IGroupRule Element { get; }

        public uint? RecognitionThreshold { get; }

        public NonTerminal(uint? recognitionThreshold, IGroupRule ruleGroup)
        {
            Element = ruleGroup ?? throw new ArgumentNullException(nameof(ruleGroup));
            RecognitionThreshold = recognitionThreshold;
        }

        public static NonTerminal Of(
            uint? recognitionThreshold,
            IGroupRule element)
            => new(recognitionThreshold, element);

        public static NonTerminal Of(
            IGroupRule element)
            => new(null, element);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(nameof(reader));
            ArgumentNullException.ThrowIfNull(nameof(symbolPath));

            var position = reader.Position;
            _ = !Element.Cardinality.TryRepeat(
                reader,
                symbolPath,
                context,
                Element,
                out var groupResult);

            result = groupResult.MapMatch(

                // data
                data => ICSTNode
                    .Of(symbolPath.Symbol, data)
                    .ApplyTo(NodeRecognitionResult.Of),

                // group recognition error
                gre => (gre.Cause, RecognitionThreshold) switch
                {
                    (PartialRecognitionError pre, _) => NodeRecognitionResult.Of(pre),
                    (FailedRecognitionError fre, null) => NodeRecognitionResult.Of(fre),
                    (FailedRecognitionError fre, _) => gre.ElementCount < RecognitionThreshold
                        ? NodeRecognitionResult.Of(fre)
                        : PartialRecognitionError
                            .Of(symbolPath,
                                position,
                                fre.TokenSegment.EndOffset - position - 1)
                            .ApplyTo(NodeRecognitionResult.Of),
                    _ => throw new InvalidOperationException(
                        $"Invalid group result cause: {gre.Cause?.GetType()}")
                });

            if (result.Is(out ICSTNode _))
                return true;

            reader.Reset(position);
            return false;
        }
    }
}
