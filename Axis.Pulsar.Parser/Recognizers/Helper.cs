using Axis.Pulsar.Parser.Utils;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    internal static class Helper
    {
        public static char ToChar(this Grammar.SymbolGroup.GroupingMode mode) => mode switch
        {
            Grammar.SymbolGroup.GroupingMode.Set => '#',
            Grammar.SymbolGroup.GroupingMode.Choice => '?',
            Grammar.SymbolGroup.GroupingMode.Sequence => '+',
            _ => throw new System.ArgumentException($"Invalid grouping mode: {mode}")
        };

        public static string AsString(this
            Grammar.SymbolGroup.GroupingMode mode,
            Cardinality cardinality,
            params IRecognizer[] recognizers)
        {
            return recognizers
                .Select(recognizer => recognizer.ToString())
                .ToArray()
                .Map(strings => string.Join(' ', strings))
                .Map(@string => $"{mode.ToChar()}[{@string}]")
                .Map(@string => $"{@string}{cardinality}");
        }
    }
}
