using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;

using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;

/// <summary>
/// RANGES - Syntax
/// <para/>
/// content accepts both excluded and included ranges in the same content. I.e
/// <code>
/// 'a-e, ^t-w, ^z, x, y'
/// </code>
/// <list type="number">
/// <item>Whitespaces are ignored</item>
/// <item>Commas signify the start of another sequence</item>
/// <item>Special Escaped characters include: "'", ",", "\", "^", "-", " ". Single space is escaped with "\s".</item>
/// <item>Regular Escaped characters are recognized: "\n", "\t", "\b", etc</item>
/// <item>Utf escape is recognized for all characters: \uffff</item>
/// <item> </item>
/// <item> </item>
/// </list>
/// </summary>
public class CharRangeRuleFactory : IAtomicRuleFactory
{
    #region Arguments

    /// <summary>
    /// Character ranges
    /// </summary>
    public static IArgument RangesArgument => IAtomicRuleFactory.Content;

    #endregion

    private static readonly IEscapeTransformer Transformer = new RangesEscapeTransformer();

    public IAtomicRule NewRule(
        string ruleId,
        LanguageMetadata context,
        ImmutableDictionary<IArgument, string> arguments)
    {
        ValidateArgs(arguments);

        var ranges = ParseRanges(arguments[RangesArgument]);
        return CharacterRanges.Of(
            ruleId,
            ranges.Includes,
            ranges.Excludes);
    }

    private static void ValidateArgs(ImmutableDictionary<IArgument, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.ContainsKey(RangesArgument))
            throw new ArgumentException("Invalid arguments: 'content' is missing");
    }

    internal static (IEnumerable<CharRange> Includes, IEnumerable<CharRange> Excludes) ParseRanges(
        string rangeText)
    {
        var includes = new List<CharRange>();
        var excludes = new List<CharRange>();
        var primedRanges = rangeText
            .Replace("\\,", "\\u002c")
            .Replace("\\-", "\\u002d")
            .Replace("\\\'", "\\u002d")
            .Replace("\\\"", "\\u0022")
            .Replace("\\\\", "\\u005c")
            .Replace("\\n", "\\u000a")
            .Replace("\\r", "\\u000d")
            .Replace("\\f", "\\u000c")
            .Replace("\\b", "\\u0008")
            .Replace("\\t", "\\u0009")
            .Replace("\\v", "\\u000b")
            .Replace("\\0", "\\u0000")
            .Replace("\\a", "\\u0007")
            .Replace("\\s", "\\u0020")
            .Split(',')
            .Select(range => range.Trim())
            .ThrowIfAny(
                string.Empty.Equals,
                new FormatException($"Invalid range: {rangeText}"));

        return (
            primedRanges
                .Where(sequence => !'^'.Equals(sequence[0]))
                .Select(Transformer.Decode)
                .Select(CharRange.Parse)
                .ToArray(),
            primedRanges
                .Where(sequence => '^'.Equals(sequence[0]))
                .Select(range => range[1..])
                .Select(Transformer.Decode)
                .Select(CharRange.Parse)
                .ToArray());
    }

    #region Nested types

    /// <summary>
    /// Encodes the following characters
    /// <list type="number">
    /// <item> ' </item>
    /// <item> ^ </item>
    /// <item> space </item>
    /// </list>
    /// </summary>
    internal class RangesEscapeTransformer : IEscapeTransformer
    {
        public string Decode(string escapedRange)
        {
            return escapedRange?
                .Replace("\\'", "'")
                .Replace("\\^", "^")
                .Replace("\\ ", "\\x20")!;
        }

        public string Encode(string rawString)
        {
            return rawString?
                .Replace("'", "\\'")
                .Replace("^", "\\^")
                .Replace(" ", "\\ ")!;
        }
    }
    #endregion
}
