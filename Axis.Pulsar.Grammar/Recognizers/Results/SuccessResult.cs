using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.CST;
using System;

namespace Axis.Pulsar.Grammar.Recognizers.Results
{
    /// <summary>
    /// Represents a successful recognition
    /// </summary>
    public record SuccessResult : IRecognitionResult
    {
        /// <inheritdoc/>
        public int Position { get; }

        /// <summary>
        /// The matched symbol/rule
        /// </summary>
        public CSTNode Symbol { get; }

        /// <summary>
        /// Indicates if this result represents the parsing of an optional symbol -
        /// i.e the token is absent in the input, but is reported as a success because it is optional.
        /// </summary>
        public bool IsOptionalRecognition => Symbol.NodeCount() == 0;

        public SuccessResult(
            int inputPosition,
            CSTNode symbol)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Position = inputPosition.ThrowIf(
                Extensions.IsNegative,
                _ => new ArgumentException($"Invalid {nameof(inputPosition)}"));
        }
    }
}
