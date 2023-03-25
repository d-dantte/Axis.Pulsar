using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Language.Rules;
using System;
using System.Linq;

namespace Axis.Pulsar.Grammar.Recognizers.Results
{
    /// <summary>
    /// Reason for a failure
    /// </summary>
    public interface IReason
    {
        #region Of
        /// <summary>
        /// Creates an instance of the <see cref="TokenMisMatch"/> reason
        /// </summary>
        /// <param name="expectedTokens">The expected tokens/pattern</param>
        public static IReason Of(string expectedTokens) => new TokenMisMatch(expectedTokens);

        /// <summary>
        /// Creates an instance of the <see cref="AggregationFailure"/> reason
        /// </summary>
        /// <param name="failureReason">The failure reason, if one exists</param>
        /// <param name="passingSymbols">The successfully recognized nodes</param>
        public static IReason Of(
            IReason failureReason,
            params CST.CSTNode[] passingSymbols)
            => new AggregationFailure(failureReason, passingSymbols);

        public static IReason Of(ProductionValidationResult.Error validationError) => new ValidationFailure(validationError);
        #endregion

        /// <summary>
        /// Represents cases where <see cref="Language.IAtomicRule"/> fails to recognize
        /// </summary>
        public record TokenMisMatch: IReason
        {
            /// <summary>
            /// The expected token/pattern
            /// </summary>
            public string ExpectedTokens { get; }

            public TokenMisMatch(string expectedTokens)
            {
                ExpectedTokens = expectedTokens.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    new ArgumentException($"Invalid {nameof(expectedTokens)}"));
            }

            public static implicit operator TokenMisMatch(string expectedTokens) => new(expectedTokens);
        }

        /// <summary>
        /// Represents cases where a sequence or repetition of rule recognition operations
        /// fails because a threshold was not reached.
        /// </summary>
        public record AggregationFailure: IReason
        {
            /// <summary>
            /// The reason for this aggregation failure. This is never null
            /// </summary>
            public IReason FailureReason { get; }

            /// <summary>
            /// The symbols that were successfully recognized
            /// </summary>
            public CST.CSTNode[] PassingSymbols { get; }

            /// <summary>
            /// Count of the successful recognitions
            /// </summary>
            public int AggregationCount => PassingSymbols.Length;

            public AggregationFailure(IReason failureReason, params CST.CSTNode[] passingSymbols)
            {
                FailureReason = failureReason 
                    ?? throw new ArgumentNullException(nameof(failureReason));

                PassingSymbols = passingSymbols
                    ?? throw new ArgumentNullException(nameof(passingSymbols));
            }
        }

        /// <summary>
        /// Represents a failure in production validation
        /// </summary>
        public record ValidationFailure: IReason
        {
            public string ErrorMessage { get; }

            public ValidationFailure(string errorMessage)
            {
                ErrorMessage = errorMessage;
            }

            public ValidationFailure(ProductionValidationResult.Error error)
            {
                ErrorMessage = error.ErrorMessages.Map(msgs => string.Join(Environment.NewLine, msgs));
            }
        }
    }
}
