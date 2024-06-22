namespace Axis.Pulsar.Core.Grammar.Errors
{
    public class GrammarValidationException: Exception
    {
        /// <summary>
        /// The validation result
        /// </summary>
        public GrammarValidator__old.ValidationResult ValidationResult { get; }

        public GrammarValidationException(
            GrammarValidator__old.ValidationResult validationResult)
        {
            ValidationResult =
                validationResult
                ?? throw new ArgumentNullException(nameof(validationResult));
        }
    }
}
