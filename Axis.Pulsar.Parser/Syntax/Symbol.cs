using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axis.Pulsar.Parser.Syntax
{
    public class Symbol
    {
        private readonly string _value;
        private readonly Symbol[] _children;

        public string Name { get; }

        public string Value => _value;

        public Symbol[] Children => _children?.ToArray();

        public Symbol(string name, string value)
        {
            Name = name.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("invalid name"));
            _value = value ?? throw new ArgumentException("invalid value");
            _children = null;
        }

        public Symbol(string name, params Symbol[] children)
        {
            Name = name.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("invalid name"));

            _children = children?.ToArray() ?? throw new ArgumentNullException(nameof(children));
            _children.ThrowIf(ContainsNull, t => new ArgumentException("Symbol array must not contain null elements"));
            _value = _children
                .Aggregate(new StringBuilder(), (acc, next) => acc.Append(next.Value))
                .ToString();
        }

        /// <summary>
        /// Get the first symbol found by searching corresponding child-symbols that match the path given.
        /// The path is a '.' separated list of symbol names. e.g expression.operator.constant
        /// </summary>
        /// <param name="path"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        public bool TryFindSymbol(string path, out Symbol child)
        {
            if (TryFindSymbols(path, out var children))
            {
                child = children[0];
                return true;
            }
            else
            {
                child = null;
                return false;
            }
        }


        /// <summary>
        /// Gets all symbols found by searching corresponding child-symbols that match the path given.
        /// The path is a '.' separated list of symbol names. e.g expression.operator.constant
        /// </summary>
        /// <param name="path"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        public bool TryFindSymbols(string path, out Symbol[] children)
        {
            var names = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

            try
            {
                children = names
                    .Aggregate(this.Enumerate(), GetChildren)
                    .ToArray();

                if (children.Length > 0)
                    return true;

                else
                {
                    children = null;
                    return false;
                }
            }
            catch
            {
                children = null;
                return false;
            }
        }


        public Symbol[] FindSymbols(string path)
        {
            if (!TryFindSymbols(path, out var symbols))
                return null;

            return symbols;
        }

        public Symbol FindSymbol(string path)
        {
            if (!TryFindSymbol(path, out var symbol))
                return null;

            return symbol;
        }

        private IEnumerable<Symbol> GetChildren(string name)
            => _children.Where(symbol => symbol.Name.Equals(name, StringComparison.InvariantCulture));

        private static bool ContainsNull(IEnumerable<Symbol> symbols) => symbols.Any(s => s == null);

        private static IEnumerable<Symbol> GetChildren(IEnumerable<Symbol> symbols, string name) => symbols.SelectMany(symbol => symbol.GetChildren(name));
    }
}
