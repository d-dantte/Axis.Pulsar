using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.CustomRules
{
    /// <summary>
    /// Recognizes a series of character ranges that represents valid or invalid characters
    /// </summary>
    public class CharacterRanges : ICustomRule
    {
        public static readonly string IncludeRangesArg = "include";
        public static readonly string ExcludeRangesArg = "exclude";

        /// <summary>
        /// List of char ranges to include
        /// </summary>
        public ImmutableArray<CharRange> IncludeList { get; }

        /// <summary>
        /// List of char ranges to exclude
        /// </summary>
        public ImmutableArray<CharRange> ExcludeList { get; }

        public ImmutableDictionary<string, string> Arguments { get; }

        private string IncludeRangesArgValue =>
            Arguments.TryGetValue(ICustomRule.DefaultArgumentKey, out string? value) ? value :
            Arguments.TryGetValue(IncludeRangesArg, out value) ? value :
            "";

        private string ExcludeRangesArgValue => 
            Arguments.TryGetValue(ExcludeRangesArg, out string? value) ? value : "";


        public CharacterRanges(IDictionary<string, string> arguments)
        {
            Arguments = arguments
                .ThrowIfNull(new ArgumentNullException(nameof(arguments)))
                .ThrowIfAny(
                    kvp => Production.SymbolPattern.IsMatch(kvp.Key),
                    kvp => new ArgumentException($"Invalid key: {kvp.Key}"))
                .ThrowIfAny(
                    kvp => string.IsNullOrEmpty(kvp.Value),
                    kvp => new ArgumentException($"Invalid arg: value missing for arg '{kvp.Key}'"))
                .ApplyTo(ImmutableDictionary.CreateRange);

            ValidateArgs();
            ExtractRanges();
        }

        public static CharacterRanges Of(IDictionary<string, string> arguments) => new(arguments);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(productionPath);

            var position = reader.Position;

            if (!reader.TryGetToken(out var token)
                || ExcludeList.Any(range => range.Contains(token[0]))
                || !IncludeList.Any(range => range.Contains(token[0])))
            {
                reader.Reset(position);
                result = UnrecognizedTokens
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);
                return false;
            }

            result = ICSTNode
                .Of(productionPath.Name, token)
                .ApplyTo(Result.Of);
            return true;
        }

        private void ValidateArgs()
        {
            var hasDefault = Arguments.ContainsKey(ICustomRule.DefaultArgumentKey);
            var hasInclude = Arguments.ContainsKey(IncludeRangesArg);
            var hasExclude = Arguments.ContainsKey(ExcludeRangesArg);

            if (hasDefault && hasInclude)
                throw new InvalidOperationException(
                    $"Invalid args: {ICustomRule.DefaultArgumentKey} and {IncludeRangesArg} cannot both be present");

            if (!hasDefault && !hasInclude && !hasExclude)
                throw new InvalidOperationException(
                    $"Invalid args: at least one arg must be present");
        }

        private (ImmutableArray<CharRange> includeList, ImmutableArray<CharRange> excludeList) ExtractRanges()
        {
            var includeList = ParseRanges(IncludeRangesArgValue);
            var excludeList = ParseRanges(ExcludeRangesArgValue);

            return (
                includeList.ToImmutableArray(),
                excludeList.ToImmutableArray());
        }

        private static List<CharRange> ParseRanges(string rangeArgValue)
        {
            return rangeArgValue
                .Split(',')
                .Select(range => range
                    .ThrowIf(
                        string.IsNullOrWhiteSpace,
                        new FormatException($"Invalid range list: separate each range by a comma."))
                    .ApplyTo(CharRange.Parse))
                .OrderBy(range => range.LowerBound)
                .Aggregate(new List<CharRange>(), (list, range) =>
                {
                    if (list.Count == 0)
                        list.Add(range);

                    else
                    {
                        var last = list[^1];

                        if (last.TryMergeWith(range, out var merged))
                            list[^1] = merged;

                        else list.Add(range);
                    }

                    return list;
                });
        }

        #region nested types
        public readonly struct CharRange
        {
            public readonly char LowerBound { get; }

            public readonly char UpperBound { get; }

            public bool IsRange => LowerBound != UpperBound;

            public CharRange(char lowerBound, char upperBound)
            {
                LowerBound = lowerBound;
                UpperBound = upperBound;

                if (lowerBound > upperBound)
                    throw new ArgumentException(
                        $"{nameof(lowerBound)} character must be less thatn {nameof(upperBound)} character");
            }

            public static CharRange Of(char lowerBound, char upperBound) => new(lowerBound, upperBound);

            public static CharRange Of(char @char) => new(@char, @char);

            public static implicit operator CharRange(string input) => Parse(input);

            public static (CharRange lower, CharRange higher) SortLowerBounds(CharRange first, CharRange second)
            {
                if (first.LowerBound <= second.LowerBound)
                    return (first, second);

                return (second, first);
            }

            public static (CharRange lower, CharRange higher) SortUpperBounds(CharRange first, CharRange second)
            {
                if (first.UpperBound >= second.UpperBound)
                    return (first, second);

                return (second, first);
            }

            public static bool IsOverlapping(CharRange first, CharRange second)
            {
                var (lower, upper) = SortLowerBounds(first, second);

                return lower.UpperBound > upper.LowerBound;
            }

            public static CharRange Parse(string input)
            {
                return input
                    .ThrowIf(
                        string.IsNullOrWhiteSpace,
                        new ArgumentException($"Invalid input"))
                    .Split('-', StringSplitOptions.RemoveEmptyEntries)
                    .ApplyTo(bounds =>
                    {
                        if (bounds.Length == 1)
                            return CharRange.Of(ParseChar(bounds[0].Trim()));

                        if (bounds.Length == 2)
                            return CharRange.Of(
                                ParseChar(bounds[0].Trim()),
                                ParseChar(bounds[1].Trim()));

                        else throw new FormatException($"Invalid range format: {input}");
                    });
            }

            public static char ParseChar(string chars)
            {
                if (chars.Length > 2 && chars[0] == '\\'
                    && (char.ToLower(chars[1]) == 'u' || char.ToLower(chars[1]) == 'x'))
                    return (char)ushort.Parse(chars[2..], System.Globalization.NumberStyles.HexNumber);

                else if (chars.Length == 2 && chars[0] == '\\'
                    && (chars[1] == '\\' || chars[1] == '\''))
                    return chars[1];

                else if (chars.Length == 1)
                    return chars[0];

                else throw new ArgumentException($"Invalid character text: {chars}");
            }

            private static char Min(char first, char second) => first < second ? first : second;

            private static char Max(char first, char second) => first > second ? first : second;

            public bool IsOverlappingWith(CharRange range) => IsOverlapping(this, range);

            public CharRange MergeWith(CharRange range, bool mergeDisjointedRanges = false)
            {
                if (TryMergeWith(range, mergeDisjointedRanges, out var merged))
                    return merged;

                throw new InvalidOperationException($"Invalid merge: disjointed ranges.");
            }

            public bool TryMergeWith(CharRange range, bool mergeDisjointedRanges, out CharRange merged)
            {
                try
                {
                    if (IsOverlappingWith(range) || mergeDisjointedRanges)
                    {
                        merged = new CharRange(
                            Min(LowerBound, range.LowerBound),
                            Max(UpperBound, range.UpperBound));
                        return true;
                    }

                    merged = default;
                    return false;
                }
                catch
                {
                    merged = default;
                    return false;
                }
            }

            public bool TryMergeWith(
                CharRange range,
                out CharRange merged)
                => TryMergeWith(range, false, out merged);

            public bool Contains(char @char)
            {
                return LowerBound <= @char && UpperBound >= @char;
            }

            public override string ToString() => IsRange ? $"{LowerBound}-{UpperBound}" : LowerBound.ToString();


        }
        #endregion
    }
}
