using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF;

/// <summary>
/// content accepts both excluded and included ranges in the same content. I.e
/// <code>
/// 'a-e, ^t-w, ^z, x, y'
/// </code>
/// </summary>
public class CharRangeRuleFactory : IAtomicRuleFactory
{
    #region Arguments

    /// <summary>
    /// Character ranges
    /// </summary>
    public static Argument RangesArgument => IAtomicRuleFactory.ContentArgument;

    #endregion

    public IAtomicRule NewRule(ImmutableDictionary<Argument, string> arguments)
    {
        ValidateArgs(arguments);

        var ranges = ParseRanges(arguments);
        return CharacterRanges.Of(
            ranges.Includes,
            ranges.Excludes);
    }

    private static void ValidateArgs(ImmutableDictionary<Argument, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.ContainsKey(RangesArgument))
            throw new ArgumentException("Invalid arguments: 'content' is missing");
    }

    private static (IEnumerable<CharRange> Includes, IEnumerable<CharRange> Excludes) ParseRanges(
        ImmutableDictionary<Argument, string> arguments)
    {
        
    }

    #region Nested types
    internal class RangesEscapeTransformer : IEscapeTransformer
    {
        public string Decode(string escapedString)
        {
            throw new NotImplementedException();
        }

        public string Encode(string rawString)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
