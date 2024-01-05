using Axis.Luna.Common.Unions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;

namespace Axis.Pulsar.Core.XBNF.Parsers
{
    using FailedError = FailedRecognitionError;
    using PartialError = PartialRecognitionError;

    internal class XBNFResult<TResult> :
        INodeRecognitionResultBase<TResult, XBNFResult<TResult>>,
        IUnionOf<TResult, FailedError, PartialError, XBNFResult<TResult>>
    {
        private readonly object? _value;

        object IUnion<TResult, FailedError, PartialError, XBNFResult<TResult>>.Value => _value!;

        public XBNFResult(object value)
        {
            _value = value switch
            {
                null => null,
                FailedError
                or PartialError
                or TResult => value,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Invalid {nameof(value)} type: '{value.GetType()}'")
            };
        }

        public static XBNFResult<TResult> Of(TResult value) => new(value!);

        public static XBNFResult<TResult> Of(
            FailedError value)
            => new(value);

        public static XBNFResult<TResult> Of(
            PartialError value)
            => new(value);

        public bool Is(out TResult value)
        {
            if (_value is TResult n)
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
            Func<TResult, TOut> resultMapper,
            Func<FailedError, TOut> failedErrorMapper,
            Func<PartialError, TOut> partialErrorMapper,
            Func<TOut> nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(resultMapper);
            ArgumentNullException.ThrowIfNull(failedErrorMapper);
            ArgumentNullException.ThrowIfNull(partialErrorMapper);

            if (_value is TResult t1)
                return resultMapper.Invoke(t1);

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
            Action<TResult> resultConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(resultConsumer);
            ArgumentNullException.ThrowIfNull(failedErrorConsumer);
            ArgumentNullException.ThrowIfNull(partialErrorConsumer);

            if (_value is TResult t1)
                resultConsumer.Invoke(t1);

            else if (_value is FailedError t2)
                failedErrorConsumer.Invoke(t2);

            else if (_value is PartialError t3)
                partialErrorConsumer.Invoke(t3);

            else if (_value is null && nullConsumer is not null)
                nullConsumer.Invoke();
        }

        public XBNFResult<TResult> WithMatch(
            Action<TResult> resultConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action nullConsumer = null!)
        {
            ConsumeMatch(resultConsumer, failedErrorConsumer, partialErrorConsumer, nullConsumer);
            return this;
        }
    }
}
