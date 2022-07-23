using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Recognizers;
using System;

namespace Axis.Pulsar.Parser.Parsers
{
    public class ExpressionParser : IParser
    {
        private readonly IRecognizer _recognizer;

        public string SymbolName { get; }

        public int RecognitionThreshold { get; }

        public ExpressionParser(string symbolName, IRecognizer recognizer)
        {
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));
            SymbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("Invalid name"));
        }

        public bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                if (_recognizer.TryRecognize(tokenReader, out var recognizerResult))
                {
                    result = new(new Syntax.Symbol(
                        SymbolName,
                        recognizerResult.Symbols));

                    return true;
                }
                else
                {
                    result = new(new ParseError(
                        SymbolName,
                        position + 1,
                        recognizerResult.Error));
                    return false;
                }
            }
            catch
            {
                //add relevant information into the parse error
                result = new ParseResult(new ParseError(SymbolName, position + 1));
                tokenReader.Reset(position);
                return false;
            }
        }

        public IResult Parse(BufferedTokenReader tokenReader)
        {
            var position = tokenReader.Position;
            try
            {
                if (_recognizer.Recognize(tokenReader) is Recognizers.IResult.Success success)
                    return new IResult.Success(
                        new Syntax.Symbol(
                            SymbolName,
                            success.Symbols));

                else
                    return new IResult.FailedRecognition(
                        SymbolName,
                        position + 1);
            }
            catch (Exception e)
            {
                //add relevant information into the parse error
                tokenReader.Reset(position);
                return new IResult.Exception(e, position + 1);
            }
        }

        public override string ToString() => _recognizer.ToString();
    }
}
