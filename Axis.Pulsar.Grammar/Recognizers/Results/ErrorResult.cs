using Axis.Luna.Extensions;
using System;

namespace Axis.Pulsar.Grammar.Recognizers.Results
{
    /// <summary>
    /// Represents a fatal error - an exception thrown during the recognition process
    /// </summary>
    public record ErrorResult : IRecognitionResult
    {
        /// <inheritdoc/>
        public int Position { get; }

        /// <summary>
        /// The error that was raised.
        /// </summary>
        public Exception Exception { get; }

        public ErrorResult(
            int inputPosition,
            Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Position = inputPosition.ThrowIf(
                Extensions.IsNegative,
                new ArgumentException($"Invalid {nameof(inputPosition)}"));
        }
    }
}
