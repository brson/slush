using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Slush.DomainObjects.Mp3;

namespace Slush.App
{
    public class Slush
    {
        public static void Main(string[] args)
        {
            string filename =
                //"compl.bit";
                //"01-Lovecraft_s Death.mp3";
                //"01-enochian_crescent-tatan.mp3";
                //"bad-info-crc.mp3";
                //"test-id3v1.mp3";
                //"test-id3v2.mp3";
                "test-lame.mp3";

            Stream mp3Stream =
                new BufferedStream(
                    new FileStream(
                        filename,
                        FileMode.Open,
                        FileAccess.Read));

            Mp3Validator validator = new Mp3Validator();
            validator.OnValidationFailure += ValidationFailureEventHandler;
            validator.Validate(mp3Stream);
            Console.WriteLine("Done");
            mp3Stream.Seek(0, SeekOrigin.Begin);

            Mp3StreamReader reader = new Mp3StreamReader(mp3Stream);
            foreach (IMp3StreamRegion region in reader)
            {
            }
            Console.WriteLine("Done");
            mp3Stream.Seek(0, SeekOrigin.Begin);

            while (-1 != mp3Stream.ReadByte())
            {
            }
            Console.WriteLine("Done");

            Console.ReadKey();
        }

        public static void ValidationFailureEventHandler(ValidationFailureEventArgs e)
        {
            Console.WriteLine(e.Failure.ValidationType + ": " + e.Failure.Details);
        }
    }
}
