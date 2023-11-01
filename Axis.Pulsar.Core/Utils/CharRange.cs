namespace Axis.Pulsar.Core.Utils;

using Axis.Luna.Extensions;


/// <summary>
/// Represents a range of characters as an inclusive lower and uppoer bound.
/// </summary>
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

    /// <summary>
    /// Orders and then merges overlapping ranges.
    /// </summary>
    /// <param name="charRanges">The list of ranges</param>
    /// <returns>The normalized collection of ranges</returns>
    public static IEnumerable<CharRange> NormalizeRanges(IEnumerable<CharRange> charRanges)
    {
        return charRanges
            .ThrowIfNull(new ArgumentNullException(nameof(charRanges)))
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