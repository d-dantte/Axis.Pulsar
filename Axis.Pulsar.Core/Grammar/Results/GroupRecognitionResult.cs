using Axis.Luna.Unions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    using GroupError = GroupRecognitionError;

    public readonly struct GroupRecognitionResult :
        IUnion<INodeSequence, GroupRecognitionError, GroupRecognitionResult>,
        IUnionOf<INodeSequence, GroupRecognitionError, GroupRecognitionResult>
    {
        private readonly object? _value;

        object IUnion<INodeSequence, GroupError, GroupRecognitionResult>.Value => _value!;


        public GroupRecognitionResult(object value)
        {
            _value = value switch
            {
                null => null,
                GroupError
                or INodeSequence => value,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Invalid {nameof(value)} type: '{value.GetType()}'")
            };
        }

        /// <summary>
        /// Rejects null sequences
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static GroupRecognitionResult Of(INodeSequence value) => value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            _ => new(value)
        };

        public static GroupRecognitionResult Of(GroupError value) => new(value);

        public bool Is(out INodeSequence value)
        {
            if (_value is INodeSequence n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Is(out GroupError value)
        {
            if (_value is GroupError n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool IsNull() => _value is null;

        public TOut MapMatch<TOut>(
            Func<INodeSequence, TOut> sequenceMapper,
            Func<GroupError, TOut> groupErrorMapper,
            Func<TOut> nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(sequenceMapper);
            ArgumentNullException.ThrowIfNull(groupErrorMapper);

            if (_value is INodeSequence t1)
                return sequenceMapper.Invoke(t1);

            if (_value is GroupError t2)
                return groupErrorMapper.Invoke(t2);

            // unknown type, assume null
            return nullMapper switch
            {
                null => default!,
                _ => nullMapper.Invoke()
            };
        }

        public void ConsumeMatch(
            Action<INodeSequence> sequenceConsumer,
            Action<GroupError> groupErrorConsumer,
            Action nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(sequenceConsumer);
            ArgumentNullException.ThrowIfNull(groupErrorConsumer);

            if (_value is INodeSequence t1)
                sequenceConsumer.Invoke(t1);

            else if (_value is GroupError t2)
                groupErrorConsumer.Invoke(t2);

            else if (_value is null && nullConsumer is not null)
                nullConsumer.Invoke();
        }

        public GroupRecognitionResult WithMatch(
            Action<INodeSequence> sequenceConsumer,
            Action<GroupError> groupErrorConsumer,
            Action nullConsumer = null!)
        {
            ConsumeMatch(sequenceConsumer, groupErrorConsumer, nullConsumer);
            return this;
        }
    }
}
