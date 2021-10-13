using Axis.Pulsar.Parser.Utils;
using System;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// Rename this to symbol?
    /// </summary>
    public class RuleRef : Rule
    {
        private RuleMap _ruleMap;

        /// <summary>
        /// 
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// 
        /// </summary>
        public Rule Rule => _ruleMap[Symbol];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbolName"></param>
        /// <param name="map"></param>
        /// <param name="cardinality"></param>
        public RuleRef(string symbolName, Cardinality cardinality = default)
            : base(cardinality)
        {
            Symbol = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                s => new ArgumentException("Invalid symbol name"));
        }

        internal RuleRef SetRuleMap(RuleMap map)
        {
            _ruleMap = map ?? throw new ArgumentNullException(nameof(map));

            return this;
        }
    }
}
