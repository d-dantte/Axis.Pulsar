using Axis.Pulsar.Parser.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser
{
    public interface IParser
    {
        bool TryParse(BufferedTokenReader tokenReader, out ParseResult result);
    }
}
