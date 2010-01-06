using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.DomainObjects.Mp3
{
    /// <summary>
    /// Represents a continuous series of bytes
    /// in an mpeg stream
    /// </summary>
    public interface IMp3StreamRegion
    {
        /// <summary>
        /// List of bytes in the region
        /// </summary>
        /// <remarks>
        /// TODO: Change Bytes to Buffer
        /// </remarks>
        IList<byte> Bytes { get; }
    }
}
