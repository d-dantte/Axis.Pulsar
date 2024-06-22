using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Rules;

namespace Axis.Pulsar.Core.Tests.Grammar.Errors
{
    [TestClass]
    public class GrammarValidationExceptionTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var error = new GrammarValidationException(
                new GrammarValidator__old.ValidationResult(
                    new FakeGrammar(),
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>()));

            Assert.IsNotNull(error);
            Assert.IsNotNull(error.ValidationResult);

            Assert.ThrowsException<ArgumentNullException>(
                () => new GrammarValidationException(null!));
        }

        public class FakeGrammar : IGrammar
        {
            public Production this[string name] => throw new NotImplementedException();

            public string Root => throw new NotImplementedException();

            public int ProductionCount => throw new NotImplementedException();

            public IEnumerable<string> ProductionSymbols => Enumerable.Empty<string>();

            public bool ContainsProduction(string symbolName)
            {
                throw new NotImplementedException();
            }

            public Production GetProduction(string name)
            {
                throw new NotImplementedException();
            }

            public bool TryGetProduction(string name, out Production? production)
            {
                throw new NotImplementedException();
            }
        }
    }
}
