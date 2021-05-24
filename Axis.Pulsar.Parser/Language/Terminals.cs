using System;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Language
{
    /// <summary>
    /// Represnts a terminal rule
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public interface ITerminal<TValue> : IRule
    {
        /// <summary>
        /// The terminal value
        /// </summary>
        TValue Value { get; }
    }

    /// <summary>
    /// Terminal that presents an exact string to be matched as it's value
    /// </summary>
    public class StringTerminal : ITerminal<string>
    {
        public string Value { get; }

        public bool IsRoot { get; }

        public bool IsCaseSensitive { get; }

        public string Name { get; }

        public StringTerminal(string name, bool isRoot, string value, bool isCaseSensitive = true)
        {
            IsRoot = isRoot;
            IsCaseSensitive = isCaseSensitive;
            Name = name.ThrowIf(string.IsNullOrWhiteSpace, n => new ArgumentException("Invalid rule name"));
            Value = value.ThrowIf(string.IsNullOrEmpty, n => new ArgumentException("Invalid rule value"));
        }

        public StringTerminal(string name, string value, bool isCaseSensitive = true)
            : this(name, false, value, isCaseSensitive)
        { }
    }

    /// <summary>
    /// Terminal that presents a regular expression to be matched as it's value
    /// </summary>
    public class PatternTerminal : ITerminal<Regex>
    {
        public Regex Value { get; }

        public bool IsRoot { get; }

        public string Name { get; }

        public PatternInfo Info { get; }

        public PatternTerminal(string name, bool isRoot, Regex value, PatternInfo info)
        {
            IsRoot = isRoot;
            Name = name.ThrowIf(string.IsNullOrWhiteSpace, n => new ArgumentException("Invalid rule name"));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Info = info.ThrowIf(PatternInfo.IsDefault, n => new ArgumentException("Invalid pattern info"));
        }

        public PatternTerminal(string name, Regex value, PatternInfo info)
            : this(name, false, value, info)
        { }


        /// <summary>
        /// Represents the length of substrings that the pattern terminal attempts to match.
        /// NOTE: all values supplied into this struct are converted to their absolute values - negatives aren't allowed
        /// </summary>
        public struct PatternInfo
        {
            public int MinLength { get; }
            public int? MaxLength { get; }

            public PatternInfo(int minLength, int? maxLength = null)
            {
                MinLength = Math.Abs(minLength);
                MaxLength = maxLength.HasValue ? Math.Abs(maxLength.Value) : null;

                Validate();
            }

            private void Validate()
            {
                if (MinLength == 0 || MaxLength == 0)
                    throw new Exception("Neither length should be 0");

                if (MinLength > MaxLength)
                    throw new Exception("Min length must be less than or equal to Max length");
            }

            public static bool IsDefault(PatternInfo info) => info.Equals(default(PatternInfo));
        }
    }
}
