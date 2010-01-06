using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;

namespace Slush.DomainObjects.Mp3
{
    /// <summary>
    /// Represent regions of non-frame data in an
    /// mpeg stream, including unrecognized data
    /// at the beginning and end of a stream
    /// </summary>
    /// <remarks>Immutable.</remarks>
    public class JunkRegion : IMp3StreamRegion
    {
        #region Public Constants

        // TODO: What's a good max length for JunkRegion?
        /// <summary>
        /// Maximum length of a junk region. Unrecognized
        /// data of longer length may be split into multiple
        /// JunkRegions
        /// </summary>
        public static readonly int MAX_JUNK_REGION_LENGTH = 2886;

        #endregion


        #region Private Members

        private ReadOnlyCollection<byte> publicBytes;

        #endregion


        #region Constructors

        public JunkRegion(IList<byte> bytes)
        {
            if (null == bytes)
            {
                throw new ArgumentNullException("bytes", "Argument may not be null");
            }
            if (bytes.Count <= 0)
            {
                throw new ArgumentException("bytes", "Length must be greater than 0");
            }
            publicBytes = new ReadOnlyCollection<byte>(Util.CloneList(bytes));
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// List of bytes in the junk region
        /// </summary>
        public IList<byte> Bytes
        {
            get
            {
                Debug.Assert(null != publicBytes);
                return publicBytes;
            }
        }

        #endregion
    }
}
