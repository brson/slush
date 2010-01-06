using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.DomainObjects.Mp3
{
    public static class LameHeaderRules
    {
        public static ushort CalculateInfoCrc(LameHeader header)
        {
            const int INFO_CRC_LENGTH = 190;

            return Crc16.Calculate(header.Bytes, 0, INFO_CRC_LENGTH);
        }

        public static bool IsInfoCrcCorrect(LameHeader header)
        {
            try
            {
                if (header.InfoCrc == CalculateInfoCrc(header))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
