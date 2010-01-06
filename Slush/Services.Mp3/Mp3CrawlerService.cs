using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Slush.Services;
using Slush.DomainObjects.Mp3;

namespace Slush.Services.Mp3
{
    #region Public Classes

    public interface IFoundMp3StreamRegionEventArgs
    {
        IMp3StreamRegion Region
        {
            get;
        }
    }

    /// <summary>
    /// Arguments for the FoundJunkRegion event,
    /// holding the JunkRegion
    /// </summary>
    public class FoundJunkRegionEventArgs : IFoundMp3StreamRegionEventArgs
    {
        private JunkRegion junkRegion;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="junkRegion"></param>
        public FoundJunkRegionEventArgs(JunkRegion junkRegion)
        {
            this.junkRegion = junkRegion;
        }

        IMp3StreamRegion IFoundMp3StreamRegionEventArgs.Region
        {
            get
            {
                return junkRegion;
            }
        }

        /// <summary>
        /// The recently discovered JunkRegion
        /// </summary>
        public new JunkRegion Region
        {
            get
            {
                return junkRegion;
            }
        }
    }

    /// <summary>
    /// Arguments for the FoundMp3Frame event
    /// </summary>
    public class FoundMp3FrameEventArgs : IFoundMp3StreamRegionEventArgs
    {
        private Mp3Frame mp3Frame;

        public FoundMp3FrameEventArgs(Mp3Frame mp3Frame)
        {
            this.mp3Frame = mp3Frame;
        }

        IMp3StreamRegion IFoundMp3StreamRegionEventArgs.Region
        {
            get
            {
                return mp3Frame;
            }
        }

        public Mp3Frame Region
        {
            get
            {
                return mp3Frame;
            }
        }
    }

    #endregion


    #region Public Delegates

    /// <summary>
    /// Delegate for the FoundJunkRegion event
    /// </summary>
    /// <param name="e">Arguments</param>
    public delegate void FoundJunkRegionEventHandler(FoundJunkRegionEventArgs e);
    /// <summary>
    /// Delegate for the FoundMp3Frame event
    /// </summary>
    /// <param name="e">Arguments</param>
    public delegate void FoundMp3FrameEventHandler(FoundMp3FrameEventArgs e);

    #endregion
    

    /// <summary>
    /// Traverses an mp3 stream, signaling
    /// when frames are encountered
    /// </summary>
    public class Mp3CrawlerService
    {
        #region Public Events

        /// <summary>
        /// Fired when the crawler determines the length of a junk region
        /// </summary>
        public event FoundJunkRegionEventHandler OnFoundJunkRegion;
        /// <summary>
        /// Fired when the crawler encounters an mp3 frame
        /// </summary>
        public event FoundMp3FrameEventHandler OnFoundMp3Frame;

        /// <summary>
        /// If there is a problem parsing the stream, then it
        /// will not validate
        /// </summary>
        public event ValidationFailureEventHandler OnValidationFailure;
        #endregion


        #region Public Methods

        /// <summary>
        /// Begins the traversal
        /// </summary>
        public void BeginStreamProcessEventHandler(BeginStreamProcessEventArgs e)
        {
            try
            {
                Mp3StreamReader reader = new Mp3StreamReader(e.Stream);
                foreach (IMp3StreamRegion region in reader)
                {
                    try
                    {
                        if (region is Mp3Frame)
                        {
                            if (OnFoundMp3Frame != null)
                            {
                                OnFoundMp3Frame(new FoundMp3FrameEventArgs(region as Mp3Frame));
                            }
                        }
                        else if (region is JunkRegion)
                        {
                            if (OnFoundJunkRegion != null)
                            {
                                OnFoundJunkRegion(new FoundJunkRegionEventArgs(region as JunkRegion));
                            }
                        }
                        else
                        {
                            Debug.Fail("Unexpected region type");
                        }
                    }
                    catch (Exception ex1)
                    {
                        // Keep going
                        Trace.WriteLine("Unexpected exception: " + ex1.Message);
                    }
                }
            }
            catch (Exception ex2)
            {
                Trace.WriteLine("Unexpected exception: " + ex2.Message);
                if (OnValidationFailure != null)
                {
                    OnValidationFailure(
                        new ValidationFailureEventArgs(
                            new ValidationFailure(
                                "Parser failure",
                                ex2.Message)));
                    return;
                }
            }
        }
        
        #endregion
    }
}
