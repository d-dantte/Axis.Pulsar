using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.XBNF;

public class XBNFGrammar: IGrammar
{
        private readonly Dictionary<string, IProduction> _productions;
        private readonly string _root;

        public string Root => _root;

        public IEnumerable<string> ProductionSymbols => _productions.Keys;

        internal XBNFGrammar(string root, IEnumerable<IProduction> productions)
        {
            _root = root.ThrowIf(
                string.IsNullOrWhiteSpace,
                new ArgumentException($"Invalid {nameof(root)}: '{root}'"));

            _productions = productions
                .ThrowIfNull(new ArgumentNullException(nameof(productions)))
                .ThrowIfAny(prod => prod is null, new ArgumentException($"Invalid production: null"))
                .ToDictionary(prod => prod.Symbol, prod => prod);

            ValidateGrammar();
        }

        public static IGrammar Of(
            string root,
            IEnumerable<IProduction> productions)
            => new XBNFGrammar(root, productions);

        public bool ContainsProduction(string symbolName) => _productions.ContainsKey(symbolName);

        public IProduction GetProduction(string name) => this[name];

        public int ProductionCount => _productions.Count;

        public IProduction this[string name] => _productions[name];

        public bool TryGetProduction(string name, out IProduction? production)
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
        /// Valiate that all rules are reachable
        /// </summary>
        private void ValidateGrammar()
        {
            throw new NotImplementedException();
        }
}
