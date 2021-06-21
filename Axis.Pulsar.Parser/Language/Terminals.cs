using Axis.Pulsar.Parser.Utils;
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

            Name = name.ThrowIf(
                string.IsNullOrWhiteSpace,
                n => new ArgumentException("Invalid rule name"));

            Value = value.ThrowIf(
                string.IsNullOrEmpty,
                n => new ArgumentException("Invalid rule value"));
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

        public Cardinality CharacterCardinality { get; }

        public PatternTerminal(string name, bool isRoot, Regex value, Cardinality characterCardinality)
        {
            IsRoot = isRoot;
            Value = value ?? throw new ArgumentNullException(nameof(value));

            Name = name.ThrowIf(
                string.IsNullOrWhiteSpace,
                n => new ArgumentException("Invalid rule name"));

            CharacterCardinality = characterCardinality.ThrowIf(
                Extensions.IsDefault,
                n => new ArgumentException("Invalid pattern cardinality"));
        }

        public PatternTerminal(string name, Regex value, Cardinality characterCardinality)
            : this(name, false, value, characterCardinality)
        { }
    }
}
