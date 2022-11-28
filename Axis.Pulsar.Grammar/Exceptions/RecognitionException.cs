using Axis.Pulsar.Grammar.Recognizers;
using System;

namespace Axis.Pulsar.Grammar.Exceptions
{
    public class RecognitionException: Exception
    {
        public IRecognitionResult RecognitionResult { get; }

        public RecognitionException(IRecognitionResult recognitionResult)
            :base("An error occured during recognition")
        {
            RecognitionResult = recognitionResult;
        }
    }
}
