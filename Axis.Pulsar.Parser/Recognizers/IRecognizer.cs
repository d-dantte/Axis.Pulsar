using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRecognizer
    {
        /// <summary>
        /// 
        /// </summary>
        Cardinality Cardinality { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryRecognize(BufferedTokenReader tokenReader, out RecognizerResult result);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <returns></returns>
        IResult Recognize(BufferedTokenReader tokenReader);
    }
}
