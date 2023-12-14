using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar
{
    //public class ProductionPath
    //{
    //    private readonly ProductionPath? _parent;
    //    private readonly string _name;

    //    public string Name => _name;

    //    public ProductionPath? Parent => _parent;

    //    internal ProductionPath(string name, ProductionPath? parent = null)
    //    {
    //        _name = name;
    //        _parent = parent;
    //    }

    //    public static ProductionPath Of(string name, ProductionPath? parent = null) => new(name, parent);

    //    public static implicit operator ProductionPath(string path) => Parse(path);

    //    public static implicit operator string(ProductionPath path) => path?.ToString() ?? throw new ArgumentNullException(nameof(path));

    //    public override int GetHashCode() => HashCode.Combine(_parent, _name);

    //    public override bool Equals(object? obj)
    //    {
    //        return obj is ProductionPath other
    //            && EqualityComparer<ProductionPath>.Default.Equals(_parent, other._parent)
    //            && EqualityComparer<string>.Default.Equals(_name, other._name);
    //    }

    //    public override string ToString()
    //    {
    //        var parentText = _parent is not null
    //            ? $"{_parent}/"
    //            : string.Empty;

    //        return $"{parentText}{_name}";
    //    }

    //    public ProductionPath Next(string content) => new(content, this);

    //    public static ProductionPath Parse(string path)
    //    {
    //        if (!TryParse(path, out var ppath))
    //            throw new FormatException($"Invalid path format: '{path}'");

    //        return ppath;
    //    }

    //    public static bool TryParse(string path, out ProductionPath productionPath)
    //    {
    //        productionPath = path
    //            .ThrowIf(
    //                string.IsNullOrEmpty,
    //                _ => new ArgumentException($"Invalid path: null/empty"))
    //            .Split('/')
    //            .Select(s => s.Trim())
    //            .Aggregate(default(ProductionPath)!, (path, name) => path switch
    //            {
    //                null => ProductionPath.Of(name),
    //                ProductionPath parent => parent.Next(name)
    //            });

    //        return productionPath is not null;
    //    }
    //}
}
