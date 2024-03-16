using Axis.Luna.Unions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    using FailedError = FailedRecognitionError;
    using PartialError = PartialRecognitionError;

    public readonly struct NodeRecognitionResult :
        INodeRecognitionResultBase<ICSTNode, NodeRecognitionResult>,
        IUnionOf<ICSTNode, FailedError, PartialError, NodeRecognitionResult>
    {
        private readonly object? _value;

        object IUnion<ICSTNode, FailedError, PartialError, NodeRecognitionResult>.Value => _value!;

        #region Construction

        private NodeRecognitionResult(object value)
        {
            _value = value switch
            {
                null => null,
                FailedError 
                or PartialError
                or ICSTNode => value,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Invalid {nameof(value)} type: '{value.GetType()}'")
            };
        }

        /// <summary>
        /// Rejects null nodes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NodeRecognitionResult Of(ICSTNode value) => value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            _ => new(value)
        };

        public static NodeRecognitionResult Of(FailedError value) => new(value);

        public static NodeRecognitionResult Of(PartialError value) => new(value);
        #endregion

        public bool Is(out ICSTNode value)
        {
            if(_value is ICSTNode n)
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
            Func<ICSTNode, TOut> nodeMapper,
            Func<FailedError, TOut> failedErrorMapper,
            Func<PartialError, TOut> partialErrorMapper,
            Func<TOut> nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(nodeMapper);
            ArgumentNullException.ThrowIfNull(failedErrorMapper);
            ArgumentNullException.ThrowIfNull(partialErrorMapper);

            if (_value is ICSTNode t1)
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
            Action<ICSTNode> nodeConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(nodeConsumer);
            ArgumentNullException.ThrowIfNull(failedErrorConsumer);
            ArgumentNullException.ThrowIfNull(partialErrorConsumer);

            if (_value is ICSTNode t1)
                nodeConsumer.Invoke(t1);

            else if (_value is FailedError t2)
                failedErrorConsumer.Invoke(t2);

            else if (_value is PartialError t3)
                partialErrorConsumer.Invoke(t3);

            else if (_value is null && nullConsumer is not null)
                nullConsumer.Invoke();
        }

        public NodeRecognitionResult WithMatch(
            Action<ICSTNode> nodeConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action nullConsumer = null!)
        {
            ConsumeMatch(nodeConsumer, failedErrorConsumer, partialErrorConsumer, nullConsumer);
            return this;
        }
    }
}
