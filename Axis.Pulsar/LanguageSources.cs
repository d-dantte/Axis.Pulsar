using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.ExceptionExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Axis.Luna;
using Axis.Pulsar.Production;

namespace Axis.Pulsar
{
    #region Xml Language Source
    public class XmlLanguageSource : ILanguageSource
    {
        public static readonly string RootSymbolElementName = "RootSymbol";
        public static readonly string NonTerminalElementName = "NonTerminal";
        public static readonly string TerminalElementName = "Terminal"; //pattern terminal
        public static readonly string CharPatternTerminalElementName = "CharPattern"; //pattern terminal
        public static readonly string CharacterTerminalElementName = "CharacterTerminal"; //literal terminal
        public static readonly string LiteralTerminalElementName = "Literal"; //literal terminal
        public static readonly string DelimitedStringElementName = "DelimitedString"; //delimited string
        public static readonly string ChoiceSymbol = "Choice";
        public static readonly string SequenceSymbol = "Sequence";
        public static readonly string AllSymbol = "All";
        public static readonly string AnySymbol = "Any";
        public static readonly string Symbol = "Symbol";

        #region Properties
        public string Id { get; private set; }
        public ProductionMap Grammar { get; private set; }
        public IEnumerable<ImportRef> Imports { get; private set; }
        #endregion

        #region Init
        private XmlLanguageSource()
        {
            this.Imports = new List<ImportRef>();
            this.Grammar = new ProductionMap(this);
        }
        public XmlLanguageSource(string markup)
            : this()
        {
            var xdoc = XDocument.Parse(markup);
            init(xdoc);
        }
        public XmlLanguageSource(XDocument xdoc)
            : this()
        {
            init(xdoc);
        }
        private void init(XDocument xdoc)
        {
            var root = xdoc.Root;

            //language id
            this.Id = root.Attribute("languageId").Value;

            //imports
            var importsAttr = root.Attribute("imports");
            if (importsAttr != null)
            {
                var itags = TagBuilder.Parse(importsAttr.Value);
                var ilist = Imports as List<ImportRef>;
                foreach (var itag in itags)
                    ilist.Add(new ImportRef { Prefix = itag.Name, LanguageId = itag.Value });
            }

            ///The Language
            //non terminals
            var nonterms =
                from nt in xdoc.Root.Elements()
                where nt.Name.LocalName == NonTerminalElementName || nt.Name.LocalName == RootSymbolElementName
                select nt;
            foreach (var nonterm in nonterms)
            {
                var prod = NewProduction(nonterm, this.Grammar);
                this.Grammar.AddProduction(prod);
            }

            //pattern terminals
            var literms =
                from lt in xdoc.Root.Elements()
                where lt.Name.LocalName == TerminalElementName || lt.Name.LocalName == CharPatternTerminalElementName
                select lt;
            foreach (var literm in literms)
            {
                var prod = NewProduction(literm, this.Grammar);
                this.Grammar.AddProduction(prod);
            }

            //character/literal terminals
            var charterms =
                from lt in xdoc.Root.Elements()
                where lt.Name.LocalName == CharacterTerminalElementName || lt.Name.LocalName == LiteralTerminalElementName
                select lt;
            foreach (var chterm in charterms)
            {
                var prod = NewProduction(chterm, this.Grammar);
                this.Grammar.AddProduction(prod);
            }

            //character/literal terminals
            var delimterms =
                from lt in xdoc.Root.Elements()
                where lt.Name.LocalName == DelimitedStringElementName
                select lt;
            foreach (var delimterm in delimterms)
            {
                var prod = NewProduction(delimterm, this.Grammar);
                this.Grammar.AddProduction(prod);
            }
        }
        #endregion

        #region Production Creation
        public static Production.Production NewProduction(XElement xelement, ProductionMap grammar)
        {
            ThrowNullArguments(() => grammar);

            if (xelement.Name.LocalName == NonTerminalElementName || xelement.Name.LocalName == RootSymbolElementName)
                return NewNonTerminal(xelement, grammar);
            else if (xelement.Name.LocalName == TerminalElementName || xelement.Name.LocalName == CharPatternTerminalElementName)
                return NewCharPatternTerminal(xelement, grammar);
            else if (xelement.Name.LocalName == CharacterTerminalElementName || xelement.Name.LocalName == LiteralTerminalElementName)
                return NewLiteralTerminal(xelement, grammar);
            else if (xelement.Name.LocalName == DelimitedStringElementName)
                return NewDelimitedStringTerminal(xelement, grammar);
            else throw new InvalidOperationException("unknown production type: " + xelement.Name.LocalName);
        }
        public static CharPatternTerminal NewCharPatternTerminal(XElement elt, ProductionMap grammar)
        {
            var options = CharPatternTerminal.Options.Blank;

            var pattern = elt.Attribute("pattern").Value.Escape("\n", "\r");

            //ignoreCase is "true" by default
            var caseAttr = elt.Attribute("ignoreCase");
            var ignoreCase = !(caseAttr != null && Convert.ToBoolean(caseAttr.Value));

            //isAbsolute is "true" by default
            var isAbsolute = elt.Attribute("isAbsolute");

            options |= (isAbsolute == null || Convert.ToBoolean(isAbsolute.Value)) ?
                       CharPatternTerminal.Options.AbsolutePattern :
                       CharPatternTerminal.Options.Blank;
            options |= ignoreCase ? 0 : CharPatternTerminal.Options.CaseSensitive;

            //termination strings
            var tstringsArr = Eval(() => elt.Attribute("terminationStrings").Value
                                            .Unescape()
                                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) ??
                                            new string[0];

            var rexterminal = new CharPatternTerminal(elt.Attribute("name").Value, pattern, tstringsArr, options);
            rexterminal.Grammar = grammar;
            return rexterminal;
        }
        public static LiteralTerminal NewLiteralTerminal(XElement elt, ProductionMap grammar)
        {
            //ignoreCase is false by default
            var ignorecasev = elt.Attribute("ignoreCase");
            bool ignorecase = false;
            bool.TryParse(ignorecasev == null ? "false" : ignorecasev.Value, out ignorecase);

            var literlTerminal = new LiteralTerminal
            {
                SymbolName = elt.Attribute("name").Value,
                MatchString = elt.Attribute("string").Value.Unescape(),
                IgnoreCase = ignorecase,
                Grammar = grammar
            };
            return literlTerminal;
        }
        public static DelimitedStringTerminal NewDelimitedStringTerminal(XElement elt, ProductionMap grammar)
        {
            //ignoreCase is false by default
            var att = elt.Attribute("includeDelimiter");
            bool include = false;
            bool.TryParse(att == null ? "false" : att.Value, out include);

            att = elt.Attribute("allowUndelimited");
            bool endofline = false;
            bool.TryParse(att == null ? "false" : att.Value, out endofline);

            var literlTerminal = new DelimitedStringTerminal
            {
                SymbolName = elt.Attribute("name").Value,
                DelimiterString = elt.Attribute("delimiter").Value.Unescape(),
                AllowUndelimited = endofline,
                Include = include,
                Grammar = grammar
            };
            return literlTerminal;
        }
        public static NonTerminal NewNonTerminal(XElement elt, ProductionMap grammar)
        {
            var nterminal = new NonTerminal { Grammar = grammar };
            nterminal.SymbolName = elt.Name.LocalName == RootSymbolElementName ?
                                   ProductionMap.RootSymbolName :
                                   elt.Attribute("name").Value;

            foreach (var child in elt.Elements())
            {
                if (child.Name.LocalName == ChoiceSymbol)
                    nterminal.RootSequence.AddSymbol(NewChoice(child, grammar));
                else if (child.Name.LocalName == SequenceSymbol)
                    nterminal.RootSequence.AddSymbol(NewSequence(child, grammar));
                else if (child.Name.LocalName == AllSymbol)
                    nterminal.RootSequence.AddSymbol(NewAll(child, grammar));
                else if (child.Name.LocalName == AnySymbol)
                    nterminal.RootSequence.AddSymbol(NewAny(child, grammar));
                else if (child.Name.LocalName == Symbol)
                    nterminal.RootSequence.AddSymbol(NewElementRef(child, grammar));
            }

            return nterminal;
        }

        public static Choice NewChoice(XElement elt, ProductionMap grammar, Cardinality forceCardinality = null)
        {
            var choice = new Choice { Cardinality = forceCardinality ?? elt.NewCardinality(), Grammar = grammar };

            foreach (var child in elt.Elements())
            {
                if (child.Name.LocalName == ChoiceSymbol)
                    choice.AddSymbol(NewChoice(child, grammar));
                else if (child.Name.LocalName == SequenceSymbol)
                    choice.AddSymbol(NewSequence(child, grammar));
                else if (child.Name.LocalName == AllSymbol)
                    choice.AddSymbol(NewAll(child, grammar));
                else if (child.Name.LocalName == AnySymbol)
                    choice.AddSymbol(NewAny(child, grammar));
                else if (child.Name.LocalName == Symbol)
                    choice.AddSymbol(NewElementRef(child, grammar));
            }
            return choice;
        }
        public static All NewAll(XElement elt, ProductionMap grammar, Cardinality forceCardinality = null)
        {
            var all = new All { Cardinality = forceCardinality ?? elt.NewCardinality(), Grammar = grammar };

            foreach (var child in elt.Elements())
            {
                if (child.Name.LocalName == ChoiceSymbol)
                    all.AddSymbol(NewChoice(child, grammar, new Cardinality()));
                else if (child.Name.LocalName == SequenceSymbol)
                    all.AddSymbol(NewSequence(child, grammar, new Cardinality()));
                else if (child.Name.LocalName == AllSymbol)
                    all.AddSymbol(NewAll(child, grammar, new Cardinality()));
                else if (child.Name.LocalName == AnySymbol)
                    all.AddSymbol(NewAny(child, grammar, new Cardinality()));
                else if (child.Name.LocalName == Symbol)
                    all.AddSymbol(NewElementRef(child, grammar, new Cardinality()));
            }
            return all;
        }
        public static Sequence NewSequence(XElement elt, ProductionMap grammar, Cardinality forceCardinality = null)
        {
            var seq = new Sequence { Cardinality = forceCardinality ?? elt.NewCardinality(), Grammar = grammar };

            foreach (var child in elt.Elements())
            {
                if (child.Name.LocalName == ChoiceSymbol)
                    seq.AddSymbol(NewChoice(child, grammar));
                else if (child.Name.LocalName == SequenceSymbol)
                    seq.AddSymbol(NewSequence(child, grammar));
                else if (child.Name.LocalName == AllSymbol)
                    seq.AddSymbol(NewAll(child, grammar));
                else if (child.Name.LocalName == AnySymbol)
                    seq.AddSymbol(NewAny(child, grammar));
                else if (child.Name.LocalName == Symbol)
                    seq.AddSymbol(NewElementRef(child, grammar));
            }
            return seq;
        }
        public static Any NewAny(XElement elt, ProductionMap grammar, Cardinality forceCardinality = null)
        {
            var any = new Any(Eval(() => new Any.MinMatchCount(elt.Attribute("minMatchCount").Value)) ?? new Any.MinMatchCount())
            {
                Cardinality = forceCardinality ?? elt.NewCardinality(),
                Grammar = grammar
            };

            foreach (var child in elt.Elements())
            {
                if (child.Name.LocalName == ChoiceSymbol)
                    any.AddSymbol(NewChoice(child, grammar));
                else if (child.Name.LocalName == SequenceSymbol)
                    any.AddSymbol(NewSequence(child, grammar));
                else if (child.Name.LocalName == AllSymbol)
                    any.AddSymbol(NewAll(child, grammar));
                else if (child.Name.LocalName == AnySymbol)
                    any.AddSymbol(NewAny(child, grammar));
                else if (child.Name.LocalName == Symbol)
                    any.AddSymbol(NewElementRef(child, grammar));
            }
            return any;
        }
        public static ProductionRef NewElementRef(XElement elt, ProductionMap grammar, Cardinality forceCardinality = null)
        {
            var eref = new ProductionRef { Cardinality = forceCardinality ?? elt.NewCardinality(), Grammar = grammar };
            var sname = elt.Attribute("name").Value;
            if (sname.Contains(":"))
            {
                var parts = sname.Split(':');
                if (parts.Length != 2) throw new Exception("invalid symbol name: " + sname);
                eref.ImportRef = new ImportProductionRef
                {
                    Prefix = parts[0],
                    SymbolName = parts[1],
                    LanguageId = grammar.Source.Imports.FirstOrDefault(x => x.Prefix == parts[0]).LanguageId //may throw exceptions
                };
                grammar.AddImportRef(eref.ImportRef);
            }
            eref.SymbolName = sname;
            return eref;
        }
        #endregion

        public override bool Equals(object obj) => this.Id == obj?.As<ILanguageSource>().Id;
        public override int GetHashCode() => Id.GetHashCode();
    }

    public static class XLangExtensions
    {
        private static Dictionary<string, string> _escapedSequences = new Dictionary<string, string>
        {
            { "@at", "@"}, //at, strudel
            { "@scol", ";"}, //quotation
            { "@quot", "\""}, //quotation
            { "@squot", "\'"}, //single quotation
            { "@nl", "\n"}, //new line
            { "@cr", "\r"}, //carriage return
            { "@bsp", "\b"}, //back space
            { "@alert", "\a"}, //alert (bell)
            { "@ffd", "\f"}, //form feed
            { "@htab", "\v"}, //horizontal tab
            { "@vtab", "\v"}, //vertical tab
            //{ "@end", "" } //special character signifying the end of the string
        };

        public static IEnumerable<string> EscapeSequences() => _escapedSequences.Keys.ToArray();

        public static string Escape(this string unescaped)
            => _escapedSequences.Aggregate(unescaped, (accum, next) => accum.Replace(next.Value, next.Key));

        public static string Escape(this string unescaped, params string[] unescapeSequences)
            => _escapedSequences.Where(es => unescapeSequences.Contains(es.Value))
                                .Aggregate(unescaped, (accum, next) => accum.Replace(next.Value, next.Key));


        public static string Unescape(this string unescaped)
            => _escapedSequences.Aggregate(unescaped, (accum, next) => accum.Replace(next.Key, next.Value));

        public static string Unescape(this string unescaped, params string[] escapeSequences)
            => _escapedSequences.Where(es => escapeSequences.Contains(es.Key))
                                .Aggregate(unescaped, (accum, next) => accum.Replace(next.Key, next.Value));

        public static Cardinality NewCardinality(this XElement elt)
        {
            var maxos = elt.Attribute("maxOccurs");
            var minos = elt.Attribute("minOccurs");
            return new Cardinality(maxos != null ? maxos.Value : "1", minos != null ? minos.Value : "1");
        }
        public static Any.MinMatchCount NewMinMatchCount(this XElement xelt)
        {
            var count = xelt.Attribute("minMatchCount");
            uint ucount = 0;
            uint.TryParse(count != null ? count.Value : "0", out ucount);
            return new Any.MinMatchCount(ucount);
        }
    }
    #endregion
}
