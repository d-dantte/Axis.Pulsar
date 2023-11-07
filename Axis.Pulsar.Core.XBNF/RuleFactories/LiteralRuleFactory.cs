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
public class LiteralRuleFactory : IDelimitedContentAtomicRuleFactory<LiteralRuleFactory>
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
    #endregion

    public AtomicContentDelimiterType ContentDelimiterType => AtomicContentDelimiterType.DoubleQuote;

    public static ImmutableArray<Argument> ContentArgumentList { get; }
        = ImmutableArray.Create(FlagsArgument);

    public IAtomicRule NewRule(ImmutableDictionary<Argument, string> arguments)
    {
        ValidateArgs(arguments);

        return TerminalLiteral.Of(
            arguments[ContentArgument],
            arguments.TryGetValue(FlagsArgument, out var flags) && flags.Contains('i'));
    }

    /// <summary>
    /// Ensure that the "content" flag exists
    /// </summary>
    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));

        if (!arguments.ContainsKey(ContentArgument))
            throw new ArgumentException($"Invalid arguments: 'content' argument is missing");
    }
}
