using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF
{
    public enum AtomicContentDelimiterType
    {
        /// <summary>
        /// '
        /// </summary>
        Quote,

        /// <summary>
        /// "
        /// </summary>
        DoubleQuote,

        /// <summary>
        /// `
        /// </summary>
        Grave,

        /// <summary>
        /// /
        /// </summary>
        Sol,

        /// <summary>
        /// \
        /// </summary>
        BackSol,

        /// <summary>
        /// |
        /// </summary>
        VerticalBar
    }

    /// <summary>
    /// Factories that implement this interface can be represented in content-form. Content form is a short-hand representation
    /// of atomic rules where the rule is defined as a string enclosed in one of the defined <see cref="AtomicContentDelimiterType"/>s,
    /// and optional arguments can be tacked on to it.
    /// <para/>
    /// 
    /// Arguments are optional, and may be represented with names, or without names, but not a mix of both forms. The exact
    /// textual syntax for this is given below:
    /// <para/>
    /// 
    /// 
    /// </summary>
    public interface IDelimitedContentAtomicRuleFactory<TFactory> : IAtomicRuleFactory
    where TFactory : IDelimitedContentAtomicRuleFactory<TFactory>
    {
        AtomicContentDelimiterType ContentDelimiterType { get; }

        abstract static ImmutableArray<Argument> ContentArgumentList { get; }
    }
}
