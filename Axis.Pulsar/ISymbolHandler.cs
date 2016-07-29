using Axis.Pulsar.Production;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar
{
    public interface ISymbolHandler : ICloneable
    {
        void BeginSymbol(string id, string symbolName);
        void EndSymbol(string id, string symbolName, IEnumerable<IChar> tokens);
        void AbortSymbol(string id, string symbolName, IEnumerable<IChar> tokens);
        void AbortChildSymbol(string id, IEnumerable<IChar> tokens);
        bool IsSemanticallyValid(Symbol symbol);

        ISymbolHandler Copy();
    }
}
