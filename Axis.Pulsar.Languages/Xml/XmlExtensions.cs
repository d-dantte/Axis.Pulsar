using System.Linq;
using System.Xml.Linq;

namespace Axis.Pulsar.Languages.Xml
{
    internal static class XmlExtensions
    {

        public static bool TryAttribute(this XElement element, string attributeName, out XAttribute attribute)
        {
            attribute = element.Attribute(attributeName);
            return attribute != null;
        }

        public static XElement FirstChild(this XElement element) => element.Elements().First();
    }
}
