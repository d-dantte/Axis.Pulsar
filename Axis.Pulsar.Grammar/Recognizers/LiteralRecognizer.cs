using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;

namespace Axis.Pulsar.Grammar.Recognizers
{
    public class LiteralRecognizer : IRecognizer
    {
        private readonly Literal _rule;

        /// <inheritdoc/>
        public IRule Rule => _rule;

        /// <inheritdoc/>
        public Language.Grammar Grammar { get; }

        public LiteralRecognizer(Literal literalRule, Language.Grammar grammar)
        {
            _rule = literalRule.ThrowIfDefault(new ArgumentException(nameof(literalRule)));
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        public override string ToString() => $"Recognizer({_rule})";

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
        ///     <item>@Literal --> _</item>
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
                if (tokenReader.TryNextTokens(_rule.Value.Length, out var tokens)
                    && _rule.Value.Equals(
                        new string(tokens),
                        _rule.IsCaseSensitive
                            ? StringComparison.InvariantCulture
                            : StringComparison.InvariantCultureIgnoreCase))
                {
                    result = new SuccessResult(
                        symbol: CSTNode.Of(
                            CSTNode.TerminalType.Literal,
                            _rule.SymbolName,
                            new string(tokens)),
                        inputPosition: position + 1);

                    return true;
                }

                result = new FailureResult(
                    position + 1,
                    IReason.Of(_rule.ToString()));
                tokenReader.Reset(position);

                return false;
            }
            catch (Exception ex)
            {
                #region Error
                _ = tokenReader.Reset(position);
                result = new ErrorResult(position + 1, ex);
                return false;
                #endregion
            }
        }
    }
}
