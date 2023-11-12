using Axis.Pulsar.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Core.XBNF.IO
{
    public class XBNFLanguageExporter : ILanguageImporter
    {
        public MetaContext MetaContext { get; }


        public ILanguageContext ImportLanguage(string inputTokens)
        {
            throw new NotImplementedException();
        }
    }
}
