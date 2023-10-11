using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar
{
    public class ProductionPath
    {
        private readonly ProductionPath? _parent;
        private readonly string _content;

        public string Name => _content;

        public ProductionPath? Parent => _parent;

        internal ProductionPath(string content, ProductionPath? parent = null)
        {
            _parent = parent;
            _content = content.ThrowIf(
                string.IsNullOrWhiteSpace,
                new ArgumentNullException(nameof(content)));
        }

        internal static ProductionPath Of(string content, ProductionPath? parent = null) => new(content, parent);


        public override int GetHashCode() => HashCode.Combine(_parent, _content);

        public override bool Equals(object? obj)
        {
            return obj is ProductionPath other
                && EqualityComparer<ProductionPath>.Default.Equals(_parent, other._parent)
                && EqualityComparer<string>.Default.Equals(_content, other._content);
        }

        public override string ToString()
        {
            var parentText = _parent is not null
                ? $"{_parent}/"
                : "";

            return $"{parentText}{_content}";
        }

        public ProductionPath Next(string content) => new(content, this);
    }
}
