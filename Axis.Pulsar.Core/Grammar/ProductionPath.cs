using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar
{
    public class ProductionPath
    {
        private readonly ProductionPath? _parent;
        private readonly string _name;

        public string Name => _name;

        public ProductionPath? Parent => _parent;

        internal ProductionPath(string name, ProductionPath? parent = null)
        {
            _parent = parent;
            _name = name.ThrowIf(
                string.IsNullOrWhiteSpace,
                new ArgumentNullException(nameof(name)));
        }

        internal static ProductionPath Of(string name, ProductionPath? parent = null) => new(name, parent);

        public static implicit operator ProductionPath(string name) => new(name, null);

        public override int GetHashCode() => HashCode.Combine(_parent, _name);

        public override bool Equals(object? obj)
        {
            return obj is ProductionPath other
                && EqualityComparer<ProductionPath>.Default.Equals(_parent, other._parent)
                && EqualityComparer<string>.Default.Equals(_name, other._name);
        }

        public override string ToString()
        {
            var parentText = _parent is not null
                ? $"{_parent}/"
                : "";

            return $"{parentText}{_name}";
        }

        public ProductionPath Next(string content) => new(content, this);
    }
}
