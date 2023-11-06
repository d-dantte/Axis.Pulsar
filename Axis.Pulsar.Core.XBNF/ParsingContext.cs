using System.Collections.Immutable;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.XBNF;

public class ParsingContext
{
    public ImmutableDictionary<string, IAtomicRuleFactory> AtomicFactoryMap{ get; }

    public ParsingContext(
        IDictionary<string, IAtomicRuleFactory> factoryMap)
    {
        AtomicFactoryMap = factoryMap
            .ThrowIfNull(new ArgumentNullException(nameof(factoryMap)))
            .ThrowIfAny(kvp => 
                !Production.SymbolPattern.IsMatch(kvp.Key)
                || kvp.Value is not null)
            .ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);

    }
}

public class ParsingContextBuilder
{
    private readonly Dictionary<string, IAtomicRuleFactory> _atomicFactoryMap = new();

    public ParsingContextBuilder()
    {        
    }

    public static ParsingContextBuilder NewBuilder() => new();

    #region AtomicFactory
    public ParsingContextBuilder WithAtomicFactory(string productionSymbol, IAtomicRuleFactory factory)
    {
        _atomicFactoryMap[productionSymbol] = factory;
        return this;
    }
    public bool ContainsAtomicFactoryFor(string productionSymbol)
    {
        return _atomicFactoryMap.ContainsKey(productionSymbol);
    }
    #endregion

    public ParsingContext Build()
    {
        return new ParsingContext(_atomicFactoryMap);
    }
}
