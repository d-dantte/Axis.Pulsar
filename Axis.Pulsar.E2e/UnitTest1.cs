using System.Reflection;

namespace Axis.Pulsar.E2e
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var type = typeof(Languages.Extensions);
            var names = type.Assembly.GetManifestResourceNames();
            (names ?? Array.Empty<string>())
                .ToList()
                .ForEach(Console.WriteLine);
        }

        [TestMethod]
        public void Stuff()
        {
            var ict = typeof(InternalC);
            var nestedTypes = ict.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            var npuct = typeof(InternalC.NPuC);
            var npoct = nestedTypes.First(t => t.Name.Equals("NPoC"));
            var npict = nestedTypes.First(t => t.Name.Equals("NPiC"));
            var nict = typeof(InternalC.NIC);
            var nipoct = nestedTypes.First(t => t.Name.Equals("NIPoC"));
            var npopict = nestedTypes.First(t => t.Name.Equals("NPoPiC"));
        }
    }

    internal class InternalC 
    {

        public class NPuC { }

        protected class NPoC { }

        private class NPiC { }

        internal class NIC { }

        internal protected class NIPoC { }

        protected private class NPoPiC { }
    }


}