using Axis.Luna.Common.StringEscape;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;

/// <summary>
/// Accepts the literal to match via the "content" argument, and the optional case-insensitive flag via the "flags" argument.
/// <para/>
/// Note that without the case-insensitive flag, this factory generates a case-sensitive rule.
/// </summary>
public class LiteralRuleFactory : IAtomicRuleFactory
{
    private static readonly IStringEscaper Escaper = new CommonStringEscaper();

    #region Arguments

    /// <summary>
    /// The content argument holds the literal string to be matched.
    /// <para/>
    /// - Escaping: all bsol-escape sequences within the 'literal' are unescaped. This means that the literal that gets passed
    /// into the final <see cref="TerminalLiteral"/> instance will be the unescaped version of this argument.
    /// </summary>
    public static IArgument LiteralArgument => IAtomicRuleFactory.Content;

    /// <summary>
    /// case sensitive flag
    /// </summary>
    public static IArgument CaseInsensitiveArgument => IArgument.Of("case-insensitive");

    #endregion

    public IAtomicRule NewRule(
        string ruleId,
        LanguageMetadata context,
        ImmutableDictionary<IArgument, string> arguments)
    {
        ValidateArgs(arguments);

        return TerminalLiteral.Of(
            ruleId,
            ParseLiteral(arguments[LiteralArgument]),
            !arguments.TryGetValue(CaseInsensitiveArgument, out _));
    }

    /// <summary>
    /// Ensure that the "content" flag exists
    /// </summary>
    private static void ValidateArgs(ImmutableDictionary<IArgument, string> arguments)
    {
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));

        if (!arguments.ContainsKey(LiteralArgument))
            throw new ArgumentException($"Invalid arguments: '{LiteralArgument}' is missing");
    }

    private static string ParseLiteral(
        string value)
        => Escaper.UnescapeString(value);
}
