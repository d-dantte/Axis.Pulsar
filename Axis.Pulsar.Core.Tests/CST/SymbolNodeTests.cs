using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Utils;
using System.Xml.Linq;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class SymbolNodeTests
    {
        [TestMethod]
        public void Atom_Constructor_Tests()
        {
            var atom = new ISymbolNode.Atom("stuff", "bleh");
            Assert.IsNotNull(atom);
            Assert.AreEqual(default, ISymbolNode.Atom.Default);
            Assert.AreEqual(Tokens.Of("bleh"), atom.Tokens);
            Assert.AreEqual("stuff", atom.Symbol);
            Assert.ThrowsException<ArgumentException>(() => new ISymbolNode.Atom(null, "bleh"));
        }

        [TestMethod]
        public void Atom_IsDefault_Tests()
        {
            var atom = new ISymbolNode.Atom("stuff", "bleh");
            var atom2 = new ISymbolNode.Atom("stuff", default);
            var atom3 = default(ISymbolNode.Atom);

            Assert.IsFalse(atom.IsDefault);
            Assert.IsFalse(atom2.IsDefault);
            Assert.IsTrue(atom3.IsDefault);
        }

        [TestMethod]
        public void Atom_Equality_Tests()
        {
            var atom = new ISymbolNode.Atom("stuff", "bleh");
            var atom2 = new ISymbolNode.Atom("stuff", "bleh");
            var atom3 = new ISymbolNode.Atom("stuff", "blehh");
            var atom4 = new ISymbolNode.Atom("stuffh", "bleh");

            Assert.IsTrue(atom.Equals(atom));
            Assert.IsTrue(atom.Equals(atom2));
            Assert.IsFalse(atom.Equals(atom3));
            Assert.IsFalse(atom.Equals(atom4));
            Assert.IsTrue(atom == atom2);
            Assert.IsTrue(atom != atom3);

            Assert.IsFalse(atom.Equals(""));
            Assert.IsTrue(atom.Equals((object)atom2));
        }

        [TestMethod]
        public void Atom_GetHashCode_Tests()
        {
            var atom = new ISymbolNode.Atom("stuff", "bleh");
            var code = atom.GetHashCode();
            Assert.AreEqual(HashCode.Combine(atom.Tokens, atom.Symbol), code);
        }

        [TestMethod]
        public void Atom_ToString_Tests()
        {
            var atom = new ISymbolNode.Atom("stuff", "bleh");
            var text = atom.ToString();
            Assert.AreEqual($"[@T Name: {atom.Symbol}; Tokens: {atom.Tokens};]", text);
        }

        [TestMethod]
        public void Composite_Constructor_Tests()
        {
            var composite = new ISymbolNode.Composite("name", new ISymbolNode.Atom("bleh", "bleh"));
            Assert.IsNotNull(composite);
        }

        [TestMethod]
        public void Composite_ToString_Tests()
        {
            var composite = new ISymbolNode.Composite("name", new ISymbolNode.Atom("bleh", "bleh"));
            Assert.AreEqual(
                $"[@N name: name; NodeCount: {composite.Nodes.Length}; Tokens: bleh;]",
                composite.ToString());

            composite = new ISymbolNode.Composite("name", new ISymbolNode.Atom("bleh", "12345678901234567890123456"));
            Assert.AreEqual(
                $"[@N name: name; NodeCount: {composite.Nodes.Length}; Tokens: 12345678901234567890...;]",
                composite.ToString());

            composite = new ISymbolNode.Composite("name");
            Assert.AreEqual(
                $"[@N name: name; NodeCount: {composite.Nodes.Length}; Tokens: ;]",
                composite.ToString());

            composite = default;
            Assert.IsNull(composite.ToString());
        }

        [TestMethod]
        public void Composite_Equals_Tests()
        {
            var composite = new ISymbolNode.Composite(
                "name",
                new ISymbolNode.Atom("bleh", "bleh"));
            var composite2 = new ISymbolNode.Composite(
                "name",
                new ISymbolNode.Atom("bleh", "bleh"));
            var composite3 = new ISymbolNode.Composite("name");
            var composite4 = new ISymbolNode.Composite(
                "name",
                new ISymbolNode.Atom("bleh", "blehx"));
            var composite5 = new ISymbolNode.Composite(
                "name",
                new ISymbolNode.Atom("bleh", default));
            var composite6 = new ISymbolNode.Composite("name2");

            Assert.AreNotEqual(composite, composite6);
            Assert.AreEqual(composite, composite2);
            Assert.IsTrue(composite == composite2);
            Assert.IsFalse(composite != composite2);
            Assert.IsTrue((composite).Equals((object)composite2));
            Assert.IsFalse(composite.Equals(composite3));
            Assert.IsFalse(composite.Equals(composite5));
            Assert.IsFalse(composite.Equals(composite5));
            Assert.IsFalse(composite5.Equals(composite));

            Assert.IsFalse(composite.Equals(""));
        }

        [TestMethod]
        public void Composite_GetHashCode()
        {
            var composite = new ISymbolNode.Composite(
                "name",
                new ISymbolNode.Atom("bleh", "bleh"));
            Assert.AreEqual(HashCode.Combine(composite.Tokens, composite.Symbol, composite.Nodes), composite.GetHashCode());
        }

        [TestMethod]
        public void Composite_IsDefault_Tests()
        {
            var composite = new ISymbolNode.Composite(
                "name",
                new ISymbolNode.Atom("bleh", "bleh"));

            Assert.IsFalse(composite.IsDefault);
            Assert.AreEqual(default, ISymbolNode.Composite.Default);
        }
    }
}
