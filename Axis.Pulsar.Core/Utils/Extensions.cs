using Axis.Misc.Pulsar.Utils;

namespace Axis.Pulsar.Core.Utils
{
    internal static class Extensions
    {
        /// <summary>
        /// Calls <see cref="Tokens.CombineWith(Tokens)"/> on each consecutive items in the given sequence.
        /// </summary>
        /// <param name="segmentTokens">A sequence of consecutively related <see cref="Tokens"/> instances</param>
        /// <returns>A new instance that is a combination of all the given consecutive instances</returns>
        internal static Tokens Combine(this IEnumerable<Tokens> segmentTokens)
        {
            ArgumentNullException.ThrowIfNull(segmentTokens);

            return segmentTokens.Aggregate(
                Tokens.Empty,
                (segmentToken, next) => segmentToken.CombineWith(next));
        }
    }
}
