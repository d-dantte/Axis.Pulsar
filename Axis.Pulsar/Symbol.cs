using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Axis.Pulsar
{
    public class Symbol : IEnumerable<Symbol>
    {
        #region Properties
        public string Id { get; internal set; }
        public string Name { get; set; }
        public Symbol Parent { get; set; }
        public Symbol Root
        {
            get
            {
                Symbol root = this;
                while (root.Parent != null) root = root.Parent;
                return root;
            }
        }
        public IEnumerable<IChar> Token { get; set; }

        public List<Symbol> ChildSymbols { get; private set; } = new List<Symbol>();

        #endregion

        #region Init
        public Symbol(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
        public Symbol()
               :this(null, null)
        { }
        public Symbol(string id, string name, IEnumerable<Symbol> children)
               :this(id, name)
        {
            this.ChildSymbols.AddRange(children);
        }
        public Symbol(string id, string name, IEnumerable<IChar> tokens)
            : this(id, name, tokens, null)
        {
        }
        public Symbol(string id, string name, IEnumerable<IChar> tokens, Symbol parent)
            : this(id, name)
        {
            this.Token = tokens;
            this.Parent = parent;
        }
        #endregion

        public IEnumerable<Symbol> Children() => ChildSymbols.ToArray();
        public IEnumerable<Symbol> Descendants()
        {
            foreach(var s in this.ChildSymbols)
            {
                foreach (var _s in s.Concat(s.Descendants())) yield return _s;
            }
        }

        #region old traversal api
        public Symbol FindChild(string name)
        {
            return this.ChildSymbols.FirstOrDefault(s => s.Name == name);
        }
        public Symbol FindChild(Func<Symbol, bool> func)
        {
            return this.ChildSymbols.FirstOrDefault(func);
        }
        public IEnumerable<Symbol> FindChildren(string name)
        {
            return this.ChildSymbols.Where(s => s.Name == name);
        }
        public IEnumerable<Symbol> FindChildren(Regex namePattern)
        {
            return this.ChildSymbols.Where(s => namePattern.IsMatch(s.Name));
        }
        public IEnumerable<Symbol> FindChildren(Func<Symbol, bool> func)
        {
            return this.ChildSymbols.Where(s => func(s));
        }
        public Symbol FindDescendant(Regex namePattern)
        {
            if (namePattern == null) return null;
            foreach (Symbol s in this.ChildSymbols)
            {
                if (namePattern.IsMatch(s.Name)) return s;
                //else
                Symbol found = s.FindDescendant(namePattern);
                if (found != null) return found;
            }
            return null;
        }
        public IEnumerable<Symbol> FindDescendants(Regex namePattern)
        {
            List<Symbol> descendants = new List<Symbol>();
            foreach (Symbol s in this.ChildSymbols)
            {
                if (namePattern.IsMatch(s.Name)) descendants.Add(s);
                descendants.AddRange(s.FindDescendants(namePattern));
            }
            return descendants;
        }
        public IEnumerable<Symbol> FindDescendants(Func<Symbol, bool> func)
        {
            List<Symbol> descendants = new List<Symbol>();
            foreach (Symbol s in this.ChildSymbols)
            {
                if (func(s)) descendants.Add(s);
                descendants.AddRange(s.FindDescendants(func));
            }
            return descendants;
        }
        public Symbol FindDescendant(Func<Symbol, bool> func)
        {
            foreach (Symbol s in this.ChildSymbols)
            {
                if (func(s)) return s;
                var ss = s.FindDescendant(func);
                if (ss != null) return ss;
            }
            return null;
        }
        public bool HasChild(Func<Symbol, bool> func)
        {
            return this.FindChild(func) != null;
        }
        public bool HasDescendant(Func<Symbol, bool> func)
        {
            return this.FindDescendant(func) != null;
        }
        public string Tostring()
        {
            return new StringBuilder("[name: ").Append(this.Name).Append(", tokens: ").Append(this.Token).Append("]").ToString();
        }

        #region IEnumerator
        public IEnumerator<Symbol> GetEnumerator()
        {
            return this.ChildSymbols.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion
        #endregion
    }
}
