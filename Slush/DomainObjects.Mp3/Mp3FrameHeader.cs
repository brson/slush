using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Slush.DomainObjects.Mp3
{
    /// <summary>
    /// MPEG Version
    /// </summary>
    public enum MpegVersion : byte
    {
        /// <summary>
        /// Version 2.5 (unofficial)
        /// </summary>
        V25 = 0x00,
        /// <summary>
        /// Reserved (invalid)
        /// </summary>
        Reserved = 0x08,
        /// <summary>
        /// Version 2
        /// </summary>
        V2 = 0x10,
        /// <summary>
        /// Version 1 (used in MP3s)
        /// </summary>
        V1 = 0x18
    }

    /// <summary>
    /// Layer
    /// </summary>
    public enum MpegLayer : byte
    {
        /// <summary>
        /// Reserved (invalid)
        /// </summary>
        Reserved = 0x00,
        /// <summary>
        /// Layer 3 (used in MP3s)
        /// </summary>
        Layer3 = 0x02,
        /// <summary>
        /// Layer 2
        /// </summary>
        Layer2 = 0x04,
        /// <summary>
        /// Layer 1
        /// </summary>
        Layer1 = 0x06
    }

    /// <summary>
    /// Channel mode (all valid)
    /// </summary>
    public enum MpegChannelMode : byte
    {
        /// <summary>
        /// Stereo
        /// </summary>
        Stereo = 0x00,
        /// <summary>
        /// Joint stereo
        /// </summary>
        JointStereo = 0x40,
        /// <summary>
        /// Dual channel
        /// </summary>
        DualChannel = 0x80,
        /// <summary>
        /// Single channel
        /// </summary>
        SingleChannel = 0xC0
    }

    /// <summary>
    /// Emphasis
    /// </summary>
    public enum MpegEmphasis : byte
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0x00,
        /// <summary>
        /// 50/15ms
        /// </summary>
        Fifty15ms = 0x01,
        /// <summary>
        /// Reserved (invalid)
        /// </summary>
        Reserved = 0x02,
        /// <summary>
        /// CCIT J.17 (whatever that is)
        /// </summary>
        CcitJ17 = 0x03
    }


    /// <summary>
    /// Interprets the 4-byte MPEG frame header. Immutable.
    /// </summary>
    /// <remarks>
    /// Only interprets enough for purposes of MP3 verification.
    /// Use IsValid to determine if the data represents
    /// a valid MPEG frame header (IsValid does not determine
    /// if it's a valid MP3 header).
    /// 
    /// Doesn't give access to mode extension bits because
    /// they aren't relevant to my needs
    /// 
    /// TODO: Reorder constant names BITRATE_SHIFT -> SHIFT_BITRATE
    /// </remarks>
    public class Mp3FrameHeader
    {
        /// <summary>
        /// Length in bytes of a frame header
        /// </summary>
        public static readonly int HEADER_SIZE = 4;

        private byte[] bytes;

        /// <summary>
        /// Create the frame header
        /// </summary>
        /// <param name="headerData">4-bytes of data</param>
        /// <exception cref="ArgumentNullException">Argument is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Length of data is not 4</exception>
        public Mp3FrameHeader(IList<byte> headerData)
        {
            if (null == headerData)
            {
                throw new ArgumentNullException("headerData", "Frame header must not be null");
            }
            if (headerData.Count != HEADER_SIZE)
            {
                throw new ArgumentOutOfRangeException("headerData", "Frame header must be 4 bytes long");
            }

            bytes = new byte[headerData.Count];
            headerData.CopyTo(bytes, 0);
        }


        #region Bytes 1&2
        
        private static readonly byte MASK_FRAME_SYNC1 = 0xFF ^ 0xFF;
        private static readonly byte MASK_FRAME_SYNC2 = 0xE0 ^ 0xFF;

        private static readonly int BIDX_FRAME_SYNC1 = 0;
        private static readonly int BIDX_FRAME_SYNC2 = 1;

        /// <summary>
        /// True if the frame sync bits are present
        /// </summary>
        /// <remarks>
        /// Frame invalid if false
        /// </remarks>
        public bool HasFrameSync
        {
            get
            {
                return !Convert.ToBoolean(
                    (0xFF ^ (MASK_FRAME_SYNC1 | bytes[BIDX_FRAME_SYNC1])
                    | 0xFF ^ (MASK_FRAME_SYNC2 | bytes[BIDX_FRAME_SYNC2])));
            }
        }

        private static readonly byte MASK_VERSION = 0x18;
        private static readonly int BIDX_VERSION = 1;

        /// <summary>
        /// The version of the standard used
        /// </summary>
        /// <remarks>
        /// Frame invalid if <see cref="MpegVersion.Reserved"/>
        /// </remarks>
        public MpegVersion Version
        {
            get
            {
                MpegVersion ret = (MpegVersion)(MASK_VERSION & bytes[BIDX_VERSION]);
                Debug.Assert(Enum.IsDefined(typeof(MpegVersion), ret));
                return ret;
            }
        }

        private static readonly byte MASK_LAYER = 0x06;
        private static readonly int BIDX_LAYER = 1;

        /// <summary>
        /// Layer
        /// </summary>
        /// <remarks>
        /// Frame invalid if <see cref="MpegLayer.Reserved"/>
        /// </remarks>
        public MpegLayer Layer
        {
            get
            {
                MpegLayer ret = (MpegLayer)(MASK_LAYER & bytes[BIDX_LAYER]);
                Debug.Assert(Enum.IsDefined(typeof(MpegLayer), ret));
                return ret;
            }
        }

        private static readonly byte MASK_CRC = 0x01;
        private static readonly int BIDX_CRC = 1;

        /// <summary>
        /// 16-bit CRC follows header
        /// </summary>
        public bool HasCRC
        {
            get
            {
                return !Convert.ToBoolean((MASK_CRC & bytes[BIDX_CRC]));
            }
        }

        #endregion


        #region Byte 3

        private static readonly byte SHIFT_BITRATE = 4;
        private static readonly int BIDX_BITRATE = 2;

        public int BitrateIndex
        {
            get
            {
                return bytes[BIDX_BITRATE] >> SHIFT_BITRATE;
            }
        }

        private static readonly byte BITRATE_FREE = 0x00;

        /// <summary>
        /// Indicates the bitrate is "free",
        /// whatever that means
        /// </summary>
        public bool HasFreeBitrate
        {
            get
            {
                return BITRATE_FREE == BitrateIndex;
            }
        }

        private static readonly byte BITRATE_INVALID = 0x0F;

        /// <summary>
        /// Indicates the bitrate is bogus
        /// </summary>
        /// <remarks>
        /// Frame is invalid if true
        /// </remarks>
        public bool HasInvalidBitrate
        {
            get
            {
                return BITRATE_INVALID == BitrateIndex;
            }
        }

        private static readonly byte SHIFT_SAMPLERATE = 2;
        private static readonly byte MASK_SAMPLERATE = 0x0C;
        private static readonly int BIDX_SAMPLERATE = 2;

        public int SamplerateIndex
        {
            get
            {
                return (MASK_SAMPLERATE & bytes[BIDX_SAMPLERATE]) >> SHIFT_SAMPLERATE;
            }
        }

        private static readonly byte SAMPLERATE_RESERVED = 0x03;

        /// <summary>
        /// Is the samplerate reserved?
        /// </summary>
        /// <remarks>
        /// Frame is invalid if true
        /// </remarks>
        public bool HasReservedSamplerate
        {
            get
            {
                return SAMPLERATE_RESERVED == SamplerateIndex;
            }
        }

        private static readonly byte MASK_PADDING = 0x02;
        private static readonly int BIDX_PADDING = 2;

        /// <summary>
        /// Is the frame padded with an extra slot?
        /// </summary>
        public bool HasPadding
        {
            get
            {
                return Convert.ToBoolean(MASK_PADDING & bytes[BIDX_PADDING]);
            }
        }

        private static readonly byte MASK_PRIVATE = 0x01;
        private static readonly int BIDX_PRIVATE = 2;

        /// <summary>
        /// Gibberish
        /// </summary>
        public bool HasPrivateFlag
        {
            get
            {
                return Convert.ToBoolean(MASK_PRIVATE & bytes[BIDX_PRIVATE]);
            }
        }

        #endregion


        #region Byte 4

        private static readonly byte MASK_CHANNELMODE = 0xC0;
        private static readonly byte BIDX_CHANNELMODE = 3;

        /// <summary>
        /// Channel mode
        /// </summary>
        public MpegChannelMode ChannelMode
        {
            get
            {
                MpegChannelMode ret = (MpegChannelMode)(MASK_CHANNELMODE & bytes[BIDX_CHANNELMODE]);
                Debug.Assert(Enum.IsDefined(typeof(MpegChannelMode), ret));
                return ret;
            }
        }

        private static readonly byte MASK_COPYRIGHT = 0x08;
        private static readonly byte BIDX_COPYRIGHT = 3;

        /// <summary>
        /// Whatever
        /// </summary>
        public bool HasCopyright
        {
            get
            {
                return Convert.ToBoolean(MASK_COPYRIGHT & bytes[BIDX_COPYRIGHT]);
            }
        }

        private static readonly byte MASK_ORIGINAL = 0x04;
        private static readonly byte BIDX_ORIGINAL = 3;

        /// <summary>
        /// Blah blah blah
        /// </summary>
        public bool HasOriginal
        {
            get
            {
                return Convert.ToBoolean(MASK_ORIGINAL & bytes[BIDX_ORIGINAL]);
            }
        }

        private static readonly byte MASK_EMPHASIS = 0x03;
        private static readonly byte BIDX_EMPHASIS = 3;

        /// <summary>
        /// Emphasis
        /// </summary>
        /// <remarks>
        /// Frame is invalid if <see cref="MpegEmphasis.Reserved"/>
        /// </remarks>
        public MpegEmphasis Emphasis
        {
            get
            {
                MpegEmphasis ret = (MpegEmphasis)(MASK_EMPHASIS & bytes[BIDX_EMPHASIS]);
                Debug.Assert(Enum.IsDefined(typeof(MpegEmphasis), ret));
                return ret;
            }
        }

        #endregion

    }
}
