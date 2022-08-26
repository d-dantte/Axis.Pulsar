using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axis.Pulsar.Parser.Recognizers;
using System;

namespace Axis.Pulsar.Parser.Tests.Recognizers
{
    [TestClass]
    public class ChoiceRecognizerTests
    {
        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = new ChoiceRecognizer(
                Cardinality.OccursOnlyOnce(),
                ("symbol", "token").CreatePassingRecognizer());

            Assert.IsNotNull(parser);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), parser.Cardinality);
        }


        [TestMethod]
        public void TryRecognize_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = new ChoiceRecognizer(
                Cardinality.OccursOnlyOnce(),
                new IResult.FailedRecognition(2, 1).CreateRecognizer(),
                ("public-symbol", "public").CreatePassingRecognizer(),
                ("private-symbol", "private").CreatePassingRecognizer(),
                ("protected-symbol", "protected").CreatePassingRecognizer(),
                ("internal-symbol", "internal").CreatePassingRecognizer());

            // 1
            var reader = new BufferedTokenReader("irrelevant");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(1, trueResult.Symbols.Length);
            Assert.AreEqual("public", trueResult.Symbols[0].TokenValue());

            // 2
            parser = new ChoiceRecognizer(
                Cardinality.OccursOnly(2),
                //CreateRecognizer(new IResult.Exception(new Exception(), 1)),
                (new IResult.FailedRecognition(2, 1)).CreateRecognizer(),
                ("private-symbol", "private").CreatePassingRecognizer(),
                ("protected-symbol", "protected").CreatePassingRecognizer(),
                ("internal-symbol", "internal").CreatePassingRecognizer());
            succeeded = parser.TryRecognize(reader, out result);
            trueResult = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(2, trueResult.Symbols.Length);
            Assert.AreEqual("private", trueResult.Symbols[0].TokenValue());
            Assert.AreEqual("private", trueResult.Symbols[1].TokenValue());
        }


        [TestMethod]
        public void TryRecognize_WithInvalidInput_Should_ReturnFailedParseResult()
        {
            var parser = new ChoiceRecognizer(
                Cardinality.OccursOnlyOnce(),
                new IResult.FailedRecognition(2, 1).CreateRecognizer(),
                new IResult.FailedRecognition(3, 1).CreateRecognizer());

            // 1
            var reader = new BufferedTokenReader("irrelevant");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(0, trueResult.RecognitionCount);
            Assert.AreEqual(0, trueResult.InputPosition);
        }


        [TestMethod]
        public void TryRecognize_WithFatalInput_Should_ReturnExceptionParseResult()
        {
            var parser = new ChoiceRecognizer(
                Cardinality.OccursOnlyOnce(),
                new IResult.FailedRecognition(2, 1).CreateRecognizer(),
                new IResult.Exception(new SomeObscureException(), 1).CreateRecognizer(),
                new IResult.FailedRecognition(3, 1).CreateRecognizer());

            // 1
            var reader = new BufferedTokenReader("irrelevant");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as IResult.Exception;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.IsNotNull(trueResult.Error);
            Assert.IsTrue(trueResult.Error is SomeObscureException);
            Assert.AreEqual(1, trueResult.InputPosition);
        }
    }

    internal class SomeObscureException: Exception
    { }
}
