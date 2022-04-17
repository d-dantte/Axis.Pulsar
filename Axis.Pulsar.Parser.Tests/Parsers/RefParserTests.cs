using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    //[TestClass]
    public class RefParserTests
    {
    //    private static readonly LiteralParser CatchKeyWordParser = new(
    //        "catch",
    //        new LiteralRule(
    //            "catch",
    //            false));

    //    private static readonly PatternMatcherParser VariableParser = new(
    //        "variable",
    //        new PatternRule(
    //            new Regex("^[a-z_\\$]\\w{0,4}$", RegexOptions.IgnoreCase),
    //            new(1, 5)));

    //    private static Grammar.Grammar BuildTestContext(string root, params KeyValuePair<string, IParser>[] parsers)
    //    {
    //        var context = new Moq.Mock<IGrammarContext>();
    //        context
    //            .SetupGet(c => c.RootName)
    //            .Returns(root);

    //        parsers.ForAll(parser =>
    //        {
    //            var name = parser.Key;
    //            context
    //                .Setup(c => c.GetParser(name))
    //                .Returns(parser.Value);
    //        });

    //        context
    //            .Setup(c => c.RootParser())
    //            .Returns(parsers.First(p => p.Key.Equals(root)).Value);

    //        context
    //            .SetupGet(c => c.ProductionNames)
    //            .Returns(parsers.Select(kvp => kvp.Key));

    //        return context.Object;
    //    }

    //    private static ParserRecognizer NewRefParser(
    //        string sourceParser,
    //        IGrammarContext context)
    //        => new RefParser(sourceParser).SetGrammarContext(context);

    //    private static ParserRecognizer NewRefParser(
    //        Cardinality cardinality,
    //        string sourceParser,
    //        IGrammarContext context)
    //        => new RefParser(cardinality, sourceParser).SetGrammarContext(context);

    //    [TestMethod]
    //    public void Constructor_Should_ReturnValidObject()
    //    {
    //        var context = BuildTestContext(
    //            "catch",
    //            new[]
    //            {
    //                new KeyValuePair<string, IParser>("catch", CatchKeyWordParser),
    //                new KeyValuePair<string, IParser>("variable", VariableParser)
    //            });
    //        var parser = NewRefParser("catch", context);
    //        Assert.IsNotNull(parser);
    //    }


    //    [TestMethod]
    //    public void TryParse_WithValidInput_Should_ReturnValidParseResult()
    //    {
    //        var context = BuildTestContext(
    //            "catch",
    //            new[]
    //            {
    //                new KeyValuePair<string, IParser>("catch", CatchKeyWordParser),
    //                new KeyValuePair<string, IParser>("variable", VariableParser)
    //            });

    //        var parser = NewRefParser(
    //            Cardinality.OccursOnlyOnce(),
    //            "variable",
    //            context);

    //        var reader = new BufferedTokenReader("varia = 5;");
    //        var succeeded = parser.TryParse(reader, out var result);

    //        Assert.IsTrue(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNull(result.Error);
    //        Assert.IsNotNull(result.Symbol);
    //        Assert.AreEqual(result.Symbol.Value, "varia");

    //        //test 2
    //        parser = NewRefParser(
    //            Cardinality.OccursAtLeast(2),
    //            "variable",
    //            context);

    //        reader = new BufferedTokenReader("varianuria = 5;");
    //        succeeded = parser.TryParse(reader, out result);

    //        Assert.IsTrue(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNull(result.Error);
    //        Assert.IsNotNull(result.Symbol);
    //        Assert.AreEqual(result.Symbol.Value, "varianuria");

    //        //test 3
    //        parser = NewRefParser(
    //            Cardinality.OccursAtLeast(2),
    //            "variable",
    //            context);

    //        reader = new BufferedTokenReader("varianuriatamaria = 5;");
    //        succeeded = parser.TryParse(reader, out result);

    //        Assert.IsTrue(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNull(result.Error);
    //        Assert.IsNotNull(result.Symbol);
    //        Assert.AreEqual(result.Symbol.Value, "varianuriatamaria");

    //        //test 4
    //        parser = NewRefParser(
    //            Cardinality.OccursNeverOrAtMost(3),
    //            "variable",
    //            context);

    //        reader = new BufferedTokenReader("{varianuriatamaria = 5;}");
    //        succeeded = parser.TryParse(reader, out result);

    //        Assert.IsTrue(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNull(result.Error);
    //        Assert.IsNotNull(result.Symbol);
    //        Assert.AreEqual(result.Symbol.Value, "");

    //        //test 5
    //        parser = NewRefParser(
    //            Cardinality.OccursNeverOrAtMost(3),
    //            "variable",
    //            context);

    //        reader = new BufferedTokenReader("varianuriatamaria = 5;");
    //        succeeded = parser.TryParse(reader, out result);

    //        Assert.IsTrue(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNull(result.Error);
    //        Assert.IsNotNull(result.Symbol);
    //        Assert.AreEqual(result.Symbol.Value, "varianuriatamar");
    //    }


    //    [TestMethod]
    //    public void TryParse_WithinvalidInput_Should_ReturnErroredParseResult()
    //    {
    //        var context = BuildTestContext(
    //            "catch",
    //            new[]
    //            {
    //                new KeyValuePair<string, IParser>("catch", CatchKeyWordParser),
    //                new KeyValuePair<string, IParser>("variable", VariableParser)
    //            });

    //        var parser = NewRefParser(
    //            Cardinality.OccursOnlyOnce(),
    //            "variable",
    //            context);

    //        var reader = new BufferedTokenReader("= 5;");
    //        var succeeded = parser.TryParse(reader, out var result);

    //        Assert.IsFalse(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNotNull(result.Error);
    //        Assert.IsNull(result.Symbol);

    //        //test 2
    //        parser = NewRefParser(
    //            Cardinality.OccursAtLeast(2),
    //            "variable",
    //            context);

    //        reader = new BufferedTokenReader(" = 5;");
    //        succeeded = parser.TryParse(reader, out result);

    //        Assert.IsFalse(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNotNull(result.Error);
    //        Assert.IsNull(result.Symbol);

    //        //test 3
    //        parser = NewRefParser(
    //            Cardinality.OccursAtLeast(2),
    //            "variable",
    //            context);

    //        reader = new BufferedTokenReader("varia = 5;");
    //        succeeded = parser.TryParse(reader, out result);

    //        Assert.IsFalse(succeeded);
    //        Assert.IsNotNull(result);
    //        Assert.IsNotNull(result.Error);
    //        Assert.IsNull(result.Symbol);
    //    }
    }
}
