using System;
using System.Collections.Generic;
using System.Text;
using Slush.DomainObjects.Mp3;
using Slush.Services;
using Slush.Services.Mp3;

namespace Slush.Validators.Mp3
{
    public class LameMusicCrcValidator : IValidator
    {
        private int mp3FrameCount = 0;
        private JunkRegion previousJunkRegion = null;
        private ushort musicCrc = 0;
        private Crc16 calcCrc = new Crc16();
        private bool shouldCalculateCrc = false;

        public event ValidationFailureEventHandler OnValidationFailure;

        public void FoundLameHeaderEventHandler(FoundLameHeaderEventArgs e)
        {
            musicCrc = e.Region.MusicCrc;
            shouldCalculateCrc = true;
        }

        public void FoundMp3FrameEventHandler(FoundMp3FrameEventArgs e)
        {
            if (mp3FrameCount > 0
                && shouldCalculateCrc)
            {
                if (previousJunkRegion != null)
                {
                    // The previous junk region is part of the music crc
                    calcCrc.AddToCrc(previousJunkRegion.Bytes);
                }
                calcCrc.AddToCrc(e.Region.Bytes);
            }
            previousJunkRegion = null;
            mp3FrameCount++;
        }

        public void FoundJunkRegionEventHandler(FoundJunkRegionEventArgs e)
        {
            previousJunkRegion = e.Region;
        }

        public void EndStreamProcessEventHandler(EndStreamProcessEventArgs e)
        {
            if (shouldCalculateCrc && musicCrc != calcCrc.GetCrc())
            {
                if (OnValidationFailure != null)
                {
                    OnValidationFailure(
                        new ValidationFailureEventArgs(
                            new ValidationFailure(
                                "Lame music CRC",
                                "Expected " + musicCrc + ", "
                                + "found " + calcCrc.GetCrc())));
                }
            }
        }
    }
}
