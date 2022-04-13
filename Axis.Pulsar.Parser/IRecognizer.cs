using Axis.Pulsar.Parser.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Pulsar.Parser
{
    public interface IRecognizer
    {
        bool TryRecognize(BufferedTokenReader tokenReader, out RecognizerResult result);
    }
}
