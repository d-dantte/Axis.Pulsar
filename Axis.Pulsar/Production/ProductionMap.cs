using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.EnumerableExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Axis.Pulsar.Production
{
    public class ProductionMap
    {
        public static readonly string LanguageUri = "http://Axis.Core.Pulsar/language";
        public static readonly string RootSymbolName = "language";

        #region Init
        public ProductionMap(ILanguageSource ls)
        {
            this.ImportRefs = new HashSet<ImportProductionRef>();
            this.Source = ls;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Dictionary mapping symbol name to it's production
        /// </summary>
        private Dictionary<string, Production> pmap = new Dictionary<string, Production>();
        public IEnumerable<Production> Productions { get { return pmap.Values; } }
        public ILanguageSource Source { get; private set; }
        public Parser Context { get; internal set; }

        public IEnumerable<ImportProductionRef> ImportRefs { get; private set; }
        #endregion

        #region Methods
        public void AddProduction(Production production)
        {
            if (this.pmap.ContainsKey(production.SymbolName))
                throw new Exception("Duplicate symbol name encountered: " + production.SymbolName);

            else this.pmap[production.SymbolName] = production;
        }
        public bool ContainsProduction(string productionName) => pmap.ContainsKey(productionName);
        internal void AddImportRef(ImportProductionRef iref)
        {
            var ireflist = this.ImportRefs as HashSet<ImportProductionRef>;
            if (!ireflist.Contains(iref)) ireflist.Add(iref);
        }
        internal void RemoveImportRef(ImportProductionRef iref)
        {
            (ImportRefs as HashSet<ImportProductionRef>).Remove(iref);
        }
        public Production GetProduction(string symbolname)
        {
            if (pmap.ContainsKey(symbolname)) return this.pmap[symbolname];
            else return null;
        }
        public Production GetProduction(ProductionRef sref)
        {
            if (sref.ImportRef == null) return pmap[sref.SymbolName];
            else
            {
                var src = Context.LanguageSources.FirstOrDefault(ls => ls.Id == sref.ImportRef.LanguageId);
                var prmap = Context[src];
                return prmap.pmap[sref.ImportRef.SymbolName];
            }
        }
        #endregion
    }

    #region Production Symbols (right-hand-side elements)
    public class ImportProductionRef : ImportRef
    {
        public string SymbolName { get; set; }

        public override string ToString() => Prefix + ":" + SymbolName;
    }

    public interface IProductionSymbol
    {
        MatchResult Parse(ParseContext pc, IBookmarkedStream reader);
        Cardinality Cardinality { get; set; }
        String SymbolName { get; }
        String SymbolId { get; set; }
        ProductionMap Grammar { get; }
    }

    public abstract class RHSContainerSymbol : IProductionSymbol, IEnumerable<IProductionSymbol>
    {
        protected RHSContainerSymbol()
        { }

        protected List<IProductionSymbol> Symbols = new List<IProductionSymbol>();
        public virtual IProductionSymbol AddSymbol(IProductionSymbol symbol)
        {
            this.Symbols.Add(symbol);
            return this;
        }
        public virtual IProductionSymbol RemoveSymbol(IProductionSymbol symbol)
        {
            this.Symbols.Remove(symbol);
            return this;
        }
        public virtual bool ContainsSymbol(IProductionSymbol symbol) => Symbols.Contains(symbol);

        #region IProductionSymbol Members
        public Cardinality Cardinality { get; set; }

        public string SymbolName { get; set; }

        public string SymbolId { get; set; }

        public ProductionMap Grammar { get; set; }

        public abstract MatchResult Parse(ParseContext pc, IBookmarkedStream reader);

        #endregion

        #region IEnumerable<IProductionSymbol> Members

        public IEnumerator<IProductionSymbol> GetEnumerator() => Symbols.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }

    public class ProductionRef : IProductionSymbol
    {
        public ImportProductionRef ImportRef { get; set; }

        public ProductionRef()
        {
            this.SymbolId = Guid.NewGuid().ToString();
        }
        public MatchResult Parse(ParseContext pc, IBookmarkedStream textStream)
        {
            var production = this.Grammar.GetProduction(this);

            var result = production.Parse(pc, textStream, this);

            return result;
        }

        public Cardinality Cardinality { get; set; }
        public string SymbolName { get; set; }
        public string SymbolId { get; set; }
        public ProductionMap Grammar { get; set; }
    }

    public class Sequence : RHSContainerSymbol
    {
        public Sequence()
        {
            this.SymbolId = Guid.NewGuid().ToString();
            this.Cardinality = new Cardinality();
            this.SymbolName = "#sequence";
        }

        #region IProductionSymbol Members
        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream)
        {
            int sequenceParseCount = 0;
            MatchResult result = null;
            var tokens = new List<IChar>();
            List<IChar> tempTokens = null;

            IBookmarkedStream startPosition = textStream.Bookmark(),
                      sequencePosition = textStream.Bookmark(),
                      cyclePosition = null;
            try
            {
                do
                {
                    cyclePosition = sequencePosition.Bookmark();
                    tempTokens = new List<IChar>();

                    foreach (IProductionSymbol s in this.Symbols)
                    {
                        result = s.Parse(pc, sequencePosition);

                        if (!result.IsParsed) break;
                        //else

                        sequencePosition = result.TextStream;
                        tempTokens.AddRange(result.Tokens);
                    }

                    //At this point, result.isParsed means that the entire sequence was parsed for this round.
                    if (result.IsParsed)
                    {
                        tokens.AddRange(tempTokens);
                        sequenceParseCount++;
                    }
                }
                while (result.IsParsed && Cardinality.IsWithinCountRange((uint)sequenceParseCount));
            }
            catch (AbortParseException e)
            {
                throw e;
            }

            //if there are successfull sequence parse iterations (within the valid cardinality range)
            if (Cardinality.IsWithinSyntaxRange((uint)sequenceParseCount))
            {
                var tstream = !result.IsParsed ? cyclePosition : result.TextStream;
                return new MatchResult(tstream, true, this.SymbolId, this.SymbolName, tokens);
            }
            else return new MatchResult(startPosition, false, this.SymbolId, this.SymbolName, tokens);
        }

        #endregion
    }

    public class Choice : RHSContainerSymbol
    {
        public Choice()
        {
            this.SymbolId = Guid.NewGuid().ToString();
            this.Cardinality = new Cardinality();
            this.SymbolName = "#choice";
        }

        #region IProductionSymbol Members
        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream)
        {
            int parseCount = 0;
            MatchResult result = null;
            var tokens = new List<IChar>();
            IBookmarkedStream startMark = textStream.Bookmark(),
                     tstream = textStream.Bookmark(),
                     choiceMark = null;
            try
            {
                do
                {
                    foreach (var element in this.Symbols)
                    {
                        choiceMark = tstream.Bookmark();

                        result = element.Parse(pc, tstream);
                        if (!result.IsParsed) tstream = choiceMark;
                        else
                        {
                            tstream = result.TextStream;
                            tokens.AddRange(result.Tokens);
                            parseCount++;
                            break;
                        }
                    }
                }
                while (result.IsParsed && (Cardinality.IsMaxOccursUnbounded || parseCount < Cardinality.MaxOccurs));
            }
            catch (AbortParseException e)
            {
                throw e;
            }

            if (parseCount >= Cardinality.MinOccurs)
                return new MatchResult(tstream, true, this.SymbolId, this.SymbolName, tokens);

            else return new MatchResult(startMark, false, this.SymbolId, this.SymbolName, tokens);
        }
        #endregion
    }

    /// <summary>
    /// This parses all of its child-symbols, much like sequences, except the order is irrelevant. Symbol cardinalities are ignored here,
    /// as all Symbols are assumed to have a cardinality of {1,1}.
    /// </summary>
    public class All : RHSContainerSymbol
    {
        public All()
        {
            this.SymbolId = Guid.NewGuid().ToString();
            this.Cardinality = new Cardinality();
            this.SymbolName = "#all";
        }

        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream)
        {
            int sequenceParseCount = 0;
            MatchResult result = null;
            var tokens = new List<IChar>();
            List<IChar> tempTokens = null;
            var allSymbols = new List<IProductionSymbol>(this.Symbols);

            IBookmarkedStream startPosition = textStream.Bookmark(),
                      sequencePosition = textStream.Bookmark(),
                      cyclePosition = null;
            try
            {
                do
                {
                    cyclePosition = sequencePosition.Bookmark();
                    tempTokens = new List<IChar>();

                    do
                    {
                        IProductionSymbol ps = null;
                        foreach (var s in allSymbols)
                            if ((result = (ps = s).Parse(pc, sequencePosition.Bookmark())).IsParsed) break;

                        if (result.IsParsed)
                        {
                            allSymbols.Remove(ps);
                            sequencePosition = result.TextStream;
                            tempTokens.AddRange(result.Tokens);
                        }
                    }
                    while (allSymbols.Count > 0 || result.IsParsed);

                    //At this point, result.isParsed means that the entire sequence was parsed for this round.
                    if (result.IsParsed)
                    {
                        tokens.AddRange(tempTokens);
                        sequenceParseCount++;
                    }
                }
                while (result.IsParsed && Cardinality.IsWithinCountRange((uint)sequenceParseCount));
            }
            catch (AbortParseException e)
            {
                throw e;
            }

            //if there are successfull sequence parse iterations (within the valid cardinality range)
            if (Cardinality.IsWithinSyntaxRange((uint)sequenceParseCount))
            {
                var tstream = !result.IsParsed ? cyclePosition : result.TextStream;
                return new MatchResult(tstream, true, this.SymbolId, this.SymbolName, tokens);
            }
            else return new MatchResult(startPosition, false, this.SymbolId, this.SymbolName, tokens);
        }
    }

    /// <summary>
    /// This is similar to <c>All</c>, except in this case, it is assumed that the children have a "?" cardinality - they may or may not exist;
    /// though this cardinality isnt implemented by the "Cardinality" class, it is inherently implemented by the parse algorithm.
    /// It also has a rule stating a minimum number of elements that must be matched to deem it valid. This minimum cannot be lower than "0",
    /// and has a maximum/default value of "all". For a better conceptualisation of this, imagine permutations and combinations; this class
    /// enables you orchestrate a combination of its child symbols.
    /// </summary>
    public class Any : RHSContainerSymbol
    {
        public MinMatchCount MinMatch { get; private set; }

        public Any(MinMatchCount mmc = null)
        {
            SymbolId = Guid.NewGuid().ToString();
            Cardinality = new Cardinality();
            SymbolName = "#any";
            Symbols = new List<IProductionSymbol>();
            MinMatch = mmc ?? new MinMatchCount();
        }

        #region IProductionSymbol Members
        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream)
        {
            int sequenceParseCount = 0;
            MatchResult result = null;
            var tokens = new List<IChar>();
            List<IChar> tempTokens = null;
            var anySymbols = new List<IProductionSymbol>(this.Symbols);

            IBookmarkedStream startPosition = textStream.Bookmark(),
                      sequencePosition = textStream.Bookmark(),
                      cyclePosition = null;
            try
            {
                bool isRoundValid = false;
                do
                {
                    cyclePosition = sequencePosition.Bookmark();
                    tempTokens = new List<IChar>();
                    do
                    {
                        IProductionSymbol ps = null;
                        result = null;
                        foreach (var s in anySymbols)
                            if ((result = (ps = s).Parse(pc, sequencePosition.Bookmark())).IsParsed) break;

                        if (Eval(() => result.IsParsed))
                        {
                            anySymbols.Remove(ps);
                            sequencePosition = result.TextStream;
                            tempTokens.AddRange(result.Tokens);
                        }
                    }
                    while (Eval(() => result.IsParsed));

                    if (isRoundValid = MinMatch.isValidMatchCount(Symbols.Count - anySymbols.Count, Symbols.Count))
                    {
                        tokens.AddRange(tempTokens);
                        sequenceParseCount++;
                    }
                }
                while (isRoundValid && Cardinality.IsWithinCountRange((uint)sequenceParseCount));

                //if there are successfull sequence parse iterations (within the valid cardinality range)
                if (Cardinality.IsWithinSyntaxRange((uint)sequenceParseCount))
                {
                    var tstream = isRoundValid ? sequencePosition : cyclePosition;
                    return new MatchResult(tstream, true, this.SymbolId, this.SymbolName, tokens);
                }
                else return new MatchResult(startPosition, false, this.SymbolId, this.SymbolName, tokens);
            }
            catch (AbortParseException e)
            {
                throw e;
            }
        }

        public override IProductionSymbol AddSymbol(IProductionSymbol symbol)
        {
            symbol.Cardinality = new Cardinality(1, 1);
            return base.AddSymbol(symbol);
        }
        #endregion

        public class MinMatchCount
        {
            public bool matchAll { get { return matchCount == 0; } }
            public uint matchCount { get; private set; }

            public bool isValidMatchCount(int count, int limit)
            {
                if (matchAll && count == limit) return true;
                else if (count >= matchCount) return true;
                else return false;
            }

            public MinMatchCount(uint count = 0)
            {
                this.matchCount = count;
            }
            public MinMatchCount(string value)
            {
                uint uvalue = 0;
                if (uint.TryParse(value, out uvalue)) this.matchCount = uvalue;
                else this.matchCount = 0;
            }
        }
    }

    #endregion


    #region Productions (left-hand-side elements)
    public abstract class Production
    {
        #region properties
        public string SymbolName { get; internal set; }
        public ProductionMap Grammar { get; internal set; }
        #endregion

        #region abstracts
        public abstract MatchResult Parse(ParseContext pc, IBookmarkedStream textStream, ProductionRef pref);
        #endregion

        #region init
        protected Production()
        { }
        #endregion
    }

    public class NonTerminal : Production
    {
        public Sequence RootSequence { get; private set; }

        public NonTerminal()
        {
            RootSequence = new Sequence();
        }

        #region Production Members
        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream, ProductionRef pref)
        {
            //create a temp sequence to parse the root sequence, using the cardinality of the pref. This is done
            //because the cardinality of this nonterminal or it's root sequence must not be tampered with as
            //other symbols up or down the recursion stack may already have called upon this same production object
            //to parse
            Sequence sq = new Sequence { Grammar = this.Grammar };
            sq.AddSymbol(this.RootSequence);

            MatchResult result = null;
            IBookmarkedStream startMark = textStream.Bookmark(),
                      parseMark = textStream.Bookmark(),
                      cycleMark = null;
            var tokens = new List<IChar>();
            var hasExceptions = false;
            var openedSymbols = new List<Symbol>();

            try
            {
                uint parseCount = 0;
                do
                {
                    cycleMark = parseMark.Bookmark();
                    hasExceptions = false;

                    pc.SymbolHandlers.ForAll((cnt, handler) => handler.BeginSymbol(pref.SymbolId, this.SymbolName));

                    //parse
                    result = sq.Parse(pc, parseMark);

                    if (result.IsParsed) parseCount++;
                    var tsymbol = new Symbol { Id = pref.SymbolId, Name = this.SymbolName, Token = result.Tokens };
                    openedSymbols.Add(tsymbol);
                    if (!pc.SymbolHandlers.All(h => h.IsSemanticallyValid(tsymbol))) hasExceptions = true;
                    else if (result.IsParsed)
                    {
                        pc.SymbolHandlers.ForAll((cnt, handler) => handler.EndSymbol(pref.SymbolId, this.SymbolName, result.Tokens));
                        tokens.AddRange(result.Tokens);
                        parseMark = result.TextStream;
                    }
                }
                while (result.IsParsed &&
                       pref.Cardinality.IsWithinCountRange(parseCount) &&
                       !pref.Cardinality.IsExactSyntaxCount(parseCount) && !hasExceptions);

                if (pref.Cardinality.IsWithinSyntaxRange(parseCount))
                {
                    IBookmarkedStream streamBookmark = null;
                    if (!result.IsParsed) //abort the unparsed symbol
                    {
                        var s = openedSymbols.Last();
                        openedSymbols.Remove(s);
                        pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(s.Id, s.Name, s.Token));
                        streamBookmark = cycleMark;
                    }
                    else streamBookmark = result.TextStream;

                    IEnumerable<IChar> tokenz = new IChar[0];
                    openedSymbols.ForEach(s => tokenz = s.Token.Concat(tokenz == null ? new IChar[0] : tokenz));
                    return new MatchResult(streamBookmark, true, this.SymbolName, tokenz.ToList());
                }
                else //if (pref.cardinality.isBelowSyntaxRange(parseCount))
                {
                    //first, abort the last un-parsed symbol.
                    if (!result.IsParsed)
                    {
                        var sym = openedSymbols.Last();
                        openedSymbols.Remove(sym);
                        pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(sym.Id, sym.Name, sym.Token));
                    }

                    //now abort all previously correctly parsed symbols
                    openedSymbols.ForEach(s => pc.SymbolHandlers.ForAll((cnt, h) => h.AbortChildSymbol(s.Id, s.Token)));
                    return new MatchResult(startMark, false, this.SymbolName, new IChar[0]);
                }
            }
            catch (AbortParseException e)
            {
                throw new Exception("", e);
            }
        }
        #endregion
    }

    public abstract class Terminal : Production
    {
        protected Terminal()
        {
        }
    }

    public class CharPatternTerminal : Terminal
    {
        [Flags]
        public enum Options
        {
            /// <summary>
            /// Neutral option
            /// </summary>
            Blank,

            /// <summary>
            /// This specifies that "$" is appended to the pattern automatically
            /// </summary>
            AbsolutePattern,

            /// <summary>
            /// Determines case sensitivity of the regular expression
            /// </summary>
            CaseSensitive
        }


        public Regex Rex { get; private set; }

        public IEnumerable<string> TerminationStrings => this._tstrings.ToList();

        private List<string> _tstrings = null;

        public CharPatternTerminal(string symbolName, string pattern, IEnumerable<string> terminationStrings, Options options = Options.Blank)
        {
            if (pattern == null) throw new ArgumentNullException();

            this._tstrings = Eval(() => terminationStrings.OrderBy(ts => ts.Length).ToList()) ?? new List<string>();

            var auxpattern = pattern;
            if (options.HasFlag(Options.AbsolutePattern)) auxpattern += "$";

            //other flags...
            var flags = RegexOptions.Singleline;
            flags |= !options.HasFlag(Options.CaseSensitive) ? RegexOptions.IgnoreCase : 0;
            this.Rex = new Regex(auxpattern, flags);
            this.SymbolName = symbolName;
        }

        private bool TerminationEncountered(IBookmarkedStream textStream)
        {
            var sb = new List<IChar>();
            foreach (var ts in this._tstrings)
            {
                sb.Clear();
                sb.AddRange(textStream.Take(ts.Length));
                if (ts == sb.AsString()) return true;
            }
            return false;
        }

        #region Terminal Members
        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream, ProductionRef pref)
        {
            IBookmarkedStream startMark = textStream.Bookmark(), cycleMark = null;
            var sb = new List<IChar>();
            int matchCount = 0;
            bool matched = false;
            Match match = null;

            try
            {
                //begin
                pc.SymbolHandlers.ForAll((cnt, h) => h.BeginSymbol(pref.SymbolId, this.SymbolName));

                do
                {
                    cycleMark = textStream.Bookmark();

                    if (TerminationEncountered(textStream.Bookmark())) break;
                    else if (textStream.MoveNext())
                    {
                        sb.Add(textStream.Current);
                        var sbstring = sb.AsString();
                        match = this.Rex.Match(sbstring);
                        matched = match.Success;
                        //if ((matched = this.Rex.IsMatch(sb.AsString())) &&
                        //    (matched = Eval(() => (match = this.Rex.Match(sb.AsString())) != null)) &&
                        //    (matched = Eval(() => match.Value == sb.AsString())))
                        if (match.Success && match.Value == sbstring)
                        {
                            matchCount++;
                        }
                    }
                    else
                    {
                        matched = true;
                        break;
                    }
                }
                while (matched);
            }
            catch
            {
                matched = false;
            }

            if (matchCount >= pref.Cardinality.MinOccurs)
            {
                if (!matched && sb.Count > 0) sb.RemoveAt(sb.Count - 1);

                var tsymbol = new Symbol { Id = pref.SymbolId, Name = this.SymbolName, Token = sb };
                var isSemanticallyValid = pc.SymbolHandlers.All(h => h.IsSemanticallyValid(tsymbol));

                if (!isSemanticallyValid)
                {
                    pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(pref.SymbolId, this.SymbolName, sb));
                    return new MatchResult(startMark, false, this.SymbolName, sb);
                }
                else
                {
                    IBookmarkedStream currentMark = null;
                    currentMark = matched ? textStream : cycleMark;
                    pc.SymbolHandlers.ForAll((cnt, h) => h.EndSymbol(pref.SymbolId, this.SymbolName, sb));
                    return new MatchResult(currentMark, true, this.SymbolName, sb);
                }
            }
            else
            {
                pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(pref.SymbolId, this.SymbolName, sb));
                return new MatchResult(startMark, false, this.SymbolName, sb);
            }
        }
        #endregion
    }

    public class LiteralTerminal : Terminal
    {
        public string MatchString { get; set; }
        public bool IgnoreCase { get; set; }

        #region Terminal Members
        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream, ProductionRef pref)
        {
            int sequenceParseCount = 0;
            MatchResult result = null;
            var tokens = new List<IChar>();
            List<IChar> tempTokens = null;

            IBookmarkedStream startPosition = textStream.Bookmark(),
                              symbolPosition = textStream.Bookmark(),
                              cyclePosition = null;
            try
            {
                do
                {
                    cyclePosition = symbolPosition.Bookmark();
                    tempTokens = new List<IChar>();

                    result = this.ParseLiteral(pc, symbolPosition, pref.SymbolId);

                    if (!result.IsParsed) break;
                    //else

                    symbolPosition = result.TextStream;
                    tempTokens.AddRange(result.Tokens);

                    //At this point, result.isParsed means that the entire sequence was parsed for this round.
                    if (result.IsParsed)
                    {
                        tokens.AddRange(tempTokens);
                        sequenceParseCount++;
                    }
                }
                while (result.IsParsed && pref.Cardinality.IsWithinCountRange((uint)sequenceParseCount));
            }
            catch (AbortParseException e)
            {
                throw e;
            }

            //if there are successfull literal parse iterations (within the valid cardinality range)
            if (pref.Cardinality.IsWithinSyntaxRange((uint)sequenceParseCount))
            {
                var tstream = !result.IsParsed ? cyclePosition : result.TextStream;
                return new MatchResult(tstream, true, this.SymbolName, tokens);
            }
            else return new MatchResult(startPosition, false, this.SymbolName, tokens);
        }
        private MatchResult ParseLiteral(ParseContext pc, IBookmarkedStream textStream, string symbolId)
        {
            IBookmarkedStream startMark = textStream.Bookmark();
            var sb = new List<IChar>();
            bool matched = false;

            try
            {
                //begin
                pc.SymbolHandlers.ForAll((cnt, h) => h.BeginSymbol(symbolId, this.SymbolName));

                int matchLength = MatchString.Length;
                while (matchLength > 0 && textStream.MoveNext())
                {
                    sb.Add(textStream.Current);
                    matchLength--;
                }

                if (this.IgnoreCase) matched = MatchString.ToLower().Equals(sb.AsString().ToLower());
                else matched = MatchString.Equals(sb.AsString());
            }
            catch (Exception e)//EOF and others..
            {
                matched = false;
            }

            if (matched)
            {
                var tsymbol = new Symbol { Id = symbolId, Name = this.SymbolName, Token = sb };
                bool isSemanticallyValid = pc.SymbolHandlers.All(h => h.IsSemanticallyValid(tsymbol));

                if (!isSemanticallyValid)
                {
                    pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(symbolId, this.SymbolName, sb));
                    return new MatchResult(startMark, false, this.SymbolName, sb);
                }
                else
                {
                    pc.SymbolHandlers.ForAll((cnt, h) => h.EndSymbol(symbolId, this.SymbolName, sb));
                    return new MatchResult(textStream, true, this.SymbolName, sb);
                }
            }
            else
            {
                pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(symbolId, this.SymbolName, sb));
                return new MatchResult(startMark, false, this.SymbolName, sb);
            }
        }

        //old implementation
        //private MatchResult parse_old(ParseContext pc, IBookmark textStream, ProductionRef pref)
        //{
        //    IBookmark startMark = textStream.bookmark(), cycleMark = null;
        //    var sb = new List<IChar>();
        //    int matchCount = 0;
        //    bool matched = false;

        //    try
        //    {
        //        //begin
        //        pc.symbolHandlers.ToList().ForEach(h => h.beginSymbol(pref.symbolId, this.symbolName));

        //        cycleMark = textStream.bookmark();
        //        int matchLength = matchString.Length;
        //        while (matchLength > 0 && textStream.MoveNext())
        //        {
        //            sb.Add(textStream.Current);
        //            matchLength--;
        //        }

        //        if (this.ignoreCase) matched = matchString.ToLower().Equals(sb.toString().ToLower());
        //        else matched = matchString.Equals(sb.toString());
        //        matchCount = matched ? 1 : 0;
        //    }
        //    catch (Exception e)//EOF and others..
        //    {
        //        matched = false;
        //    }

        //    if (matchCount >= pref.cardinality.minOccurs)
        //    {
        //        var tsymbol = new Symbol { id = pref.symbolId, symbolName = this.symbolName, token = sb };
        //        bool isSemanticallyValid = pc.symbolHandlers.All(h => h.isSemanticallyValid(tsymbol));

        //        if (!isSemanticallyValid)
        //        {
        //            pc.symbolHandlers.ToList().ForEach(h => h.abortSymbol(pref.symbolId, this.symbolName, sb));
        //            return new MatchResult(startMark, false, this.symbolName, sb);
        //        }
        //        else
        //        {
        //            pc.symbolHandlers.ToList().ForEach(h => h.endSymbol(pref.symbolId, this.symbolName, sb));
        //            return new MatchResult(textStream, true, this.symbolName, sb);
        //        }
        //    }
        //    else
        //    {
        //        pc.symbolHandlers.ToList().ForEach(h => h.abortSymbol(pref.symbolId, this.symbolName, sb));
        //        return new MatchResult(startMark, false, this.symbolName, sb);
        //    }
        //}
        #endregion
    }

    public class DelimitedStringTerminal : Terminal
    {
        private List<string> _delimiters = new List<string>();
        public string DelimiterString
        {
            get { return _delimiters.Aggregate(new StringBuilder(), (sb, next) => sb.Append($"{EncodeDelimiter(next)};")).ToString(); }
            set
            {
                _delimiters.Clear();
                value?.Split(';').ForAll((_cnt, next) => _delimiters.Add(DecodeDelimiter(next)));
            }
        }
        public IEnumerable<string> Delimiters => _delimiters.ToArray();
        public bool Include { get; set; }
        public bool AllowUndelimited { get; set; }

        public override MatchResult Parse(ParseContext pc, IBookmarkedStream textStream, ProductionRef pref)
        {
            IBookmarkedStream startMark = textStream.Bookmark();

            //begin
            pc.SymbolHandlers.ForAll((cnt, h) => h.BeginSymbol(pref.SymbolId, pref.SymbolName));

            //pull out the entire string
            var str = textStream.Select(cb => cb.Character)
                                .Aggregate(new StringBuilder(), (sb, chr) => sb.Append(chr))
                                .ToString();

            var termination = Delimiters.Select(del => new { del = del, indx = str.IndexOf(del, StringComparison.Ordinal) })
                                        .OrderBy(del => del.indx)
                                        .FirstOrDefault(del => del.indx >= 0);
            if (textStream.IsEndOfStream || (termination == null && !AllowUndelimited)) //failure
            {
                pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(pref.SymbolId, this.SymbolName, new IChar[0]));
                return new MatchResult(startMark, false, this.SymbolName, new IChar[0]);
            }
            else //success
            {
                termination = termination ?? new { del = AllowUndelimited ? str : "", indx = AllowUndelimited ? str.Length : 0 };
                var tokens = new List<IChar>();
                var newPosition = termination.indx + (Include ? termination.del.Length : 0);
                tokens.AddRange(startMark.Bookmark().Take(newPosition));
                var tsymbol = new Symbol { Id = pref.SymbolId, Name = this.SymbolName, Token = tokens };
                bool isSemanticallyValid = pc.SymbolHandlers.All(h => h.IsSemanticallyValid(tsymbol));

                if (!isSemanticallyValid)
                {
                    pc.SymbolHandlers.ForAll((cnt, h) => h.AbortSymbol(pref.SymbolId, this.SymbolName, new IChar[0]));
                    return new MatchResult(startMark, false, this.SymbolName, new IChar[0]);
                }
                else
                {
                    pc.SymbolHandlers.ForAll((cnt, h) => h.EndSymbol(pref.SymbolId, this.SymbolName, tokens));
                    return new MatchResult(startMark.Bookmark(newPosition), true, this.SymbolName, tokens);
                }
            }
        }

        private string EncodeDelimiter(string decoded) => decoded?.Replace(";", "&scol");
        private string DecodeDelimiter(string encoded) => encoded?.Replace("&scol", ";");
    }
    #endregion

    public class Cardinality : ICloneable
    {
        #region Properties
        public int MinOccurs { get; private set; }
        public int MaxOccurs { get; private set; }
        public bool IsMaxOccursUnbounded { get { return this.MaxOccurs == -1; } }
        public bool IsValid
        {
            get
            {
                if (MinOccurs < 0) return false;
                else if (IsMaxOccursUnbounded || MinOccurs <= MaxOccurs) return true;
                else return false;
            }
        }
        #endregion

        #region Init
        public Cardinality()
        {
            var one = 1.ToString();
            init(one, one);
        }
        public Cardinality(String max, String min)
        {
            init(max, min);
        }
        public Cardinality(int max, int min)
        {
            init(max.ToString(), min.ToString());
        }
        private void init(string max, string min)
        {
            //max
            int maxo;
            if (string.IsNullOrWhiteSpace(max)) maxo = 1;
            else if (max.ToLower().Equals("unbounded")) maxo = -1;
            else maxo = int.Parse(max);

            //min
            int mino;
            if (string.IsNullOrWhiteSpace(min)) mino = 1;
            else mino = int.Parse(min);

            this.MaxOccurs = maxo;
            this.MinOccurs = mino;

            if (!IsValid) throw new ArgumentException("invalid cardinality");
        }

        #endregion

        #region ICloneable Members
        public Object Clone() => clone();
        #endregion

        public Cardinality clone()
        {
            Cardinality c = new Cardinality();
            c.MaxOccurs = this.MaxOccurs;
            c.MinOccurs = this.MinOccurs;
            return c;
        }

        #region states
        public bool IsWithinSyntaxRange(uint value)
        {
            return MinOccurs <= value && (IsMaxOccursUnbounded || value <= MaxOccurs);
        }
        public bool IsWithinCountRange(uint value)
        {
            return IsMaxOccursUnbounded || value < MaxOccurs;
        }
        public bool IsExactSyntaxCount(uint value)
        {
            return this.MaxOccurs == this.MinOccurs && value == this.MinOccurs;
        }
        public bool IsBelowSyntaxRange(uint value)
        {
            return value < this.MinOccurs;
        }
        public bool IsAboveSyntaxRange(uint value)
        {
            return !this.IsMaxOccursUnbounded && value > this.MaxOccurs;
        }
        #endregion
    }

    public class MatchResult
    {
        #region Init
        public MatchResult(IBookmarkedStream tstream, bool parsed, String symbolName, IEnumerable<IChar> tokens)
            : this(tstream, parsed, null, symbolName, tokens)
        { }
        public MatchResult(IBookmarkedStream tstream, bool parsed, String symbolId, String symbolName, IEnumerable<IChar> tokens)
        {
            if (tokens == null || symbolName == null) throw new NullReferenceException("");

            this.IsParsed = parsed;
            this.Tokens = tokens;
            this.SymbolName = symbolName;
            this.SymbolId = symbolId;
            this.TextStream = tstream;
        }
        public MatchResult()
        { }
        #endregion

        #region Properties
        public String StringTokens { get { return this.Tokens.AsString(); } }
        public IEnumerable<IChar> Tokens { get; set; }
        public bool IsParsed { get; set; }
        public String SymbolName { get; set; }
        public String SymbolId { get; set; }
        public IBookmarkedStream TextStream { get; private set; }
        #endregion
    }

    public class ParseContext
    {
        #region Init
        public ParseContext(params ISymbolHandler[] handlers)
        {
            //symbol stack
            this.SymbolStack = new Stack<MatchResult>();

            //handlers
            (this.SymbolHandlers as List<ISymbolHandler>).Add(new HelperHandler()); //default handler
            if (handlers != null) (this.SymbolHandlers as List<ISymbolHandler>).AddRange(handlers);

        }
        #endregion

        #region Properties
        public IEnumerable<ISymbolHandler> SymbolHandlers = new List<ISymbolHandler>();
        public Stack<MatchResult> SymbolStack { get; private set; }
        #endregion


        #region HelperHandler Class
        public class HelperHandler : ISymbolHandler
        {
            public Symbol RootSymbol { get; private set; }
            public Symbol CurrentSymbol { get; private set; }

            internal HelperHandler()
            { }

            public void BeginSymbol(String id, String symbolName)
            {
                Symbol s = new Symbol(id, symbolName);

                if (this.CurrentSymbol == null) this.RootSymbol = this.CurrentSymbol = s;
                else s.Parent = this.CurrentSymbol;
                this.CurrentSymbol = s;
            }

            public void EndSymbol(String id, String symbolName, IEnumerable<IChar> tokens)
            {
                this.CurrentSymbol.Token = new List<IChar>(tokens);
                if (this.CurrentSymbol.Parent != null) this.CurrentSymbol.Parent.ChildSymbols.Add(this.CurrentSymbol);
                this.CurrentSymbol = this.CurrentSymbol.Parent;
            }

            public void AbortSymbol(String id, String symbolName, IEnumerable<IChar> tokens)
            {
                if (this.CurrentSymbol != null) this.CurrentSymbol = this.CurrentSymbol.Parent;
            }

            public void AbortChildSymbol(String id, IEnumerable<IChar> tokens)
            {
                if (this.CurrentSymbol != null)
                {
                    var child = this.CurrentSymbol.ChildSymbols.FirstOrDefault(ch => ch.Id == id);
                    if (child != null) this.CurrentSymbol.ChildSymbols.Remove(child);
                }
            }

            public bool IsSemanticallyValid(Symbol symbol) => true;


            public ISymbolHandler Copy() => new HelperHandler
            {
                CurrentSymbol = this.CurrentSymbol,
                RootSymbol = this.RootSymbol
            };

            public object Clone() => Copy();

            private Symbol findSymbol(string id)
            {
                if (RootSymbol == null) return null;
                else return findSymbol(RootSymbol, id);
            }
            private Symbol findSymbol(Symbol symbol, string id)
            {
                if (symbol.Id == id) return symbol;
                else foreach (var s in symbol.ChildSymbols)
                    {
                        var f = findSymbol(s, id);
                        if (f != null) return f;
                    }
                return null;
            }
        }
        #endregion
    }
}
