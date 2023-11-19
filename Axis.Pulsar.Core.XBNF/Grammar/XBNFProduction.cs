using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF;

public class XBNFProduction : IProduction
{
    /// <summary>
    /// The rule
    /// </summary>
    public IRule Rule { get; }

    /// <summary>
    /// The symbol name for this production
    /// </summary>
    public string Symbol { get; }

    public XBNFProduction(string symbol, IRule rule)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Symbol = symbol.ThrowIfNot(
            IProduction.SymbolPattern.IsMatch,
            new ArgumentException($"Invalid {nameof(symbol)}: {symbol}"));
    }

    public static XBNFProduction Of(string symbol, IRule rule) => new(symbol, rule);

    public bool TryProcessRule(
        TokenReader reader,
        ProductionPath? parentPath,
        ILanguageContext context,
        out IResult<ICSTNode> result)
    {
        var productionPath = parentPath?.Next(Symbol) ?? ProductionPath.Of(Symbol);
        return Rule.TryRecognize(reader, productionPath, context, out result);
    }
}
