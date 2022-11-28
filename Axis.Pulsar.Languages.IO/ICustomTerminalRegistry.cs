using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using System;

namespace Axis.Pulsar.Languages
{
    public interface ICustomTerminalRegistry
    {
        #region Custom Terminal API
        /// <summary>
        /// Registers a <see cref="ICustomTerminal"/> for the given symbol. Duplicates raise exceptions.
        /// </summary>
        /// <param name="symbolName">the symbol name of the terminal</param>
        /// <exception cref="ArgumentNullException">If the argument is null</exception>
        /// <exception cref="ArgumentException">If the a terminal with the same symbol has already been registered.</exception>
        ICustomTerminalRegistry RegisterTerminal(ICustomTerminal validator);

        /// <summary>
        /// Registers a <see cref="ICustomTerminal"/> for the given symbol. Duplicates are ignored.
        /// </summary>
        /// <param name="terminal"></param>
        /// <returns><c>true</c> if the terminal was registered, <c>false</c> if a terminal with the same symbol has already been registered.</returns>
        bool TryRegister(ICustomTerminal terminal);

        /// <summary>
        /// Gets an array of all symbols currently registered.
        /// </summary>
        string[] RegisteredSymbols();

        /// <summary>
        /// Gets the terminal registered with the given symbol, or null.
        /// </summary>
        /// <param name="symbolName">the symbol</param>
        ICustomTerminal RegisteredTerminal(string symbolName);
        #endregion
    }
}
