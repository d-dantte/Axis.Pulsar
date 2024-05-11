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
    /// Represents an aggregate rule - a rule made up of the aggregation of other rules, based on some precedent.
    /// <para/>
    /// The NonTerminal implement the concept of a recognition threshold. This represents the minimum number of
    /// INITIAL sub-rules that must be parsed for the rule to be established. A typical example of this is with delimited
    /// tokens, e.g a CLASSIC c# string literal. The recognition threshold will be 1, because if the initial double-quote
    /// is recognized, we are fairly certain we are meant to recognize a string literal.
    /// <para/>
    /// NOTE: rename this to CompositeRule
    /// </summary>
    public class CompositeRule// : ICompositeRule
    {
        public IAggregationElementRule Element { get; }

        public uint? RecognitionThreshold { get; }

        public CompositeRule(uint? recognitionThreshold, IAggregationElementRule ruleGroup)
        {
            Element = ruleGroup ?? throw new ArgumentNullException(nameof(ruleGroup));
            RecognitionThreshold = recognitionThreshold;
        }

        public static CompositeRule Of(
            uint? recognitionThreshold,
            IAggregationElementRule element)
            => new(recognitionThreshold, element);

        public static CompositeRule Of(
            IAggregationElementRule element)
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
                data => ISymbolNode
                    .Of(symbolPath.Symbol, data.AllNodes())
                    .ApplyTo(NodeRecognitionResult.Of),

                gre => gre.Cause switch
                {
                    PartialRecognitionError pre => NodeRecognitionResult.Of(pre),
                    _ => RecognitionThreshold switch
                    {
                        null => NodeRecognitionResult.Of(gre.Cause.As<FailedRecognitionError>()),
                        _ => gre.ElementCount < RecognitionThreshold.Value
                            ? NodeRecognitionResult.Of(gre.Cause.As<FailedRecognitionError>())
                            : PartialRecognitionError
                                .Of(symbolPath,
                                    position,
                                    gre.Cause.As<FailedRecognitionError>().TokenSegment.Count)
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
