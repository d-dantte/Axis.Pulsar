using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axis.Pulsar.Parser.Recognizers;
using Axis.Pulsar.Parser.Input;
using System.Linq;

namespace Axis.Pulsar.Parser.Tests.Recognizers
{
    [TestClass]
    public class SequenceRecognizerTest
    {
        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = new SequenceRecognizer(
                Cardinality.OccursOnlyOnce(),
                ("symbol-1", "token").CreatePassingRecognizer());

            Assert.IsNotNull(parser);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), parser.Cardinality);
        }

        [TestMethod]
        public void TryRecognize_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = new SequenceRecognizer(
                Cardinality.OccursOnlyOnce(),
                ("public-symbol", "public").CreatePassingRecognizer(),
                ("whitespace-symbol", " ").CreatePassingRecognizer(),
                ("static-symbol", "static").CreatePassingRecognizer(),
                ("whitespace-symbol", " ").CreatePassingRecognizer(),
                ("void-symbol", "void").CreatePassingRecognizer());

            // 1
            var reader = new BufferedTokenReader("irrelevant");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(5, trueResult.Symbols.Length);
            Assert.AreEqual(
                "public static void",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));

            // 2
            parser = new SequenceRecognizer(
                Cardinality.OccursOnly(2),
                ("public-symbol", "public").CreatePassingRecognizer(),
                ("whitespace-symbol", " ").CreatePassingRecognizer(),
                ("static-symbol", "static").CreatePassingRecognizer(),
                ("whitespace-symbol", " ").CreatePassingRecognizer(),
                ("void-symbol", "void").CreatePassingRecognizer());

            succeeded = parser.TryRecognize(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(10, trueResult.Symbols.Length);
            Assert.AreEqual(
                "public static voidpublic static void",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));
        }


        [TestMethod]
        public void TryRecognize_WithInvalidInput_Should_ReturnFailedParseResult()
        {
            var parser = new SequenceRecognizer(
                Cardinality.OccursOnlyOnce(),
                ("public-symbol", "public").CreatePassingRecognizer(),
                ("whitespace-symbol", " ").CreatePassingRecognizer(),
                ("static-symbol", "static").CreatePassingRecognizer(),
                new IResult.FailedRecognition(2, 1).CreateRecognizer(),
                new IResult.FailedRecognition(3, 1).CreateRecognizer());

            // 1
            var reader = new BufferedTokenReader("irrelevant");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(3, trueResult.RecognitionCount);
            Assert.AreEqual(0, trueResult.InputPosition);

            // 2
            parser = new SequenceRecognizer(
                Cardinality.OccursOnlyOnce(),
                ("public-symbol", "public").CreatePassingRecognizer(),
                ("whitespace-symbol", " ").CreatePassingRecognizer(),
                ("static-symbol", "static").CreatePassingRecognizer(),
                new IResult.Exception(new System.Exception(), 1).CreateRecognizer(),
                new IResult.FailedRecognition(3, 1).CreateRecognizer());

            reader = new BufferedTokenReader("irrelevant");
            succeeded = parser.TryRecognize(reader, out result);
            var exceptionResult = result as IResult.Exception;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(exceptionResult);
            Assert.IsNotNull(exceptionResult.Error);
            Assert.AreEqual(1, exceptionResult.InputPosition);

        }
    }
}
