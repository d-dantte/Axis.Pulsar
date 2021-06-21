using System;

namespace Axis.Pulsar.Parser.XLanguage
{
    public class RuleRef
    {
        public string Symbol { get; }

        public RuleMap RuleMap { get; }

        public Rule Rule => RuleMap[this];

        public RuleRef(string symbolName, RuleMap map)
        {
            Symbol = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                s => new ArgumentException("Invalid symbol name"));

            RuleMap = map ?? throw new ArgumentNullException(nameof(map));
        }
    }
}
