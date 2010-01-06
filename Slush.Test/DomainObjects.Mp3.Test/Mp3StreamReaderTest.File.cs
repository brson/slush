using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Extensions;
using NUnit.Framework.SyntaxHelpers;

namespace Slush.DomainObjects.Mp3.Test
{
    public partial class Mp3StreamReaderTest
    {
        private string testDir = @"..\s022691_ISO_IEC_11172-4_1995_Compliance_Testing\layer3\";

        [Row("compl.bit"   , 217)]
        [Row("he_32khz.bit", 150)]
        [Row("he_44khz.bit", 410)]
        [Row("he_48khz.bit", 150)]
        [Row("he_free.bit" ,  -1)]
        [Row("he_mode.bit" , 128)]
        [Row("hecommon.bit",  30)]
        [Row("si.bit"      , 118)]
        [Row("si_block.bit",  64)]
        [Row("si_huff.bit" ,  75)]
        [Row("sin1k0db.bit", 318)]
        [RowTest]
        public void FileTest(string fileName, int numFrames)
        {
            Assert.That(File.Exists(fileName));
            Mp3StreamReader reader = 
                new Mp3StreamReader
                    (
                    new BufferedStream
                        (
                        new FileStream
                            (
                            fileName,
                            FileMode.Open,
                            FileAccess.Read
                            )
                        )
                    );

            int frameCount = 0;

            foreach (IMp3StreamRegion region in reader)
            {
                if (region is Mp3Frame)
                {
                    frameCount++;
                }
            }
            Assert.That(frameCount, Is.EqualTo(numFrames));
        }
    }
}
