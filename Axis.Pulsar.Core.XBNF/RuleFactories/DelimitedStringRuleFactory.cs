using Axis.Pulsar.Core.Grammar;
using System.Collections.Immutable;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;

public class DelimitedStringRuleFactory : IAtomicRuleFactory
{
    #region Arguments

    /// <summary>
    /// Character ranges - includes and excludes
    /// </summary>
    public static Argument RangesArgument => Argument.Of("ranges");

    /// <summary>
    /// Character sequences - includes and excludes
    /// </summary>
    public static Argument SequencesArgument => Argument.Of("sequences");

    /// <summary>
    /// Start delimiter argument
    /// </summary>
    public static Argument StartDelimArgument => Argument.Of("start");

    /// <summary>
    /// End delimiter argument
    /// </summary>
    public static Argument EndDelimArgument => Argument.Of("end");

    /// <summary>
    /// Accepts empty argument
    /// </summary>
    public static Argument AcceptsEmptyArgument => Argument.Of("accepts-empty");

    /// <summary>
    /// Escape matchers argument
    /// </summary>
    public static Argument EscapeMatchersArgument => Argument.Of("escape-matchers");

    #endregion

    public IAtomicRule NewRule(ImmutableDictionary<Argument, string> arguments)
    {
        ValidateArgs(arguments);


    }

    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.ContainsKey(StartDelimArgument))
            throw new ArgumentException($"Invalid arguments: '{StartDelimArgument}' is missing");

        if (!arguments.ContainsKey(EscapeMatchersArgument))
            throw new ArgumentException($"Invalid arguments: '{EscapeMatchersArgument}' is missing");
    }
}
