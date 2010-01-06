using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using Slush.DomainObjects.Mp3;

namespace Slush.DomainObjects.Mp3.Test
{
    [TestFixture]
    public class JunkRegionTest
    {
        #region Tests

        /// <summary>
        /// Argument of constructor may not be null
        /// </summary>
        [Test]
        [ExpectedException(typeof(ArgumentNullException),
            ExpectedMessage="Argument may not be null",
            MatchType=MessageMatch.Contains)]
        public void Constructor_NullTest()
        {
            new JunkRegion(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException),
            ExpectedMessage="Length must be greater than 0",
            MatchType=MessageMatch.Contains)]
        public void Constructor_0LengthTest()
        {
            byte[] b = new byte[0];
            new JunkRegion(b);
        }

        [Test]
        public void Constructor_CopyInputTest()
        {
            byte[] b = new byte[] { 1, 1 };
            JunkRegion jr = new JunkRegion(b);
            b[1] = 2;
            Assert.AreEqual(1, jr.Bytes[1]);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void Bytes_NotWritableTest()
        {
            JunkRegion jr = new JunkRegion(new byte[10]);
            IList<byte> bytes = jr.Bytes;
            Assert.AreEqual(10, bytes.Count);
            // Modify junk region data
            bytes[9] = 0xFF;
        }

        #endregion
    }
}
