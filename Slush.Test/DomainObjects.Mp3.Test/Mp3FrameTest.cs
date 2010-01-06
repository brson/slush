using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.SyntaxHelpers;
using Slush.DomainObjects.Mp3;

namespace Slush.DomainObjects.Mp3.Test
{
    [TestFixture]
    public class Mp3FrameTest
    {
        //            11111111 / 111 11 01 0 / 1001 00 0 0 / 00 00 0 0 00
        // new byte[] {  0xFF,      0xFA,         0x90,         0x00 }, // V1L3, 128/44, Stereo, CRC0
        //            11111011 / 111 10 00 1 / 1111 00 0 0 / 01 00 0 0 01
        // new byte[] {  0xFB,      0xF1,         0xF0,         0x41 }, // Bad frame sync, V2LR, CRC1, invalid bitrate

        private Mp3Frame f;

        public static byte[] BuildFrame(
            MpegVersion version,
            MpegLayer layer,
            bool hasCRC,
            int frameSizeOffset,
            int frameSize)
        {
            byte[] b = new byte[] { 0xFF, 0xE0, 0x90, 0x00 };

            b[1] |= (byte) version;
            b[1] |= (byte) layer;
            if (!hasCRC)
            {
                b[1]++;
            }

            byte[] frameData;
            Mp3FrameHeader h = new Mp3FrameHeader(b);
            if (0 == frameSize)
            {
                frameData = new byte[Mp3FrameHeaderRules.CalculateFrameLength(h) + frameSizeOffset];
            }
            else
            {
                frameData = new byte[frameSize + frameSizeOffset];
            }
            try
            {
                Array.Copy(b, frameData, 4);
            }
            catch
            {
            }

            return frameData;
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException),
            ExpectedMessage="Frame data may not be null",
            MatchType=MessageMatch.Contains)]
        public void Constructor_NullTest()
        {
            new Mp3Frame(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException),
            ExpectedMessage = "Invalid frame header",
            MatchType = MessageMatch.Contains)]
        public void Constructor_InvalidHeaderTest()
        {
            new Mp3Frame(BuildFrame(MpegVersion.Reserved, MpegLayer.Layer3, true, 0, 7));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException),
            ExpectedMessage = "Data too long for frame",
            MatchType = MessageMatch.Contains)]
        public void Constructor_FrameTooLongTest()
        {
            new Mp3Frame(BuildFrame(MpegVersion.V1, MpegLayer.Layer3, true, +1, 0));
        }

        /// <summary>
        /// Free bitrates should accept large frames
        /// </summary>
        [Test]
        public void Constructor_FrameFreeTest()
        {
            byte[] data = BuildFrame(MpegVersion.V1, MpegLayer.Layer3, true, +1, 5000);
            // set free bitrate
            data[2] &= 0x0F;
            f = new Mp3Frame(data);
            Assert.That(f.Bytes.Count, Is.EqualTo(data.Length));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException),
            ExpectedMessage = "Data too short for frame. Must be at least 4 bytes",
            MatchType = MessageMatch.Contains)]
        public void Constructor_FrameTooShortNoCRCTest()
        {
            new Mp3Frame(BuildFrame(MpegVersion.V1, MpegLayer.Layer3, false, 0, 3));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException),
            ExpectedMessage = "Data too short for frame. Must be at least 6 bytes with CRC",
            MatchType = MessageMatch.Contains)]
        public void Constructor_FrameTooShortCRCTest()
        {
            new Mp3Frame(BuildFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 5));
        }

        [Test]
        public void Constructor_CopyInputTest()
        {
            byte[] b = BuildFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);
            f = new Mp3Frame(b);
            b[7] = 0xFF;
            Assert.That(f.Bytes[7], Is.EqualTo(0x00));
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void Bytes_NotWritableTest()
        {
            byte[] frame = BuildFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);
            f = new Mp3Frame(frame);
            IList<byte> bytes = f.Bytes;
            // Modify data
            bytes[0] = 0xFF;
        }

        [Test]
        public void Bytes_Test()
        {
            byte[] bytes = BuildFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);
            Mp3Frame f = new Mp3Frame(bytes);
            Assert.That(f.Bytes.Count, Is.EqualTo(bytes.Length));
        }

        [Test]
        public void Header_Test()
        {
            byte[] bytes = BuildFrame(MpegVersion.V1, MpegLayer.Layer2, true, 0, 0);
            byte[] header = new byte[4];
            for (int i = 0; i < 4; i++) header[i] = bytes[i];
            Mp3FrameHeader h1 = new Mp3FrameHeader(header);

            f = new Mp3Frame(bytes);
            Mp3FrameHeader h2 = f.Header;
            Assert.That(h2.Version, Is.EqualTo(h1.Version));
            Assert.That(h2.Layer, Is.EqualTo(h1.Layer));
            Assert.That(h2.SamplerateIndex, Is.EqualTo(h1.SamplerateIndex));
        }

        [Test]
        public void CalculatedFrameLength_Test()
        {
            f = new Mp3Frame(BuildFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0));
            Assert.That(f.CalculatedFrameLength,
                Is.EqualTo(Mp3FrameHeaderRules.CalculateFrameLength(f.Header)));
        }

        [Test]
        public void ActualFrameLength_Test()
        {
            f = new Mp3Frame(BuildFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 50));
            Assert.That(f.ActualFrameLength, Is.EqualTo(50));
        }

        [Test]
        public void IsTruncated_Test()
        {
            f = new Mp3Frame(BuildFrame(MpegVersion.V25, MpegLayer.Layer1, true, -1, 0));
            Assert.That(f.IsTruncated, Is.True);
            f = new Mp3Frame(BuildFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0));
            Assert.That(f.IsTruncated, Is.False);
        }

        [Test]
        public void HasCRC_Test()
        {
            f = new Mp3Frame(BuildFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0));
            Assert.That(f.HasCRC, Is.True);
            f = new Mp3Frame(BuildFrame(MpegVersion.V25, MpegLayer.Layer1, false, 0, 0));
            Assert.That(f.HasCRC, Is.False);
        }

        [Test]
        public void CRC_Test()
        {
            byte[] b = BuildFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);
            // set the crc
            b[4] = 0xAA;
            b[5] = 0xBB;
            f = new Mp3Frame(b);
            Assert.That(f.CRC, Is.EqualTo(new byte[] { 0xAA, 0xBB }));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame does not have a CRC")]
        public void CRC_NoHasCRCTest()
        {
            f = new Mp3Frame(BuildFrame(MpegVersion.V25, MpegLayer.Layer1, false, 0, 0));
            IList<byte> b = f.CRC;
        }
    }
}
