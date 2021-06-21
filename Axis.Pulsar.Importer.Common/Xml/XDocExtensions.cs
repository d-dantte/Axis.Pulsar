using System.Xml.Linq;

namespace Axis.Pulsar.Importer.Common.Xml
{
    public static class XDocExtensions
    {
        public static bool HasAttribute(this XElement element, string attributeName)
        {
            return element.Attribute(attributeName) != null;
        }

        public static bool TryAttribute(this XElement element, string attributeName, out XAttribute attribute)
        {
            attribute = element.Attribute(attributeName);
            return attribute != null;
        }

        public static XElement FirstChild(this XElement element) => element.FirstNode as XElement;
    }
}
