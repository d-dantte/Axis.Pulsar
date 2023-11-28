using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils;

[TestClass]
public class ParserAccumulatorTests
{
    [TestMethod]
    public void Construction_Tests()
    {

        #region Argument exceptions
        Assert.ThrowsException<ArgumentNullException>(() => ParserAccumulator.Of(
            null!,
            "symbol",
            new object(),
            0));

        Assert.ThrowsException<ArgumentNullException>(() => ParserAccumulator.Of(
            "token reader",
            default(string)!,
            new object(),
            0));

        Assert.ThrowsException<ArgumentNullException>(() => ParserAccumulator.Of(
            "token reader",
            "symbol",
            default(object)!,
            0));
        #endregion

        var cxt = new object();
        var accummulator = ParserAccumulator.Of(
            "token reader",
            "symbol",
            cxt,
            12);

        Assert.IsFalse(accummulator.IsErrored);
        Assert.IsFalse(accummulator.IsFailedRecognitionError);
    }

    [TestMethod]
    public void ThenTry_Tests()
    {
        #region TryParseSuccess

        var accummulator = ParserAccumulator.Of(
            "stuff",
            "symbol",
            new object(),
            12);

        _ = accummulator.ThenTry<int>(
            TryParseSuccess,
            (x, y) => x + y + 1);
        Assert.IsFalse(accummulator.IsErrored);
        var data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(13, data);

        _ = accummulator.ThenTry<int>(
            TryParseSuccess,
            (x, y) => x + y + 1,
            x => 2 * x);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(14, data);
        #endregion

        #region TryParseFailed
        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(
                TryParseFailed,
                (x, y) => x + y);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(
                TryParseFailed,
                (x, y) => x + y,
                x => 14);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(14, data);
        #endregion

        #region TryParsePartial
        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(
                TryParsePartial,
                (x, y) => 20);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(
                TryParsePartial,
                (x, y) => 20,
                x => x * 8);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);
        #endregion

        #region TryParseUnknown
        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(
                TryParseUnknown,
                (x, y) => 20);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(
                TryParseUnknown,
                (x, y) => 20,
                x => x * 8);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);
        #endregion
    }

    [TestMethod]
    public void OrTry_Tests()
    {
        #region TryParseSuccess

        var accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .OrTry<int>(
                TryParseSuccess,
                (x, y) => x + y + 1);
        Assert.IsFalse(accummulator.IsErrored);
        var data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(12, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParseSuccess,
                (x, y) => x + y + 1);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(13, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParseFailed,
                (x, y) => x + y + 1);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParseFailed,
                (x, y) => x + y + 1,
                x => x * 2);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(24, data);
        #endregion

        #region TryParseFailed

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .OrTry<int>(
                TryParseFailed,
                (x, y) => x + y + 1);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(12, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParseFailed,
                (x, y) => 1);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParseFailed,
                (x, y) => 1,
                x => 2);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(2, data);

        #endregion

        #region TryParsePartial

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .OrTry<int>(
                TryParsePartial,
                (x, y) => x + y + 1);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(12, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParsePartial,
                (x, y) => 1);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParsePartial,
                (x, y) => 1,
                x => 2);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        #endregion

        #region TryParseUnknown

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .OrTry<int>(
                TryParseUnknown,
                (x, y) => x + y + 1);
        Assert.IsFalse(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(12, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParseUnknown,
                (x, y) => 1);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);

        accummulator = ParserAccumulator
            .Of("stuff", "symbol", new object(), 12)
            .ThenTry<int>(TryParseFailed, (x, y) => 0)
            .OrTry<int>(
                TryParseUnknown,
                (x, y) => 1,
                x => 2);
        Assert.IsTrue(accummulator.IsErrored);
        data = 0;
        accummulator.Consume(d => data = d);
        Assert.AreEqual(0, data);
        #endregion
    }

    private static bool TryParseSuccess<TData>(
        TokenReader reader,
        string symbol,
        object context,
        out IResult<TData> result)
    {
        result = Result.Of(default(TData)!);
        return true;
    }

    private static bool TryParseFailed<TData>(
        TokenReader reader,
        string symbol,
        object context,
        out IResult<TData> result)
    {
        result = Result.Of<TData>(new FailedRecognitionError("symbol", 0));
        return false;
    }

    private static bool TryParsePartial<TData>(
        TokenReader reader,
        string symbol,
        object context,
        out IResult<TData> result)
    {
        result = Result.Of<TData>(new PartialRecognitionError("symbol", 0, 1));
        return false;
    }

    private static bool TryParseUnknown<TData>(
        TokenReader reader,
        string symbol,
        object context,
        out IResult<TData> result)
    {
        result = Result.Of<TData>(new Exception());
        return false;
    }
}
