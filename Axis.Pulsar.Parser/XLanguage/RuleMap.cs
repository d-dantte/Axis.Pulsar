using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.XLanguage
{
    public class RuleMap
    {
        private readonly Dictionary<string, Rule> _ruleMap = new();


        public Rule this[RuleRef @ref]
        {
            get
            {
                if (@ref == null)
                    throw new ArgumentNullException(nameof(@ref));

                else return _ruleMap[@ref.Symbol];
            }
        }

        public RuleMap AddRule(string symbol, Rule rule)
        {
            _ruleMap[symbol] = rule;
            return this;
        }

        public bool TryAddRule(string symbol, Rule rule)
        {
            if (_ruleMap.ContainsKey(symbol))
                return false;

            _ruleMap[symbol] = rule;
            return true;
        }
    }
}
