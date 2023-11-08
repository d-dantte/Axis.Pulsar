using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Core.XBNF
{
    /// <summary>
    /// 
    /// </summary>
    public class AtomicRuleDefinition
    {
        public string Symbol { get; }

        public IAtomicRuleFactory Factory { get; }

        public AtomicContentDelimiterType ContentDelimiterType { get; }

        public AtomicRuleDefinition(
            string symbol,
            AtomicContentDelimiterType contentDelimiterType,
            IAtomicRuleFactory factory)
        {
            ContentDelimiterType = contentDelimiterType;
            Factory = factory.ThrowIfNull(new ArgumentNullException(nameof(factory)));
            Symbol = symbol.ThrowIfNot(
                Production.SymbolPattern.IsMatch,
                new FormatException($"Invalid symbol format: '{symbol}'"));
        }

        public static AtomicRuleDefinition Of(
            string symbol,
            AtomicContentDelimiterType contentDelimiterType,
            IAtomicRuleFactory factory)
            => new(symbol, contentDelimiterType, factory);
    }
}
