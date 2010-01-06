using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.SyntaxHelpers;
using NUnit.Framework.Extensions;
using Slush.DomainObjects.Mp3;

namespace Slush.DomainObjects.Mp3.Test
{
    [TestFixture]
    public partial class Mp3StreamReaderTest
    {
        #region Members

        protected Mp3StreamReader reader;
        private Mp3StreamBuilder builder;

        #endregion


        #region Private Types

        private class WriteOnlyMemoryStream : MemoryStream
        {
            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }
        }

        #endregion


        #region Abstract Factory Method

        private Mp3StreamReader CreateReader(Stream stream)
        {
            return new Mp3StreamReader(stream);
        }

        #endregion


        #region Housekeeping

        [SetUp]
        public void SetUp()
        {
            builder = new Mp3StreamBuilder();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullTest()
        {
            CreateReader(null);
        }

        #endregion


        #region Tests

        [Test]
        [ExpectedException(typeof(ArgumentException),
            ExpectedMessage = "Stream must be readable",
            MatchType = MessageMatch.Contains)]
        public void Constructor_StreamNotReadableTest()
        {
            CreateReader(new WriteOnlyMemoryStream());
        }

        [Test]
        public void GetEnumerator_FreeBitrateTest()
        {
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 20);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 30);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 40);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 50);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 60);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                Assert.That(region.Bytes.Count, Is.EqualTo(count * 10 + 20));
                count++;
            }
            Assert.That(count, Is.EqualTo(5));
        }

        [Test]
        public void GetEnumerator_FreeBitrateJunkBeforeTest()
        {
            builder.AddJunk(20);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 30);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 40);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 50);
            builder.AddFreeFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 60);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                if (count == 0)
                {
                    Assert.That(region, Is.InstanceOfType(typeof(JunkRegion)));
                }
                else
                {
                    Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                }
                Assert.That(region.Bytes.Count, Is.EqualTo(count * 10 + 20));
                count++;
            }
            Assert.That(count, Is.EqualTo(5));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "Cannot enumerate more than once")]
        public void GetEnumerator_NotReentrantTest()
        {
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                if (count == 1) break;
                count++;
            }

            object o = reader.GetEnumerator().MoveNext();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "Cannot enumerate more than once")]
        public void GetEnumerator_NotReEnumerableTest()
        {
            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                count++;
            }

            reader.GetEnumerator().MoveNext();
        }

        [Test]
        public void GetEnumerator_AsIEnumerableTest()
        {
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in (System.Collections.IEnumerable) reader)
            {
                Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                count++;
            }
            Assert.That(count, Is.EqualTo(3));
        }

        /// <summary>
        /// Tests that the bytes returned as regions are
        /// identical to the bytes in the stream
        /// </summary>
        [Test]
        public void GetEnumerator_IdenticalStreamTest()
        {
            Random r = new Random(0x1234);
            builder.UseRandom = true;

            const int NUM_REGIONS = 1000;
            for (int regionCount = 0; regionCount < NUM_REGIONS; regionCount++)
            {
                if (r.Next() % 2 == 0)
                {
                    builder.AddJunk(r.Next() % 50 + 1);
                }
                else
                {
                    if (r.Next() % 2 == 0)
                    {
                        builder.AddFrame(MpegVersion.V1, MpegLayer.Layer2, true, 0, 0);
                    }
                    else
                    {
                        builder.AddFrame(MpegVersion.V2, MpegLayer.Layer1, false, - (r.Next() % 10), 0);
                    }
                }
            }

            byte[] streamCopy;
            {
                MemoryStream stream = (MemoryStream) builder.BuildStream();
                streamCopy = stream.ToArray();
                reader = CreateReader(stream);
            }

            int copyIndex = 0;
            foreach (IMp3StreamRegion region in reader)
            {
                foreach (byte b in region.Bytes)
                {
                    Assert.That(b, Is.EqualTo(streamCopy[copyIndex]));

                    copyIndex++;
                }
            }

        }

        [Test]
        [Ignore]
        public void GetEnumerator_LongJunkTest()
        {
        }

        [Test]
        [Ignore]
        public void GetEnumerator_LongFreeFrameTest()
        {
        }

        [Test]
        [Ignore]
        public void TODO()
        {
            // run NCover to find more codepaths to test
        }
        #endregion


        #region Parameterized Tests

        [Row(true)]
        [Row(false)]
        [RowTest]
        public void GetEnumerator_SmokeTest(bool useCrc)
        {
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, useCrc, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, useCrc, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, useCrc, 0, 0);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                count++;
            }
            Assert.That(count, Is.EqualTo(3));
        }

        [Row(1)]
        [Row(2)]
        [Row(3)]
        [Row(4)]
        [Row(5)]
        [RowTest]
        public void GetEnumerator_JunkBeforeStreamTest(int junkCount)
        {
            builder.AddJunk(junkCount);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                if (count == 0)
                {
                    Assert.That(region, Is.InstanceOfType(typeof(JunkRegion)));
                }
                else
                {
                    Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                }
                count++;
            }
            Assert.That(count, Is.EqualTo(3));
        }

        [Row(1)]
        [Row(2)]
        [Row(3)]
        [Row(4)]
        [Row(5)]
        [RowTest]
        public void GetEnumerator_JunkMiddleStreamTest(int junkCount)
        {
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0); // junk
            builder.AddJunk(junkCount);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0); // frame
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0); // frame
            builder.AddJunk(junkCount);                                     // junk
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0); // frame
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0); // frame
            builder.AddJunk(junkCount);                                     // junk
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                if (count == 0 || count == 3 || count == 6)
                {
                    Assert.That(region, Is.InstanceOfType(typeof(JunkRegion)));
                }
                else
                {
                    Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                }
                count++;
            }
            // TODO: should the last frame be counted or not?
            Assert.That(count, Is.EqualTo(7));
        }

        [Row(1)]
        [Row(2)]
        [Row(3)]
        [Row(4)]
        [Row(5)]
        [RowTest]
        public void GetEnumerator_JunkEndEstablishedStreamTest(int junkCount)
        {
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddJunk(junkCount);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                if (count == 2)
                {
                    Assert.That(region, Is.InstanceOfType(typeof(JunkRegion)));
                }
                else
                {
                    Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                }
                count++;
            }
            Assert.That(count, Is.EqualTo(3));
        }

        [Row(1)]
        [Row(2)]
        [Row(3)]
        [Row(4)]
        [Row(5)]
        [RowTest]
        public void GetEnumerator_JunkEndNonEstablishedStreamTest(int junkCount)
        {
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
            builder.AddJunk(junkCount);

            int count = 0;
            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                Assert.That(region, Is.InstanceOfType(typeof(JunkRegion)));
                count++;
            }
            Assert.That(count, Is.EqualTo(1));
        }

        [Row(0, 04, false)][Row(0, 05, false)][Row(0, 08, false)][Row(0, 09, false)]
        [Row(0, 06, true)] [Row(0, 07, true)] [Row(0, 10, true)] [Row(0, 11, true)]
        [Row(1, 04, false)][Row(1, 05, false)][Row(1, 08, false)][Row(1, 09, false)]
        [Row(1, 06, true)] [Row(1, 07, true)] [Row(1, 10, true)] [Row(1, 11, true)]
        [Row(2, 04, false)][Row(2, 05, false)][Row(2, 08, false)][Row(2, 09, false)]
        [Row(2, 06, true)] [Row(2, 07, true)] [Row(2, 10, true)] [Row(2, 11, true)]
        [Row(3, 04, false)][Row(3, 05, false)][Row(3, 08, false)][Row(3, 09, false)]
        [Row(3, 06, true)] [Row(3, 07, true)] [Row(3, 10, true)] [Row(3, 11, true)]
        [Row(4, 04, false)][Row(4, 05, false)][Row(4, 08, false)][Row(4, 09, false)]
        [Row(4, 06, true)] [Row(4, 07, true)] [Row(4, 10, true)] [Row(4, 11, true)]
        [RowTest]
        public void GetEnumerator_TruncatedFrameTest(
            int truncatedIndex, int truncatedFrameLength, bool useCrc)
        {
            const int NUM_FRAMES = 5;

            // Row sanity test
            Assert.That(truncatedIndex, Is.LessThan(NUM_FRAMES));
            for (int frameNumber = 0; frameNumber < NUM_FRAMES; frameNumber++)
            {
                if (frameNumber != truncatedIndex)
                {
                    this.builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 0);
                }
                else
                {
                    this.builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, useCrc, 0, truncatedFrameLength);
                }
            }


            int count = 0;
            this.reader = CreateReader(this.builder.BuildStream());
            foreach (IMp3StreamRegion region in this.reader)
            {
                if (count == truncatedIndex && count == 0)
                {
                    // truncated frame at beginning of stream shouldn't be identified
                    Assert.That(region, Is.InstanceOfType(typeof(JunkRegion)));
                }
                else
                {
                    Assert.That(region, Is.InstanceOfType(typeof(Mp3Frame)));
                }
                if (count == truncatedIndex)
                {
                    Assert.That(region.Bytes.Count, Is.EqualTo(truncatedFrameLength));
                }
                count++;
            }
            Assert.That(count, Is.EqualTo(NUM_FRAMES));
        }


        // Rows for GetEnumerator_TruncatedFrameAfterJunkTest
        [Row(0, 04, 1, false)][Row(0, 05, 1, false)][Row(0, 08, 1, false)][Row(0, 09, 1, false)]
        [Row(0, 06, 1, true)] [Row(0, 07, 1, true)] [Row(0, 10, 1, true)] [Row(0, 11, 1, true)]
        [Row(1, 04, 1, false)][Row(1, 05, 1, false)][Row(1, 08, 1, false)][Row(1, 09, 1, false)]
        [Row(1, 06, 1, true)] [Row(1, 07, 1, true)] [Row(1, 10, 1, true)] [Row(1, 11, 1, true)]
        [Row(2, 04, 1, false)][Row(2, 05, 1, false)][Row(2, 08, 1, false)][Row(2, 09, 1, false)]
        [Row(2, 06, 1, true)] [Row(2, 07, 1, true)] [Row(2, 10, 1, true)] [Row(2, 11, 1, true)]
        [Row(3, 04, 1, false)][Row(3, 05, 1, false)][Row(3, 08, 1, false)][Row(3, 09, 1, false)]
        [Row(3, 06, 1, true)] [Row(3, 07, 1, true)] [Row(3, 10, 1, true)] [Row(3, 11, 1, true)]
        [Row(4, 04, 1, false)][Row(4, 05, 1, false)][Row(4, 08, 1, false)][Row(4, 09, 1, false)]
        [Row(4, 06, 1, true)] [Row(4, 07, 1, true)] [Row(4, 10, 1, true)] [Row(4, 11, 1, true)]

        [Row(0, 04, 4, false)][Row(0, 05, 4, false)][Row(0, 08, 4, false)][Row(0, 09, 4, false)]
        [Row(0, 06, 4, true)] [Row(0, 07, 4, true)] [Row(0, 10, 4, true)] [Row(0, 11, 4, true)]
        [Row(1, 04, 4, false)][Row(1, 05, 4, false)][Row(1, 08, 4, false)][Row(1, 09, 4, false)]
        [Row(1, 06, 4, true)] [Row(1, 07, 4, true)] [Row(1, 10, 4, true)] [Row(1, 11, 4, true)]
        [Row(2, 04, 4, false)][Row(2, 05, 4, false)][Row(2, 08, 4, false)][Row(2, 09, 4, false)]
        [Row(2, 06, 4, true)] [Row(2, 07, 4, true)] [Row(2, 10, 4, true)] [Row(2, 11, 4, true)]
        [Row(3, 04, 4, false)][Row(3, 05, 4, false)][Row(3, 08, 4, false)][Row(3, 09, 4, false)]
        [Row(3, 06, 4, true)] [Row(3, 07, 4, true)] [Row(3, 10, 4, true)] [Row(3, 11, 4, true)]
        [Row(4, 04, 4, false)][Row(4, 05, 4, false)][Row(4, 08, 4, false)][Row(4, 09, 4, false)]
        [Row(4, 06, 4, true)] [Row(4, 07, 4, true)] [Row(4, 10, 4, true)] [Row(4, 11, 4, true)]

        [Row(0, 04, 5, false)][Row(0, 05, 5, false)][Row(0, 08, 5, false)][Row(0, 09, 5, false)]
        [Row(0, 06, 5, true)] [Row(0, 07, 5, true)] [Row(0, 10, 5, true)] [Row(0, 11, 5, true)]
        [Row(1, 04, 5, false)][Row(1, 05, 5, false)][Row(1, 08, 5, false)][Row(1, 09, 5, false)]
        [Row(1, 06, 5, true)] [Row(1, 07, 5, true)] [Row(1, 10, 5, true)] [Row(1, 11, 5, true)]
        [Row(2, 04, 5, false)][Row(2, 05, 5, false)][Row(2, 08, 5, false)][Row(2, 09, 5, false)]
        [Row(2, 06, 5, true)] [Row(2, 07, 5, true)] [Row(2, 10, 5, true)] [Row(2, 11, 5, true)]
        [Row(3, 04, 5, false)][Row(3, 05, 5, false)][Row(3, 08, 5, false)][Row(3, 09, 5, false)]
        [Row(3, 06, 5, true)] [Row(3, 07, 5, true)] [Row(3, 10, 5, true)] [Row(3, 11, 5, true)]
        [Row(4, 04, 5, false)][Row(4, 05, 5, false)][Row(4, 08, 5, false)][Row(4, 09, 5, false)]
        [Row(4, 06, 5, true)] [Row(4, 07, 5, true)] [Row(4, 10, 5, true)] [Row(4, 11, 5, true)]

        [RowTest]
        public void GetEnumerator_TruncatedFrameAfterJunkTest(
            int truncatedIndex, int truncatedFrameLength, int junkLength, bool useCrc)
        {
            const int NUM_FRAMES = 5;

            // Row sanity test
            Assert.That(truncatedIndex, Is.LessThan(NUM_FRAMES));
            for (int frameNumber = 0; frameNumber < NUM_FRAMES; frameNumber++)
            {
                if (frameNumber != truncatedIndex)
                {
                    builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);
                }
                else
                {
                    builder.AddJunk(junkLength);
                    builder.AddFrame(MpegVersion.V1, MpegLayer.Layer3, useCrc, 0, truncatedFrameLength);
                }
            }

            int count = 0;

            reader = CreateReader(builder.BuildStream());
            foreach (IMp3StreamRegion region in reader)
            {
                if (truncatedIndex == 1)
                {
                    // first frame, junk, and truncated frame are all one junk region
                    if (count == 0)
                    {
                        Assert.That(region, Is.TypeOf(typeof(JunkRegion)));
                    }
                    else
                    {
                        Assert.That(region, Is.TypeOf(typeof(Mp3Frame)));
                    }
                }
                else if (truncatedIndex == NUM_FRAMES - 2) // second to last
                {
                    // junk, truncated frame, and last frame are all one junk region
                    if (count == NUM_FRAMES - 2)
                    {
                        Assert.That(region, Is.TypeOf(typeof(JunkRegion)));
                    }
                    else
                    {
                        Assert.That(region, Is.TypeOf(typeof(Mp3Frame)));
                    }
                }
                else
                {
                    // junk and truncated frame are both junk
                    if (count == truncatedIndex)
                    {
                        Assert.That(region, Is.TypeOf(typeof(JunkRegion)));
                    }
                    else
                    {
                        Assert.That(region, Is.TypeOf(typeof(Mp3Frame)));
                    }
                }

                count++;
            }

            if (truncatedIndex == 1
                || truncatedIndex == NUM_FRAMES - 2)
            {
                Assert.That(count, Is.EqualTo(NUM_FRAMES - 1));
            }
            else
            {
                Assert.That(count, Is.EqualTo(NUM_FRAMES));
            }

        }

        [Test]
        public void GetEnumerator_Id3Tag()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
