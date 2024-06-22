using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar
{
    internal delegate bool NodeRecognition(
        TokenReader reader,
        SymbolPath path,
        ILanguageContext context,
        out NodeRecognitionResult result);

    internal delegate bool AggregateRecognition(
        TokenReader reader,
        SymbolPath path,
        ILanguageContext context,
        out NodeAggregationResult result);
}
