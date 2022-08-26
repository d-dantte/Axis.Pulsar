using Axis.Pulsar.Parser.CST;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Recognizers;
using System;

namespace Axis.Pulsar.Parser.Parsers
{
    /// <summary>
    /// Parser for <see cref="Grammar.ISymbolExpression"/>
    /// </summary>
    public class ExpressionParser : IParser
    {
        private readonly IRecognizer _recognizer;

        /// <inheritdoc/>
        public string SymbolName { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold { get; }

        public ExpressionParser(string symbolName, int? recognitionThreshold, IRecognizer recognizer)
        {
            RecognitionThreshold = recognitionThreshold;
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));
            SymbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("Invalid name"));
        }

        /// <inheritdoc/>
        public bool TryParse(BufferedTokenReader tokenReader, out IResult result)
        {
            var position = tokenReader.Position;
            try
            {
                var recognizerResult = _recognizer.Recognize(tokenReader);
                result = recognizerResult switch
                {
                    Recognizers.IResult.Success success => new IResult.Success(
                        ICSTNode.Of(
                            SymbolName,
                            success.Symbols)),

                    Recognizers.IResult.Exception exception => new IResult.Exception(
                        exception.Error,
                        exception.InputPosition),

                    Recognizers.IResult.FailedRecognition failed =>
                        failed.RecognitionCount >= RecognitionThreshold
                            ? new IResult.PartialRecognition(
                                failed.RecognitionCount,
                                SymbolName,
                                failed.InputPosition,
                                failed.Reason)

                            : new IResult.FailedRecognition(
                                SymbolName,
                                failed.InputPosition,
                                failed.Reason),

                    _ => tokenReader
                        .Reset(position)
                        .Map(_ => 
                            new IResult.Exception(
                                inputPosition: position + 1,
                                error: new InvalidOperationException($"Invalid result type: {recognizerResult?.GetType()}")))
                };

                return result is IResult.Success;
            }
            catch(Exception ex)
            {
                _ = tokenReader.Reset(position);
                result = new IResult.Exception(ex, position + 1);
                return false;
            }
        }

        /// <inheritdoc/>
        public IResult Parse(BufferedTokenReader tokenReader)
        {
            _ = TryParse(tokenReader, out var result);
            return result;
        }

        public override string ToString() => _recognizer.ToString();
    }
}
