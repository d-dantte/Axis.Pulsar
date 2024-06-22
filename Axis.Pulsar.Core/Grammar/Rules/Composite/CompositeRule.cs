using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules.Composite
{
    /// <summary>
    /// Represents the seed of all aggregate-type rules.
    /// <para/>
    /// This rule implement the concept of a recognition threshold. This represents the minimum number of
    /// INITIAL sub-rules that must be parsed for the rule to be established. A typical example of this is with delimited
    /// tokens, e.g a CLASSIC c# string literal. The recognition threshold will be 1, because if the initial double-quote
    /// is recognized, we are fairly certain we are meant to recognize a string literal.
    /// <para/>
    /// NOTE: rename this to CompositeRule
    /// </summary>
    public class CompositeRule : Production.IRule
    {
        public IAggregationElement Element { get; }

        public uint? RecognitionThreshold { get; }

        public CompositeRule(uint? recognitionThreshold, IAggregationElement element)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            RecognitionThreshold = recognitionThreshold;
        }

        public static CompositeRule Of(
            uint? recognitionThreshold,
            IAggregationElement element)
            => new(recognitionThreshold, element);

        public static CompositeRule Of(
            IAggregationElement element)
            => new(null, element);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(nameof(reader));

            var position = reader.Position;
            var elementResult = Element.Recognize(reader, symbolPath, context);

            result = elementResult.MapMatch(

                // data
                data => ISymbolNode
                    .Of(symbolPath.Symbol, data.FlattenAggregates())
                    .ApplyTo(NodeRecognitionResult.Of),

                are => are.Cause switch
                {
                    PartialRecognitionError pre => NodeRecognitionResult.Of(pre),
                    _ => RecognitionThreshold switch
                    {
                        null => NodeRecognitionResult.Of(are.Cause.As<FailedRecognitionError>()),
                        _ => are.RequiredNodeCount < RecognitionThreshold.Value
                            ? NodeRecognitionResult.Of(are.Cause.As<FailedRecognitionError>())
                            : PartialRecognitionError
                                .Of(symbolPath,
                                    position,
                                    are.Cause.As<FailedRecognitionError>().TokenSegment.Count)
                                .ApplyTo(NodeRecognitionResult.Of)
                    }
                });

            if (result.Is(out ISymbolNode _))
                return true;

            reader.Reset(position);
            return false;
        }
    }
}
