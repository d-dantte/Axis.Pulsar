using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
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
    /// The content argument holds the literal string to be matched. The 
    /// </summary>
    public static Argument PatternArgument => IAtomicRuleFactory.ContentArgument;

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
    public static Argument FlagsArgument => Argument.Of("flags");

    /// <summary>
    /// Match type of the pattern.
    /// </summary>
    public static Argument MatchTypeArgument => Argument.Of("match-type");

    #endregion

    public IAtomicRule NewRule(
        LanguageContext context,
        ImmutableDictionary<Argument, string> arguments)
    {
        ValidateArgs(arguments);

        return TerminalPattern.Of(
            ParseRegex(arguments),
            ParseMatchType(arguments));
    }

    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.ContainsKey(PatternArgument))
            throw new ArgumentException("Invalid arguments: 'content' is missing");
    }

    private static IMatchType ParseMatchType(ImmutableDictionary<Argument, string> arguments)
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
                if ("*".Equals(parts[1]))
                    return IMatchType.Of(lhs, true);

                if ("+".Equals(parts[1]))
                    return IMatchType.Of(lhs, false);

                if(string.Empty.Equals(parts[1]))
                    return IMatchType.Of(lhs, -1);

                else if(int.TryParse(parts[1], out var rhs) && rhs > 0)
                    return IMatchType.Of(lhs, rhs);
            }
        }

        throw new FormatException($"Invalid match-type format: '{value}'");
    }

    private static Regex ParseRegex(ImmutableDictionary<Argument, string> arguments)
    {
        var pattern = arguments[PatternArgument];
        var options = arguments[FlagsArgument]
            .Aggregate(RegexOptions.None, (opt, @char) => opt |= @char switch
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
            });

        return new Regex(pattern, options | RegexOptions.Compiled);
    }
}
