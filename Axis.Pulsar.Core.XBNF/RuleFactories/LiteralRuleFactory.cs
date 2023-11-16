using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
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
    public static Argument LiteralArgument => IAtomicRuleFactory.ContentArgument;

    /// <summary>
    /// case insensitive flag
    /// </summary>
    public static Argument CaseInsensitiveArgument => Argument.Of("case-insensitive");

    #endregion

    public IAtomicRule NewRule(
        MetaContext context,
        ImmutableDictionary<Argument, string> arguments)
    {
        ValidateArgs(arguments);

        return TerminalLiteral.Of(
            arguments[LiteralArgument],
            arguments.TryGetValue(CaseInsensitiveArgument, out _));
    }

    /// <summary>
    /// Ensure that the "content" flag exists
    /// </summary>
    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));

        if (!arguments.ContainsKey(LiteralArgument))
            throw new ArgumentException($"Invalid arguments: '{LiteralArgument}' is missing");
    }
}
