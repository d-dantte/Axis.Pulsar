using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRule : IRecognizer<NodeRecognitionResult>
    {
    }

    public static class RuleExtensions
    {
        public static NodeRecognitionResult Recognize(this
            IRule rule,
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context)
        {
            _ = rule.TryRecognize(reader, symbolPath, context, out var result);
            return result;
        }
    }
}
