using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;

namespace Axis.Pulsar.Core.XBNF;

public class XBNFGrammar : IGrammar
{
    private readonly Dictionary<string, Production> _productions;
    private readonly string _root;

    public string Root => _root;

    public IEnumerable<string> ProductionSymbols => _productions.Keys;

    internal XBNFGrammar(string root, IEnumerable<Production> productions)
    {
        _root = root
            .ThrowIfNull(() => new ArgumentNullException(nameof(root)))
            .ThrowIfNot(
                Production.SymbolPattern.IsMatch,
                _ => new FormatException($"Invalid symbol format: '{root}'"));

        _productions = productions
            .ThrowIfNull(() => new ArgumentNullException(nameof(productions)))
            .ThrowIf(prods => prods.IsEmpty(), _ => new ArgumentException($"Invalid {nameof(productions)}: empty"))
            .ThrowIfAny(prod => prod is null, _ => new ArgumentException($"Invalid production: null"))
            .ToDictionary(prod => prod.Symbol, prod => prod);
    }

    public static IGrammar Of(
        string root,
        IEnumerable<Production> productions)
        => new XBNFGrammar(root, productions);

    public bool ContainsProduction(string symbolName) => _productions.ContainsKey(symbolName);

    public Production GetProduction(string name) => this[name];

    public int ProductionCount => _productions.Count;

    public Production this[string name] => _productions[name];

    public bool TryGetProduction(string name, out Production? production)
    {
        if (!ContainsProduction(name))
        {
            production = null;
            return false;
        }

        production = _productions[name];
        return true;
    }
}
