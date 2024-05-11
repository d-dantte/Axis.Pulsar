using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Utils
{
    internal class DeferredValue<TValue> : IEquatable<DeferredValue<TValue>>
    {
        private readonly Func<TValue> _valueFactory;
        private TValue _value;
        private Exception? _exception;

        /// <summary>
        /// 
        /// </summary>
        public bool IsGenerated { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public TValue Value
        {
            get
            {
                if (!IsGenerated)
                {
                    try
                    {
                        _value = _valueFactory.Invoke();
                    }
                    catch (Exception ex)
                    {
                        _exception = ex;
                        throw;
                    }
                    finally
                    {
                        IsGenerated = true;
                    }
                }

                if (_exception is null)
                    return _value;

                else throw _exception;
            }
        }

        public DeferredValue(Func<TValue> valueFactory)
        {
            ArgumentNullException.ThrowIfNull(valueFactory);

            _valueFactory = valueFactory;
            IsGenerated = false;
            _value = default!;
        }

        public bool TryValue(out TValue? value)
        {
            try
            {
                value = Value;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public static implicit operator DeferredValue<TValue>(
            Func<TValue> valueFactory)
            => new(valueFactory);

        public override bool Equals(object? obj)
        {
            return obj is DeferredValue<TValue> other && Equals(other);
        }

        public bool Equals(DeferredValue<TValue>? other)
        {
            if (other is null)
                return false;

            _ = TryValue(out TValue? value);
            _ = other!.TryValue(out TValue? otherValue);

            return Common.NullOrEquals(value, otherValue)
                && Common.NullOrEquals(_exception, other!._exception);
        }

        public override int GetHashCode()
        {
            _ = TryValue(out TValue? value);
            return HashCode.Combine(value, _exception);
        }
    }
}
