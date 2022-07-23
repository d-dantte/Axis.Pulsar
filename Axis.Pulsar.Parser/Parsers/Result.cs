using Axis.Pulsar.Parser.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Parsers
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
            public Symbol Symbol { get; }

            public Success(Symbol symbol)
            {
                Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public record PartialRecognition: IResult
        {
            public Symbol PartialSymbol { get; }

            public string ExpectedSymbol { get; }

            public int InputPosition { get; }

            public PartialRecognition(
                Symbol partialSymbol,
                string expectedSymbol,
                int inputPosition)
            {
                PartialSymbol = partialSymbol ?? throw new ArgumentNullException(nameof(partialSymbol));

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
        public record FailedRecognition: IResult
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
