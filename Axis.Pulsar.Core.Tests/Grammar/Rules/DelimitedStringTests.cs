using Axis.Luna.Common.Utils;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class DelimitedStringTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            var dstring = DelimitedString.Of(
                true, "\"", "\"",
                Enumerable.Empty<Tokens>(),
                ArrayUtil.Of(Tokens.Of("xyz"),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>(),
                En))
        }
    }
}
