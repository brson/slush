using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.DomainObjects.Mp3
{
    public class LameHeader : IMp3StreamRegion
    {
        private Mp3Frame frame;

        public LameHeader(Mp3Frame frame)
        {
            this.frame = frame;
        }

        public IList<byte> Bytes
        {
            get
            {
                return frame.Bytes;
            }
        }

        public bool IsValid
        {
            get
            {
                if (
                       Bytes.Count > 0x9F
                    && Bytes[0x9C] == 0x4C
                    && Bytes[0x9D] == 0x41
                    && Bytes[0x9E] == 0x4D
                    && Bytes[0x9F] == 0x45
                    )
                {
                    return true;
                }
                return false;
            }
        }

        public ushort MusicCrc
        {
            get
            {
                const int MUSIC_CRC_OFFSET = 0xBC;

                ushort musicCrc = 0x00;
                musicCrc |= (ushort)((uint)Bytes[MUSIC_CRC_OFFSET] << 8);
                musicCrc |= Bytes[MUSIC_CRC_OFFSET + 1];
                return musicCrc;
            }
        }

        public ushort InfoCrc
        {
            get
            {
                const int INFO_CRC_OFFSET = 0xBE;

                ushort infoCrc = 0x00;
                infoCrc |= (ushort)((uint)Bytes[INFO_CRC_OFFSET] << 8);
                infoCrc |= Bytes[INFO_CRC_OFFSET + 1];

                return infoCrc;
            }
        }

    }
}
