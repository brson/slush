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
    public class Mp3FrameHeaderRulesTest
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
            //            11111011 / 111 10 00 1 / 1101 00 0 0 / 01 00 0 0 01
            new byte[] {  0xFB,      0xF1,         0xD0,         0x41 }, // Bad frame sync, V2LR, CRC1, valid bitrate
            //            11111111 / 111 11 01 0 / 1111 00 0 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xFA,         0xF0,         0x00 }, // V1L3, 128/44, Stereo, CRC0, invalid bitrate
            //            11111111 / 111 11 01 0 / 1001 11 0 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xFA,         0x9C,         0x00 }, // V1L3, 128/44, Stereo, CRC0, samplerate res

            //            11111111 / 111 00 11 1 / 1001 00 0 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xE7,         0x90,         0x00 }, // V25L1, 144/11025, no padding
            //            11111111 / 111 00 11 1 / 0001 01 1 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xE7,         0x16,         0x00 }, // V25L1, 32/12000, padding
            //            11111111 / 111 10 10 1 / 1101 10 0 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xF5,         0xD8,         0x00 }, // V2L2, 144/16000, no padding
            //            11111111 / 111 10 10 1 / 0011 01 1 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xF5,         0x36,         0x00 }, // V2L2, 24/24000, padding
            //            11111111 / 111 11 01 1 / 1000 10 0 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xFB,         0x88,         0x00 }, // V1L3, 112/32000, no padding
            //            11111111 / 111 11 01 1 / 0100 01 1 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xFB,         0x46,         0x00 }, // V1L3, 56/48000, padding
            //            11111111 / 111 11 01 0 / 0100 01 1 0 / 00 00 0 0 00
            new byte[] {  0xFF,      0xFA,         0x46,         0x00 }, // V1L3, 56/48000, padding, Use CRC
        };

        // indexes into TEST_HEADER
        private static readonly int THIDX_SMOKE = 0;
        private static readonly int THIDX_BR_128 = 0;
        private static readonly int THIDX_BR_INVALID = 1;
        private static readonly int THIDX_BR_FREE = 2;
        private static readonly int THIDX_BR_VR = 3;
        private static readonly int THIDX_BR_LR = 4;
        private static readonly int THIDX_SR_RESERVED = 4;
        private static readonly int THIDX_SR00_V25 = 2;
        private static readonly int THIDX_LEN_OK = 0;
        private static readonly int THIDX_LEN_LR = 6;
        private static readonly int THIDX_LEN_BR_INVALID = 7;
        private static readonly int THIDX_LEN_BR_FREE = 2;
        private static readonly int THIDX_LEN_SR_RESERVED = 8;
        private static readonly int THIDX_LEN_VR = 3;
        private static readonly int THIDX_LEN_V25L1_NO_PADDING = 9; // 628
        private static readonly int THIDX_LEN_V25L1_PADDING = 10; // 136
        private static readonly int THIDX_LEN_V2L2_NO_PADDING = 11; // 1300
        private static readonly int THIDX_LEN_V2L2_PADDING = 12; // 149
        private static readonly int THIDX_LEN_V1L3_NO_PADDING = 13; // 508
        private static readonly int THIDX_LEN_V1L3_PADDING = 14; // 173
        private static readonly int THIDX_LEN_WITH_CRC = 15; // 175

        #endregion


        #region Private Members

        private Mp3FrameHeader h;

        #endregion


        #region Tests

        [Test]
        public void CalculateBitrate_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_128]);
            Assert.That(Mp3FrameHeaderRules.CalculateBitrate(h), Is.EqualTo(128000));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CalculateBitrate_NullTest()
        {
            Mp3FrameHeaderRules.CalculateBitrate(null);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame has a free bitrate")]
        public void CalculateBitrate_HasFreeTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_FREE]);
            Mp3FrameHeaderRules.CalculateBitrate(h);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame has an invalid bitrate")]
        public void CalculateBitrate_HasInvalidTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_INVALID]);
            Mp3FrameHeaderRules.CalculateBitrate(h);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame does not specify a version")]
        public void CalculateBitrate_ReservedVersionTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_VR]);
            Mp3FrameHeaderRules.CalculateBitrate(h);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame does not specify a layer")]
        public void CalculateBitrate_ReservedLayerTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_LR]);
            Mp3FrameHeaderRules.CalculateBitrate(h);
        }

        [Test]
        public void CanCalculateBitrate_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_128]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateBitrate(h), Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_FREE]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateBitrate(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_INVALID]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateBitrate(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_VR]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateBitrate(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_LR]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateBitrate(h), Is.False);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CanCalculateBitrate_NullTest()
        {
            Mp3FrameHeaderRules.CanCalculateBitrate(null);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame has a reserved samplerate")]
        public void CalculateSamplerate_HasReservedTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SR_RESERVED]);
            Mp3FrameHeaderRules.CalculateSamplerate(h);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame has a reserved version")]
        public void CalculateSamplerate_ReservedVersionTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_VR]);
            Mp3FrameHeaderRules.CalculateSamplerate(h);
        }

        [Test]
        public void CalculateSamplerate_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SMOKE]);
            Assert.That(Mp3FrameHeaderRules.CalculateSamplerate(h), Is.EqualTo(44100));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SR00_V25]);
            Assert.That(Mp3FrameHeaderRules.CalculateSamplerate(h), Is.EqualTo(11025));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CalculateSamplerate_NullTest()
        {
            Mp3FrameHeaderRules.CalculateSamplerate(null);
        }

        [Test]
        public void CanCalculateSamplerate_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SR_RESERVED]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateSamplerate(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_BR_VR]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateSamplerate(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_SMOKE]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateSamplerate(h), Is.True);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CanCalculateSamplerate_NullTest()
        {
            Mp3FrameHeaderRules.CanCalculateSamplerate(null);
        }

        // Frame length depends on version, layer, bitrate, samplerate
        [Test]
        public void CanCalculateFrameLength_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_OK]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateFrameLength(h), Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_LR]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateFrameLength(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_BR_INVALID]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateFrameLength(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_BR_FREE]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateFrameLength(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_SR_RESERVED]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateFrameLength(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_VR]);
            Assert.That(Mp3FrameHeaderRules.CanCalculateFrameLength(h), Is.False);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CanCalculateFrameLength_NullTest()
        {
            Mp3FrameHeaderRules.CalculateFrameLength(null);
        }

        [Test]
        public void CalculateFrameLength_Test()
        {
            // V25, Layer 1, no padding
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_V25L1_NO_PADDING]);
            Assert.That(Mp3FrameHeaderRules.CalculateFrameLength(h), Is.EqualTo(624));
            // V25, Layer 1, padding
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_V25L1_PADDING]);
            Assert.That(Mp3FrameHeaderRules.CalculateFrameLength(h), Is.EqualTo(132));
            // V2, Layer 2, no padding
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_V2L2_NO_PADDING]);
            Assert.That(Mp3FrameHeaderRules.CalculateFrameLength(h), Is.EqualTo(1296));
            // V2, Layer 2, padding
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_V2L2_PADDING]);
            Assert.That(Mp3FrameHeaderRules.CalculateFrameLength(h), Is.EqualTo(145));
            // V1, Layer 3, no padding
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_V1L3_NO_PADDING]);
            Assert.AreEqual(112000, Mp3FrameHeaderRules.CalculateBitrate(h));
            Assert.AreEqual(32000, Mp3FrameHeaderRules.CalculateSamplerate(h));
            Assert.That(Mp3FrameHeaderRules.CalculateFrameLength(h), Is.EqualTo(504));
            // V1, Layer 3, padding
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_V1L3_PADDING]);
            Assert.That(Mp3FrameHeaderRules.CalculateFrameLength(h), Is.EqualTo(169));
            // Add a CRC
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_WITH_CRC]);
            Assert.That(Mp3FrameHeaderRules.CalculateFrameLength(h), Is.EqualTo(171));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CalculateFrameLength_NullTest()
        {
            Mp3FrameHeaderRules.CalculateFrameLength(null);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "Cannot calculate bitrate")]
        public void CalculateFrameLength_NoBitrateTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_BR_FREE]);
            Mp3FrameHeaderRules.CalculateFrameLength(h);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "Cannot calculate samplerate")]
        public void CalculateFrameLength_NoSamplerateTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_SR_RESERVED]);
            Mp3FrameHeaderRules.CalculateFrameLength(h);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame has a reserved layer")]
        public void CalculateFrameLength_ReservedLayerTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[4]);
            Mp3FrameHeaderRules.CalculateFrameLength(h);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "This frame has a reserved version")]
        public void CalculateFrameLength_ReservedVersionTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[3]);
            Mp3FrameHeaderRules.CalculateFrameLength(h);
        }

        [Test]
        public void HasInvalidBitrateForChannelMode_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[0]);
            Assert.That(Mp3FrameHeaderRules.HasInvalidBitrateForChannelMode(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[1]);
            Assert.That(Mp3FrameHeaderRules.HasInvalidBitrateForChannelMode(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[2]);
            Assert.That(Mp3FrameHeaderRules.HasInvalidBitrateForChannelMode(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[3]);
            Assert.That(Mp3FrameHeaderRules.HasInvalidBitrateForChannelMode(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[4]);
            Assert.That(Mp3FrameHeaderRules.HasInvalidBitrateForChannelMode(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[5]);
            Assert.That(Mp3FrameHeaderRules.HasInvalidBitrateForChannelMode(h), Is.True);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HasInvalidBitrateForChannelMode_NullTest()
        {
            Mp3FrameHeaderRules.HasInvalidBitrateForChannelMode(null);
        }

        [Test]
        public void IsValid_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[0]);
            Assert.That(Mp3FrameHeaderRules.IsValid(h), Is.True);
            h = new Mp3FrameHeader(TEST_HEADER[1]);
            Assert.That(Mp3FrameHeaderRules.IsValid(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[2]);
            Assert.That(Mp3FrameHeaderRules.IsValid(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[3]);
            Assert.That(Mp3FrameHeaderRules.IsValid(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[4]);
            Assert.That(Mp3FrameHeaderRules.IsValid(h), Is.False);
            h = new Mp3FrameHeader(TEST_HEADER[5]);
            Assert.That(Mp3FrameHeaderRules.IsValid(h), Is.False);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsValid_NullTest()
        {
            Mp3FrameHeaderRules.IsValid(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CalculatePayloadLength_NullTest()
        {
            Mp3FrameHeaderRules.CalculatePayloadLength(null);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CalculatePayloadLength_CantCalculateTest()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_BR_FREE]);
            Mp3FrameHeaderRules.CalculatePayloadLength(h);
        }

        [Test]
        public void CalculatePayloadLength_Test()
        {
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_V1L3_PADDING]);
            Assert.That(Mp3FrameHeaderRules.CalculatePayloadLength(h), Is.EqualTo(169 - 4));
            h = new Mp3FrameHeader(TEST_HEADER[THIDX_LEN_WITH_CRC]);
            Assert.That(Mp3FrameHeaderRules.CalculatePayloadLength(h), Is.EqualTo(171 - 6));
        }


        #endregion
    }
}
