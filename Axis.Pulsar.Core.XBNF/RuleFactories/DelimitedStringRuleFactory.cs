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
    
    #endregion

    public IAtomicRule NewRule(ImmutableDictionary<Argument, string> arguments)
    {
        throw new NotImplementedException();
    }

    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {

    }
}
