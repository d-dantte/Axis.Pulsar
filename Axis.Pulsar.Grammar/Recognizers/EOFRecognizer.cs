using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;

namespace Axis.Pulsar.Grammar.Recognizers
{
    public class EOFRecognizer : IRecognizer
    {
        public const string SYMBOL_NAME = nameof(EOF);

        /// <inheritdoc/>
        public IRule Rule { get; }

        /// <inheritdoc/>
        public Language.Grammar Grammar { get; }

        public EOFRecognizer(EOF eof, Language.Grammar grammar)
        {
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            Rule = eof;
        }

        public virtual IRecognitionResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        /// <summary>
        /// 
        /// <para>
        /// Syntax tree structure:
        /// <list type="bullet">
        ///     <item>EOF --> _</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual bool TryRecognize(BufferedTokenReader tokenReader, out IRecognitionResult result)
        {
            var position = tokenReader.Position;

            try
            {
                if (!tokenReader.TryNextToken(out _))
                {
                    result = new SuccessResult(
                        inputPosition: position + 1, 
                        symbol: CST.CSTNode.Of(
                            CST.CSTNode.TerminalType.Literal,
                            SYMBOL_NAME,
                            ""));
                    return true;
                }
                else
                {
                    result = new FailureResult(
                        inputPosition: position + 1,
                        IReason.Of(SYMBOL_NAME));
                    tokenReader.Reset(position);
                    return false;
                }
            }
            catch (Exception e)
            {
                #region Error
                _ = tokenReader.Reset(position);
                result = new ErrorResult(position + 1, e);
                return false;
                #endregion
            }
        }
    }
}
