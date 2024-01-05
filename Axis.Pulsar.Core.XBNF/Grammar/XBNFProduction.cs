using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF;

//public class XBNFProduction : IProduction
//{
//    /// <summary>
//    /// The rule
//    /// </summary>
//    public INodeRule Rule { get; }

//    /// <summary>
//    /// The symbol name for this production
//    /// </summary>
//    public string Symbol { get; }

//    public XBNFProduction(string symbol, INodeRule rule)
//    {
//        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
//        Symbol = symbol.ThrowIfNot(
//            IProduction.SymbolPattern.IsMatch,
//            _ => new ArgumentException($"Invalid {nameof(symbol)}: {symbol}"));
//    }

//    public static XBNFProduction Of(string symbol, INodeRule rule) => new(symbol, rule);

//    public bool TryProcessRule(
//        TokenReader reader,
//        ProductionPath? parentPath,
//        ILanguageContext context,
//        out IRecognitionResult<ICSTNode> result)
//    {
//        var productionPath = parentPath?.Next(Symbol) ?? ProductionPath.Of(Symbol);

//        if (!Rule.TryRecognize(reader, productionPath, context, out result))
//            return false;

//        if (!context.ProductionValidators.TryGetValue(Symbol, out var validator))
//            return true;

//        result = result.WithResult(node => validator.Validate(productionPath, context, node));
//        return result.IsDataResult();
//    }
//}
