using Axis.Pulsar.Parser.CST;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Tests.CST
{
    [TestClass]
    public class CSTNodeTests
    {
        [TestMethod]
        public void ConstructLeafNode_ShouldReturnSuccessfully()
        {
            // arrange
            var symbolName = "xyz_symbol";
            var tokens = "the recognized tokens";
            var leaf = ICSTNode.Of(symbolName, tokens);
            var trueLeaf = leaf as ICSTNode.LeafNode;

            // assert
            Assert.IsNotNull(leaf);
            Assert.AreEqual(symbolName, leaf.SymbolName);
            Assert.IsTrue(leaf is ICSTNode.LeafNode);
            Assert.AreEqual(tokens, trueLeaf.Tokens);
        }

        [TestMethod]
        public void LeafConstructor_ShouldThrowException_WhenInputIsInvalid()
        {
            // arrange
            var symbolName = "xyz_symbol";
            var tokens = "the recognized tokens";

            // assert
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of(null, tokens));
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of("", tokens));
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of("   ", tokens));
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of(symbolName, (string)null));
        }

        [TestMethod]
        public void ConstructBranchNode_ShouldReturnSuccessfully()
        {
            // arrange
            var symbolName = "xyz_symbol";
            var tokens = "the recognized tokens";
            var innerNodes = new[]
            {
                ICSTNode.Of(symbolName + 1, tokens + 1),
                ICSTNode.Of(symbolName + 2, tokens + 2),
                ICSTNode.Of(symbolName + 3, tokens + 3),
                ICSTNode.Of(symbolName + 4, tokens + 4)
            };
            var branch = ICSTNode.Of(
                symbolName,
                innerNodes);
            var trueBranch = branch as ICSTNode.BranchNode;

            // assert
            Assert.IsNotNull(branch);
            Assert.AreEqual(symbolName, branch.SymbolName);
            Assert.IsTrue(branch is ICSTNode.BranchNode);
            Assert.IsTrue(innerNodes.SequenceEqual(trueBranch.Nodes));
            Assert.AreEqual(
                innerNodes.Select(n => n.TokenValue()).Concat(),
                trueBranch.AggregateTokens);
        }

        [TestMethod]
        public void ConstructBranchNode_ShouldThrowException_WhenInputIsInvalid()
        {
            // arrange
            var symbolName = "xyz_symbol";
            var tokens = "the recognized tokens";
            var innerNodes = new[]
            {
                ICSTNode.Of(symbolName + 1, tokens + 1),
                ICSTNode.Of(symbolName + 2, tokens + 2),
                ICSTNode.Of(symbolName + 3, tokens + 3),
                ICSTNode.Of(symbolName + 4, tokens + 4)
            };

            // assert
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of(null, innerNodes));
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of("", innerNodes));
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of("   ", innerNodes));
            Assert.ThrowsException<ArgumentNullException>(() => ICSTNode.Of(symbolName, (ICSTNode[])null));
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of(symbolName, innerNodes.Append(null).ToArray()));
            Assert.ThrowsException<ArgumentException>(() => ICSTNode.Of(symbolName, innerNodes.Append(new FakeICSTNode()).ToArray()));
        }

        public class FakeICSTNode : ICSTNode
        {
            public string SymbolName { get; set; }

            public IEnumerable<ICSTNode> AllChildNodes()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ICSTNode> FindAllNodes(string symbolName)
            {
                throw new NotImplementedException();
            }

            public ICSTNode FindNode(string path)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ICSTNode> FindNodes(string path)
            {
                throw new NotImplementedException();
            }

            public ICSTNode FirstNode()
            {
                throw new NotImplementedException();
            }

            public ICSTNode LastNode()
            {
                throw new NotImplementedException();
            }

            public ICSTNode NodeAt(int index)
            {
                throw new NotImplementedException();
            }

            public bool TryFindAllNodes(string symbolName, out ICSTNode[] nodes)
            {
                throw new NotImplementedException();
            }

            public bool TryFindNode(string path, out ICSTNode node)
            {
                throw new NotImplementedException();
            }

            public bool TryFindNodes(string path, out ICSTNode[] nodes)
            {
                throw new NotImplementedException();
            }
        }
    }
}
