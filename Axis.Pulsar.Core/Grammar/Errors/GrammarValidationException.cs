namespace Axis.Pulsar.Core.Grammar.Errors
{
    public class GrammarValidationException: Exception
    {
        /// <summary>
        /// The validation result
        /// </summary>
        public GrammarValidator.ValidationResult ValidationResult { get; }

        public GrammarValidationException(
            GrammarValidator.ValidationResult validationResult)
        {
            ValidationResult =
                validationResult
                ?? throw new ArgumentNullException(nameof(validationResult));
        }
    }
}
