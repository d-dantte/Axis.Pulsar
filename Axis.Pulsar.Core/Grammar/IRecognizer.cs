using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IRecognizer<TResult>
    {
        /// <summary>
        /// Attempt to recognize tokens according to the logic represented by the implementing rule.
        /// <para/>
        /// Note: All failed attempts to recognize MUST reset the <paramref name="reader"/>'s position so it remains the same prior to calling the method.
        /// </summary>
        /// <param name="reader">the reader from which tokens are read</param>
        /// <param name="symbolPath">the logical symbol-path of the parent rule, or null if this is the root rule</param>
        /// <param name="result">the result of the recognition</param>
        /// <returns>True if this rule successfully recognized tokens from the <paramref name="reader"/>, false otherwise</returns>
        bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out TResult result);
    }

    public static class RecognizerExtensions
    {
        public static TResult Recognize<TResult>(this
            IRecognizer<TResult> recognizer,
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context)
        {
            _ = recognizer.TryRecognize(reader, symbolPath, context, out var result);
            return result;
        }
    }
}
