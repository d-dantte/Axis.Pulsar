using Axis.Pulsar.Parser.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// 
        /// </summary>
        public record Success : IResult
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
        public record FailedRecognition: IResult
        {
            public Symbol[] RecognizedSymbols { get; }

            public Symbol ExpectedSymbol { get; }

            public int InputPosition { get; }

            public FailedRecognition Cause { get; }

            public FailedRecognition(
                IEnumerable<Symbol> recognizedSymbols,
                Symbol expectedSymbol,
                int inputPosition,
                FailedRecognition cause = null)
            {
                RecognizedSymbols = recognizedSymbols?.ToArray() ?? throw new ArgumentNullException(nameof(recognizedSymbols));

                Cause = cause;

                ExpectedSymbol = expectedSymbol ?? throw new ArgumentNullException(nameof(expectedSymbol));

                InputPosition = inputPosition.ThrowIf(
                    i => i < 0,
                    _ => new ArgumentException($"{nameof(InputPosition)} must be >= 0"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public record Exception: IResult
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
