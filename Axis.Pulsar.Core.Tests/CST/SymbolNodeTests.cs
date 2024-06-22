using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Utils;
using System.Xml.Linq;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class SymbolNodeTests
    {
        #region Atom
        [TestMethod]
        public void Atom_Constructor_Tests()
        {
            var atom = new ISymbolNode.Atom("stuff", "bleh");
            Assert.IsNotNull(atom);
            Assert.AreEqual(default, ISymbolNode.Atom.Default);
            Assert.AreEqual(Tokens.Of("bleh"), atom.Tokens);
            Assert.AreEqual("stuff", atom.Symbol);
            Assert.ThrowsException<ArgumentException>(() => new ISymbolNode.Atom(null!, "bleh"));
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
        #endregion

        #region Composite

        [TestMethod]
        public void Composite_Constructor_Tests()
        {
            var composite = new ISymbolNode.Composite("name", new ISymbolNode.Atom("bleh", "bleh"));
            Assert.IsNotNull(composite);

            composite = ISymbolNode.Composite.Default;
            Assert.IsTrue(composite.IsDefault);
            Assert.AreEqual(Tokens.Default, composite.Tokens);

            Assert.ThrowsException<ArgumentNullException>(
                () => new ISymbolNode.Composite("name", (IEnumerable<ISymbolNode>)null!));
            Assert.ThrowsException<InvalidOperationException>(
                () => new ISymbolNode.Composite("name", null!, null!));
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

        #endregion

        #region Aggregate

        [TestMethod]
        public void Aggregate_Constructor_Tests()
        {
            var aggregate = new ISymbolNode.Aggregate(
                AggregationType.Unit,
                false,
                new ISymbolNode.Atom("bleh", "bleh"),
                new ISymbolNode.Atom("bleh", "bleh"));
            Assert.IsNotNull(aggregate);
            Assert.AreEqual("Unit", aggregate.Symbol);
            Assert.AreEqual(AggregationType.Unit, aggregate.Type);
            Assert.AreEqual<Tokens>("bleh", aggregate.Tokens);
            Assert.IsFalse(aggregate.IsDefault);
            Assert.IsFalse(aggregate.IsOptional);

            aggregate = ISymbolNode.Aggregate.Default;
            Assert.IsTrue(aggregate.IsDefault);
            Assert.AreEqual(Tokens.Default, aggregate.Tokens);

            Assert.ThrowsException<ArgumentNullException>(
                () => ISymbolNode.Of(AggregationType.Unit, false, (IEnumerable<ISymbolNode>)null!));
            Assert.ThrowsException<InvalidOperationException>(
                () => new ISymbolNode.Aggregate(AggregationType.Unit, false, null!, null!));
        }

        [TestMethod]
        public void Aggregate_ToString_Tests()
        {
            var aggregate = new ISymbolNode.Aggregate(AggregationType.Unit, true, new ISymbolNode.Atom("bleh", "bleh"));
            Assert.AreEqual(
                $"<@N type: Unit; NodeCount: {aggregate.Nodes.Length}; Tokens: bleh;>",
                aggregate.ToString());

            aggregate = new ISymbolNode.Aggregate(AggregationType.Unit, false, new ISymbolNode.Atom("bleh", "12345678901234567890123456"));
            Assert.AreEqual(
                $"<@N type: Unit; NodeCount: {aggregate.Nodes.Length}; Tokens: 12345678901234567890...;>",
                aggregate.ToString());

            aggregate = new ISymbolNode.Aggregate(AggregationType.Unit, true);
            Assert.AreEqual(
                $"<@N type: Unit; NodeCount: {aggregate.Nodes.Length}; Tokens: ;>",
                aggregate.ToString());

            aggregate = default;
            Assert.IsNull(aggregate.ToString());
        }

        [TestMethod]
        public void Aggregate_Equals_Tests()
        {
            var aggregate = new ISymbolNode.Aggregate(
                AggregationType.Unit, true,
                new ISymbolNode.Atom("bleh", "bleh"));
            var aggregate2 = new ISymbolNode.Aggregate(
                AggregationType.Unit, true,
                new ISymbolNode.Atom("bleh", "bleh"));
            var aggregate3 = new ISymbolNode.Aggregate(AggregationType.Unit, true);
            var aggregate4 = new ISymbolNode.Aggregate(
                AggregationType.Sequence, true,
                new ISymbolNode.Atom("bleh", "blehx"));
            var aggregate5 = new ISymbolNode.Aggregate(
                AggregationType.Unit, true,
                new ISymbolNode.Atom("bleh", default));
            var aggregate6 = new ISymbolNode.Aggregate(AggregationType.Unit, true);

            Assert.AreNotEqual(aggregate, aggregate6);
            Assert.AreEqual(aggregate, aggregate2);
            Assert.IsTrue(aggregate == aggregate2);
            Assert.IsFalse(aggregate != aggregate2);
            Assert.IsTrue((aggregate).Equals((object)aggregate2));
            Assert.IsFalse(aggregate.Equals(aggregate3));
            Assert.IsFalse(aggregate.Equals(aggregate5));
            Assert.IsFalse(aggregate.Equals(aggregate5));
            Assert.IsFalse(aggregate5.Equals(aggregate));
            Assert.IsFalse(aggregate2.Equals(aggregate4));

            Assert.IsFalse(aggregate.Equals(""));
        }

        [TestMethod]
        public void Aggregate_GetHashCode()
        {
            var aggregate = new ISymbolNode.Aggregate(
                AggregationType.Unit, false,
                new ISymbolNode.Atom("bleh", "bleh"));

            var hash = HashCode.Combine(aggregate.IsOptional, aggregate.Type);
            hash = aggregate.Nodes.Aggregate(hash, HashCode.Combine);
            Assert.AreEqual(hash, aggregate.GetHashCode());
        }

        [TestMethod]
        public void Aggregate_IsDefault_Tests()
        {
            var aggregate = new ISymbolNode.Aggregate(
                AggregationType.Unit, false,
                new ISymbolNode.Atom("bleh", "bleh"));

            Assert.IsFalse(aggregate.IsDefault);
            Assert.AreEqual(default, ISymbolNode.Aggregate.Default);
        }

        #endregion
    }

    [TestClass]
    public class SymbolNodeExtensionsTest
    {
        [TestMethod]
        public void FlattenAggregates_Tests()
        {
            var node = ISymbolNode.Of(
                AggregationType.Sequence,
                false,
                new ISymbolNode.Atom("atomic", "tokens"),
                new ISymbolNode.Composite("composite"),
                new ISymbolNode.Aggregate(
                    AggregationType.Unit,
                    false,
                    new ISymbolNode.Atom("inner-atomic", "toknes")));

            var nodes = node.FlattenAggregates().ToArray();
            Assert.AreEqual(3, nodes.Length);

            Assert.ThrowsException<InvalidOperationException>(
                () => new FakeSymbolNode().FlattenAggregates());

            Assert.ThrowsException<ArgumentNullException>(
                () => default(ISymbolNode)!.FlattenAggregates());
        }

        [TestMethod]
        public void RequiredNodeCount_Tests()
        {
            var node = ISymbolNode.Of(
                AggregationType.Sequence,
                false,
                new ISymbolNode.Atom("atomic", "tokens"),
                new ISymbolNode.Composite("composite"),
                new ISymbolNode.Aggregate(
                    AggregationType.Unit,
                    true,
                    new ISymbolNode.Atom("inner-atomic", "toknes")));
            
            var count = node.RequiredNodeCount();
            Assert.AreEqual(2, count);

            Assert.ThrowsException<InvalidOperationException>(
                () => new FakeSymbolNode().RequiredNodeCount());

            Assert.ThrowsException<InvalidOperationException>(
                () => default(ISymbolNode)!.RequiredNodeCount());
        }

        public class FakeSymbolNode : ISymbolNode
        {
            public Tokens Tokens => throw new NotImplementedException();

            public string Symbol => throw new NotImplementedException();
        }
    }
}
