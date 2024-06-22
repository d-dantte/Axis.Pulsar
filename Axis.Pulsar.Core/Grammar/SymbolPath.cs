using Axis.Luna.Common;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar
{
    public readonly struct SymbolPath:
        IEquatable<SymbolPath>,
        IDefaultValueProvider<SymbolPath>
    {
        private readonly string _symbol;

        /// <summary>
        /// Using an object to avoid the struct limitation of cyclic references
        /// </summary>
        private readonly object? _parentSymbol;

        public string Symbol => _symbol;

        public SymbolPath? Parent => _parentSymbol switch
        {
            SymbolPath sp => sp,
            _ => (SymbolPath?)null!
        };

        #region DefaultProvider
        public bool IsDefault => _symbol is null && _parentSymbol is null;

        public static SymbolPath Default => default;
        #endregion

        public SymbolPath(string symbol, SymbolPath? parentSymbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);

            _symbol = symbol;
            _parentSymbol = parentSymbol switch
            {
                null => (object) null!,
                SymbolPath parent => parent.IsDefault switch
                {
                    true => (object) null!,
                    _ => parent
                }
            };
        }

        public static SymbolPath Of(
            string symbol,
            SymbolPath? parentSymbol = null)
            => new(symbol, parentSymbol);

        public static implicit operator SymbolPath(string path) => Parse(path);

        public static implicit operator string(SymbolPath path) => path.ToString()!;

        public SymbolPath Next(string symbol) => new(symbol, this);

        public bool Equals(SymbolPath other)
        {
            if (!EqualityComparer<string>.Default.Equals(_symbol, other._symbol))
                return false;

            return (_parentSymbol, other._parentSymbol) switch
            {
                (null, null) => true,
                (SymbolPath first, SymbolPath second) => first.Equals(second),
                _ => false
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is SymbolPath other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(
            _symbol,
            _parentSymbol);

        public override string ToString()
        {
            var parentText = _parentSymbol switch
            {
                SymbolPath parent => $"{parent}/",
                _ => string.Empty
            };

            return $"{parentText}{_symbol}";
        }

        public static SymbolPath Parse(string path)
        {
            if (!TryParse(path, out var ppath))
                throw new FormatException($"Invalid path format: '{path}'");

            return ppath!.Value;
        }

        public static bool TryParse(string path, out SymbolPath? symbolPath)
        {
            symbolPath = path switch
            {
                null => default(SymbolPath?),
                _ => path
                    .Split('/')
                    .Select(s => s.Trim())
                    .Aggregate(default(SymbolPath), (path, name) => path.Next(name))
            };
            return symbolPath != null;
        }
    }
}
