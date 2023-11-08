using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.Utils.EscapeMatchers;
using System.Collections.Immutable;
using System.Globalization;

using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;

/// <summary>
/// content accepts both excluded and included ranges in the same content. I.e
/// <code>
/// 'a-e, ^t-w, ^z, x, y'
/// </code>
/// </summary>
public class CharRangeRuleFactory : IAtomicRuleFactory
{
    #region Arguments

    /// <summary>
    /// Character ranges
    /// </summary>
    public static Argument RangesArgument => IAtomicRuleFactory.ContentArgument;

    #endregion

    private static readonly IEscapeTransformer Transformer = new RangesEscapeTransformer();

    public IAtomicRule NewRule(ImmutableDictionary<Argument, string> arguments)
    {
        ValidateArgs(arguments);

        var ranges = ParseRanges(arguments);
        return CharacterRanges.Of(
            ranges.Includes,
            ranges.Excludes);
    }

    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.ContainsKey(RangesArgument))
            throw new ArgumentException("Invalid arguments: 'content' is missing");
    }

    private static (IEnumerable<CharRange> Includes, IEnumerable<CharRange> Excludes) ParseRanges(
        ImmutableDictionary<Argument, string> arguments)
    {
        var includes = new List<CharRange>();
        var excludes = new List<CharRange>();
        arguments[RangesArgument]
            .Split(',')
            .ForAll(range =>
            {
                if (string.Empty.Equals(range))
                    throw new FormatException($"Invalid range: {arguments[RangesArgument]}");

                range = Transformer.Decode(range.Trim());

                if ('^'.Equals(range[0]))
                    excludes.Add(CharRange.Parse(range[1..]));

                else includes.Add(CharRange.Parse(range));
            });

        return (includes, excludes);
    }

    #region Nested types

    /// <summary>
    /// Implementation is identical to <see cref="Axis.Pulsar.Core.Utils.EscapeMatchers.BSolUTFEscapeMatcher"/>, with
    /// the addition of escaping the following characters:
    /// <list type="number">
    /// <item> ' </item>
    /// <item> ^ </item>
    /// <item> - </item>
    /// <item> , </item>
    /// <item> \ </item>
    /// <item> new-line </item>
    /// <item> carriage-return </item>
    /// <item> space </item>
    /// </list>
    /// </summary>
    internal class RangesEscapeTransformer : IEscapeTransformer
    {

        private static readonly HashSet<char> EscapeArgs = new HashSet<char>
        {
            '\'', '\\', '^', '-', ',', ' '
        };

        public string Decode(string escapedString)
        {
            if (escapedString is null)
                return escapedString!;

            var substrings = new List<Tokens>();
            var offset = 0;

            do
            {
                var newOffset = escapedString.IndexOf("\\", offset);

                if (newOffset < 0)
                    substrings.Add(Tokens.Of(escapedString, offset));

                else 
                {
                    if (newOffset > offset)
                        substrings.Add(Tokens.Of(escapedString, offset, newOffset - offset));

                    // read the escape sequence
                    var argChar = escapedString[newOffset + 1];
                    Tokens replacement;

                    if (EscapeArgs.Contains(argChar))
                        replacement = Tokens.Of(argChar.ToString());

                    else if ('n'.Equals(argChar))
                        replacement = Tokens.Of("\n");

                    else if ('r'.Equals(argChar))
                        replacement = Tokens.Of("\r");

                    else if ('s'.Equals(argChar))
                        replacement = Tokens.Of(" ");

                    else if ('u'.Equals(argChar))
                        replacement = short
                            .Parse(
                                escapedString.AsSpan(newOffset + 2, 4),
                                NumberStyles.HexNumber)
                            .ApplyTo(code => (char)code)
                            .ApplyTo(@char => Tokens.Of(@char.ToString()));

                    else throw new InvalidOperationException($"Invalid escape arg charactre: {argChar}");

                    substrings.Add(replacement);
                    offset = newOffset + argChar switch
                    {
                        '\'' or '\\'
                        or 'n' or 'r' or 's'
                        or '^' or '-' or ',' => 2,

                        'u' or _ => 6
                    };
                }
            }
            while (offset < escapedString.Length);

            return substrings
                .Select(s => s.ToString())
                .JoinUsing("");
        }

        public string Encode(string rawString)
        {
            if (rawString is null)
                return rawString!;

            var substrings = new List<Tokens>();
            var offset = 0;
            for (int index = 0; index < rawString.Length; index++)
            {
                if (EscapeArgs.Contains(rawString[index]))
                {
                    var prev = Tokens.Of(rawString, offset, index - offset);
                    if (!prev.IsEmpty)
                        substrings.Add(prev);

                    offset = index + 1;
                    var escapeArg = rawString[index] switch
                    {
                        '\n' => 'n',
                        '\r' => 'r',
                        ' ' => 's',
                        char c => c
                    };
                    substrings.Add(Tokens.Of($"\\{escapeArg}"));
                }
                else if (BSolAsciiEscapeMatcher.UnprintableAsciiCharCodes.Contains(rawString[index])
                    || rawString[index] > 255)
                {
                    var prev = Tokens.Of(rawString, offset, index - offset);
                    if (!prev.IsEmpty)
                        substrings.Add(prev);

                    offset = index + 1;
                    substrings.Add(Tokens.Of($"\\u{(int)rawString[index]:x4}"));
                }
            }

            return substrings
                .Select(s => s.ToString())
                .JoinUsing("");
        }
    }
    #endregion
}
