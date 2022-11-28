using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axis.Pulsar.Parser.Recognizers;
using Moq;
using Axis.Pulsar.Parser.Input;
using System.Linq;
using Axis.Pulsar.Parser.CST;

namespace Axis.Pulsar.Parser.Tests.Recognizers
{
    [TestClass]
    public class EOFRecognizerTests
    {

        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var recognizer = new EOFRecognizer();

            Assert.IsNotNull(recognizer);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), recognizer.Cardinality);
        }


        [TestMethod]
        public void TryRecognize_WithValidInput_Should_ReturnValidParseResult()
        {
            // 1
            var recognizer = new EOFRecognizer();
            var reader = new BufferedTokenReader("");
            var succeeded = recognizer.TryRecognize(reader, out var result);
            var trueResult = result as Parser.Recognizers.IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(1, trueResult.Symbols.Length);
            Assert.AreEqual(
                "",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));

            // 2
            recognizer = new EOFRecognizer();
            reader = new BufferedTokenReader("blehri");
            _ = reader.TryNextTokens(6, out _);
            succeeded = recognizer.TryRecognize(reader, out result);
            trueResult = result as Parser.Recognizers.IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(1, trueResult.Symbols.Length);
            Assert.AreEqual(
                "",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));
        }

        [TestMethod]
        public void TryRecognize_WithFailingInput_ShouldReturnFailedResult()
        {
            // 1
            var recognizer = new EOFRecognizer();
            var reader = new BufferedTokenReader("gfdfd");
            var recognized = recognizer.TryRecognize(reader, out var result);
            var trueResult = result as Parser.Recognizers.IResult.FailedRecognition;

            Assert.IsFalse(recognized);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
        }
    }
}
