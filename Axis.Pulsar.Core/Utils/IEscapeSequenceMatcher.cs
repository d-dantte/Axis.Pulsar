namespace Axis.Pulsar.Core.Utils;

/// <summary>
/// Matches tokens against the escape sequence.
/// <para>
/// An escape sequence comprises 2 parts: <c>{delimiter}{argument}</c>. e.g: <c>{\}{n}</c>, <c>{\u}{0A2E}</c>, <c>{&amp;}{lt;}</c>.
/// </para>
/// <para>
/// Escape matchers are grouped into sets by their <see cref="IEscapeSequenceMatcher.EscapeDelimiter"/> property, meaning matchers with
/// duplicate escape delimiters cannot be used together in a <see cref="DelimitedString"/>. The recognition algorithm scans through
/// the matchers based on the length of their "Delimiters", finding which metchers delimiter matches with incoming tokens from the
/// <see cref="BufferedTokenReader"/>, then calling <see cref="IEscapeSequenceMatcher.TryMatch(BufferedTokenReader, out char[])"/>
/// to match the escape arguments
/// </para>
/// <para>
/// A classic solution for this is to combine both into another matcher, as is done with the
/// <see cref="BSolGeneralEscapeMatcher"/>.
/// </para>
/// </summary>
public interface IEscapeSequenceMatcher
{
    /// <summary>
    /// The escape delimiter.
    /// </summary>
    public string EscapeDelimiter { get; }

    /// <summary>
    /// Attempts to match the escape-argument.
    /// If matching fails, this method MUST reset the reader to the position before it started reading.
    /// </summary>
    /// <param name="reader">the token reader</param>
    /// <param name="tokens">returns the matched arguments if sucessful, or the unmatched tokens</param>
    /// <returns>true if matched, otherwise false</returns>
    bool TryMatchEscapeArgument(TokenReader reader, out Tokens tokens);
}
