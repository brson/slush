using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Slush.Services
{
    #region Public Classes

    /// <summary>
    /// Arguments for the BeginStreamProcess event handler
    /// </summary>
    public class BeginStreamProcessEventArgs
    {
        private Stream stream;

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public BeginStreamProcessEventArgs(Stream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Stream being processed
        /// </summary>
        public Stream Stream
        {
            get
            {
                return stream;
            }
        }
    }

    /// <summary>
    /// Arguments for the EndStreamProcess event handler
    /// </summary>
    public class EndStreamProcessEventArgs
    {
    }

    #endregion


    #region Public Delegates

    /// <summary>
    /// Used to notify others to begin processing a stream
    /// </summary>
    /// <param name="e">Arguments</param>
    public delegate void BeginStreamProcessEventHandler(BeginStreamProcessEventArgs e);
    /// <summary>
    /// Used to notify others that processing of a stream has ended
    /// </summary>
    /// <param name="e">Arguments</param>
    public delegate void EndStreamProcessEventHandler(EndStreamProcessEventArgs e);

    #endregion


    /// <summary>
    /// Provides access to a stream for the purpose
    /// of processing, with events triggered to begin
    /// and end processing.
    /// </summary>
    public class StreamProcessService
    {
        #region Private Members

        private Stream stream;

        #endregion


        #region Public Events
        
        /// <summary>
        /// Triggered to begin processing a stream
        /// </summary>
        public event BeginStreamProcessEventHandler OnBeginStreamProcess;
        /// <summary>
        /// Triggered that processing is over
        /// </summary>
        public event EndStreamProcessEventHandler OnEndStreamProcess;

        #endregion


        #region Constructors

        /// <summary>
        /// Creates a StreamProcessService object
        /// </summary>
        /// <param name="stream">A stream</param>
        /// <exception cref="ArgumentNullException">stream is null</exception>
        public StreamProcessService(Stream stream)
        {
            if (null == stream)
            {
                throw new ArgumentNullException();
            }
            this.stream = stream;
        }

        /// <summary>
        /// Begins processing a stream by firing OnBeginStreamProcess.
        /// Immediately fires OnEndStreamProcess.
        /// </summary>
        public void Begin()
        {
            if (null != OnBeginStreamProcess)
            {
                try
                {
                    OnBeginStreamProcess(new BeginStreamProcessEventArgs(stream));
                }
                catch (Exception e)
                {
                    Trace.WriteLine("OnBeginStreamProcess threw " + e.Message);
                }
            }
            if (null != OnEndStreamProcess)
            {
                try
                {
                    OnEndStreamProcess(new EndStreamProcessEventArgs());
                }
                catch (Exception e)
                {
                    Trace.WriteLine("OnEndStreamProcess threw " + e.Message);
                }
            }
        }

        #endregion
    }
}
