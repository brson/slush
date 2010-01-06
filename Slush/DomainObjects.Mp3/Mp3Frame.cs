using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;

namespace Slush.DomainObjects.Mp3
{
    /// <summary>
    /// A frame of an mpeg stream
    /// </summary>
    public class Mp3Frame : IMp3StreamRegion
    {
        /// <summary>
        /// Length in bytes of a frame CRC
        /// </summary>
        public static readonly int CRC_SIZE = 2;

        private ReadOnlyCollection<byte> publicBytes;
        private Mp3FrameHeader header;

        //TODO: Create an internal interface with a constructor
        //that doesn't copy the input array
        /// <summary>
        /// Creates an instance from the bytes of a frame
        /// </summary>
        /// <remarks>
        /// This class can hold truncated frames,
        /// but it will not hold a frame with length
        /// greater than calculated from the header info.
        /// </remarks>
        /// <param name="bytes">The frame data</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Argument is less than 4 bytes long or greater
        /// than the calculated frame size
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The provided bytes don't begin with a
        /// valid mpeg header
        /// </exception>
        public Mp3Frame(IList<byte> bytes)
        {
            if (null == bytes)
            {
                throw new ArgumentNullException("Frame data may not be null");
            }

            // Create header
            if (bytes.Count < Mp3FrameHeader.HEADER_SIZE)
            {
                throw new ArgumentException("Data too short for frame. Must be at least 4 bytes");
            }

            header = new Mp3FrameHeader(Util.SliceList(bytes, 0, 4));

            if (!Mp3FrameHeaderRules.IsValid(header))
            {
                throw new ArgumentException("Invalid frame header");
            }

            if (header.HasCRC
                && bytes.Count < Mp3FrameHeader.HEADER_SIZE + CRC_SIZE)
            {
                throw new ArgumentException("Data too short for frame. Must be at least 6 bytes with CRC");
            }

            if (Mp3FrameHeaderRules.CanCalculateFrameLength(header)
                && bytes.Count > Mp3FrameHeaderRules.CalculateFrameLength(header))
            {
                throw new ArgumentException("Data too long for frame");
            }

            publicBytes = new ReadOnlyCollection<byte>(Util.CloneList(bytes));
        }

        /// <summary>
        /// The raw datas
        /// </summary>
        public IList<byte> Bytes
        {
            get
            {
                return publicBytes;
            }
        }

        public Mp3FrameHeader Header
        {
            get
            {
                return header;
            }
        }

        public int CalculatedFrameLength
        {
            get
            {
                return Mp3FrameHeaderRules.CalculateFrameLength(header);
            }
        }

        public int ActualFrameLength
        {
            get
            {
                return Bytes.Count;
            }
        }

        public bool IsTruncated
        {
            get
            {
                Debug.Assert(ActualFrameLength <= CalculatedFrameLength);
                return ActualFrameLength < CalculatedFrameLength;
            }
        }

        public bool HasCRC
        {
            get
            {
                Debug.Assert(Bytes.Count >= Mp3FrameHeader.HEADER_SIZE + CRC_SIZE);
                return header.HasCRC;
            }
        }

        public IList<byte> CRC
        {
            get
            {
                if (HasCRC)
                {
                    // TODO: cache this, but remember to return a readonly list
                    Debug.Assert(Mp3FrameHeader.HEADER_SIZE + CRC_SIZE <= Bytes.Count, "Constructor should reject frames with missing CRCs");
                    return Util.SliceList(Bytes, Mp3FrameHeader.HEADER_SIZE, CRC_SIZE);
                }
                else
                {
                    throw new InvalidOperationException("This frame does not have a CRC");
                }
            }
        }

    }
}
