using Axis.Pulsar.Parser.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// 
    /// </summary>
    public interface Result
    {
        /// <summary>
        /// 
        /// </summary>
        public record Success : Result
        {
            public Symbol[] Symbols { get; }

            public Success(params Symbol[] symbols)
                :this((IEnumerable<Symbol>)symbols)
            { }

            public Success(IEnumerable<Symbol> symbols)
            {
                Symbols = symbols?.ToArray() ?? throw new ArgumentNullException(nameof(symbols));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public record PartialRecognition: Result
        {
            public Symbol[] RecognizedSymbols { get; }

            public string ExpectedSymbol { get; }

            public int InputPosition { get; }

            public PartialRecognition(
                IEnumerable<Symbol> recognizedSymbols,
                string expectedSymbol,
                int inputPosition)
            {
                RecognizedSymbols = recognizedSymbols?.ToArray() ?? throw new ArgumentNullException(nameof(recognizedSymbols));

                ExpectedSymbol = expectedSymbol.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    _ => new ArgumentException($"Invalid {nameof(expectedSymbol)}"));

                InputPosition = inputPosition.ThrowIf(
                    i => i < 0,
                    _ => new ArgumentException($"{nameof(InputPosition)} must be >= 0"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public record FailedRecognition: Result
        {
            public string SymbolName { get; }

            public int InputPosition { get; }

            public FailedRecognition(string symbolName, int inputPosition)
            {
                symbolName = symbolName.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    _ => new ArgumentException($"Invalid {nameof(symbolName)}"));

                InputPosition = inputPosition.ThrowIf(
                    i => i < 0,
                    _ => new ArgumentException($"{nameof(InputPosition)} must be >= 0"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public record Exception: Result
        {
            public System.Exception Error { get; }

            public int InputPosition { get; }

            public Exception(System.Exception error, int inputPosition)
            {
                Error = error ?? throw new ArgumentNullException(nameof(error));

                InputPosition = inputPosition.ThrowIf(
                    i => i < 0,
                    _ => new ArgumentException($"{nameof(InputPosition)} must be >= 0"));
            }
        }
    }
}
