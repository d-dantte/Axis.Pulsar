using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Axis.Luna.Common;
using Axis.Luna.Common.StringEscape;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF;

/// <summary>
/// Implementations of this interfaces are charged with producing instances of the <see cref="IAtomicRule"/> interface,
/// given a list of parameters.
/// 
/// Syntax: <para/>
/// <code>
/// @name-form-with-params{param: 'value', param-2: 'value 2', flag-param}
/// @name-form-without-params
/// 
/// 'content form with params'{param: 'value', param-2: 'value 2'}
/// 'content form without params'
/// </code>
/// </summary>
public interface IAtomicRuleFactory
{
    /// <summary>
    /// Special argument recognized by rule factories and the language parser.
    /// </summary>
    public static readonly IArgument Content = default(ContentArgument);

    /// <summary>
    /// Creates a new <see cref="IAtomicRule"/> instance given a list of arguments.
    /// </summary>
    /// <param name="ruleId"></param>
    /// <param name="metadata"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    IAtomicRule NewRule(
        string ruleId,
        LanguageMetadata metadata,
        ImmutableDictionary<IArgument, string> arguments);

    #region Nested Types

    /// <summary>
    /// Atomic Rule argument. Atomic rules take a list of key-value-pair representing parameters
    /// used to configure themselves. <see cref="IArgument"/> instances are used to represent the
    /// "keys" in the pair.
    /// </summary>
    public interface IArgument
    {
        public static readonly Regex RegularArgumentPattern = new(
            "^[a-zA-Z_][a-zA-Z0-9-_]*\\z",
            RegexOptions.Compiled);

        string Key { get; }

        /// <summary>
        /// Creates a <see cref="RegularArgument"/> instance.
        /// </summary>
        /// <param name="key">The argument key</param>
        public static IArgument Of(string key) => new RegularArgument(key);

        /// <summary>
        /// Creates a <see cref="ContentArgument"/> instance.
        /// </summary>
        /// <param name="delimiter">The delimiter</param>
        public static IArgument Of(ContentArgumentDelimiter delimiter) => new ContentArgument(delimiter);
    }

    /// <summary>
    /// Regular argument, where the key adheres to the pattern: <see cref="IArgument.RegularArgumentPattern"/>
    /// </summary>
    public readonly struct RegularArgument :
        IArgument,
        IEquatable<RegularArgument>,
        IDefaultValueProvider<RegularArgument>
    {
        private readonly string _key;

        public string Key => _key;

        public RegularArgument(string key)
        {
            _key = key.ThrowIfNot(
                IArgument.RegularArgumentPattern.IsMatch,
                _ => new ArgumentException($"Invalid argument key: {key}"));
        }

        public static RegularArgument Of(string key) => new(key);

        public static implicit operator RegularArgument(string key) => new(key);

        #region DefaultValueProvider
        public static RegularArgument Default => default;

        public bool IsDefault => default(RegularArgument).Equals(this);
        #endregion

        public override string ToString() => _key;

        public override int GetHashCode() => HashCode.Combine(_key);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is RegularArgument arg && Equals(arg);
        }

        public bool Equals(RegularArgument arg)
        {
            return EqualityComparer<string>.Default.Equals(_key, arg._key);
        }

        public bool Equals(string key)
        {
            return EqualityComparer<string>.Default.Equals(_key, key);
        }

        public static bool operator ==(RegularArgument left, RegularArgument right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RegularArgument left, RegularArgument right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Delimiter enum for content arguments
    /// </summary>
    public enum ContentArgumentDelimiter
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

    /// <summary>
    /// Content Argument - special argument with "content" as the key, and a <see cref="ContentArgumentDelimiter"/>
    /// </summary>
    public readonly struct ContentArgument :
        IArgument,
        IEquatable<ContentArgument>,
        IDefaultValueProvider<ContentArgument>
    {
        public static readonly string KEY = "content";

        public string Key => KEY;

        public ContentArgumentDelimiter Delimiter { get; }

        public ContentArgument(ContentArgumentDelimiter delimiter)
        {
            Delimiter = delimiter.ThrowIfNot(
                Enum.IsDefined,
                _ => new ArgumentOutOfRangeException(nameof(delimiter)));
        }

        public static ContentArgument Of(ContentArgumentDelimiter delimiter) => new(delimiter);

        public static implicit operator ContentArgument(ContentArgumentDelimiter delimiter) => new(delimiter);

        #region DefaultValueProvider
        public static ContentArgument Default => default;

        public bool IsDefault => Delimiter == ContentArgumentDelimiter.None;
        #endregion

        public override string ToString() => KEY;

        public override int GetHashCode() => HashCode.Combine(Delimiter);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is ContentArgument arg && Equals(arg);
        }

        public bool Equals(ContentArgument arg)
        {
            return arg.Delimiter.Equals(Delimiter);
        }

        public bool Equals(char delimiterCharacter)
        {
            return delimiterCharacter.Equals(Delimiter.DelimiterCharacter());
        }

        public static bool operator ==(ContentArgument left, ContentArgument right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentArgument left, ContentArgument right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Comparer used in the <see cref="ImmutableDictionary{TKey, TValue}"/> passed into
    /// the <see cref="IAtomicRuleFactory.NewRule(string, LanguageMetadata, ImmutableDictionary{IArgument, string})"/> method.
    /// </summary>
    public class ArgumentKeyComparer : IEqualityComparer<IArgument>
    {
        public bool Equals(IArgument? x, IArgument? y) =>  (x, y) switch
        {
            (RegularArgument argx, RegularArgument argy) => argx.Equals(argy),
            (ContentArgument, ContentArgument) => true,
            (ContentArgument, RegularArgument argy) => ContentArgument.KEY.Equals(argy.ToString()),
            (RegularArgument argX, ContentArgument) => argX.ToString().Equals(ContentArgument.KEY),
            (null, null) => true,
            _ => false
        };

        public int GetHashCode([DisallowNull] IArgument obj) => obj switch
        {
            RegularArgument arg => arg.ToString().GetHashCode(),
            ContentArgument => ContentArgument.KEY.GetHashCode(),
            null => throw new ArgumentNullException(nameof(obj)),
            _ => throw new InvalidOperationException($"Invalid argument type: {obj.GetType()}")
        };

        public static ArgumentKeyComparer Default { get; } = new ArgumentKeyComparer();
    }

    /// <summary>
    /// Combination of an <see cref="IArgument"/>, and its optional value.
    /// <para/>
    /// Note that the value is a raw copy of whatever is found between the delimiters of the argument; interpretation of this
    /// raw value is left for the implementation of the factory.
    /// </summary>
    public readonly struct Parameter :
        IEquatable<Parameter>,
        IDefaultValueProvider<Parameter>
    {
        public IArgument Argument { get; }

        public string? RawValue { get; }

        public Parameter(
            IArgument argument,
            string? rawValue)
        {
            Argument = argument ?? throw new ArgumentNullException(nameof(argument));
            RawValue = rawValue;
        }

        public static Parameter Of(
            IArgument argument,
            string? value = null)
            => new(argument, value);

        #region DefaultValueProvider
        public static Parameter Default => default;

        public bool IsDefault => RawValue is null && Argument is null;
        #endregion

        public bool Equals(Parameter other)
        {
            return 
                EqualityComparer<IArgument>.Default.Equals(Argument, other.Argument)
                && EqualityComparer<string>.Default.Equals(RawValue, other.RawValue);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Parameter other
                && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(Argument, RawValue);

        public override string ToString()
        {
            return Argument switch
            {
                null => "{}",
                _ => RawValue switch
                {
                    null => $"{{flag: {Argument}}}",
                    _ => $"{{key: {Argument}, value: {RawValue}}}"
                }
            };
        }

        public static bool operator ==(Parameter left, Parameter right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Parameter left, Parameter right)
        {
            return !(left == right);
        }
    }
    #endregion
}
