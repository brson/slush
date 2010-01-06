using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Slush.DomainObjects.Mp3;

namespace Slush.DomainObjects.Mp3
{
    /// <summary>
    /// Reads frames from an mpeg stream
    /// </summary>
    /// <remarks>
    /// This class uses a state machine to implement an iterator
    /// over the regions of an mpeg stream. Every byte in the stream
    /// is part of either a JunkRegion or an Mp3Frame.
    /// 
    /// The primary code path is:
    /// - Read mpeg header (StateReadHeader)
    /// - Read CRC + payload (StateReadCRC, StateReadPayload)
    /// - Return frame (StateReturnFrame)
    /// 
    /// TODO: backtracking, proven streams, junk, buffer maintenance
    /// </remarks>
    public class Mp3StreamReader : IEnumerable<IMp3StreamRegion>
    {
        #region Constructor

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="stream">The stream to read</param>
        public Mp3StreamReader(Stream stream)
        {
            if (null == stream)
            {
                throw new ArgumentNullException();
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("stream", "Stream must be readable");
            }

            this.stream = stream;
        }

        #endregion


        #region Private Types

        private class StateArgs
        {
            public IMp3StreamRegion RetVal;

            public StateArgs()
            {
                Reset();
            }

            public void Reset()
            {
                RetVal = null;
            }
        }

        private delegate StateDelegate StateDelegate(StateArgs e);

        #endregion


        #region Private Constants

        // Maximum frame length
        private static readonly int MAX_FRAME_LENGTH = 2886;
        // Don't know for sure how long the buffer needs to be
        private static readonly int BUFFER_LENGTH = JunkRegion.MAX_JUNK_REGION_LENGTH * 3;

        #endregion


        #region Private Members

        private Stream stream;
        private byte[] buffer = new byte[BUFFER_LENGTH];
        private int bufferOffset = 0;
        private Mp3FrameHeader currentHeader = null;
        private bool haveStoredFrame = false;
        private bool isStreamProven = false;
        private int returnFrameSize = 0;
        private int junkCount = 0;
        private int secondaryJunkCount = 0;
        private int backtrackOffset = 0;
        private int streamByteCount = 0;

        #endregion


        #region Helper Functions
        
        private int ReadIntoBuffer(int count)
        {
            Debug.Assert(count <= buffer.Length - bufferOffset);
            int retCount = stream.Read(buffer, bufferOffset, count);
            bufferOffset += retCount;
            streamByteCount += retCount;
            return retCount;
        }

        private int DequeueFromBuffer(int count)
        {
            for (int i = 0; i < bufferOffset - count; i++)
            {
                buffer[i] = buffer[count + i];
            }

            bufferOffset -= count;

            return bufferOffset;
        }

        #endregion


        #region Region Enumerator

        private bool startedReading = false;

        private IEnumerator<IMp3StreamRegion> GetRegionEnumerator()
        {
            if (startedReading)
            {
                throw new InvalidOperationException("Cannot enumerate more than once");
            }
            startedReading = true;

            StateDelegate state = StateStarting;
            StateArgs args = new StateArgs();
            do
            {
                state = state(args);
                if (args.RetVal != null)
                {
                    yield return args.RetVal;
                }
                args.Reset();
            } while (state != null);

            if (bufferOffset != 0)
            {
                throw new UnexpectedException("Stream reader state machine left data in the buffers");
            }
            if (-1 != stream.ReadByte())
            {
                throw new UnexpectedException("Stream reader state machine left data in the stream");
            }

            yield break;
        }
        
        #endregion


        #region State Delegates

        private StateDelegate StateStarting(StateArgs e)
        {
            // Max junk region length needs to be greater than max frame
            // length because we could be looking up to max frame length bytes
            // before we realize it's junk
            Debug.Assert(JunkRegion.MAX_JUNK_REGION_LENGTH >= MAX_FRAME_LENGTH);
            return StateReadHeader;
        }

        private StateDelegate StateReadHeader(StateArgs e)
        {
            int count = ReadIntoBuffer(Mp3FrameHeader.HEADER_SIZE);

            if (Mp3FrameHeader.HEADER_SIZE == count)
            {
                return StateInterpretHeader;
            }
            else
            {
                if (haveStoredFrame)
                {
                    if (isStreamProven)
                    {
                        return StateReturnQueuedRegions;
                    }
                    else
                    {
                        Debug.Assert(returnFrameSize != 0);
                        junkCount += returnFrameSize + count;
                        returnFrameSize = 0;
                        haveStoredFrame = false;
                        return StateReturnQueuedRegions;
                    }
                }
                else
                {
                    junkCount += count;
                    if (junkCount > 0)
                    {
                        return StateReturnQueuedRegions;
                    }
                    else
                    {
                        return StateFinished;
                    }
                }
            }
        }

        private StateDelegate StateInterpretHeader(StateArgs e)
        {
            byte[] headerBuffer = new byte[Mp3FrameHeader.HEADER_SIZE];
            Buffer.BlockCopy(
                buffer,
                bufferOffset - headerBuffer.Length,
                headerBuffer,
                0,
                headerBuffer.Length);
            currentHeader = new Mp3FrameHeader(headerBuffer);

            if (Mp3FrameHeaderRules.IsValid(currentHeader))
            {
                if (haveStoredFrame)
                {
                    Debug.Assert(returnFrameSize != 0);
                    return StateReturnQueuedRegions;
                }
                else
                {
                    return StateReadCrc;
                }
            }
            else
            {
                if (streamByteCount == Mp3FrameHeader.HEADER_SIZE)
                {
                    // TODO: Really need to document and test this
                    // support skipping ID3 tags (which may have
                    // bogus frame syncs)
                    if (buffer[0] == 0x49
                     && buffer[1] == 0x44
                     && buffer[2] == 0x33
                        )
                    {
                        const int ID3_HEADER_SIZE = 10;
                        int hdrDiff = ID3_HEADER_SIZE - Mp3FrameHeader.HEADER_SIZE;
                        int count = ReadIntoBuffer(hdrDiff);
                        if (count < hdrDiff)
                        {
                            junkCount = bufferOffset;
                            return StateReturnJunk;
                        }
                        else
                        {
                            // calculate id3 tag size
                            uint size = 0;
                            size |= (uint)(buffer[6] << 21);
                            size |= (uint)(buffer[7] << 14);
                            size |= (uint)(buffer[8] << 7);
                            size |= buffer[9];
                            count = ReadIntoBuffer(
                                (int)size
                                + Mp3FrameHeader.HEADER_SIZE);
                            junkCount = bufferOffset - Mp3FrameHeader.HEADER_SIZE;
                            return StateInterpretHeader;
                        }
                    }
                }

                if (haveStoredFrame)
                {
                    return StateFindNextHeaderBacktracking;
                }
                else
                {
                    return StateFindNextHeader;
                }
            }
        }

        // Find next header when buffer is junk
        private StateDelegate StateFindNextHeader(StateArgs e)
        {
            // Don't exceed the maximum amount of junk
            if (junkCount == JunkRegion.MAX_JUNK_REGION_LENGTH)
            {
                return StateReturnQueuedRegions;
            }

            int count = ReadIntoBuffer(1);
            if (bufferOffset > Mp3FrameHeader.HEADER_SIZE)
            {
                if (returnFrameSize == 0)
                {
                    junkCount += count;
                }
                else
                {
                    // The junk is after a frame
                    secondaryJunkCount += count;
                }
            }

            if (count == 1)
            {
                return StateInterpretHeader;
            }
            else
            {
                // End of stream. Fuck it
                junkCount = bufferOffset;
                return StateReturnJunk;
            }
        }


        private StateDelegate StateReadCrc(StateArgs e)
        {
            if (currentHeader.HasCRC)
            {
                int count = ReadIntoBuffer(Mp3Frame.CRC_SIZE);
                if (Mp3Frame.CRC_SIZE == count)
                {
                    return StateReadPayload;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return StateReadPayload;
            }
        }

        private StateDelegate StateReadPayload(StateArgs e)
        {
            Debug.Assert(currentHeader != null);
            Debug.Assert(haveStoredFrame == false);
            Debug.Assert(
                   (bufferOffset - junkCount == Mp3FrameHeader.HEADER_SIZE)
                || (bufferOffset - junkCount == Mp3FrameHeader.HEADER_SIZE + Mp3Frame.CRC_SIZE));

            if (!currentHeader.HasFreeBitrate)
            {
                Debug.Assert(Mp3FrameHeaderRules.CanCalculateFrameLength(currentHeader));
                int count = ReadIntoBuffer(Mp3FrameHeaderRules.CalculatePayloadLength(currentHeader));

                if (count == Mp3FrameHeaderRules.CalculatePayloadLength(currentHeader))
                {
                    Debug.Assert(returnFrameSize == 0);
                    returnFrameSize = Mp3FrameHeaderRules.CalculateFrameLength(currentHeader);
                    haveStoredFrame = true;
                    return StateReadHeader;
                }
                else
                {
                    // end of stream
                    Debug.Assert(returnFrameSize == 0);
                    if (isStreamProven)
                    {
                        returnFrameSize =
                            Mp3FrameHeaderRules.CalculateFrameLength(currentHeader)
                            - Mp3FrameHeaderRules.CalculatePayloadLength(currentHeader)
                            + count;
                        haveStoredFrame = true;
                        return StateReturnQueuedRegions;
                    }
                    else
                    {
                        junkCount = bufferOffset;
                        return StateReturnQueuedRegions;
                    }
                }
            }
            else
            {
                return StateReadFreeFrame;
            }
        }

        private StateDelegate StateReturnQueuedRegions(StateArgs e)
        {
            if (junkCount > 0)
            {
                return StateReturnJunk;
            }
            else
            {
                if (haveStoredFrame)
                {
                    return StateReturnFrame;
                }
                else
                {
                    return StateFinished;
                }
            }
        }

        private StateDelegate StateReturnJunk(StateArgs e)
        {
            Debug.Assert(junkCount > 0);

            byte[] junk = new byte[junkCount];
            Buffer.BlockCopy(buffer, 0, junk, 0, junkCount);
            e.RetVal = new JunkRegion(junk);

            DequeueFromBuffer(junkCount);

            backtrackOffset -= junkCount;

            isStreamProven = false;
            junkCount = 0;

            if (haveStoredFrame)
            {
                return StateReturnFrame;
            }
            else
            {
                return null;
            }
        }

        private StateDelegate StateReturnFrame(StateArgs e)
        {
#if DEBUG // TODO: Get rid of this
            if (returnFrameSize > bufferOffset
                || returnFrameSize < Mp3FrameHeader.HEADER_SIZE)
            {
                Debug.WriteLine(string.Format(
                    "returnFrameSize = {0}; bufferOffset = {1}",
                    returnFrameSize, bufferOffset));
            }
#endif
            Debug.Assert(returnFrameSize >= Mp3FrameHeader.HEADER_SIZE);
            // TODO: Add this back
            //Debug.Assert(returnFrameSize <= bufferOffset);

            byte[] frameBuffer = new byte[returnFrameSize];
            Buffer.BlockCopy(buffer, 0, frameBuffer, 0, returnFrameSize);
            e.RetVal = new Mp3Frame(frameBuffer);

            // move remaining contents over
            int remainingLength =
                DequeueFromBuffer(returnFrameSize);

            // if there is a junk region after this frame
            // then it is the next junk region out the pipeline
            junkCount = secondaryJunkCount;
            secondaryJunkCount = 0;

            backtrackOffset -= returnFrameSize;
            returnFrameSize = 0;

            isStreamProven = true;
            haveStoredFrame = false;

            if (remainingLength == 0)
            {
                return StateReadHeader;
            }
            else if (remainingLength >= Mp3FrameHeader.HEADER_SIZE)
            {
                // Frame has been found, go to crc
                return StateReadCrc;
            }
            else
            {
                return StateFindNextHeader;
            }
        }

        private StateDelegate StateReadFreeFrame(StateArgs e)
        {
            return null;
        }

        private StateDelegate StateFinished(StateArgs e)
        {
            return null;
        }

        #endregion


        #region State Delegates - Backtracking Path
        
        private StateDelegate StateInterpretHeaderBacktracking(StateArgs e)
        {
            byte[] headerBuffer = new byte[Mp3FrameHeader.HEADER_SIZE];
            Buffer.BlockCopy(
                buffer,
                backtrackOffset - Mp3FrameHeader.HEADER_SIZE,
                headerBuffer,
                0,
                headerBuffer.Length);
            currentHeader = new Mp3FrameHeader(headerBuffer);

            Debug.Assert(Mp3FrameHeaderRules.IsValid(currentHeader));

            return StateReadCrcBacktracking;
        }

        // Find next header when buffer has a frame
        private StateDelegate StateFindNextHeaderBacktracking(StateArgs e)
        {
            Debug.Assert(null != currentHeader);
            Debug.Assert(returnFrameSize > 0);
            Debug.Assert(haveStoredFrame);

            {
                byte[] oldHeaderBuffer = new byte[Mp3FrameHeader.HEADER_SIZE];
                Buffer.BlockCopy(
                    buffer, junkCount, oldHeaderBuffer, 0, Mp3FrameHeader.HEADER_SIZE);
                Mp3FrameHeader oldHeader = new Mp3FrameHeader(oldHeaderBuffer);

                backtrackOffset = junkCount
                    + Mp3FrameHeader.HEADER_SIZE
                    + (oldHeader.HasCRC ? Mp3Frame.CRC_SIZE : 0);

                Debug.Assert(backtrackOffset <= bufferOffset);
            }

            // search through buffer, starting at the byte following
            // the frame header and CRC, for a valid header.
            while (backtrackOffset <= bufferOffset - Mp3FrameHeader.HEADER_SIZE)
            {
                byte[] maybeHeaderData = new byte[Mp3FrameHeader.HEADER_SIZE];
                Buffer.BlockCopy(
                    buffer, backtrackOffset, maybeHeaderData, 0, maybeHeaderData.Length);
                Mp3FrameHeader maybeHeader = new Mp3FrameHeader(maybeHeaderData);

                if (Mp3FrameHeaderRules.IsValid(maybeHeader))
                {
                    // TODO: Is this necessary, considering we need to subtract in both branches
                    backtrackOffset += Mp3FrameHeader.HEADER_SIZE;
                    // found a header
                    if (isStreamProven)
                    {
                        // recalculate length of stored, truncated frame
                        returnFrameSize = backtrackOffset - Mp3FrameHeader.HEADER_SIZE - junkCount;
                        // this means the current frame is truncated
                        return StateReturnQueuedRegionsBacktracking;
                    }
                    else
                    {
                        // probably was a coincedental header sync
                        returnFrameSize = 0;
                        haveStoredFrame = false;
                        junkCount = backtrackOffset - Mp3FrameHeader.HEADER_SIZE;
                        return StateInterpretHeaderBacktracking;
                    }
                }

                backtrackOffset++;
            }
            backtrackOffset--;

            if (isStreamProven)
            {
                isStreamProven = false;
                return StateReturnQueuedRegionsBacktracking;
            }
            else
            {
                junkCount += returnFrameSize;
                returnFrameSize = 0;
                haveStoredFrame = false;
                backtrackOffset = 0;
                // rejoin main codepath
                return StateFindNextHeader;
            }
        }

        private StateDelegate StateReadCrcBacktracking(StateArgs e)
        {
            if (currentHeader.HasCRC)
            {
                int difference = bufferOffset - backtrackOffset;
                if (difference >= Mp3Frame.CRC_SIZE)
                {
                    backtrackOffset += Mp3Frame.CRC_SIZE;
                    return StateReadPayloadBacktracking;
                }
                else
                {
                    backtrackOffset = 0;
                    // Read bytes from stream
                    int count = ReadIntoBuffer(difference);
                    if (count == difference)
                    {
                        // rejoin main codepath
                        return StateReadPayload;
                    }
                    else
                    {
                        // end of stream
                        junkCount = bufferOffset;
                        // rejoin main codepath
                        return StateReturnQueuedRegions;
                    }
                }
            }
            else
            {
                return StateReadPayloadBacktracking;
            }
        }

        private StateDelegate StateReadPayloadBacktracking(StateArgs e)
        {
            Debug.Assert(currentHeader != null);
            Debug.Assert(haveStoredFrame == false);
            Debug.Assert(
                   (backtrackOffset - junkCount == Mp3FrameHeader.HEADER_SIZE)
                || (backtrackOffset - junkCount == Mp3FrameHeader.HEADER_SIZE + Mp3Frame.CRC_SIZE));

            if (!currentHeader.HasFreeBitrate)
            {
                Debug.Assert(Mp3FrameHeaderRules.CanCalculateFrameLength(currentHeader));
                int payloadLength = Mp3FrameHeaderRules.CalculatePayloadLength(currentHeader);
                int difference = bufferOffset - backtrackOffset;
                if (payloadLength == difference)
                {
                    backtrackOffset = 0;
                    Debug.Assert(returnFrameSize == 0);
                    returnFrameSize = Mp3FrameHeaderRules.CalculateFrameLength(currentHeader);
                    haveStoredFrame = true;
                    // rejoin main codepath
                    return StateReadHeader;
                }
                else if (payloadLength < difference)
                {
                    backtrackOffset += payloadLength;
                    Debug.Assert(returnFrameSize == 0);
                    returnFrameSize = Mp3FrameHeaderRules.CalculateFrameLength(currentHeader);
                    haveStoredFrame = true;
                    return null;
                }
                else
                {
                    backtrackOffset = 0;
                    int count = ReadIntoBuffer(payloadLength - difference);
                    if (count == payloadLength - difference)
                    {
                        // rejoin main codepath
                        Debug.Assert(returnFrameSize == 0);
                        returnFrameSize = Mp3FrameHeaderRules.CalculateFrameLength(currentHeader);
                        haveStoredFrame = true;
                        return StateReadHeader;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        private StateDelegate StateReturnQueuedRegionsBacktracking(StateArgs e)
        {
            Debug.Assert(haveStoredFrame);
            if (junkCount > 0)
            {
                return StateReturnJunkBacktracking;
            }
            else
            {
                return StateReturnFrameBacktracking;
            }
        }

        private StateDelegate StateReturnJunkBacktracking(StateArgs e)
        {
            Debug.Assert(junkCount > 0);
            Debug.Assert(haveStoredFrame);

            byte[] junk = new byte[junkCount];
            Buffer.BlockCopy(buffer, 0, junk, 0, junkCount);
            e.RetVal = new JunkRegion(junk);

            DequeueFromBuffer(junkCount);

            // adjust backtrack offset
            backtrackOffset -= junkCount;

            junkCount = 0;

            return StateReturnFrameBacktracking;
        }

        private StateDelegate StateReturnFrameBacktracking(StateArgs e)
        {
            Debug.Assert(returnFrameSize >= Mp3FrameHeader.HEADER_SIZE);
            Debug.Assert(returnFrameSize <= bufferOffset);

            byte[] frameBuffer = new byte[returnFrameSize];
            Buffer.BlockCopy(buffer, 0, frameBuffer, 0, returnFrameSize);
            e.RetVal = new Mp3Frame(frameBuffer);

            int remainingLength =
                DequeueFromBuffer(returnFrameSize);
            backtrackOffset -= returnFrameSize;

            junkCount = 0;
            returnFrameSize = 0;

            //isStreamProven = false;
            haveStoredFrame = false;

            if (remainingLength == 0)
            {
                // rejoin the main codepath
                return StateReadHeader;
            }
            else if (remainingLength == Mp3FrameHeader.HEADER_SIZE)
            {
                return StateFindNextHeader;
            }
            else if (remainingLength > Mp3FrameHeader.HEADER_SIZE)
            {
                return StateInterpretHeaderBacktracking;
            }
            else
            {
                // rejoin the main codepath
                return StateFindNextHeader;
            }
        }

        #endregion


        #region IEnumerable

        /// <summary>
        /// Get an enumerator over the regions of the stream
        /// </summary>
        /// <returns>An enumerator that allows the caller to traverse the stream</returns>
        public IEnumerator<IMp3StreamRegion> GetEnumerator()
        {
            return GetRegionEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IMp3StreamRegion>)this).GetEnumerator();
        }

        #endregion
    }
}
