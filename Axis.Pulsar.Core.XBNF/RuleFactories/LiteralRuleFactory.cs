using Axis.Pulsar.Core.Grammar.Nodes;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.Utils.EscapeMatchers;
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
    #region Arguments

    /// <summary>
    /// The content argument holds the literal string to be matched. The 
    /// </summary>
    public static IArgument LiteralArgument => IAtomicRuleFactory.Content;

    /// <summary>
    /// case insensitive flag
    /// </summary>
    public static IArgument CaseInsensitiveArgument => IArgument.Of("case-insensitive");

    #endregion

    private static readonly IEscapeTransformer _BasicEscapeTransformer = new BSolBasicEscapeMatcher(); 

    public IAtomicRule NewRule(
        string ruleId,
        LanguageMetadata context,
        ImmutableDictionary<IArgument, string> arguments)
    {
        ValidateArgs(arguments);

        return TerminalLiteral.Of(
            ruleId,
            ParseLiteral(arguments[LiteralArgument]),
            arguments.TryGetValue(CaseInsensitiveArgument, out _));
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

    /// <summary>
    /// Decodes basic escape sequences. See <see cref="BSolBasicEscapeMatcher"/>.
    /// </summary>
    /// <param name="literal"></param>
    /// <returns></returns>
    private static string ParseLiteral(string literal)
    {
        return _BasicEscapeTransformer.Decode(literal);
    }
}
