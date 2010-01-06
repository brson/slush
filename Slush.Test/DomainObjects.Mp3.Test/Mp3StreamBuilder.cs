using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Slush.DomainObjects.Mp3.Test
{
    internal class Mp3StreamBuilder
    {
        private IList<IMp3StreamRegion> regions = new List<IMp3StreamRegion>();

        private Random random = null;

        public void Add(IMp3StreamRegion r)
        {
            regions.Add(r);
        }

        public byte[] BuildFrame(
            MpegVersion version,
            MpegLayer layer,
            bool hasCRC,
            int frameSizeOffset,
            int frameSize)
        {
            byte[] buf = Slush.DomainObjects.Mp3.Test.Mp3FrameTest.BuildFrame(
                version,
                layer,
                hasCRC,
                frameSizeOffset,
                frameSize);

            if (UseRandom)
            {
                for (int i = 6; i < buf.Length; i++)
                {
                    buf[i] = (byte)random.Next();
                }
            }
            return buf;
        }

        public void AddFrame()
        {
            AddFrame(MpegVersion.V1, MpegLayer.Layer3, true, 0, 0);
        }

        public void AddFrame(
            MpegVersion version,
            MpegLayer layer,
            bool hasCRC,
            int frameSizeOffset,
            int frameSize)
        {
            Add(new Mp3Frame(BuildFrame(
                version,
                layer,
                hasCRC,
                frameSizeOffset,
                frameSize)));
        }

        public void AddFreeFrame(
            MpegVersion version,
            MpegLayer layer,
            bool hasCRC,
            int frameSizeOffset,
            int frameSize)
        {
            byte[] buf = BuildFrame(
                version,
                layer,
                hasCRC,
                frameSizeOffset,
                frameSize);

            buf[2] &= 0x0F;

            Add(new Mp3Frame(buf));
        }

        public void AddJunk(int length)
        {
            byte[] buf = new byte[length];

            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(
                    UseRandom ?
                    random.Next()
                    : 0);
            }

            Add(new JunkRegion(buf));
        }

        public Stream BuildStream()
        {
            MemoryStream stream = new MemoryStream();

            foreach (IMp3StreamRegion r in regions)
            {
                byte[] buf = new byte[r.Bytes.Count];
                r.Bytes.CopyTo(buf, 0);
                stream.Write(buf, 0, buf.Length);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public bool UseRandom
        {
            get
            {
                return random != null;
            }
            set
            {
                random = value ?
                    new Random(0xABCD)
                    : null;
            }
        }
    }
}
