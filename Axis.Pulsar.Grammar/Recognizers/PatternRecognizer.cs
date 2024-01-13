using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Text;

namespace Axis.Pulsar.Grammar.Recognizers
{
    public class PatternRecognizer: IRecognizer
    {
        private readonly Pattern _rule;

        /// <inheritdoc/>
        public IRule Rule => _rule;

        /// <inheritdoc/>
        public Language.Grammar Grammar { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="symbolName"></param>
        /// <param name="rule"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PatternRecognizer(Pattern rule, Language.Grammar grammar)
        {
            _rule = rule.ThrowIfDefault(_ => new ArgumentException($"Invalid {nameof(rule)}: default"));
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        /// <summary>
        /// 
        /// <para>
        /// Syntax tree structure:
        /// <list type="bullet">
        ///     <item>@Pattern --> _</item>
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
                if (_rule.MatchType is MatchType.Open open)
                {
                    // match each pulled character, and only fail when the number of non-matches is equal to MatchCardinality.MinOccurences
                    var sbuffer = new StringBuilder();
                    var mismatchCount = 0;
                    while (tokenReader.TryNextToken(out var token))
                    {
                        if (!_rule.Regex.IsMatch(sbuffer.Append(token).ToString()))
                        {
                            if (++mismatchCount == open.MaxMismatch)
                                break;
                        }
                        else mismatchCount = 0;
                    }

                    // walk back any mis-matches
                    tokenReader.Back(mismatchCount);
                    sbuffer.RemoveLast(mismatchCount);

                    if (sbuffer.Length == 0 && !open.AllowsEmptyTokens)
                    {
                        result = new FailureResult(
                            position + 1,
                            IReason.Of(_rule.ToString()));
                        tokenReader.Reset(position);
                        return false;
                    }

                    var node = CSTNode.Of(
                        CSTNode.TerminalType.Pattern,
                        _rule.SymbolName,
                        sbuffer.ToString());
                    result = new SuccessResult(position + 1, node);
                    return true;
                }
                else if (_rule.MatchType is MatchType.Closed closed)
                {
                    _ = tokenReader.TryNextTokens(closed.MaxMatch, out var tokens, false);

                    var lim = tokens?.Length ?? 0 - closed.MinMatch;
                    string symbolValue = null;
                    for (int index = 0; index < lim; index++)
                    {
                        var subtokens = tokens[..^index];
                        if (_rule.Regex.IsMatch(new string(subtokens)))
                        {
                            symbolValue = new(subtokens);
                            break;
                        }
                    }

                    //no match at all
                    if (symbolValue == null)
                    {
                        result = new FailureResult(
                            position + 1,
                            IReason.Of(_rule.ToString()));
                        tokenReader.Reset(position);
                        return false;
                    }

                    var node = CSTNode.Of(
                        CSTNode.TerminalType.Pattern,
                        _rule.SymbolName,
                        symbolValue);
                    result = new SuccessResult(position + 1, node);
                    return true;
                }
                else
                {
                    // error
                    result = new ErrorResult(
                        position + 1,
                        new InvalidCastException($"Invaid match-type: {_rule.MatchType}"));
                    tokenReader.Reset(position);
                    return false;
                }
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

        public virtual IRecognitionResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        public override string ToString() => $"Recognizer({_rule})";
    }
}
