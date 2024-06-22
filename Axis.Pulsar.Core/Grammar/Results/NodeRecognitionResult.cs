using Axis.Luna.Unions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    using FailedError = FailedRecognitionError;
    using PartialError = PartialRecognitionError;

    public readonly struct NodeRecognitionResult :
        INodeRecognitionResult<ISymbolNode, NodeRecognitionResult>,
        IUnionOf<ISymbolNode, FailedError, PartialError, NodeRecognitionResult>
    {
        private readonly object? _value;

        object IUnion<ISymbolNode, FailedError, PartialError, NodeRecognitionResult>.Value => _value!;

        #region Construction

        /// <summary>
        /// This method expects the Of(...) methods to never pass in a value that is not
        /// an ISymbolNode, FailedError, or PartialError.
        /// </summary>
        /// <param name="value"></param>
        private NodeRecognitionResult(object value)
        {
            _value = value;
        }

        /// <summary>
        /// Rejects null nodes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NodeRecognitionResult Of(ISymbolNode value) => value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            _ => new(value!)
        };

        public static NodeRecognitionResult Of(FailedError value) => new(value);

        public static NodeRecognitionResult Of(PartialError value) => new(value);
        #endregion

        public bool Is(out ISymbolNode value)
        {
            if(_value is ISymbolNode n)
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

        #region Map
        public TOut Map<TIn, TOut>(Func<TIn, TOut> seqFunc)
        {
            ArgumentNullException.ThrowIfNull(seqFunc);

            if (_value is TIn @in)
                return seqFunc.Invoke(@in);

            throw new InvalidOperationException($"Invalid map operation");
        }

        public TOut MapMatch<TOut>(
            Func<ISymbolNode, TOut> nodeMapper,
            Func<FailedError, TOut> failedErrorMapper,
            Func<PartialError, TOut> partialErrorMapper,
            Func<TOut>? nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(nodeMapper);
            ArgumentNullException.ThrowIfNull(failedErrorMapper);
            ArgumentNullException.ThrowIfNull(partialErrorMapper);

            if (_value is ISymbolNode t1)
                return nodeMapper.Invoke(t1);

            if (_value is FailedError t2)
                return failedErrorMapper.Invoke(t2);

            if (_value is PartialError t4)
                return partialErrorMapper.Invoke(t4);

            // unknown type, assume null
            return nullMapper switch
            {
                null => default!,
                _ => nullMapper.Invoke()
            };
        }
        #endregion

        #region Consume
        public void Consume<TIn>(Action<TIn> seqFunc)
        {
            ArgumentNullException.ThrowIfNull(seqFunc);

            if (_value is TIn @in)
                seqFunc.Invoke(@in);
            else
                throw new InvalidOperationException($"Invalid consume operation");
        }

        public void ConsumeMatch(
            Action<ISymbolNode> nodeConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action? nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(nodeConsumer);
            ArgumentNullException.ThrowIfNull(failedErrorConsumer);
            ArgumentNullException.ThrowIfNull(partialErrorConsumer);

            if (_value is ISymbolNode t1)
                nodeConsumer.Invoke(t1);

            else if (_value is FailedError t2)
                failedErrorConsumer.Invoke(t2);

            else if (_value is PartialError t4)
                partialErrorConsumer.Invoke(t4);

            else if (nullConsumer is not null)
                nullConsumer.Invoke();
        }
        #endregion

        #region With
        public TIn With<TIn>(Action<TIn> seqFunc)
        {
            ArgumentNullException.ThrowIfNull(seqFunc);

            if (_value is TIn @in)
            {
                seqFunc.Invoke(@in);
                return @in;
            }
            else
                throw new InvalidOperationException($"Invalid consume operation");
        }

        public NodeRecognitionResult WithMatch(
            Action<ISymbolNode> nodeConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action? nullConsumer = null!)
        {
            ConsumeMatch(
                nodeConsumer,
                failedErrorConsumer,
                partialErrorConsumer,
                nullConsumer);

            return this;
        }
        #endregion

        #region Get
        public TOut Get<TOut>()
        {
            if (_value is TOut @out)
                return @out;
            else
                throw new InvalidOperationException($"Invalid consume operation");
        }
        #endregion
    }
}
