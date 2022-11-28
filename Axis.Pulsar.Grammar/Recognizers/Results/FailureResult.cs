using System;

namespace Axis.Pulsar.Grammar.Recognizers.Results
{
    /// <summary>
    /// Represents a failed recognition
    /// </summary>
    public record FailureResult : IRecognitionResult
    {
        /// <inheritdoc/>
        public int Position { get; }

        /// <summary>
        /// If some inner failure caused the symbol to fail, that result is assigned here.
        /// </summary>
        public IReason Reason { get; }

        public FailureResult(
            int inputPosition,
            IReason reason)
        {
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
            Position = inputPosition.ThrowIf(
                Extensions.IsNegative,
                new ArgumentException($"Invalid {nameof(inputPosition)}"));
        }
    }
}
