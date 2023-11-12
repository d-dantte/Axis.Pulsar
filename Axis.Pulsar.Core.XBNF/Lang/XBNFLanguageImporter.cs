using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Core.XBNF.Lang
{
    public class XBNFLanguageImporter : ILanguageImporter
    {
        public MetaContext MetaContext { get; }

        public XBNFLanguageImporter(MetaContext metaContext)
        {
            MetaContext = metaContext ?? throw new ArgumentNullException(nameof(metaContext));
        }

        public ILanguageContext ImportLanguage(string inputTokens)
        {
            _ = GrammarParser.TryParseGrammar(inputTokens, MetaContext, out var grammarResult);

            return grammarResult
                .Map(grammar => new XBNFLanguageContext(
                    grammar,
                    MetaContext))
                .Resolve();
        }
    }
}
