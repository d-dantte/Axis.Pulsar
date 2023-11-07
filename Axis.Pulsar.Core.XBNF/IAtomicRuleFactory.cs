using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.XBNF;

public interface IAtomicRuleFactory
{
    #region Special Arguments

    /// <summary>
    /// When presented in "content" form, AtomicRules will be passed the content via an argument named "content"
    /// </summary>
    public static readonly Argument ContentArgument = "content";

    /// <summary>
    /// Applied both in "content" and "regular" form, the flags argument is used to pass a finite series of 62 flags
    /// into the AtomicRuleFactory. Each flag is designated a case sensitive alphabet or numeric digit.
    /// </summary>
    public static readonly Argument FlagsArgument = "flags";

    #endregion

    /// <summary>
    /// Creates a new <see cref="IAtomicRule"/> instance given a list of arguments.
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    IAtomicRule NewRule(ImmutableDictionary<Argument, string> arguments);

    #region Nested Types
    public readonly struct Argument:
        IEquatable<Argument>,
        IDefaultValueProvider<Argument>
    {
        internal static readonly Regex ArgumentPattern = new Regex(
            "^[a-zA-Z_][a-zA-Z0-9-_]*\\z",
            RegexOptions.Compiled);

        private readonly string _key;

        public Argument(string key)
        {
            _key = key.ThrowIfNot(
                ArgumentPattern.IsMatch,
                new ArgumentException($"Invalid argument key: {key}"));
        }

        public static Argument Of(string key) => new(key);

        public static implicit operator Argument(string key) => new(key);


        #region DefaultValueProvider
        public static Argument Default => default;

        public bool IsDefault => default(Argument).Equals(this);
        #endregion

        public override string ToString() => _key;

        public override int GetHashCode() => HashCode.Combine(_key);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Argument arg && Equals(arg);
        }

        public bool Equals(Argument arg)
        {
            return EqualityComparer<string>.Default.Equals(_key, arg._key);
        }

        public static bool operator ==(Argument left, Argument right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Argument left, Argument right)
        {
            return !(left == right);
        }
    }
    #endregion
}
