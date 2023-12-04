using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System.Text;

namespace Axis.Pulsar.Grammar.Benchmarks.Json
{
    internal class CommentRecognizer : IRecognizer
    {
        public Language.Grammar Grammar { get; }

        public IRule Rule { get; }

        public CommentRecognizer(Language.Grammar grammar, IRule rule)
        {
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public IRecognitionResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        public bool TryRecognize(BufferedTokenReader tokenReader, out IRecognitionResult result)
        {
            var position = tokenReader.Position;

            try
            {
                // read delimiter
                if (!tokenReader.TryNextTokens(2, out var tokens)
                    || tokens[0] != '/' || tokens[1] != '/')
                {
                    result = new FailureResult(
                        position + 1,
                        IReason.Of(Rule.SymbolName));
                    tokenReader.Reset(position);
                    return false;
                }

                var sbuffer = new StringBuilder();
                while (tokenReader.TryNextToken(out var @char))
                {
                    if (IsEndOfLine(@char))
                    {
                        tokenReader.Back();
                        break;
                    }
                    sbuffer.Append(@char);
                }

                result = new SuccessResult(position + 1, CSTNode.Of(Rule.SymbolName, sbuffer.ToString()));
                return true;
            }
            catch (Exception ex)
            {
                _ = tokenReader.Reset(position);
                result = new ErrorResult(position + 1, ex);
                return false;
            }
        }

        private bool IsEndOfLine(char c)
        {
            return c == '\n' || c == '\r';
        }
    }
}
