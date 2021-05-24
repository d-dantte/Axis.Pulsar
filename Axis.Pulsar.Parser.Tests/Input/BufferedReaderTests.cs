using Axis.Pulsar.Parser.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Tests.Input
{
    [TestClass]
    public class BufferedReaderTests
    {
        [TestMethod]
        public void Constructor_ShouldCreateValidObject()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            Assert.IsNotNull(reader);
            Assert.AreEqual(-1, reader.Position);
        }

        [TestMethod]
        public void TryNextToken_Should_ReadCorrectTokensFromSource()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            //read and assert
            var success = reader.TryNextToken(out var token);
            Assert.IsTrue(success);
            Assert.AreEqual('\r', token);

            //read and assert
            success = reader.TryNextToken(out token);
            Assert.IsTrue(success);
            Assert.AreEqual('\n', token);

            //read and assert
            success = reader.TryNextToken(out token);
            Assert.IsTrue(success);
            Assert.AreEqual('{', token);

            //read and assert
            success = reader.TryNextToken(out token);
            Assert.IsTrue(success);
            Assert.AreEqual('\r', token);

            //read and assert
            success = reader.TryNextToken(out token);
            Assert.IsTrue(success);
            Assert.AreEqual('\n', token);
        }

        [TestMethod]
        public void TryNextTokens_Should_ReadCorrectTokensFromSource()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            //read and assert
            var success = reader.TryNextTokens(5, out var tokens);
            Assert.IsTrue(success);
            Assert.IsTrue(tokens.SequenceEqual(new[] { '\r', '\n', '{', '\r', '\n' }));
        }

        [TestMethod]
        public void TryNextToken_PastTheEndOfTheSource_Should_ReturnFalse_And_NotChangeThePosition()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            var succeeded = false;
            var token = '\0';

            for (int cnt = 0; cnt < 450; cnt++)
            {
                succeeded = reader.TryNextToken(out token);
            }

            Assert.IsFalse(succeeded);
            Assert.AreEqual(default, token);
            Assert.AreEqual(SAMPLE_JSON.Length - 1, reader.Position);
        }

        [TestMethod]
        public void TryNextTokens_PastTheEndOfTheSource_Should_ReturnFalse_And_NotChangeThePosition()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            var succeeded = reader.TryNextTokens(450, out var tokens);

            Assert.IsFalse(succeeded);
            Assert.AreEqual(null, tokens);
            Assert.AreEqual(-1, reader.Position);
        }

        [TestMethod]
        public void MixingReadStyles_Should_ReturnCorrectTokens()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            var succeeded = reader.TryNextToken(out var token);
            Assert.IsTrue(succeeded);
            Assert.AreEqual('\r', token);
            Assert.AreEqual(0, reader.Position);

            succeeded = reader.TryNextTokens(3, out var tokens);
            Assert.IsTrue(succeeded);
            Assert.IsTrue(tokens.SequenceEqual(new[] { '\n', '{', '\r' }));
            Assert.AreEqual(3, reader.Position);
        }

        [TestMethod]
        public void Reset_Should_ResetThePositionCorrectly()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            var succeeded = reader.TryNextTokens(100, out var first100);
            Assert.AreEqual(99, reader.Position);
            Assert.IsTrue(first100.SequenceEqual(SAMPLE_JSON.Substring(0, 100)));

            reader.Reset(0);
            Assert.AreEqual(0, reader.Position);

            succeeded = reader.TryNextToken(out var token0);
            Assert.IsTrue(succeeded);
            Assert.AreEqual('\n', token0);

            succeeded = reader.TryNextTokens(2, out var tokens1_2);
            Assert.IsTrue(succeeded);
            Assert.IsTrue(tokens1_2.SequenceEqual(new[] { '{', '\r' }));

            reader.Reset();
            Assert.AreEqual(-1, reader.Position);
        }

        [TestMethod]
        public void Reset_WithInvalidPosition_Should_ThrowExceptions()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);
            _ = reader.TryNextTokens(120, out _);

            Assert.ThrowsException<ArgumentException>(() => reader.Reset(-4));
            Assert.AreEqual(119, reader.Position);
            Assert.ThrowsException<ArgumentException>(() => reader.Reset(120));
            Assert.AreEqual(119, reader.Position);
        }

        [TestMethod]
        public void Back_Should_ResetThePositionBackwardsBy1()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            //read and assert
            _ = reader.TryNextTokens(42, out _);
            Assert.AreEqual(41, reader.Position);

            reader.Back();
            Assert.AreEqual(41 - 1, reader.Position);

            reader.Back(22);
            Assert.AreEqual(40 - 22, reader.Position);

            reader.Back(0);
            Assert.AreEqual(40 - 22, reader.Position);
        }

        [TestMethod]
        public void Back_WithInvalidOffset_ShouldThrowException()
        {
            var reader = new BufferedTokenReader(SAMPLE_JSON);

            //read and assert
            _ = reader.TryNextTokens(42, out _);
            Assert.AreEqual(41, reader.Position);

            Assert.ThrowsException<ArgumentException>(() => reader.Back(-1));
        }

        private static readonly string SAMPLE_JSON = @"
{
  ""firstName"": ""John"",
  ""lastName"": ""Smith"",
  ""isAlive"": true,
  ""age"": 27,
  ""address"": {
    ""streetAddress"": ""21 2nd Street"",
    ""city"": ""New York"",
    ""state"": ""NY"",
    ""postalCode"": ""10021-3100""
  },
  ""phoneNumbers"": [
    {
      ""type"": ""home"",
      ""number"": ""212 555-1234""
    },
    {
      ""type"": ""office"",
      ""number"": ""646 555-4567""
    }
  ],
  ""children"": [],
  ""spouse"": null
}";
    }
}
