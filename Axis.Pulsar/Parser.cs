using Axis.Pulsar.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Axis.Pulsar
{
    public class Parser
    {
        #region Fields
        private Dictionary<ILanguageSource, ProductionMap> languageMap = new Dictionary<ILanguageSource, ProductionMap>();
        private ILanguageSource root = null;
        #endregion

        #region Properties
        public List<ISymbolHandler> SymbolHandlers { get; private set; }
        public IEnumerable<ILanguageSource> LanguageSources => languageMap.Keys;
        public IEnumerable<ProductionMap> ProductionMaps => languageMap.Values;
        internal ProductionMap this[ILanguageSource source] => languageMap[source];
        #endregion

        #region Inits
        public Parser(params ILanguageSource[] languages)
        {
            //set up the languages
            foreach (var ls in languages)
            {
                ValidateLocalRefs(ls.Grammar);

                ls.Grammar.Context = this;
                languageMap[ls] = ls.Grammar;
            }
            root = languages[0];

            //handlers
            SymbolHandlers = new List<ISymbolHandler>();

            //validate production references
            ValidateSymbolImports();
        }

        private void ValidateSymbolImports()
        {
            foreach (var lang in ProductionMaps)
            {
                foreach (var iref in lang.ImportRefs)
                {
                    var l = languageMap.Keys.FirstOrDefault(k => k.Id == iref.LanguageId);
                    if (l == null) throw new Exception("invalid import language id: " + iref.LanguageId);

                    else if (!languageMap[l].Productions.Any(prd => prd.SymbolName == iref.SymbolName))
                        throw new Exception("symbol could not be located: " + iref);
                }
            }
        }
        #endregion

        //Parse
        public ParseResult Parse(ICharBuffer input) => Parse(input, ProductionMap.RootSymbolName);

        public ParseResult Parse(ICharBuffer input, string productionName)
        {
            var grammar = this.languageMap[this.root];
            var lprod = grammar.Productions.FirstOrDefault(prd => prd.SymbolName == productionName);

            var sessionHandlers = this.SymbolHandlers.Select(shh => shh.Copy());
            var pc = new ParseContext(sessionHandlers.ToArray());

            var rootRef = new ProductionRef
            {
                Cardinality = new Cardinality(1, 1),
                Grammar = grammar,
                SymbolName = "#root",
                SymbolId = Guid.NewGuid().ToString()
            };

            var presult = lprod.Parse(pc, input.Bookmark(), rootRef);
            return new ParseResult(presult.IsParsed, presult.Tokens, presult.TextStream, pc.SymbolHandlers.ToArray());
        }


        #region Utils
        private void ValidateLocalRefs(ProductionMap pmap)
        {
            foreach (var prod in pmap.Productions.Where(p => p is NonTerminal))
            {
                if (!ValidateLocalRefs(pmap, (prod as NonTerminal).RootSequence)) throw new Exception("invalid symbol ref found");
            }
        }
        private bool ValidateLocalRefs(ProductionMap pmap, IProductionSymbol psymbol)
        {
            if (psymbol is Sequence) foreach (var elt in (psymbol as Sequence))
            {
                if (!ValidateLocalRefs(pmap, elt))
                    return false;
            }
            else if (psymbol is Choice) foreach (var elt in (psymbol as Choice))
            {
                if (!ValidateLocalRefs(pmap, elt))
                    return false;
            }
            else if (psymbol is Any) foreach (var elt in (psymbol as Any))
            {
                if (!ValidateLocalRefs(pmap, elt))
                    return false;
            }
            else if (psymbol is All) foreach (var elt in (psymbol as All))
            {
                if (!ValidateLocalRefs(pmap, elt))
                    return false;
            }
            else if (psymbol is ProductionRef && (psymbol as ProductionRef).ImportRef == null)
                return pmap.ContainsProduction(psymbol.SymbolName);

            return true;
        }
        #endregion

    }
}
