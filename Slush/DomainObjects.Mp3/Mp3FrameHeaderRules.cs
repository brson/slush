using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Slush.DomainObjects.Mp3
{
    public static class Mp3FrameHeaderRules
    {

        private static readonly int[,] BITRATE_TABLE = new int[,]
        {
            {0xDEAD, 32000, 64000, 96000, 128000, 160000, 192000, 224000, 256000, 288000, 320000, 352000, 384000, 416000, 448000, 0xBEEF},
            {0xDEAD, 32000, 48000, 56000,  64000,  80000,  96000, 112000, 128000, 160000, 192000, 224000, 256000, 320000, 384000, 0xBEEF},
            {0xDEAD, 32000, 40000, 48000,  56000,  64000,  80000,  96000, 112000, 128000, 160000, 192000, 224000, 256000, 320000, 0xBEEF},
            {0xDEAD, 32000, 48000, 56000,  64000,  80000,  96000, 112000, 128000, 144000, 160000, 176000, 192000, 224000, 256000, 0xBEEF},
            {0xDEAD,  8000, 16000, 24000,  32000,  40000,  48000,  56000,  64000,  80000,  96000, 112000, 128000, 144000, 160000, 0xBEEF}
        };

        private static readonly int BRIDX_V1L1 = 0;
        private static readonly int BRIDX_V1L2 = 1;
        private static readonly int BRIDX_V1L3 = 2;
        private static readonly int BRIDX_V2L1 = 3;
        private static readonly int BRIDX_V2L2 = 4;
        private static readonly int BRIDX_V2L3 = 4;

        private static int GetBitrateTableIndex(Mp3FrameHeader fh)
        {
            // This is gorgeous
            // Could also be done using more tables
            switch (fh.Version)
            {
                case MpegVersion.V1:
                    switch (fh.Layer)
                    {
                        case MpegLayer.Layer1:
                            return BRIDX_V1L1;
                        case MpegLayer.Layer2:
                            return BRIDX_V1L2;
                        case MpegLayer.Layer3:
                            return BRIDX_V1L3;
                        case MpegLayer.Reserved:
                            break;
                        default:
                            Debug.Fail(UnexpectedException.Message);
                            throw new UnexpectedException();
                    }
                    break;
                case MpegVersion.V2:
                case MpegVersion.V25:
                    switch (fh.Layer)
                    {
                        case MpegLayer.Layer1:
                            return BRIDX_V2L1;
                        case MpegLayer.Layer2:
                            return BRIDX_V2L2;
                        case MpegLayer.Layer3:
                            return BRIDX_V2L3;
                        case MpegLayer.Reserved:
                            break;
                        default:
                            Debug.Fail(UnexpectedException.Message);
                            throw new UnexpectedException();
                    }
                    break;
                case MpegVersion.Reserved:
                    throw new InvalidOperationException("This frame does not specify a version");
                default:
                    Debug.Fail(UnexpectedException.Message);
                    throw new UnexpectedException();
            }
            throw new InvalidOperationException("This frame does not specify a layer");
        }

        /// <summary>
        /// Bitrate
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Bitrate is free or invalid, or the version or layer is reserved
        /// </exception>
        public static int CalculateBitrate(Mp3FrameHeader fh)
        {
            if (null == fh)
            {
                throw new ArgumentNullException("fh");
            }
            if (fh.HasFreeBitrate)
            {
                throw new InvalidOperationException("This frame has a free bitrate");
            }
            if (fh.HasInvalidBitrate)
            {
                throw new InvalidOperationException("This frame has an invalid bitrate");
            }

            return BITRATE_TABLE[GetBitrateTableIndex(fh), fh.BitrateIndex];
        }

        /// <summary>
        /// If a bitrate can be calculated from the frame header
        /// (i.e. if CalculateBitrate won't throw)
        /// </summary>
        public static bool CanCalculateBitrate(Mp3FrameHeader fh)
        {
            if (null == fh)
            {
                throw new ArgumentNullException("fh");
            }
            return !(
                   fh.HasFreeBitrate
                || fh.HasInvalidBitrate
                || fh.Version == MpegVersion.Reserved
                || fh.Layer == MpegLayer.Reserved);
        }

        private static readonly int[,] SAMPLERATE_TABLE = new int[,]
        {
            {44100, 48000, 32000},
            {22050, 24000, 16000},
            {11025, 12000,  8000}
        };

        private static readonly int SRIDX_V1 = 0;
        private static readonly int SRIDX_V2 = 1;
        private static readonly int SRIDX_V25 = 2;

        /// <summary>
        /// Get the samplerate
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// HasReservedSamplerate is true or mpeg version is reserved
        /// </exception>
        public static int CalculateSamplerate(Mp3FrameHeader fh)
        {
            if (null == fh)
            {
                throw new ArgumentNullException("fh");
            }
            if (fh.HasReservedSamplerate)
            {
                throw new InvalidOperationException("This frame has a reserved samplerate");
            }
            if (fh.Version == MpegVersion.Reserved)
            {
                throw new InvalidOperationException("This frame has a reserved version");
            }
            switch (fh.Version)
            {
                case MpegVersion.V1:
                    return SAMPLERATE_TABLE[SRIDX_V1, fh.SamplerateIndex];
                case MpegVersion.V2:
                    return SAMPLERATE_TABLE[SRIDX_V2, fh.SamplerateIndex];
                case MpegVersion.V25:
                    return SAMPLERATE_TABLE[SRIDX_V25, fh.SamplerateIndex];
                default:
                    Debug.Fail(UnexpectedException.Message);
                    throw new UnexpectedException();
            }
        }

        /// <summary>
        /// The samplerate for this frame is known, if the
        /// sample rate is not reserved and the mpeg version is not reserved
        /// </summary>
        public static bool CanCalculateSamplerate(Mp3FrameHeader fh)
        {
            if (null == fh)
            {
                throw new ArgumentNullException("fh");
            }
            return !(fh.HasReservedSamplerate || fh.Version == MpegVersion.Reserved);
        }

        /// <summary>
        /// The frame length can be calculated
        /// </summary>
        /// <remarks> The frame length depends on the bitrate,
        /// samplerate, and layer.
        /// 
        /// Header is invalid if false</remarks>
        /// <returns></returns>
        public static bool CanCalculateFrameLength(Mp3FrameHeader fh)
        {
            // Layer is also needed in the calculation, but
            // that's taken into account by CanCalculateBitrate
            return CanCalculateBitrate(fh) && CanCalculateSamplerate(fh);
        }

        // Not sure what these numbers represent
        private static readonly int L1_SCALE = 12;
        private static readonly int L23_SCALE = 144;
        private static readonly int L1_SLOTSIZE = 4;
        private static readonly int L23_SLOTSIZE = 1;

        /// <summary>
        /// Returns the calculated frame length (including header
        /// and CRC)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// CanCalculateFrameLength is false
        /// </exception>
        public static int CalculateFrameLength(Mp3FrameHeader fh)
        {
            if (null == fh)
            {
                throw new ArgumentNullException("fh");
            }
            if (fh.Version == MpegVersion.Reserved)
            {
                throw new InvalidOperationException("This frame has a reserved version");
            }

            int scaleFactor;
            int slotSize;

            switch (fh.Layer)
            {
                case MpegLayer.Layer1:
                    scaleFactor = L1_SCALE;
                    slotSize = L1_SLOTSIZE;
                    break;
                case MpegLayer.Layer2:
                case MpegLayer.Layer3:
                    scaleFactor = L23_SCALE;
                    slotSize = L23_SLOTSIZE;
                    break;
                case MpegLayer.Reserved:
                    throw new InvalidOperationException("This frame has a reserved layer");
                default:
                    Debug.Fail(UnexpectedException.Message);
                    throw new UnexpectedException();
            }

            try
            {
                int paddingSlots = fh.HasPadding ? 1 : 0;
                int crcSize = fh.HasCRC ? Mp3Frame.CRC_SIZE : 0;
                int numSlots = scaleFactor * CalculateBitrate(fh) / CalculateSamplerate(fh) + paddingSlots;
                int dataSize = numSlots * slotSize;
                return dataSize + crcSize;
            }
            catch (InvalidOperationException e)
            {
                // I know this is brittle. Fortunately I've got unit test power
                if (e.Message.Contains("bitrate"))
                {
                    throw new InvalidOperationException("Cannot calculate bitrate");
                }
                else
                {
                    throw new InvalidOperationException("Cannot calculate samplerate");
                }
            }
            catch
            {
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
        }

        public static int CalculatePayloadLength(Mp3FrameHeader fh)
        {
            return CalculateFrameLength(fh)
                - Mp3FrameHeader.HEADER_SIZE
                - (fh.HasCRC ? Mp3Frame.CRC_SIZE : 0);
        }

        /// <summary>
        /// Determines if the header claims a valid
        /// combination of bitrate and channel mode
        /// </summary>
        /// <remarks>
        /// Frame is invalid if true
        /// </remarks>
        public static  bool HasInvalidBitrateForChannelMode(Mp3FrameHeader fh)
        {
            if (null == fh)
            {
                throw new ArgumentNullException("fh");
            }
            if (fh.Layer != MpegLayer.Layer2)
            {
                return false;
            }
            if (fh.HasFreeBitrate || fh.HasInvalidBitrate)
            {
                return false;
            }

            switch (CalculateBitrate(fh))
            {
                case 32000:
                case 48000:
                case 56000:
                case 80000:
                    if (fh.ChannelMode == MpegChannelMode.SingleChannel)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                case 224000:
                case 256000:
                case 320000:
                case 384000:
                    if (fh.ChannelMode != MpegChannelMode.SingleChannel)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// True if this represents a valid MPEG frame
        /// </summary>
        /// <remarks>
        /// This does not indicate a valid MP3 frame
        /// </remarks>
        public static bool IsValid(Mp3FrameHeader fh)
        {
            if (null == fh)
            {
                throw new ArgumentNullException("fh");
            }
            bool isInvalid =
                   !fh.HasFrameSync
                || fh.Version == MpegVersion.Reserved
                || fh.Layer == MpegLayer.Reserved
                || fh.HasInvalidBitrate
                || fh.HasReservedSamplerate
                || fh.Emphasis == MpegEmphasis.Reserved
                || HasInvalidBitrateForChannelMode(fh);

            return !isInvalid;
        }
    }
}
