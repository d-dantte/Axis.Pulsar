using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Errors
{
    /// <summary>
    /// 
    /// </summary>
    public class GroupRecognitionError : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public IRecognitionError Cause => (InnerException as IRecognitionError)!;

        /// <summary>
        /// 
        /// </summary>
        public int ElementCount { get; }

        public GroupRecognitionError(
            IRecognitionError cause,
            int elementCount)
            : base("Group Recognition Error", (Exception) cause)
        {
            _ = cause
                .ThrowIfNull(new ArgumentNullException(nameof(cause)))
                .ThrowIf(
                    c => c is not FailedRecognitionError && c is not PartialRecognitionError,
                    new ArgumentException($"Invalid cause type: '{cause.GetType()}'"));

            ElementCount = elementCount.ThrowIf(
                i => i < 0,
                new ArgumentOutOfRangeException(nameof(elementCount)));
        }

        public static GroupRecognitionError Of(
            IRecognitionError cause,
            int elementCount)
            => new(cause, elementCount);

        public static GroupRecognitionError Of(
            IRecognitionError cause)
            => new(cause, 0);
    }
}
