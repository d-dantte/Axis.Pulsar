using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    /// <summary>
    /// Recognizes a series of character ranges that represents valid or invalid characters
    /// </summary>
    public class CharacterRanges : IAtomicRule
    {
        public string Id { get; }

        /// <summary>
        /// List of char ranges to include
        /// </summary>
        public ImmutableArray<CharRange> IncludeList { get; }

        /// <summary>
        /// List of char ranges to exclude
        /// </summary>
        public ImmutableArray<CharRange> ExcludeList { get; }

        /// <summary>
        /// Use IEnumerables
        /// </summary>
        /// <param name="includes"></param>
        /// <param name="excludes"></param>
        public CharacterRanges(
            string id,
            IEnumerable<CharRange> includes,
            IEnumerable<CharRange> excludes)
        {
            ArgumentNullException.ThrowIfNull(includes);
            ArgumentNullException.ThrowIfNull(excludes);

            IncludeList = includes
                .ApplyTo(CharRange.NormalizeRanges)
                .ToImmutableArray();

            ExcludeList = excludes
                .ApplyTo(CharRange.NormalizeRanges)
                .ToImmutableArray();

            Id = id.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid atomic rule {nameof(id)}: '{id}'"));
        }

        public static CharacterRanges Of(
            string id,
            IEnumerable<CharRange> includes,
            IEnumerable<CharRange> excludes)
            => new(id, includes, excludes);
        public static CharacterRanges Of(
            string id,
            params CharRange[] includes)
            => new(id, includes, Array.Empty<CharRange>());

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IRecognitionResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(productionPath);

            var position = reader.Position;
            var charRangePath = productionPath.Next(Id);

            if (!reader.TryGetToken(out var token)
                || ExcludeList.Any(range => range.Contains(token[0]))
                || !IncludeList.Any(range => range.Contains(token[0])))
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(charRangePath, position)
                    .ApplyTo(RecognitionResult.Of<ICSTNode, FailedRecognitionError>);
                return false;
            }

            result = ICSTNode
                .Of(charRangePath.Name, token)
                .ApplyTo(RecognitionResult.Of);
            return true;
        }
        
        /// <summary>
        /// Accepts a comma-separated list of char ranges, and parses them into an array of <see cref="CharRange"/> instances.
        /// <para/>The ordinal values of characters are evaluated based on the ascii ordinal values.
        /// <code>
        /// e.g. "a-b, c-d, x, y, 1-4"
        /// </code>
        /// </summary>
        /// <param name="rangeArgValue">the range list as a string</param>
        /// <returns>An array of <see cref="CharRange"/> instances</returns>
        public static CharRange[] ParseRanges(string rangeArgValue)
        {
            return rangeArgValue
                .Split(',')
                .Select(range => range
                    .ThrowIf(
                        string.IsNullOrWhiteSpace,
                        _ => new FormatException($"Invalid range list: separate each range by a comma."))
                    .ApplyTo(CharRange.Parse))
                .ApplyTo(CharRange.NormalizeRanges)
                .ToArray();
        }

    }
}
