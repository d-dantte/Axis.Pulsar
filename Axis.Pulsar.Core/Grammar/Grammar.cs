using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar
{
    public interface IGrammar
    {
        public string Root { get; }

        public int ProductionCount { get; }

        public Production this[string name] { get; }

        public IEnumerable<string> ProductionSymbols { get; }


        public bool ContainsProduction(string symbolName);

        public Production GetProduction(string name);

        public bool TryGetProduction(string name, out Production? production);
    }

    public class Grammar: IGrammar
    {
        private readonly Dictionary<string, Production> _productions;
        private string _root;

        public string Root => _root;

        public IEnumerable<string> ProductionSymbols => _productions.Keys;

        internal Grammar(string root, IEnumerable<Production> productions)
        {
            _root = root.ThrowIf(
                string.IsNullOrWhiteSpace,
                new ArgumentException($"Invalid {nameof(root)}: '{root}'"));

            _productions = productions
                .ThrowIfNull(new ArgumentNullException(nameof(productions)))
                .ThrowIfAny(prod => prod is null, new ArgumentException($"Invalid production: null"))
                .ToDictionary(prod => prod.Symbol, prod => prod);
        }

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
}
