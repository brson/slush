using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.SyntaxHelpers;
using Slush.DomainObjects.Mp3;

namespace Slush.DomainObjects.Mp3.Test
{
    [TestFixture]
    public class Mp3FrameHeaderTest
    {
        #region Data

        private static readonly byte[][] TEST_HEADER = 
        {
            //            Frame sync
            //            |              Version
            //            |              |  Layer
            //            |              |  |  CRC
            //            |              |  |  |   Bitrate index
            //            |              |  |  |   |    Samplerate index
            //            |              |  |  |   |    |  Padding bit
            //            |              |  |  |   |    |  | Private bit
            //            |              |  |  |   |    |  | |   Channel mode
            //            |              |  |  |   |    |  | |   |  Mode extension
            //            |              |  |  |   |    |  | |   |  |  Copyright
            //            |              |  |  |   |    |  | |   |  |  | Original
            //            |              |  |  |   |    |  | |   |  |  | | Emphasis
            //            |              |  |  |   |    |  | |   |  |  | | |
            //            11111111 / 111 11 01 0 / 1001 00 0 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xFA,         0x90,         0x00 }, // V1L3, 128/44, Stereo, CRC0
            //            11111011 / 111 10 00 1 / 1111 00 0 0 / 01 00 0 0 01
            new byte[] {  0xFB,      0xF1,         0xF0,         0x41 }, // Bad frame sync, V2LR, CRC1, invalid bitrate
            //            11111011 / 111 00 10 1 / 0000 00 0 0 / 10 00 0 0 10
            new byte[] {  0xFB,      0xE5,         0x00,         0x82 }, // Bad frame sync, V2.5L2, free bitrate, 11025 samplerate
            //            11111011 / 111 01 11 1 / 1001 00 1 1 / 11 00 0 1 11
            new byte[] {  0xFB,      0xEF,         0x93,         0xC7 }, // Bad frame sync, VRL1, bitrate, padding, private
            //            11111011 / 111 10 00 1 / 1001 11 0 0 / 00 00 1 0 00
            new byte[] {  0xFF,      0xF1,         0x9C,         0x08 }, // V2LR, bitrate, samplerate reserved
            //            11111011 / 111 10 10 1 / 0100 00 0 0 / 10 00 1 0 00
            new byte[] {  0xFF,      0xF5,         0x4C,         0x88 }, // L2, 32kbps, dual channel
        };

        // indexes into TEST_HEADER
        private static readonly int THIDX_SMOKE = 0;
        private static readonly int THIDX_BAD_FRAME_SYNC = 1;
        private static readonly int THIDX_V1 = 0;
        private static readonly int THIDX_V2 = 1;
        private static readonly int THIDX_V25 = 2;
        private static readonly int THIDX_VR = 3;
        private static readonly int THIDX_LR = 1;
        private static readonly int THIDX_L3 = 0;
        private static readonly int THIDX_L2 = 2;
        private static readonly int THIDX_L1 = 3;
        private static readonly int THIDX_CRC0 = 0; // has CRC
        private static readonly int THIDX_CRC1 = 1; // doesn't have CRC
        private static readonly int THIDX_BR_INVALID = 1;
        private static readonly int THIDX_BR_FREE = 2;
        private static readonly int THIDX_SR_RESERVED = 4;
        private static readonly int THIDX_SR00_V25 = 2;
        private static readonly int THIDX_PADDING = 3;
        private static readonly int THIDX_PRIVATE = 3;
        private static readonly int THIDX_CM_STEREO = 0;
        private static readonly int THIDX_CM_JS = 1;
        private static readonly int THIDX_CM_DUAL = 2;
        private static readonly int THIDX_CM_MONO = 3;
        private static readonly int THIDX_COPY = 4;
        private static readonly int THIDX_ORIG = 3;
        private static readonly int THIDX_EM_NONE = 0;
        private static readonly int THIDX_EM_50 = 1;
        private static readonly int THIDX_EM_R = 2;
        private static readonly int THIDX_EM_C = 3;

        #endregion


        #region Private Members

        private Mp3FrameHeader h;

        #endregion


        #region Tests

        [Test]
        [ExpectedException(typeof(ArgumentNullException),
            ExpectedMessage = "Frame header must not be null",
            MatchType = MessageMatch.Contains)]
        public void Constructor_NullTest()
        {
            new Mp3FrameHeader(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException),
            ExpectedMessage = "Frame header must be 4 bytes long",
            MatchType = MessageMatch.Contains)]
        public void Constructor_ShortTest()
        {
            new Mp3FrameHeader(new byte[3]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException),
            ExpectedMessage = "Frame header must be 4 bytes long",
            MatchType = MessageMatch.Contains)]
        public void Constructor_LongTest()
        {
            new Mp3FrameHeader(new byte[5]);
        }

        /// <summary>
        /// The constructor should make a copy of the incoming list
        /// </summary>
        [Test]
        public void Constructor_CopyTest()
        {
            byte[] b = {0xFF, 0xFF, 0, 0};
            h = new Mp3FrameHeader(b);
            b[0] = 0;
            b[1] = 0;
            Assert.That(h.HasFrameSync, Is.True);
        }

        [Test]
        public void SmokeTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SMOKE]);
            Assert.That(h.HasFrameSync, Is.True);
            Assert.That(h.Version, Is.EqualTo(MpegVersion.V1));
            Assert.That(h.Layer, Is.EqualTo(MpegLayer.Layer3));
            Assert.That(h.HasCRC, Is.True);
            Assert.That(Mp3FrameHeaderRules.CalculateBitrate(h), Is.EqualTo(128000));
            Assert.That(h.HasFreeBitrate, Is.False);
            Assert.That(h.HasInvalidBitrate, Is.False);
            Assert.That(Mp3FrameHeaderRules.CalculateSamplerate(h), Is.EqualTo(44100));
            Assert.That(h.HasReservedSamplerate, Is.False);
            Assert.That(h.HasPadding, Is.False);
            Assert.That(h.HasPrivateFlag, Is.False);
            Assert.That(h.ChannelMode, Is.EqualTo(MpegChannelMode.Stereo));
            Assert.That(h.HasCopyright, Is.False);
            Assert.That(h.HasOriginal, Is.False);
            Assert.That(h.Emphasis, Is.EqualTo(MpegEmphasis.None));
            Assert.That(Mp3FrameHeaderRules.IsValid(h), Is.True);
        }

        [Test]
        public void HasFrameSync_FalseTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BAD_FRAME_SYNC]);
            Assert.That(h.HasFrameSync, Is.False);
        }

        [Test]
        public void Version_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_V1]);
            Assert.That(h.Version, Is.EqualTo(MpegVersion.V1));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_V2]);
            Assert.That(h.Version, Is.EqualTo(MpegVersion.V2));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_V25]);
            Assert.That(h.Version, Is.EqualTo(MpegVersion.V25));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_VR]);
            Assert.That(h.Version, Is.EqualTo(MpegVersion.Reserved));
        }

        [Test]
        public void Layer_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LR]);
            Assert.That(h.Layer, Is.EqualTo(MpegLayer.Reserved));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_L3]);
            Assert.That(h.Layer, Is.EqualTo(MpegLayer.Layer3));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_L2]);
            Assert.That(h.Layer, Is.EqualTo(MpegLayer.Layer2));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_L1]);
            Assert.That(h.Layer, Is.EqualTo(MpegLayer.Layer1));
        }

        [Test]
        public void HasCrc_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_CRC0]);
            Assert.That(h.HasCRC, Is.True, "If the CRC bit is zero, the frame has a CRC");
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_CRC1]);
            Assert.That(h.HasCRC, Is.False, "If the CRC bit is one, the frame has no CRC");
        }

        [Test]
        public void HasFreeBitrate_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_FREE]);
            Assert.That(h.HasFreeBitrate, Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SMOKE]);
            Assert.That(h.HasFreeBitrate, Is.False);
        }

        [Test]
        public void HasInvalidBitrate_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_INVALID]);
            Assert.That(h.HasInvalidBitrate, Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SMOKE]);
            Assert.That(h.HasInvalidBitrate, Is.False);
        }

        [Test]
        public void HasReservedSamplerate_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SR_RESERVED]);
            Assert.That(h.HasReservedSamplerate, Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SR00_V25]);
            Assert.That(h.HasReservedSamplerate, Is.False);
        }

        [Test]
        public void HasPadding_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_PADDING]);
            Assert.That(h.HasPadding, Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SMOKE]);
            Assert.That(h.HasPadding, Is.False);
        }

        [Test]
        public void HasPrivateFlag_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_PRIVATE]);
            Assert.That(h.HasPrivateFlag, Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SMOKE]);
            Assert.That(h.HasPrivateFlag, Is.False);
        }

        [Test]
        public void ChannelMode_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_CM_STEREO]);
            Assert.That(h.ChannelMode, Is.EqualTo(MpegChannelMode.Stereo));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_CM_JS]);
            Assert.That(h.ChannelMode, Is.EqualTo(MpegChannelMode.JointStereo));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_CM_DUAL]);
            Assert.That(h.ChannelMode, Is.EqualTo(MpegChannelMode.DualChannel));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_CM_MONO]);
            Assert.That(h.ChannelMode, Is.EqualTo(MpegChannelMode.SingleChannel));
        }

        [Test]
        public void HasCopyright_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_COPY]);
            Assert.That(h.HasCopyright, Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_ORIG]);
            Assert.That(h.HasCopyright, Is.False);
        }

        [Test]
        public void HasOriginal_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_COPY]);
            Assert.That(h.HasOriginal, Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_ORIG]);
            Assert.That(h.HasOriginal, Is.True);
        }

        [Test]
        public void Emphasis_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_EM_NONE]);
            Assert.That(h.Emphasis, Is.EqualTo(MpegEmphasis.None));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_EM_50]);
            Assert.That(h.Emphasis, Is.EqualTo(MpegEmphasis.Fifty15ms));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_EM_R]);
            Assert.That(h.Emphasis, Is.EqualTo(MpegEmphasis.Reserved));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_EM_C]);
            Assert.That(h.Emphasis, Is.EqualTo(MpegEmphasis.CcitJ17));
        }

        #endregion
    }
}
