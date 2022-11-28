namespace Axis.Pulsar.Grammar.Language.Rules.CustomTerminals
{
    /// <summary>
    /// A custom terminal represents a point of customizable pluggability for the Pulsar library.
    /// <para>
    /// It is an <see cref="IAtomicRule"/> that presents the opportunity to customize token processing logic.
    /// Each language represents it as a "special symbol", rather than a literal as in the case with
    /// <see cref="Literal"/> and <see cref="Pattern"/>.
    /// </para>
    /// <para>The symbol name for <see cref="ICustomTerminal"/>s
    /// are set by the implementer, and presented to the grammar builder. For language importers, the
    /// <see cref="ICustomTerminal"/>s are passed to the importers, and can then be used in the language,
    /// in the language's own representation of the custom terminals.
    /// </para>
    /// </summary>
    public interface ICustomTerminal: IAtomicRule
    {
    }
}
