using Axis.Pulsar.Grammar.CST;
using System;
using System.Linq;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    public interface IProductionValidator
    {
        ProductionValidationResult ValidateCSTNode(ProductionRule rule, CSTNode node);
    }

    public interface ProductionValidationResult
    {
        /// <summary>
        /// Default success result
        /// </summary>
        public static Success SuccessResult { get; } = new Success();

        /// <summary>
        /// Default error result
        /// </summary>
        public static Error ErrorResult { get; } = new Error();

        public record Success: ProductionValidationResult
        {
        }

        public record Error: ProductionValidationResult
        {
            private readonly string[] _errorMessages;

            public string[] ErrorMessages => _errorMessages.ToArray();

            public Error(params string[] errorMessages)
            {
                _errorMessages = errorMessages?.ToArray() ?? throw new ArgumentNullException(nameof(errorMessages));
            }
        }
    }
}
