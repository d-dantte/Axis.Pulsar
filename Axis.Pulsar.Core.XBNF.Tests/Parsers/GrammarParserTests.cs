using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axis.Luna.Common.Results;

namespace Axis.Pulsar.Core.XBNF.Tests.Parsers
{
    [TestClass]
    public class GrammarParserTests
    {
        #region Silent elements

        [TestMethod]
        public void TryParseWhtespsace_Tests()
        {            
            var metaContext = MetaContext.Builder.NewBuilder().Build();

            // new line
            var success = GrammarParser.TryParseWhitespace(
                "\n",
                metaContext,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var ws = result.Resolve();
            Assert.AreEqual(Whitespace.WhitespaceChar.LineFeed, ws.Char);
        }
        #endregion
    }
}
