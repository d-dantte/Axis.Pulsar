namespace Axis.Pulsar.Core.XBNF.Tests
{
    internal static class ResourceLoader
    {
        internal static Stream? Load(string relativePathFromRootNamespace)
        {
            return typeof(ResourceLoader).Assembly.GetManifestResourceStream(
                $"{typeof(ResourceLoader).Namespace}.{relativePathFromRootNamespace}");
        }
    }
}
