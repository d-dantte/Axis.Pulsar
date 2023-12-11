using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar.Errors
{
    /// <summary>
    /// 
    /// </summary>
    public class GroupRecognitionError__ : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public IRecognitionError__ Cause => (InnerException as IRecognitionError__)!;

        /// <summary>
        /// 
        /// </summary>
        public int ElementCount { get; }

        public GroupRecognitionError__(
            IRecognitionError__ cause,
            int elementCount)
            : base("Group Recognition Error", (Exception) cause)
        {
            _ = cause
                .ThrowIfNull(() => new ArgumentNullException(nameof(cause)))
                .ThrowIf(
                    c => c is not FailedRecognitionError && c is not PartialRecognitionError,
                    _ => new ArgumentException($"Invalid cause type: '{cause.GetType()}'"));

            ElementCount = elementCount.ThrowIf(
                i => i < 0,
                _ => new ArgumentOutOfRangeException(nameof(elementCount)));
        }

        public static GroupRecognitionError__ Of(
            IRecognitionError__ cause,
            int elementCount)
            => new(cause, elementCount);

        public static GroupRecognitionError__ Of(
            IRecognitionError__ cause)
            => new(cause, 0);
    }
}
