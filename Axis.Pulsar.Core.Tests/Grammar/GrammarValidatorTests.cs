using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Grammar.Rules.Composite;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;
using static Axis.Pulsar.Core.Grammar.GrammarValidator;

namespace Axis.Pulsar.Core.Tests.Grammar
{
    [TestClass]
    public class GrammarValidatorTests
    {
        [TestMethod]
        public void TraverseAtomicRule_Tests()
        {
            var grammar = new Mock<IGrammar>();
            var rule = new TerminalLiteral("abc", "efg");
            var context = new ValidationContext(grammar.Object);

            // act
            GrammarValidator.TraverseAtomicRule(rule, context);

            // assert
            Assert.AreEqual(1, context.Symbols.Count);
            Assert.IsTrue(context.Symbols.Contains("@abc"));
        }

        [TestMethod]
        public void TraverseAggregationElementRule_Tests()
        {
            // production ref
            var grammar = new Mock<IGrammar>();
            var context = new ValidationContext(grammar.Object);
            var prodRef = new ProductionRef("me");
            var prod = new Production("me", new TerminalLiteral("bleh", "helb"));
            grammar
                .Setup(g => g.GetProduction("me"))
                .Returns(prod);
            GrammarValidator.TraverseAggregationElementRule(0, prodRef, context);
            Assert.AreEqual(2, context.Symbols.Count);
            Assert.IsTrue(context.Symbols.Contains("$me"));
            Assert.IsTrue(context.Symbols.Contains("@bleh"));

            // atomic ref
            grammar = new Mock<IGrammar>();
            context = new ValidationContext(grammar.Object);
            var atomicRef = new AtomicRuleRef(new TerminalLiteral("bleh", "helb"));
            GrammarValidator.TraverseAggregationElementRule(0, atomicRef, context);
            Assert.AreEqual(1, context.Symbols.Count);
            Assert.IsTrue(context.Symbols.Contains("@bleh"));

            // repetition
            grammar = new Mock<IGrammar>();
            context = new ValidationContext(grammar.Object);
            var repetition = new Repetition(Cardinality.OccursOnlyOnce(), atomicRef);
            GrammarValidator.TraverseAggregationElementRule(0, repetition, context);
            Assert.AreEqual(1, context.Symbols.Count);
            Assert.IsTrue(context.Symbols.Contains("@bleh"));

            // aggregation element
            grammar = new Mock<IGrammar>();
            context = new ValidationContext(grammar.Object);
            var choice = new Choice(new AtomicRuleRef(new TerminalLiteral("bleh", "helb")));
            GrammarValidator.TraverseAggregationElementRule(0, choice, context);
            Assert.AreEqual(1, context.Symbols.Count);
            Assert.IsTrue(context.Symbols.Contains("@bleh"));
        }

        [TestMethod]
        public void TraverseProduction_Tests()
        {
            // production holds atomic rule
            var me = new Production("me", new TerminalLiteral("bleh", "helb"));
            var meRef = new ProductionRef("me");
            var context = new ValidationContext(new FauxGrammar());

            GrammarValidator.TraverseProduction(me, context);
            Assert.AreEqual(2, context.Symbols.Count);
            Assert.IsTrue(context.Symbols.Contains("$me"));
            Assert.IsTrue(context.Symbols.Contains("@bleh"));

            // production holds composite rule
            var grammar = new Mock<IGrammar>();
            me = new Production("me", new CompositeRule(null, meRef));
            meRef = new ProductionRef("me");
            grammar
                .Setup(g => g.GetProduction("me"))
                .Returns(me);
            context = new ValidationContext(grammar.Object);

            GrammarValidator.TraverseProduction(me, context);
            Assert.AreEqual(1, context.Symbols.Count);
            Assert.IsTrue(context.Symbols.Contains("$me"));
            Assert.AreEqual(1, context.UnresolvableProductions.Count);
            Assert.IsTrue(context.UnresolvableProductions.Contains("#$me"));

            // unknown rule type
            Assert.ThrowsException<InvalidOperationException>(
                () => GrammarValidator.TraverseProduction(new Production("bleh", new FauxRule()), context));
        }

        [TestMethod]
        public void HasNonHaltingProductionLoop_Tests()
        {
            // no previous production
            var context = new ValidationContext(new FauxGrammar());
            var me = new Production("me", new TerminalLiteral("bleh", "helb"));
            var meRef = new ProductionRef("me");

            var result = GrammarValidator.HasNonHaltingProductionLoop(me, context);
            Assert.IsFalse(result);

            // production directly referencing itself
            context.TraversalStack.Push(new ProductionNode(me));
            context.TraversalStack.Push(new AggregateRuleNode(0, meRef));

            result = GrammarValidator.HasNonHaltingProductionLoop(me, context);
            Assert.IsTrue(result);

            // production references itself via choice, at index 0.
            context.Clear();
            context.TraversalStack.Push(new AggregateRuleNode(0, new Choice(meRef)));
            context.TraversalStack.Push(new AggregateRuleNode(0, meRef));
            context.TraversalStack.Push(new ProductionNode(me));
            context.TraversalStack.Push(new AggregateRuleNode(0, new Choice(meRef)));
            context.TraversalStack.Push(new AggregateRuleNode(0, meRef));

            result = GrammarValidator.HasNonHaltingProductionLoop(me, context);
            Assert.IsTrue(result);

            // production references itself via choice, at index > 0.
            context.Clear();
            context.TraversalStack.Push(new ProductionNode(me));
            context.TraversalStack.Push(new AggregateRuleNode(0, new Choice(meRef)));
            context.TraversalStack.Push(new AggregateRuleNode(2, meRef));

            result = GrammarValidator.HasNonHaltingProductionLoop(me, context);
            Assert.IsFalse(result);

            // production references itself via non-optional repetition
            context.Clear();
            context.TraversalStack.Push(new AggregateRuleNode(0, new Repetition(Cardinality.OccursOnlyOnce(), meRef)));
            context.TraversalStack.Push(new AggregateRuleNode(0, meRef));
            context.TraversalStack.Push(new ProductionNode(me));
            context.TraversalStack.Push(new AggregateRuleNode(0, new Repetition(Cardinality.OccursOnlyOnce(), meRef)));
            context.TraversalStack.Push(new AggregateRuleNode(0, meRef));

            result = GrammarValidator.HasNonHaltingProductionLoop(me, context);
            Assert.IsTrue(result);

            // production references itself via optional repetition.
            context.Clear();
            context.TraversalStack.Push(new ProductionNode(me));
            context.TraversalStack.Push(new AggregateRuleNode(0, new Repetition(Cardinality.OccursOptionally(), meRef)));
            context.TraversalStack.Push(new AggregateRuleNode(0, meRef));

            result = GrammarValidator.HasNonHaltingProductionLoop(me, context);
            Assert.IsFalse(result);
        }

        #region Nested types
        public class FauxAggregationElement : IAggregationElement
        {
            public AggregationType Type => throw new NotImplementedException();

            public bool TryRecognize(TokenReader reader, SymbolPath symbolPath, ILanguageContext context, out NodeAggregationResult result)
            {
                throw new NotImplementedException();
            }
        }

        public class FauxGrammar : IGrammar
        {
            public Production this[string name] => throw new NotImplementedException();

            public string Root => throw new NotImplementedException();

            public int ProductionCount => throw new NotImplementedException();

            public IEnumerable<string> ProductionSymbols => throw new NotImplementedException();

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

        public class FauxRule : Production.IRule
        {
            public bool TryRecognize(TokenReader reader, SymbolPath symbolPath, ILanguageContext context, out NodeRecognitionResult result)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }

    [TestClass]
    public class NodeStackTests
    {
        [TestMethod]
        public void ProductionRefKeyFor_Tests()
        {
            var symbol = "abcd";
            Assert.AreEqual(
                $"{NodeStack.RefPrefix}{NodeStack.ProductionPrefix}{symbol}",
                NodeStack.ProductionRefKeyFor(symbol));
        }

        [TestMethod]
        public void AtomicRefKeyFor_Tests()
        {
            var symbol = "abcd";
            Assert.AreEqual(
                $"{NodeStack.RefPrefix}{NodeStack.AtomicPrefix}{symbol}",
                NodeStack.AtomicRefKeyFor(symbol));
        }

        [TestMethod]
        public void ProductionKeyFor_Tests()
        {
            var symbol = "abcd";
            Assert.AreEqual(
                $"{NodeStack.ProductionPrefix}{symbol}",
                NodeStack.ProductionKeyFor(symbol));
        }

        [TestMethod]
        public void AtomicKeyFor_Tests()
        {
            var symbol = "abcd";
            Assert.AreEqual(
                $"{NodeStack.AtomicPrefix}{symbol}",
                NodeStack.AtomicKeyFor(symbol));
        }

        [TestMethod]
        public void NodeString()
        {
            var symbol = "abcd";
            var rule = new TerminalLiteral(symbol, "abcd");
            var atomicRef = new AtomicRuleRef(rule);
            var node = new AggregateRuleNode(0, atomicRef);
            Assert.AreEqual(
                $"{NodeStack.RefPrefix}{NodeStack.AtomicPrefix}{symbol}{{0}}",
                NodeStack.NodeString(node));
        }

        [TestMethod]
        public void EvaluateNodeKey_Tests()
        {
            //production node
            GrammarValidator.INode node = new ProductionNode(
                new Core.Grammar.Rules.Production("abc", new TerminalLiteral("abc", "xyz")));
            Assert.AreEqual("$abc", NodeStack.EvaluateNodeKey(node));

            // production rule node
            node = new ProductionRuleNode(new TerminalLiteral("abc", "xyz"));
            Assert.AreEqual("#@abc", NodeStack.EvaluateNodeKey(node));

            // set node
            node = new AggregateRuleNode(1, new Set(
                new AtomicRuleRef(new TerminalLiteral("abc", "xyz"))));
            Assert.AreEqual("Set", NodeStack.EvaluateNodeKey(node));

            // choice node
            node = new AggregateRuleNode(1, new Choice(
                new AtomicRuleRef(new TerminalLiteral("abc", "xyz")),
                new AtomicRuleRef(new TerminalLiteral("abce", "xyzs"))));
            Assert.AreEqual("Choice", NodeStack.EvaluateNodeKey(node));

            // sequence node
            node = new AggregateRuleNode(1, new Sequence(
                new AtomicRuleRef(new TerminalLiteral("abc", "xyz")),
                new AtomicRuleRef(new TerminalLiteral("abce", "xyzs"))));
            Assert.AreEqual("Sequence", NodeStack.EvaluateNodeKey(node));

            // production ref node
            node = new AggregateRuleNode(1, new ProductionRef("abc"));
            Assert.AreEqual("#$abc", NodeStack.EvaluateNodeKey(node));

            // atomic ref node
            node = new AggregateRuleNode(1, new AtomicRuleRef(new TerminalLiteral("abce", "xyzs")));
            Assert.AreEqual("#@abce", NodeStack.EvaluateNodeKey(node));

            // unknown aggregate
            node = new AggregateRuleNode(1, new FauxAggregate());
            Assert.ThrowsException<InvalidOperationException>(
                () =>  NodeStack.EvaluateNodeKey(node));

            // unknown node
            node = new FauxNode();
            Assert.ThrowsException<InvalidOperationException>(
                () => NodeStack.EvaluateNodeKey(node));
        }

        [TestMethod]
        public void Push_Tests()
        {
            var symbol = "abcd";
            var rule = new TerminalLiteral(symbol, "abcd");
            var atomicRef = new AtomicRuleRef(rule);
            var node = new AggregateRuleNode(0, atomicRef);
            var stack = new NodeStack();

            var x = stack.Push(node);

            Assert.AreEqual(x, stack);
            Assert.AreEqual(1, stack.Count);
            Assert.AreEqual("/#@abcd{0}", stack.SymbolPath);

            var node2 = new ProductionNode(
                new Production("abc", new TerminalLiteral("abc", "xyz")));
            stack.Push(node2);
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual("/#@abcd{0}/$abc{0}", stack.SymbolPath);

            stack.Push(node2);
            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual("/#@abcd{0}/$abc{0}/$abc{0}", stack.SymbolPath);
        }

        [TestMethod]
        public void Pop_Tests()
        {
            var symbol = "abcd";
            var rule = new TerminalLiteral(symbol, "abcd");
            var atomicRef = new AtomicRuleRef(rule);
            var node = new AggregateRuleNode(0, atomicRef);
            var stack = new NodeStack();

            var node2 = new ProductionNode(
                new Production("abc", new TerminalLiteral("abc", "xyz")));

            var x = stack.Pop();
            Assert.AreEqual(x, stack);
            Assert.AreEqual(0, stack.Count);

            stack.Push(node);
            stack.Push(node2);

            stack.Pop();
            Assert.AreEqual(1, stack.Count);

            stack.Pop();
            Assert.AreEqual(0, stack.Count);
        }


        public class FauxAggregate : IAggregationElement
        {
            public AggregationType Type => throw new NotImplementedException();

            public bool TryRecognize(TokenReader reader, SymbolPath symbolPath, ILanguageContext context, out NodeAggregationResult result)
            {
                throw new NotImplementedException();
            }
        }

        public class FauxNode : INode
        {
            public int Index => throw new NotImplementedException();
        }
    }

    [TestClass]
    public class NodeTests
    {
        [TestMethod]
        public void AggregateRuleNode_Constructor_Tests()
        {
            var @ref = new ProductionRef("bleh");
            var node = new AggregateRuleNode(2, @ref);
            Assert.IsNotNull(node);
            Assert.AreEqual(2, node.Index);
            Assert.AreEqual(@ref, node.Rule);

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new AggregateRuleNode(-1, @ref));

            Assert.AreEqual("Node.#$bleh", node.ToString());
        }

        [TestMethod]
        public void ProductionRuleNode_Constructor_Tests()
        {
            var rule = new TerminalLiteral("bleh", "bleh");
            var node = new ProductionRuleNode(rule);
            Assert.IsNotNull(node);
            Assert.AreEqual(0, node.Index);
            Assert.AreEqual(rule, node.Rule);
            Assert.AreEqual("Node.@bleh", node.ToString());
        }

        [TestMethod]
        public void ProductioneNode_Constructor_Tests()
        {
            var rule = new TerminalLiteral("bleh", "bleh");
            var prod = new Production("sss", rule);
            var node = new ProductionNode(prod);
            Assert.IsNotNull(node);
            Assert.AreEqual(0, node.Index);
            Assert.AreEqual(prod, node.Production);
            Assert.AreEqual("Node.$sss", node.ToString());
        }

        [TestMethod]
        public void ToString_WithProduction_Tests()
        {
            var prod = new Production("abc", new FauxRule());
            Assert.AreEqual("Node.$abc", INode.ToString(prod));
        }

        [TestMethod]
        public void ToString_WithProductionRule_Tests()
        {
            var atomic = new TerminalLiteral("abc", "def");
            var composite = new CompositeRule(null, new AtomicRuleRef(atomic));
            var fake = new FauxRule();

            Assert.AreEqual("Node.@abc", INode.ToString(atomic));
            Assert.AreEqual("Node.#@abc", INode.ToString(composite));
            Assert.ThrowsException<InvalidOperationException>(
                () => INode.ToString(fake));
        }

        [TestMethod]
        public void ToString_With_AggregateElement_Tests()
        {
            var atomicRef = new AtomicRuleRef(new TerminalLiteral("abc", "def"));

            IAggregationElement elt = new Set(atomicRef);
            Assert.AreEqual("Node.Set", INode.ToString(elt));

            elt = new Choice(atomicRef);
            Assert.AreEqual("Node.Choice", INode.ToString(elt));

            elt = new Sequence(atomicRef);
            Assert.AreEqual("Node.Sequence", INode.ToString(elt));

            elt = new Repetition(Cardinality.OccursOnlyOnce(), atomicRef);
            Assert.AreEqual("Node.Repetition", INode.ToString(elt));

            elt = atomicRef;
            Assert.AreEqual("Node.#@abc", INode.ToString(elt));

            elt = new ProductionRef("xyz");
            Assert.AreEqual("Node.#$xyz", INode.ToString(elt));

            Assert.ThrowsException<InvalidOperationException>(
                () => INode.ToString(new FauxElement()));

            Assert.ThrowsException<InvalidOperationException>(
                () => INode.ToString(default(IAggregationElement)!));
        }

        internal class FauxRule : Production.IRule
        {
            public bool TryRecognize(TokenReader reader, SymbolPath symbolPath, ILanguageContext context, out NodeRecognitionResult result)
            {
                throw new NotImplementedException();
            }
        }

        internal class FauxElement : IAggregationElement
        {
            public AggregationType Type => AggregationType.Unit;

            public bool TryRecognize(TokenReader reader, SymbolPath symbolPath, ILanguageContext context, out NodeAggregationResult result)
            {
                throw new NotImplementedException();
            }
        }

    }

    [TestClass]
    public class ValidationResultTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var result = new ValidationResult([], [], [], []);
            Assert.IsTrue(result.HealthyRefs.IsEmpty);
            Assert.IsTrue(result.UnreferencedProductions.IsEmpty);
            Assert.IsTrue(result.UnresolvableProductions.IsEmpty);
            Assert.IsTrue(result.UnresolvedSymbolRefs.IsEmpty);
        }

        [TestMethod]
        public void IsValid_Tests()
        {
            var result = new ValidationResult([], [], [], []);
            Assert.IsTrue(result.IsValid);

            result = new ValidationResult([], ["bleh"], [], []);
            Assert.IsFalse(result.IsValid);

            result = new ValidationResult([], [], ["bleh"], []);
            Assert.IsFalse(result.IsValid);

            result = new ValidationResult([], [], [], ["bleh"]);
            Assert.IsFalse(result.IsValid);
        }
    }
}
