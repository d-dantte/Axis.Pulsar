namespace Axis.Pulsar.Core.Utils
{
    internal class DeferredValue<TValue>
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

        public static implicit operator DeferredValue<TValue>(
            Func<TValue> valueFactory)
            => new(valueFactory);
    }
}
