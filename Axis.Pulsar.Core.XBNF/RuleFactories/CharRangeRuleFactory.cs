using Axis.Pulsar.Core.Grammar;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF;

/// <summary>
/// content accepts both excluded and included ranges in the same content. I.e
/// <code>
/// 'a-e, ^t-w, ^z, x, y'
/// </code>
/// </summary>
public class CharRangeRuleFactory : IDelimitedContentAtomicRuleFactory<CharRangeRuleFactory>
{
    public static ImmutableArray<IAtomicRuleFactory.Argument> ContentArgumentList => throw new NotImplementedException();

    public AtomicContentDelimiterType ContentDelimiterType => AtomicContentDelimiterType.Quote;

    public IAtomicRule NewRule(ImmutableDictionary<IAtomicRuleFactory.Argument, string> arguments)
    {
        throw new NotImplementedException();
    }
}
