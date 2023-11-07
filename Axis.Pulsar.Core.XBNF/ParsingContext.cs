using System.Collections.Immutable;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.XBNF;

public class ParsingContext
{
    public ImmutableDictionary<string, IAtomicRuleFactory> AtomicFactoryMap { get; }

    public ImmutableDictionary<AtomicContentDelimiterType, string> AtomicContentDelimiterMap { get; }

    public ParsingContext(IDictionary<string, IAtomicRuleFactory> factoryMap)
    {
        var delimiterMap = new Dictionary<AtomicContentDelimiterType, string>();
        foreach (var kvp in factoryMap)
        {
            if (!Production.SymbolPattern.IsMatch(kvp.Key))
                throw new FormatException($"Invalid symbol format: '{kvp.Key}'");

            if (kvp.Value is null)
                throw new ArgumentException($"Invalid factory instance: null");

            if (kvp.Value is IDelimitedContentAtomicRuleFactory dcarf
                && !delimiterMap.TryAdd(dcarf.ContentDelimiterType, kvp.Key))
                throw new ArgumentException($"Duplicate ContentDelimiterType: {dcarf.ContentDelimiterType}");
        }

        AtomicFactoryMap = factoryMap.ToImmutableDictionary();
        AtomicContentDelimiterMap = delimiterMap.ToImmutableDictionary();
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
