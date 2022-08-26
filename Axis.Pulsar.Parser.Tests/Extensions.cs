using Axis.Pulsar.Parser.CST;
using Axis.Pulsar.Parser.Input;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Tests
{
    internal static class Extensions
    {

        #region ICSTNode accessors

        public static ICSTNode FirstNode(this ICSTNode node) => node switch
        {
            ICSTNode.LeafNode => null,

            ICSTNode.BranchNode branch => branch.FirstNode(),

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };

        public static ICSTNode LastNode(this ICSTNode node) => node switch
        {
            ICSTNode.LeafNode => null,

            ICSTNode.BranchNode branch => branch.LastNode(),

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };

        public static ICSTNode NodeAt(this ICSTNode node, int index) => node switch
        {
            ICSTNode.LeafNode => null,

            ICSTNode.BranchNode branch => branch.NodeAt(index),

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };

        public static IEnumerable<ICSTNode> Nodes(this ICSTNode node) => node switch
        {
            ICSTNode.LeafNode => Enumerable.Empty<ICSTNode>(),

            ICSTNode.BranchNode branch => branch.Nodes,

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };

        public static IEnumerable<ICSTNode> FindNodes(this ICSTNode node, string path) => node switch
        {
            ICSTNode.LeafNode => Enumerable.Empty<ICSTNode>(),

            ICSTNode.BranchNode branch => branch.FindNodes(path),

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };

        public static ICSTNode FindNode(this ICSTNode node, string path) => node switch
        {
            ICSTNode.LeafNode => null,

            ICSTNode.BranchNode branch => branch.FindNode(path),

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };

        public static string TokenValue(this ICSTNode node) => node switch
        {
            ICSTNode.LeafNode leaf => leaf.Tokens,

            ICSTNode.BranchNode branch => branch.AggregateTokens,

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };

        #endregion

        public static string Concat(this IEnumerable<string> strings) => string.Join("", strings);

        internal static Parser.Recognizers.IRecognizer CreateRecognizer(this Parser.Recognizers.IResult expectedResult)
        {
            var mock = new Mock<Parser.Recognizers.IRecognizer>();
            _ = mock.Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns(expectedResult);

            return mock.Object;
        }

        internal static Parser.Recognizers.IRecognizer CreatePassingRecognizer(this (string symbol, string token)  tuple)
            => CreateRecognizer(new Parser.Recognizers.IResult.Success(ICSTNode.Of(tuple.symbol, tuple.token)));
    }
}
