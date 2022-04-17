using Axis.Pulsar.Parser.Utils;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    internal static class Helper
    {
        /// <summary>
        /// Check if, given the completed repetitions, it is legal to repeat the parse cycle based on the cardinality
        /// </summary>
        /// <param name="completedRepetitions"></param>
        /// <returns>Value indicating if a repetition is legal</returns>
        public static bool CanRepeat(this Cardinality cardinality, int completedRepetitions)
        {
            if (completedRepetitions < cardinality.MinOccurence)
                return true;

            else if (cardinality.MaxOccurence == null)
                return true;

            else if (completedRepetitions < cardinality.MaxOccurence)
                return true;

            else return false;
        }

        public static char ToChar(this Grammar.SymbolGroup.GroupingMode mode) => mode switch
        {
            Grammar.SymbolGroup.GroupingMode.Set => '#',
            Grammar.SymbolGroup.GroupingMode.Choice => '?',
            Grammar.SymbolGroup.GroupingMode.Sequence => '+',
            _ => throw new System.ArgumentException($"Invalid grouping mode: {mode}")
        };

        public static string AsString(
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
