using System;
using System.Collections.Generic;
using System.Text;
using Slush.DomainObjects.Mp3;

namespace Slush.Services.Mp3
{
    public class FoundLameHeaderEventArgs : IFoundMp3StreamRegionEventArgs
    {
        private LameHeader header;

        public FoundLameHeaderEventArgs(LameHeader header)
        {
            this.header = header;
        }

        IMp3StreamRegion IFoundMp3StreamRegionEventArgs.Region
        {
            get
            {
                return header;
            }
        }

        public LameHeader Region
        {
            get
            {
                return header;
            }
        }
    }

    public class MissedLameHeaderEventArgs
    {
    }

    public delegate void FoundLameHeaderEventHandler(FoundLameHeaderEventArgs e);
    public delegate void MissedLameHeaderEventHandler(MissedLameHeaderEventArgs e);

    public class LameHeaderService
    {
        private int visitCount = 0;

        public event FoundLameHeaderEventHandler OnFoundLameHeader;
        public event MissedLameHeaderEventHandler OnMissedLameHeader;

        public void FoundMp3FrameEventHandler(FoundMp3FrameEventArgs e)
        {
            if (visitCount++ == 0)
            {
                LameHeader header = new LameHeader(e.Region);
                if (header.IsValid)
                {
                    if (OnFoundLameHeader != null)
                    {
                        OnFoundLameHeader(
                            new FoundLameHeaderEventArgs(header));
                    }
                }
                else
                {
                    if (OnMissedLameHeader != null)
                    {
                        OnMissedLameHeader(
                            new MissedLameHeaderEventArgs());
                    }
                }
            }
        }
    }
}
