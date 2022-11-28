using Axis.Pulsar.Parser.Utils;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    internal static class Helper
    {
        public static string AsString(this
            Grammar.SymbolGroup.GroupingMode mode,
            Cardinality cardinality,
            params IRecognizer[] recognizers)
        {
            return recognizers
                .Select(recognizer => recognizer.ToString())
                .ToArray()
                .Map(strings => string.Join(' ', strings))
                .Map(@string => $"[{@string}]{cardinality}");
        }
    }
}
