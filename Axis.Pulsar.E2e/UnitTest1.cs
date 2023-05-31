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
    }
}