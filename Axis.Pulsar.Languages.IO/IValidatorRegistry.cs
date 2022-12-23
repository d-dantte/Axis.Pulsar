using Axis.Pulsar.Grammar.Language.Rules;
using System;

namespace Axis.Pulsar.Languages
{
    public interface IValidatorRegistry
    {
        #region Validator API
        /// <summary>
        /// Registers a validator for the given symbol. Duplicates are overriden
        /// </summary>
        /// <param name="symbolName">the production symbol</param>
        /// <param name="validator">the validator</param>
        /// <exception cref="ArgumentNullException">If either of the arguments is null</exception>
        /// <exception cref="ArgumentException">If the <paramref name="symbolName"/> does not fit the pattern: <see cref="SymbolHelper.SymbolPattern"/></exception>
        IValidatorRegistry RegisterValidator(string symbolName, IProductionValidator validator);

        /// <summary>
        /// Gets an array of all symbols currently having validators registered for them.
        /// </summary>
        string[] RegisteredValidatorSymbols();

        /// <summary>
        /// Gets the validator registered for the given symbol, or null.
        /// </summary>
        /// <param name="symbolName">the symbol</param>
        IProductionValidator RegisteredValidator(string symbolName);
        #endregion
    }
}
