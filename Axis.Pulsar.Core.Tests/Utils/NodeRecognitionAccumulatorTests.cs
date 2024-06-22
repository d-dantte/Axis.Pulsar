using Axis.Luna.Unions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils;

using FailedError = FailedRecognitionError;
using PartialError = PartialRecognitionError;

[TestClass]
public class NodeRecognitionAccumulatorTests
{
    [TestMethod]
    public void Construction_Tests()
    {
        var accummulator = NodeRecognitionAccumulator.Of<int, string, object>(0);
        Assert.IsFalse(accummulator.IsDefault);
        Assert.IsTrue(accummulator.CanTryRequired);
        Assert.IsTrue(accummulator.CanTryAlternatives);
        Assert.AreEqual(0, accummulator.Data);

        accummulator = NodeRecognitionAccumulator.Of<int, string, object>(6);
        Assert.IsFalse(accummulator.IsDefault);
        Assert.IsTrue(accummulator.CanTryRequired);
        Assert.IsTrue(accummulator.CanTryAlternatives);

        accummulator = NodeRecognitionAccumulator<int, string, object>.Default;
        Assert.IsTrue(accummulator.IsDefault);
        Assert.AreEqual(default, accummulator);
    }

    [TestMethod]
    public void ThenTry_Tests()
    {
        var accummulator = NodeRecognitionAccumulator.Of<int, string, object>(12);

        #region TryParseSuccess
        var rAccumulator = accummulator.ThenTry<int, SRR>(
            TryParseSuccess,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1);
        Assert.IsTrue(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(14, rAccumulator.Data);

        rAccumulator = accummulator.ThenTry<int, SRR>(
            TryParseSuccess,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1,
            (x, y) => 2 * x);
        Assert.IsTrue(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(14, rAccumulator.Data);
        #endregion

        #region TryParseFailed
        rAccumulator = accummulator.ThenTry<int, SRR>(
            TryParseFailed,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1);
        Assert.IsFalse(rAccumulator.CanTryRequired);
        Assert.IsTrue(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(12, rAccumulator.Data);

        rAccumulator = accummulator.ThenTry<int, SRR>(
            TryParseFailed,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1,
            (x, y) => 2 * x);
        Assert.IsTrue(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(24, rAccumulator.Data);
        #endregion

        #region TryParsePartial
        rAccumulator = accummulator.ThenTry<int, SRR>(
            TryParsePartial,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1);
        Assert.IsFalse(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(12, rAccumulator.Data);

        rAccumulator = accummulator.ThenTry<int, SRR>(
            TryParsePartial,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1,
            (x, y) => 2 * x);
        Assert.IsFalse(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(12, rAccumulator.Data);
        #endregion
    }

    [TestMethod]
    public void OrTry_Tests()
    {
        var accumulator = NodeRecognitionAccumulator.Of<int, string, object>(12);

        #region TryParseSuccess
        var rAccumulator = accumulator.OrTry<int, SRR>(
            TryParseSuccess,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1);
        Assert.IsTrue(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(14, rAccumulator.Data);

        rAccumulator = accumulator.OrTry<int, SRR>(
            TryParseSuccess,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1,
            (x, y) => 2 * x);
        Assert.IsTrue(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(14, rAccumulator.Data);

        var xAccumulator = rAccumulator.OrTry<int, SRR>(
            TryParseSuccess,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1,
            (x, y) => 2 * x);
        Assert.AreEqual(rAccumulator, xAccumulator);
        #endregion

        #region TryParseFailed
        rAccumulator = accumulator.OrTry<int, SRR>(
            TryParseFailed,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1);
        Assert.IsFalse(rAccumulator.CanTryRequired);
        Assert.IsTrue(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(12, rAccumulator.Data);

        // when CanTryRequired is false, and CanTryAlternatives is true
        rAccumulator = rAccumulator.OrTry<int, SRR>(
            TryParseSuccess,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1);
        Assert.IsTrue(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(14, rAccumulator.Data);

        rAccumulator = accumulator.ThenTry<int, SRR>(
            TryParseFailed,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1,
            (x, y) => 2 * x);
        Assert.IsTrue(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(24, rAccumulator.Data);
        #endregion

        #region TryParsePartial
        rAccumulator = accumulator.OrTry<int, SRR>(
            TryParsePartial,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1);
        Assert.IsFalse(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(12, rAccumulator.Data);

        rAccumulator = accumulator.ThenTry<int, SRR>(
            TryParsePartial,
            NodeRecognitionAccumulator.Args("stuff", "symbol", new object()),
            (x, y) => x + y + 1,
            (x, y) => 2 * x);
        Assert.IsFalse(rAccumulator.CanTryRequired);
        Assert.IsFalse(rAccumulator.CanTryAlternatives);
        Assert.AreEqual(12, rAccumulator.Data);
        #endregion
    }

    [TestMethod]
    public void TryIf_Tests()
    {
        var accumulator = NodeRecognitionAccumulator.Of<int, SymbolPath, object>(12);
        NodeRecognitionAccumulator.TryParse<NodeRecognitionResult, ISymbolNode, SymbolPath, object> parseOperation =
            (TokenReader a, SymbolPath b, object c, out NodeRecognitionResult d) =>
                !(d = NodeRecognitionResult.Of(ISymbolNode.Of("a", "b"))).Equals(null);

        var raccumulator = accumulator.TryIf(
            parseOperation,
            new RecognitionArgs<SymbolPath, object>("abcd", "path", null!),
            (_, _, _, _) => true,
            (_, _) => 0);
        Assert.AreNotEqual(accumulator, raccumulator);

        raccumulator = accumulator.TryIf(
            parseOperation,
            new RecognitionArgs<SymbolPath, object>("abcd", "path", null!),
            (_, _, _, _) => false,
            (_, _) => 0);
        Assert.AreEqual(accumulator, raccumulator);
    }

    [TestMethod]
    public void MapAll_Tests()
    {
        var naccummulator = NodeRecognitionAccumulator.Of<int, string, object>(12);
        var faccummulator = NodeRecognitionAccumulator.OfFailed<int, string, object>(
            FailedError.Of("path", 0));
        var paccummulator = NodeRecognitionAccumulator.OfPartial<int, string, object>(
            PartialError.Of("path", 0, 1));

        var x = naccummulator.MapAll(
            data => data * 2,
            (_, _) => 0,
            (_, _) => 0);
        Assert.AreEqual(24, x);

        x = faccummulator.MapAll(
            _ => 0,
            (_, data) => data + 36,
            (_, _) => 0);
        Assert.AreEqual(36, x);

        x = paccummulator.MapAll(
            _ => 0,
            (_, _) => 0,
            (_, data) => data + 48);
        Assert.AreEqual(48, x);
    }

    [TestMethod]
    public void ConsumeAll_Tests()
    {
        var naccummulator = NodeRecognitionAccumulator.Of<int, string, object>(12);
        var faccummulator = NodeRecognitionAccumulator.OfFailed<int, string, object>(
            FailedError.Of("path", 0));
        var paccummulator = NodeRecognitionAccumulator.OfPartial<int, string, object>(
            PartialError.Of("path", 0, 1));

        var x = 0;
        naccummulator.ConsumeAll(
            data => x = data * 2,
            (_, _) => {},
            (_, _) => {});
        Assert.AreEqual(24, x);

        x = 0;
        faccummulator.ConsumeAll(
            _ => {},
            (_, data) => x = data + 36,
            (_, _) => {});
        Assert.AreEqual(36, x);

        x = 0;
        paccummulator.ConsumeAll(
            _ => {},
            (_, _) => {},
            (_, data) => x = data + 48);
        Assert.AreEqual(48, x);
    }

    private static bool TryParseSuccess(
        TokenReader reader,
        string symbol,
        object context,
        out SRR result)
    {
        result = new SRR(1);
        return true;
    }

    private static bool TryParseFailed(
        TokenReader reader,
        string symbol,
        object context,
        out SRR result)
    {
        result = new SRR(new FailedRecognitionError("symbol", 0));
        return false;
    }

    private static bool TryParsePartial(
        TokenReader reader,
        string symbol,
        object context,
        out SRR result)
    {
        result = new SRR(new PartialRecognitionError("symbol", 0, 1));
        return false;
    }

    internal class SRR : INodeRecognitionResult<int, SRR>
    {
        private readonly object? _value;

        object IUnion<int, FailedError, PartialError, SRR>.Value => _value!;


        public SRR(object value)
        {
            _value = value switch
            {
                null => null,
                FailedError
                or PartialError
                or int => value,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Invalid {nameof(value)} type: '{value.GetType()}'")
            };
        }

        public static PathParserResult<int> Of(
            int value)
            => new(value!);

        public static PathParserResult<int> Of(
            FailedError value)
            => new(value);

        public static PathParserResult<int> Of(
            PartialError value)
            => new(value);


        public bool Is(out int value)
        {
            if (_value is int n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Is(out FailedError value)
        {
            if (_value is FailedError n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Is(out PartialError value)
        {
            if (_value is PartialError n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool IsNull() => _value is null;

        public TOut MapMatch<TOut>(
            Func<int, TOut> nodeMapper,
            Func<FailedError, TOut> failedErrorMapper,
            Func<PartialError, TOut> partialErrorMapper,
            Func<TOut>? nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(nodeMapper);
            ArgumentNullException.ThrowIfNull(failedErrorMapper);

            if (_value is int t1)
                return nodeMapper.Invoke(t1);

            if (_value is FailedError t2)
                return failedErrorMapper.Invoke(t2);

            if (_value is PartialError t3)
                return partialErrorMapper.Invoke(t3);

            // unknown type, assume null
            return nullMapper switch
            {
                null => default!,
                _ => nullMapper.Invoke()
            };
        }

        public void ConsumeMatch(
            Action<int> resultConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action? nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(resultConsumer);
            ArgumentNullException.ThrowIfNull(failedErrorConsumer);
            ArgumentNullException.ThrowIfNull(partialErrorConsumer);

            if (_value is int t1)
                resultConsumer.Invoke(t1);

            else if (_value is FailedError t2)
                failedErrorConsumer.Invoke(t2);

            else if (_value is PartialError t3)
                partialErrorConsumer.Invoke(t3);

            else if (_value is null && nullConsumer is not null)
                nullConsumer.Invoke();
        }

        public SRR WithMatch(
            Action<int> resultConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action? nullConsumer = null!)
        {
            ConsumeMatch(resultConsumer, failedErrorConsumer, partialErrorConsumer, nullConsumer);
            return this;
        }
    }
}
