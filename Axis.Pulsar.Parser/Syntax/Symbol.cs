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
            Name = name.ThrowIf(string.IsNullOrWhiteSpace, t => new ArgumentException("invalid name"));
            _value = value.ThrowIf(string.IsNullOrEmpty, t => new ArgumentException("invalid value"));
            _children = null;
        }

        public Symbol(string name, params Symbol[] children)
        {
            Name = name.ThrowIf(string.IsNullOrWhiteSpace, t => new ArgumentException("invalid name"));

            _children = children?.ToArray() ?? throw new ArgumentNullException(nameof(children));
            _children.ThrowIf(ContainsNull, t => new ArgumentException("Symbol array must not contain null elements"));
            _value = _children
                .Aggregate(new StringBuilder(), (acc, next) => acc.Append(next))
                .ToString();
        }

        private static bool ContainsNull(IEnumerable<Symbol> symbols) => symbols.Any(s => s == null);
    }
}
