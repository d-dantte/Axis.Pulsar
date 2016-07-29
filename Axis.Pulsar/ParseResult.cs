using Axis.Pulsar.Production;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar
{
    public class ParseResult
    {
        public bool Parsed { get; internal set; }
        public IEnumerable<IChar> Tokens { get; internal set; }
        public bool IsInputExhausted { get { return !this.bookmark.Bookmark().MoveNext(); } }
        public IEnumerable<IChar> UnparsedInput => this.bookmark.Bookmark();

        public IEnumerable<IChar> InputStream { get { return this.bookmark; } }
        private IBookmarkedStream bookmark;

        public IEnumerable<ISymbolHandler> SymbolHandlers { get; internal set; }

        public Symbol SymbolTree
        {
            get
            {
                var sgenerator = this.SymbolHandlers.FirstOrDefault(h => h is ParseContext.HelperHandler) as ParseContext.HelperHandler;
                if (sgenerator == null) return null;
                else return sgenerator.RootSymbol;
            }
        }

        public ParseResult(bool parsed, IEnumerable<IChar> tokens, IBookmarkedStream input, params ISymbolHandler[] handlers)
        {
            this.Parsed = parsed;
            this.Tokens = tokens;
            this.bookmark = input;
            this.SymbolHandlers = new List<ISymbolHandler>(handlers);
        }
    }
}
