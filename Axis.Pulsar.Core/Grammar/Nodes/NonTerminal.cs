using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Nodes
{
    /// <summary>
    /// 
    /// </summary>
    public class NonTerminal : ICompositeRule
    {
        public IGroupElement Element { get; }

        public uint RecognitionThreshold { get; }

        public NonTerminal(uint recognitionThreshold, IGroupElement ruleGroup)
        {
            Element = ruleGroup ?? throw new ArgumentNullException(nameof(ruleGroup));
            RecognitionThreshold = recognitionThreshold;
        }

        public static NonTerminal Of(
            uint recognitionThreshold,
            IGroupElement element)
            => new(recognitionThreshold, element);

        public static NonTerminal Of(
            IGroupElement element)
            => new(1, element);

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
                gre => gre.Cause switch
                {
                    PartialRecognitionError pre => NodeRecognitionResult.Of(pre),
                    FailedRecognitionError fre => gre.ElementCount < RecognitionThreshold
                        ? NodeRecognitionResult.Of(fre)
                        : PartialRecognitionError
                            .Of(symbolPath,
                                position,
                                fre.TokenSegment.EndOffset - position  - 1)
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
