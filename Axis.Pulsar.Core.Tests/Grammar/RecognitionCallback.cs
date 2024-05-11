using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar
{
    internal delegate bool TryRecognizeNode(
        TokenReader reader,
        SymbolPath path,
        ILanguageContext context,
        out NodeRecognitionResult result);

    internal delegate bool TryRecognizeNodeSequence(
        TokenReader reader,
        SymbolPath path,
        ILanguageContext context,
        out SymbolAggregationResult result);
}
