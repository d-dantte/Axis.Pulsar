using Axis.Luna.Common.StringEscape;
using Axis.Luna.Extensions;
using Axis.Luna.Result;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static Axis.Pulsar.Core.Grammar.Rules.Atomic.DelimitedContent;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.RuleFactories
{
    public class DelimitedContentRuleFactory : IAtomicRuleFactory
    {
        internal static readonly Regex QualifierPattern = new("^[a-zA-Z0-9][-\\.a-zA-Z0-9]*$", RegexOptions.Compiled);

        internal static readonly CommonStringEscaper Escaper = new();

        #region Arguments

        /// <summary>
        /// Accepts empty argument
        /// </summary>
        public static IArgument AcceptsEmptyArgument => IArgument.Of("accepts-empty");

        /// <summary>
        /// Start delimiter argument.
        /// <para/>
        /// String containing escape-sequences. The factory is responsible for unescaping the sequences so the raw string
        /// is passed into the <see cref="DelimitedContent"/> instance.
        /// </summary>
        public static IArgument StartDelimArgument => IArgument.Of("start");

        /// <summary>
        /// Start delimiter escape argument.
        /// <para/>
        /// String containing escape-sequences. The factory is responsible for unescaping the sequences so the raw string
        /// is passed into the <see cref="DelimitedContent"/> instance.
        /// </summary>
        public static IArgument StartDelimEscapeArgument => IArgument.Of("start-escape");

        /// <summary>
        /// End delimiter argument
        /// <para/>
        /// String containing escape-sequences. The factory is responsible for unescaping the sequences so the raw string
        /// is passed into the <see cref="DelimitedContent"/> instance.
        /// </summary>
        public static IArgument EndDelimArgument => IArgument.Of("end");

        /// <summary>
        /// End delimiter escape argument.
        /// <para/>
        /// String containing escape-sequences. The factory is responsible for unescaping the sequences so the raw string
        /// is passed into the <see cref="DelimitedContent"/> instance.
        /// </summary>
        public static IArgument EndDelimEscapeArgument => IArgument.Of("end-escape");

        /// <summary>
        /// The rule that constrains the content of the delimiter-bound string.
        /// <para/>
        /// Syntax: <code>'qualifier: constraint-rule-text'</code>
        /// The delimiter between the qualifier and the rule text is <code>': '</code>
        /// </summary>
        public static IArgument ContentRuleArgument => IArgument.Of("content-rule");

        #endregion

        private readonly ConstraintQualifierMap constraintMap;


        public DelimitedContentRuleFactory(ConstraintQualifierMap constraintMap)
        {
            ArgumentNullException.ThrowIfNull(constraintMap);
            this.constraintMap = constraintMap.Clone();
        }

        public IAtomicRule NewRule(
            string ruleId,
            LanguageMetadata metadata,
            ImmutableDictionary<IArgument, string> arguments)
        {
            ValidateArgs(arguments);

            var (Start, End) = ParseDelimiters(arguments);
            var acceptsEmpty = ParseAcceptsEmpty(arguments);
            var constraint = ParseConstraint(arguments, constraintMap);

            return new DelimitedContent(ruleId, acceptsEmpty, Start, End, constraint);
        }

        internal static void ValidateArgs(ImmutableDictionary<IArgument, string> arguments)
        {
            ArgumentNullException.ThrowIfNull(arguments);

            if (!arguments.ContainsKey(StartDelimArgument))
                throw new ArgumentException($"Invalid arguments: '{StartDelimArgument}' is missing");

            if (!arguments.ContainsKey(ContentRuleArgument))
                throw new ArgumentException($"Invalid arguments: '{ContentRuleArgument}' is missing");
        }

        internal static bool ParseAcceptsEmpty(ImmutableDictionary<IArgument, string> arguments)
        {
            return !arguments.TryGetValue(AcceptsEmptyArgument, out var value) || bool.Parse(value);
        }

        internal static (DelimiterInfo Start, DelimiterInfo? End) ParseDelimiters(ImmutableDictionary<IArgument, string> arguments)
        {
            // start
            var startDelim = arguments[StartDelimArgument].ApplyTo(Escaper.UnescapeString);
            var startEscape = arguments.TryGetValue(StartDelimEscapeArgument, out var value)
                ? value.ApplyTo(Escaper.UnescapeString)
                : null;

            // end
            var endDelim = arguments.TryGetValue(EndDelimArgument, out value)
                ? value.ApplyTo(Escaper.UnescapeString)
                : null;
            var endEscape = arguments.TryGetValue(EndDelimEscapeArgument, out value)
                ? value.ApplyTo(Escaper.UnescapeString)
                : null;

            return (
                new DelimiterInfo(startDelim, startEscape),
                endDelim is not null ? new DelimiterInfo(endDelim!, endEscape) : null);
        }

        /// <summary>
        /// NOTE: constraints are not automatically unescaped - this is left for the parsers to do
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="qualifierMap"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static IContentConstraint ParseConstraint(
            ImmutableDictionary<IArgument, string> arguments,
            ConstraintQualifierMap qualifierMap)
        {
            // NOTE: do not unescape the strings
            var text = arguments[ContentRuleArgument];

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException($"Invalid content rule: no qualifier delimiter found");

            var delimIndex = text.IndexOf(':');
            var qualifier = delimIndex switch
            {
                < 0 => text,
                _ => text[..delimIndex]
            };

            if (!QualifierPattern.IsMatch(qualifier))
                throw new FormatException($"Invalid qualifier format: '{qualifier}'");

            var constraintParser = qualifierMap[qualifier];
            return constraintParser.Parse(
                delimIndex < 0 ? string.Empty :
                delimIndex == text.Length - 1 ? string.Empty :
                delimIndex == text.Length - 2 ? string.Empty :
                text[(delimIndex + 2)..]);
        }


        #region Nested types

        public class ConstraintQualifierMap
        {
            private readonly Dictionary<string, IConstraintParser> constraintMap = [];

            public int ConstraintCount => Constraints.Count();

            public static ConstraintQualifierMap New() => new();

            public IEnumerable<IConstraintParser> Constraints => constraintMap.Values.Distinct();

            public IConstraintParser this[string key] => constraintMap[key];

            public ImmutableArray<string> QualifiersFor(IConstraintParser constraint)
            {
                ArgumentNullException.ThrowIfNull(constraint);

                return constraintMap
                    .Where(kvp => kvp.Value.Equals(constraint))
                    .Select(kvp => kvp.Key)
                    .ToImmutableArray();
            }

            public ConstraintQualifierMap AddQualifiers(IConstraintParser constraint, params string[] qualifiers)
            {
                ArgumentNullException.ThrowIfNull(constraint);
                ArgumentNullException.ThrowIfNull(qualifiers);

                foreach (var qualifier in qualifiers)
                {
                    if (!QualifierPattern.IsMatch(qualifier))
                        throw new ArgumentException($"Invalid qualifier: '{qualifier}'");

                    if (constraintMap.ContainsKey(qualifier))
                        throw new InvalidOperationException($"Invalid qualifier: duplicate detected for '{qualifier}'");

                    constraintMap.Add(qualifier, constraint);
                }

                return this;
            }

            public ConstraintQualifierMap Clone()
            {
                var clone = new ConstraintQualifierMap();
                constraintMap.ForEvery(kvp => clone.AddQualifiers(kvp.Value, kvp.Key));

                return clone;
            }
        }

        /// <summary>
        /// Contract for parsing <see cref="IContentConstraint"/> instances from the input string of the <code>content</code> argument
        /// </summary>
        public interface IConstraintParser
        {
            IContentConstraint Parse(string tokenSource);
        }

        #region CharRangeParser
        /// <summary>
        /// Comma-space separated list of character ranges. E.g <code>a-b, 4-9, \xe3-\xff</code>
        /// </summary>
        public abstract class CharacterRangesParser : IConstraintParser
        {
            public abstract IContentConstraint CreateConstraint(CharRange[] ranges);

            public IContentConstraint Parse(string tokens)
            {
                if (!TryParseRanges(tokens, out var ranges))
                    throw new FormatException($"Invalid constraint rule format");

                return CreateConstraint(ranges);
            }

            internal static bool TryParseRanges(TokenReader reader, out CharRange[] ranges)
            {
                var position = reader.Position;
                var rangeList = new List<CharRange>();
                while (TryParseRange(reader, out var range))
                {
                    rangeList.Add(range);

                    _ = TryParseConcatDelimiter(reader, out _);
                }

                if (rangeList.IsEmpty())
                {
                    reader.Reset(position);
                    ranges = Array.Empty<CharRange>();
                    return false;
                }

                ranges = rangeList.ToArray();
                return true;
            }

            internal static bool TryParseConcatDelimiter(TokenReader reader, out string delimiter)
            {
                var position = reader.Position;
                var delim = new StringBuilder();

                // Optional Spaces
                if (TryParseSpaces(reader, out var spaces))
                    delim.Append(spaces);

                // Mandatory Comma
                if (reader.TryGetTokens(",", out var comma))
                    delim.Append(comma);

                else
                {
                    delimiter = string.Empty;
                    reader.Reset(position);
                    return false;
                }

                // Optional Spaces
                if (TryParseSpaces(reader, out spaces))
                    delim.Append(spaces);

                delimiter = delim.ToString();
                return true;
            }

            internal static bool TryParseSpaces(TokenReader reader, out string spaces)
            {
                var spaceBuilder = new StringBuilder();
                while (reader.TryGetToken(out var token))
                {
                    if (token.Equals(" ") || token.Equals("\t")
                        || token.Equals("\n") || token.Equals("\r"))
                        spaceBuilder.Append(token);

                    else
                    {
                        reader.Back();
                        break;
                    }
                }

                spaces = spaceBuilder.ToString();
                return !string.Empty.Equals(spaces);
            }

            /// <summary>
            /// <code> -> +[ $char +[ $space.? $dash $space.? $char].? +[$space.? $comma $space.? $char +[ $space.? $dash $space.? $char].?].*]</code>
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="range"></param>
            /// <returns></returns>
            internal static bool TryParseRange(TokenReader reader, out CharRange range)
            {
                var position = reader.Position;
                range = default;

                // First character
                if (!TryParseChar(reader, out var firstChar))
                    return false;

                // Optional spaces
                _ = TryParseSpaces(reader, out _);

                // Dash?
                char? secondChar = null;
                if (reader.TryGetTokens("-", out _))
                {
                    // Optional spaces
                    _ = TryParseSpaces(reader, out _);

                    // Second character
                    if (!TryParseChar(reader, out var char2))
                    {
                        reader.Reset(position);
                        return false;
                    }

                    secondChar = char2;
                }

                range = new CharRange(firstChar, secondChar ?? firstChar);
                return true;
            }

            internal static bool TryParseChar(TokenReader reader, out char @char)
            {
                var position = reader.Position;
                @char = default;

                // First char
                if (!reader.TryGetToken(out var firstChar))
                    return false;

                if (!firstChar.Equals("\\"))
                {
                    @char = firstChar[0];
                    return true;
                }

                // escaped char
                _ = reader.TryGetToken(out var secondChar);

                var escapeSequence = secondChar[0] switch
                {
                    'u' or 'U' => reader.TryGetTokens(4, true, out var uEscapeCode)
                        ? $"\\u{uEscapeCode}" : null,
                    'x' or 'X' => reader.TryGetTokens(2, true, out var uEscapeCode)
                        ? $"\\x{uEscapeCode}" : null,

                    '0' or 'a' or 'b' or 'f' or
                    'n' or 'r' or 't' or 'v' or
                    '"' or '\'' or '\\' => $"\\{secondChar[0]}",

                    _ => null
                };

                if (escapeSequence is null || !Escaper.IsValidEscapeSequence(escapeSequence))
                {
                    @char = default;
                    reader.Reset(position);
                    return false;
                }

                @char = Escaper.Unescape(escapeSequence)[0];
                return true;
            }
        }

        public class IllegalCharacterRangesParser : CharacterRangesParser
        {
            public override IContentConstraint CreateConstraint(CharRange[] ranges)
            {
                return new IllegalCharacterRanges(ranges);
            }
        }

        public class LegalCharacterRangesParser : CharacterRangesParser
        {
            public override IContentConstraint CreateConstraint(CharRange[] ranges)
            {
                return new LegalCharacterRanges(ranges);
            }
        }
        #endregion

        #region DiscretePatternParser
        /// <summary>
        /// Expects a comma-space separated list of tokens each representing a pattern, based on the following syntax:
        /// <para/>
        /// <code>{type-char}{case-char}:{pattern}[, {type-char}{case-char}:{pattern}]*</code>
        /// E.g:
        /// <list type="bullet">
        ///     <item>
        ///         <code>wi:some.text.with......whild cards and a fullstop at the end\.</code>
        ///         this represents a wilecard-pattern (w), with case-insensitive characters (i).
        ///     </item>
        ///     <item>
        ///         <code>ls:some text with no whild cards and a fullstop at the end.</code>
        ///         this represents a literal-pattern (i), with case-sensitive characters (s).
        ///     </item>
        /// </list>
        /// 
        /// The above means that commas and spaces, in the context of each pattern, MUST be escaped
        /// </summary>
        public abstract class DiscretePatternsParser : IConstraintParser
        {
            public abstract IContentConstraint CreateConstraint(IPattern[] pattern);

            public IContentConstraint Parse(string tokens)
            {
                if (!TryParsePatterns(tokens, out var patterns))
                    throw new FormatException($"Invalid constraint rule format");

                return CreateConstraint(patterns);
            }

            internal static bool TryParsePatterns(TokenReader reader, out IPattern[] ranges)
            {
                var options =
                    StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries;

                var patternTexts = reader.Source
                    .Split(',', options);

                ranges = patternTexts
                    .Select(ParsePattern)
                    .Where(pattern => pattern is not null)
                    .Select(pattern => pattern!)
                    .ToArray();

                return ranges.Length != patternTexts.Length;
            }

            internal static IPattern? ParsePattern(string source)
            {
                return source[..3] switch
                {
                    "wi:" => source[3..]
                        .Replace("\\x2c", ",")
                        .Replace("\\x20", " ")
                        .ApplyTo(text => WildcardExpression.Parse(text, false))
                        .Map(exp => new WildcardPattern(exp))
                        .MapError(ex => null!)
                        .Resolve(),

                    "ws:" => source[3..]
                        .Replace("\\x2c", ",")
                        .Replace("\\x20", " ")
                        .ApplyTo(WildcardExpression.Parse)
                        .Map(exp => new WildcardPattern(exp))
                        .MapError(ex => null!)
                        .Resolve(),

                    "li:" => new LiteralPattern(source[3..], false),
                    "ls:" => new LiteralPattern(source[3..], true),
                    _ => null
                };
            }
        }

        public class LegalDiscretePatternsParser : DiscretePatternsParser
        {
            public override IContentConstraint CreateConstraint(IPattern[] patterns)
            {
                return new LegalDiscretePatterns(patterns);
            }
        }

        public class IllegalDiscretePatternsParser : DiscretePatternsParser
        {
            public override IContentConstraint CreateConstraint(IPattern[] patterns)
            {
                return new IllegalDiscretePatterns(patterns);
            }
        }
        #endregion

        #region Default Parser
        public class DefaultDelimitedContentParser : IConstraintParser
        {
            public static readonly DefaultDelimitedContentParser DefaultConstraintParser = new();

            public IContentConstraint Parse(string tokenSource) => DelimitedContent.DefaultContentConstraint.SingletonInstance;
        }

        #endregion

        #endregion
    }
}
