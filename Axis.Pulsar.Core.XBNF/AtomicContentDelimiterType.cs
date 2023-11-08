namespace Axis.Pulsar.Core.XBNF
{
    public enum AtomicContentDelimiterType
    {
        None,

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
}
