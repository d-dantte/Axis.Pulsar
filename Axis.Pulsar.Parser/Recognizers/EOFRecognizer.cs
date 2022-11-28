using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;

namespace Axis.Pulsar.Parser.Recognizers
{
    public class EOFRecognizer : IRecognizer
    {
        public Cardinality Cardinality => default(EOF).Cardinality;

        public IResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        public bool TryRecognize(BufferedTokenReader tokenReader, out IResult result)
        {
            var position = tokenReader.Position;

            try
            {
                if (!tokenReader.TryNextToken(out _))
                {
                    result = IResult.Of(CST.ICSTNode.Of(nameof(EOF), ""));
                    return true;
                }
                else
                {
                    result = IResult.Of(0, tokenReader.Position);
                    tokenReader.Reset(position);
                    return false;
                }
            }
            catch (Exception e)
            {
                #region Failed
                _ = tokenReader.Reset(position);
                result = new IResult.Exception(e, position + 1);
                return false;
                #endregion
            }
        }
    }
}
