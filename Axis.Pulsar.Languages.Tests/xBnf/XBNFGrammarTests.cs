using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Axis.Pulsar.Languages.xBNF;

namespace Axis.Pulsar.Languages.Tests.xBnf
{
    [TestClass]
    public class XBNFGrammarTests
    {
        private static Importer XBNFImporter = new Importer();

        #region Terminals

        [TestMethod]
        public void EOF_Tests()
        {
            var eofRecognizer = XBNFImporter.ImporterGrammar.GetRecognizer("eof");
            var result = eofRecognizer.Recognize("EOF");
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
        }

        [DataRow("hash", "#")]
        [DataRow("plus", "+")]
        [DataRow("fslash", "/")]
        [DataRow("arrow", "->")]
        [DataRow("assign", "::=")]
        [DataRow("comma", ",")]
        [DataRow("ignore-whitespace-flag", "x")]
        [DataRow("single-line-flag", "s")]
        [DataRow("multi-line-flag", "m")]
        [DataRow("explicit-capture-flag", "n")]
        [DataRow("ignore-case-flag", "i")]
        [DataTestMethod]
        public void Literal_Tests(string symbol, string value)
        {
            var recognizer = XBNFImporter.ImporterGrammar.GetRecognizer(symbol);
            var result = recognizer.Recognize(value);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
        }

        #endregion

        #region Non-Terminals
        [TestMethod]
        public void CaseSensitiveLiteral_Tests()
        {
            var cslRecognizer = XBNFImporter.ImporterGrammar.GetRecognizer("case-sensitive");
            var result = cslRecognizer.Recognize("\"stuff\\nother stuff\\u000f\"");
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Console.WriteLine(success.Symbol.TokenValue());
        }
        #endregion
    }
}
