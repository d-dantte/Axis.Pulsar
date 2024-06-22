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
        _root = root.ThrowIf(
            string.IsNullOrWhiteSpace,
            _ => new ArgumentException($"Invalid {nameof(root)}: '{root}'"));

        _productions = productions
            .ThrowIfNull(() => new ArgumentNullException(nameof(productions)))
            .ThrowIfAny(prod => prod is null, _ => new ArgumentException($"Invalid production: null"))
            .ToDictionary(prod => prod.Symbol, prod => prod);

        ValidateGrammar();
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

    /// <summary>
    /// Validate the grammar. A valid grammar is one that:
    /// <list type="number">
    ///     <item>Has no unreferenced production. An unreferenced production is one that cannot be traced back to the root</item>
    ///     <item>Has no orphaned symbol-references. An orphaned symbol-reference is one that refers to a non-existent production</item>
    ///     <item>All symbols terminate in terminals</item>
    ///     <item>Has no infinite recursion</item>
    /// </list>
    /// </summary>
    private void ValidateGrammar()
    {
        var result = GrammarValidator__old.Validate(this);

        // based on the result, throw some exceptions, or return
    }
}
