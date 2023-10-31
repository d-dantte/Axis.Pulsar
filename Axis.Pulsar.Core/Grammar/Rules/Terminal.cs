using Axis.Luna.Common;
using Axis.Luna.Common.Results;
using Axis.Luna.Common.Utils;
using Axis.Luna.Extensions;
using Axis.Misc.Pulsar.Utils;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public abstract class Terminal: IAtomicRule
    {
        /// <summary>
        /// Arguments used to build this custom rule instance
        /// </summary>
        public ImmutableDictionary<Argument, object> Arguments { get; }

        public Terminal(params KeyValuePair<Argument, object>[] arguments)
        {
            Arguments = arguments
                .ThrowIfNull(new ArgumentNullException(nameof(arguments)))
                .ThrowIfAny(
                    kvp => kvp.Value is null,
                    kvp => new ArgumentException($"Null argument value found for argument: {kvp.Key}"))
                .ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value);
        }

        public abstract bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            out IResult<ICSTNode> result);


        #region Nested Types

        /// <summary>
        /// Represents an argument name
        /// </summary>
        public readonly struct Argument: IDefaultValueProvider<Argument>
        {
            private static readonly Regex ArgumentNamePattern = new(
                "^[a-z][-_a-z0-9]*\\z",
                RegexOptions.Compiled
                | RegexOptions.IgnoreCase);

            private readonly string _name;

            #region DefaultValueProvider
            public bool IsDefault => _name is null;

            public static Argument Default => default;
            #endregion

            #region Construct
            public Argument(string name)
            {
                ArgumentNullException.ThrowIfNull(name);

                _name = name.ThrowIfNot(
                    ArgumentNamePattern.IsMatch,
                    new FormatException($"Invalid argument name: {name}"));
            }

            public static Argument Of(string name) => new(name);

            public static implicit operator Argument(string name) => new(name);
            #endregion

            public override string ToString() => _name ?? "*";

            public override int GetHashCode() => HashCode.Combine(_name?.GetHashCode() ?? 0);

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is Argument other
                    && Equals(other._name);
            }

            public bool Equals(string arg)
            {
                return EqualityComparer<string>.Default.Equals(_name, arg);
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

    public class Literal : Terminal
    {
        public readonly static Argument Argument_Tokens = Argument.Default;

        public string Tokens => (string) Arguments[Argument.Default];

        public Literal(params KeyValuePair<Argument, object>[] args)
            : base(args)
        {
            if (Arguments.Count != 1)
                throw new ArgumentException($"Argument count must be 1");

            if (!Arguments.ContainsKey(Argument_Tokens))
                throw new ArgumentException($"Invalid argument: {args.First().Key}");
        }

        public static Literal Of(string token)
        {
            return new(ArrayUtil.Of(KeyValuePair.Create(Argument_Tokens, token as object)));
        }

        public override bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;

            if (reader.TryGetTokens(Tokens.Length, true, out var tokens)
                && tokens.Equals(Tokens))
            {
                result = ICSTNode
                    .Of(tokens)
                    .ApplyTo(Result.Of);
                return true;
            }
            else
            {
                reader.Reset(position);
                result = UnrecognizedTokens
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);
                return false;
            }
        }
    }

    public class Pattern : Terminal
    {
        #region Arguments
        /// <summary>
        /// 
        /// </summary>
        public readonly static Argument Argument_Regex = Argument.Default;

        /// <summary>
        /// 
        /// </summary>
        public readonly static Argument Argument_MatchType = Argument.Of("match-type");

        /// <summary>
        /// 
        /// </summary>
        public readonly static Argument Argument_RegexOptions = Argument.Of("options");
        #endregion

        public Regex Regex { get; }

        public IMatchType MatchType { get; }

        public Pattern(params KeyValuePair<Argument, object>[] args)
            : base(args)
        {
            if (Arguments.Count < 1 || Arguments.Count > 2)
                throw new ArgumentException($"Invalid argument count: {Arguments.Count}");

            if (!Arguments.ContainsKey(Argument_Regex))
                throw new ArgumentException($"Regex argument not found");

            Regex = new Regex(
                (string)Arguments[Argument_Regex],
                ToOptions(Arguments[Argument_RegexOptions]));

            MatchType = Arguments.TryGetValue(Argument_MatchType, out var matchType)
                ? ToMatchType(matchType)
                : IMatchType.Open.DefaultMatch;
        }

        public static Pattern Of(
            Regex regex,
            RegexOptions options,
            IMatchType matchType)
        {
            return new(ArrayUtil.Of(
                KeyValuePair.Create(Argument_Regex, regex.ToString()),
                KeyValuePair.Create(Argument_RegexOptions, ToOptionsString(options)))
        }

        public override bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(productionPath);

            result = MatchType switch
            {
                IMatchType.Closed closed => RecognizeClosedPattern(
                    reader,
                    productionPath,
                    Regex,
                    closed),

                IMatchType.Open open => RecognizeOpenPattern(
                    reader,
                    productionPath,
                    Regex,
                    open),

                _ => Result.Of<ICSTNode>(
                    new RecognitionRuntimeError(
                        new InvalidOperationException(
                            $"Invalid match type: {MatchType}")))
            };

            return result.IsDataResult();
        }

        private static IResult<ICSTNode> RecognizeClosedPattern(
            TokenReader reader,
            ProductionPath productionPath,
            Regex pattern,
            IMatchType.Closed matchType)
        {
            var position = reader.Position;
            if (reader.TryGetTokens(matchType.MaxMatch, out var tokens))
            {
                var matchRange = matchType.MaxMatch - matchType.MinMatch;
                for (int cnt = 0; cnt <= matchRange; cnt++)
                {
                    var subtokens = tokens[0..^cnt];

                    if (pattern.IsMatch(subtokens.AsSpan()))
                        return ICSTNode
                            .Of(pattern, subtokens)
                            .ApplyTo(Result.Of<ICSTNode>);
                }
            }

            reader.Reset(position);
            return UnrecognizedTokens
                .Of(productionPath, position)
                .ApplyTo(Result.Of<ICSTNode>);
        }

        private static IResult<ICSTNode> RecognizeOpenPattern(
            TokenReader reader,
            ProductionPath productionPath,
            Regex pattern,
            IMatchType.Open matchType)
        {
            var position = reader.Position;
            var length = 0;
            var mismatchCount = 0;

            while (reader.TryPeekTokens(++length, out var tokens))
            {
                if (pattern.IsMatch(tokens.AsSpan()))
                    mismatchCount = 0;

                mismatchCount++;
                if (mismatchCount > matchType.MaxMismatch)
                    break;
            }

            var trueLength = length - mismatchCount;
            if ((trueLength == 0 && matchType.AllowsEmptyTokens)
                || trueLength > 0)
                return ICSTNode
                    .Of(pattern, reader.GetTokens(trueLength, true))
                    .ApplyTo(Result.Of);

            else
                return UnrecognizedTokens
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);
        }
    }
}
