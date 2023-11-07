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
public class PatternRuleFactory : IDelimitedContentAtomicRuleFactory<PatternRuleFactory>
{
    #region Arguments

    /// <summary>
    /// The content argument holds the literal string to be matched. The 
    /// </summary>
    public static Argument ContentArgument => IAtomicRuleFactory.ContentArgument;

    /// <summary>
    /// Flags recognizes a single flag, 'i', which turns on case-insensitivity.
    /// </summary>
    public static Argument FlagsArgument => IAtomicRuleFactory.FlagsArgument;

    /// <summary>
    /// Match type of the pattern.
    /// </summary>
    public static Argument MatchTypeArgument => Argument.Of("match-type");
    #endregion

    public AtomicContentDelimiterType ContentDelimiterType => AtomicContentDelimiterType.Sol;

    public static ImmutableArray<Argument> ContentArgumentList { get; }
        = ImmutableArray.Create(FlagsArgument, MatchTypeArgument);

    public IAtomicRule NewRule(ImmutableDictionary<Argument, string> arguments)
    {
        ValidateArgs(arguments);

        return TerminalPattern.Of(
            ParseRegex(arguments),
            ParseMatchType(arguments));
    }

    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {

    }

    private static IMatchType ParseMatchType(ImmutableDictionary<Argument, string> arguments)
    {

    }

    private static Regex ParseRegex(ImmutableDictionary<Argument, string> arguments)
    {

    }
}
