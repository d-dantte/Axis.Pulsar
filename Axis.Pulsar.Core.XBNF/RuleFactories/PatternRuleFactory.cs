using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;

/// <summary>
/// Accepts the pattern via the "content" argument, and the optional regex flags via the "flags" argument.
/// <para/>
/// Note that the "Compiled" regex flag is assumed by default, and is not recognized via the input flag arguments.
/// Argument mappings are:
/// <list type="number">
/// <item></item>
/// </list>
/// </summary>
public class PatternRuleFactory : IAtomicRuleFactory
{
    #region Arguments

    /// <summary>
    /// The content argument holds the regex definition
    /// <para/>
    /// - Escaping: No unescaping is done on the value of this argument - it is passed as-is into the <see cref="Regex"/> instance.
    /// </summary>
    public static IArgument PatternArgument => IAtomicRuleFactory.Content;

    /// <summary>
    /// Flags recognizes the flags:
    /// <list type="number">
    /// <item><see cref="RegexOptions.IgnoreCase"/> -> i</item>
    /// <item><see cref="RegexOptions.Multiline"/> -> m</item>
    /// <item><see cref="RegexOptions.ExplicitCapture"/> -> x</item>
    /// <item><see cref="RegexOptions.Singleline"/> -> s</item>
    /// <item><see cref="RegexOptions.IgnorePatternWhitespace"/> -> w</item>
    /// <item><see cref="RegexOptions.RightToLeft"/> -> r</item>
    /// <item><see cref="RegexOptions.ECMAScript"/> -> e</item>
    /// <item><see cref="RegexOptions.CultureInvariant"/> -> c</item>
    /// <item><see cref="RegexOptions.NonBacktracking"/> -> n</item>
    /// </list>
    /// </summary>
    public static IArgument FlagsArgument => IArgument.Of("flags");

    /// <summary>
    /// Match type of the pattern.
    /// </summary>
    public static IArgument MatchTypeArgument => IArgument.Of("match-type");

    #endregion

    public IAtomicRule NewRule(
        string ruleId,
        LanguageMetadata context,
        ImmutableDictionary<IArgument, string> arguments)
    {
        ValidateArgs(arguments);

        return TerminalPattern.Of(
            ruleId,
            ParseRegex(arguments),
            ParseMatchType(arguments));
    }

    private static void ValidateArgs(ImmutableDictionary<IArgument, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.ContainsKey(PatternArgument))
            throw new ArgumentException("Invalid arguments: 'content' is missing");
    }

    private static IMatchType ParseMatchType(ImmutableDictionary<IArgument, string> arguments)
    {
        if (!arguments.TryGetValue(MatchTypeArgument, out var value))
            return IMatchType.Of(1);

        var parts = value.Split(',');

        if (int.TryParse(parts[0], out var lhs) && lhs > 0)
        {
            if (parts.Length == 1)
                return IMatchType.Of(lhs, lhs);

            if (parts.Length == 2)
            {
                var part1 = parts[1].Trim();
                if ("*".Equals(part1))
                    return IMatchType.Of(lhs, true);

                if ("+".Equals(part1))
                    return IMatchType.Of(lhs, false);

                if(string.Empty.Equals(part1))
                    return IMatchType.Of(lhs, -1);

                else if(int.TryParse(part1, out var rhs) && rhs > 0)
                    return IMatchType.Of(lhs, rhs);
            }
        }

        throw new FormatException($"Invalid match-type format: '{value}'");
    }

    private static Regex ParseRegex(ImmutableDictionary<IArgument, string> arguments)
    {
        var pattern = arguments[PatternArgument];
        var options = arguments.TryGetValue(FlagsArgument, out var flags)
            ? flags.Aggregate(RegexOptions.None, (opt, @char) => opt |= @char switch
            {
                'i' => RegexOptions.IgnoreCase,
                'm' => RegexOptions.Multiline,
                'x' => RegexOptions.ExplicitCapture,
                's' => RegexOptions.Singleline,
                'w' => RegexOptions.IgnorePatternWhitespace,
                'r' => RegexOptions.RightToLeft,
                'e' => RegexOptions.ECMAScript,
                'c' => RegexOptions.CultureInvariant,
                'n' => RegexOptions.NonBacktracking,
                _ => throw new FormatException($"Invalid regex flag: {@char}")
            })
            : RegexOptions.None;

        return new Regex(pattern, options | RegexOptions.Compiled);
    }
}
