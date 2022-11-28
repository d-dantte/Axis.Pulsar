using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Grammar.Language
{
    public class Grammar
    {
        private readonly Dictionary<string, ProductionRule> _ruleMap = new();
        private readonly Dictionary<string, IRecognizer> _recognizers = new();

        #region Properties
        /// <summary>
        /// Gets the root symbol for this grammar
        /// </summary>
        public virtual string RootSymbol { get; internal set; }

        /// <summary>
        /// Get all the available productions, in no particular order
        /// </summary>
        public virtual Production[] Productions
            => _ruleMap
                .Select(kvp => new Production(kvp.Key, kvp.Value))
                .ToArray();

        /// <summary>
        /// Get all of the symbols in the grammar, in no particular order
        /// </summary>
        public virtual string[] Symbols => _ruleMap.Keys.ToArray();

        /// <summary>
        /// Get the count of productions in this grammar
        /// </summary>
        public virtual int ProductionCount => _ruleMap.Count;
        #endregion

        internal protected Grammar()
        { }

        #region Public API
        /// <summary>
        /// Returns the recognizer for the root symbol.
        /// </summary>
        public virtual IRecognizer RootRecognizer() => _recognizers[RootSymbol];

        /// <summary>
        /// Get the recognizer for the symbol specified in the argument,  Throws <see cref="SymbolNotFoundException"/> if the symbol is absent.
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        public virtual IRecognizer GetRecognizer(string symbolName) => _recognizers[symbolName];

        /// <summary>
        /// Get the production for the root symbol.
        /// </summary>
        public virtual Production RootProduction() 
            => new Production(
                RootSymbol,
                _ruleMap[RootSymbol]);

        /// <summary>
        /// Returns the production for the given symbol.  Throws <see cref="SymbolNotFoundException"/> if the root symbol is absent.
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        public virtual Production GetProduction(string symbolName)
            => new Production(
                symbolName,
                _ruleMap[symbolName]);

        /// <summary>
        /// Returns the result of trying to get the production
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        /// <param name="production">The production</param>
        /// <returns>true if the production was found, false otherwise</returns>
        public virtual bool TryGetProduction(string symbolName, out Production production)
        {
            if(_ruleMap.ContainsKey(symbolName))
            {
                production = new Production(
                    symbolName,
                    _ruleMap[symbolName]);
                return true;
            }

            production = default;
            return false;
        }

        /// <summary>
        /// Indicates the a production exists for the given symbol name.
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        public virtual bool HasProduction(string symbolName) => _ruleMap.ContainsKey(symbolName);
        #endregion

        #region Internal API
        internal Grammar AddProduction(Production production)
        {
            _ruleMap[production.Symbol] = production.Rule;
            _recognizers[production.Symbol] = production.Rule.ToRecognizer(this);

            return this;
        }

        internal bool TryAddProduction(Production production)
        {
            return _ruleMap.TryAdd(production.Symbol, production.Rule)
                && _recognizers.TryAdd(production.Symbol, production.Rule.ToRecognizer(this));
        }
        #endregion
    }
}
