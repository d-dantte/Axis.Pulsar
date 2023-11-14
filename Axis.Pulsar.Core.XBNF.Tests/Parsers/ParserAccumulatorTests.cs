using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF.Tests;


[TestClass]
public class ParserAccumulatorTests
{
    [TestMethod]
    public void Construction_Tests()
    {
        var metaContext = MetaContext.Builder.NewBuilder().Build();

        #region Argument exceptions
        Assert.ThrowsException<ArgumentNullException>(() => ParserAccumulator.Of(
            null!,
            metaContext,
            0));

        Assert.ThrowsException<ArgumentNullException>(() => ParserAccumulator.Of(
            "something",
            null!,
            0));

        Assert.ThrowsException<ArgumentNullException>(() => ParserAccumulator.OfAlternative(
            null!,
            metaContext,
            0));

        Assert.ThrowsException<ArgumentNullException>(() => ParserAccumulator.OfAlternative(
            "something",
            null!,
            0));
        #endregion

        var accummulator = ParserAccumulator.Of(
            "stuff",
            metaContext,
            12);

        Assert.AreEqual(metaContext, accummulator.Context);
        Assert.AreEqual("stuff", accummulator.Reader.Source);
        Assert.AreEqual(12, accummulator.Data);

        accummulator = ParserAccumulator.OfAlternative(
            "stuff",
            metaContext,
            12,
            "x-zymbol");

        Assert.AreEqual(metaContext, accummulator.Context);
        Assert.AreEqual("stuff", accummulator.Reader.Source);
        Assert.AreEqual(12, accummulator.Data);
        Assert.IsTrue(accummulator.IsPreviousOpErrored);
        Assert.IsTrue(accummulator.IsPreviousOpUnmatched);
        Exception error = null!;
        accummulator.ConsumeError(err => error = err);
        Assert.IsNotNull(error);
        Assert.AreEqual("x-zymbol", (error as UnmatchedError)!.ExpectedSymbol);
    }

    [TestMethod]
    public void ThenTry_Tests()
    {
        var metaContext = MetaContext.Builder.NewBuilder().Build();
        var accummulator = ParserAccumulator.Of(
            "stuff",
            metaContext,
            12);
        
        _ = accummulator.ThenTry<int>(
            TryParseSuccess,
            (x, y) => x + y);

        Assert.IsFalse(accummulator.IsPreviousOpErrored);
        Assert.AreEqual(12, accummulator.Data);
        
        _ = accummulator.ThenTry<int>(
            TryParseUnmatched,
            (x, y) => x + y,
            x => 14);

        Assert.IsFalse(accummulator.IsPreviousOpErrored);
        Assert.AreEqual(14, accummulator.Data);
        
        _ = accummulator.ThenTry<int>(
            TryParseUnmatched,
            (x, y) => x + y);

        Assert.IsTrue(accummulator.IsPreviousOpErrored);
        Assert.IsTrue(accummulator.IsPreviousOpUnmatched);
        Assert.AreEqual(14, accummulator.Data); // <-- retains old value
        
        accummulator = ParserAccumulator
            .Of("stuff", metaContext, 12)
            .ThenTry<int>(
                TryParseFaultyMatch,
                (x, y) => 20);

        Assert.IsTrue(accummulator.IsPreviousOpFaultyMatch);
        Assert.AreEqual(12, accummulator.Data); // <-- retains old value
        
        accummulator = ParserAccumulator
            .Of("stuff", metaContext, 12)
            .ThenTry<int>(
                TryParseFaultyMatch,
                (x, y) => 20,
                x => x);

        Assert.IsTrue(accummulator.IsPreviousOpFaultyMatch);
        Assert.AreEqual(12, accummulator.Data); // <-- retains old value
        
        accummulator = ParserAccumulator
            .Of("stuff", metaContext, 12)
            .ThenTry<int>(
                TryParseUnknown,
                (x, y) => 20);

        Assert.IsTrue(accummulator.IsPreviousOpUnknown);
        Assert.AreEqual(12, accummulator.Data); // <-- retains old value
        
        accummulator = ParserAccumulator
            .Of("stuff", metaContext, 12)
            .ThenTry<int>(
                TryParseUnknown,
                (x, y) => 20,
                x => x);

        Assert.IsTrue(accummulator.IsPreviousOpUnknown);
        Assert.AreEqual(12, accummulator.Data); // <-- retains old value
    }

    [TestMethod]
    public void OrTry_Tests()
    {
        var metaContext = MetaContext.Builder.NewBuilder().Build();
        var accummulator = ParserAccumulator.Of(
            "stuff",
            metaContext,
            12);
        
        _ = accummulator.OrTry<int>(
            TryParseSuccess,
            (x, y) => x + y);

        Assert.IsFalse(accummulator.IsPreviousOpErrored);
        Assert.AreEqual(12, accummulator.Data); // previous op succeeded, so alternative is ignored
        
        
        accummulator = ParserAccumulator
            .OfAlternative("stuff", metaContext, 12)
            .OrTry<int>(
                TryParseSuccess,
                (x, y) => x + y);

        Assert.IsFalse(accummulator.IsPreviousOpErrored);
        Assert.AreEqual(12, accummulator.Data);
        
        
        accummulator = ParserAccumulator
            .OfAlternative("stuff", metaContext, 12)
            .OrTry<int>(
                TryParseUnmatched,
                (x, y) => x + y,
                x => 3);

        Assert.IsFalse(accummulator.IsPreviousOpErrored);
        Assert.AreEqual(3, accummulator.Data);
        
        
        accummulator = ParserAccumulator
            .OfAlternative("stuff", metaContext, 12)
            .OrTry<int>(
                TryParseFaultyMatch,
                (x, y) => x + y,
                x => 3);

        Assert.IsTrue(accummulator.IsPreviousOpFaultyMatch);
        
        
        accummulator = ParserAccumulator
            .OfAlternative("stuff", metaContext, 12)
            .OrTry<int>(
                TryParseUnknown,
                (x, y) => x + y,
                x => 3);

        Assert.IsTrue(accummulator.IsPreviousOpUnknown);
    }

    private static bool TryParseSuccess<TData>(
        TokenReader reader,
        MetaContext context,
        out IResult<TData> result)
    {
        result = Result.Of(default(TData)!);
        return true;
    }

    private static bool TryParseUnmatched<TData>(
        TokenReader reader,
        MetaContext context,
        out IResult<TData> result)
    {
        result = Result.Of<TData>(new UnmatchedError("symbol", 0));
        return false;
    }

    private static bool TryParseFaultyMatch<TData>(
        TokenReader reader,
        MetaContext context,
        out IResult<TData> result)
    {
        result = Result.Of<TData>(new FaultyMatchError("symbol", 0, 1));
        return false;
    }

    private static bool TryParseUnknown<TData>(
        TokenReader reader,
        MetaContext context,
        out IResult<TData> result)
    {
        result = Result.Of<TData>(new UnknownError(new Exception()));
        return false;
    }
}
