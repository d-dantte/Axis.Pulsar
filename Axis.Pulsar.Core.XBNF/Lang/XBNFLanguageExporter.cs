﻿using Axis.Pulsar.Core.IO;

namespace Axis.Pulsar.Core.XBNF.Lang
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