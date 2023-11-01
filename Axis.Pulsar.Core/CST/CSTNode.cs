using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Misc.Pulsar.Utils;
using Axis.Pulsar.Core.Grammar;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// TODO: Add <see cref="ProductionPath"/> as a member of all nodes
    /// </summary>
    public interface ICSTNode
    {
        Tokens Tokens { get; }

        #region Of
        public static ICSTNode Of(string name, params ICSTNode[] nodes) => new NonTerminal(name, nodes);

        public static ICSTNode Of(string name, NodeSequence nodes) => new NonTerminal(name, nodes);

        public static ICSTNode Of(Tokens tokens) => new Literal(tokens);

        public static ICSTNode Of(Regex regex, Tokens tokens) => new Pattern(regex, tokens);

        public static ICSTNode Of(string name, Tokens tokens) => new CustomTerminal(name, tokens);
        #endregion


        public readonly struct NonTerminal :
            ICSTNode,
            IDefaultValueProvider<NonTerminal>
        {
            private readonly string _name;
            private readonly NodeSequence _nodes;
            private readonly Lazy<Tokens> _tokens;

            #region DefaultValueProvider
            public static NonTerminal Default => default;

            public bool IsDefault => Default.Equals(this);
            #endregion

            public string Name => _name;

            public NodeSequence Nodes => _nodes;

            public Tokens Tokens => _tokens.Value;

            public NonTerminal(string name, NodeSequence nodes)
            {
                _name = name.ThrowIf(string.IsNullOrWhiteSpace, new ArgumentNullException(nameof(name)));
                _nodes = nodes.ThrowIfNull(new ArgumentNullException(nameof(nodes)));
                _tokens = new Lazy<Tokens>(() => nodes.Aggregate(default(Tokens), (tokens, node) =>
                {
                    if (tokens.IsDefault)
                        return node.Tokens;

                    if (!tokens.IsSourceRefEqual(node.Tokens))
                        throw new InvalidOperationException($"Invalid token: non-relative");

                    return tokens.CombineWith(node.Tokens);
                }));
            }

            public override string ToString()
            {
                return $"[@NT name:{Name}; NodeCount:{_nodes.Count}]";
            }
        }

        public readonly struct Literal :
            ICSTNode,
            IDefaultValueProvider<Literal>
        {
            private readonly Tokens _tokens;

            #region DefaultValueProvider
            public static Literal Default => default;

            public bool IsDefault => _tokens.IsDefault;
            #endregion

            public Tokens Tokens => _tokens;

            public Literal(Tokens tokens)
            {
                _tokens = tokens.ThrowIfDefault(new ArgumentException(
                    $"Invalid {nameof(tokens)}: default"));
            }

            public static bool operator ==(Literal left, Literal right) => left.Equals(right);
            public static bool operator !=(Literal left, Literal right) => !left.Equals(right);

            public override bool Equals(object? obj)
            {
                return obj is Literal other
                    && _tokens.Equals(other.Tokens);
            }

            public override int GetHashCode() => _tokens.GetHashCode();

            public override string ToString()
            {
                return $"[@L Tokens:{Tokens}]";
            }
        }

        public readonly struct Pattern :
            ICSTNode,
            IDefaultValueProvider<Pattern>
        {
            private readonly Tokens _tokens;
            private readonly Regex _regex;

            #region DefaultValueProvider
            public static Pattern Default => default;

            public bool IsDefault => _tokens.IsDefault && _regex is null;
            #endregion

            public Tokens Tokens => _tokens;

            public Regex Regex => _regex;

            public Pattern(Regex regex, Tokens tokens)
            {
                _tokens = tokens.ThrowIfDefault(new ArgumentException(
                    $"Invalid {nameof(tokens)}: default"));

                _regex = regex ?? throw new ArgumentNullException(nameof(regex));
            }

            public static bool operator ==(Pattern left, Pattern right) => left.Equals(right);

            public static bool operator !=(Pattern left, Pattern right) => !left.Equals(right);

            public override bool Equals(object? obj)
            {
                return obj is Pattern other
                    && _tokens.Equals(other.Tokens)
                    && EqualityComparer<string>.Default.Equals(
                        _regex?.ToString(),
                        other._regex?.ToString())
                    && EqualityComparer<RegexOptions?>.Default.Equals(
                        _regex?.Options,
                        other._regex?.Options);
            }

            public override int GetHashCode() => HashCode.Combine(_tokens, _regex);

            public override string ToString()
            {
                return $"[@P Regex:{Regex}; Tokens:{Tokens}]";
            }
        }

        public readonly struct CustomTerminal :
            ICSTNode,
            IDefaultValueProvider<CustomTerminal>
        {
            private readonly Tokens _tokens;
            private readonly string _name;

            #region DefaultValueProvider
            public static CustomTerminal Default => default;

            public bool IsDefault => _tokens.IsDefault && _name is null;
            #endregion

            public Tokens Tokens => _tokens;

            public string Name => _name;

            public CustomTerminal(string name, Tokens tokens)
            {
                _tokens = tokens.ThrowIfDefault(new ArgumentException(
                    $"Invalid {nameof(tokens)}: default"));

                _name = name.ThrowIf(
                    n => !Production.SymbolPattern.IsMatch(n),
                    new ArgumentException($"Invalid {nameof(name)}: '{name}'"));
            }

            public static bool operator ==(CustomTerminal left, CustomTerminal right) => left.Equals(right);

            public static bool operator !=(CustomTerminal left, CustomTerminal right) => !left.Equals(right);

            public override bool Equals(object? obj)
            {
                return obj is CustomTerminal other
                    && _tokens.Equals(other.Tokens)
                    && EqualityComparer<string>.Default.Equals(_name, other._name);
            }

            public override int GetHashCode() => HashCode.Combine(_tokens, _name);

            public override string ToString() => $"[@C Name:{Name}; Tokens:{Tokens}]";
        }
    }
}
