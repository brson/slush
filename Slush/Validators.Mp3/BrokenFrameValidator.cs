using System;
using System.Collections.Generic;
using System.Text;
using Slush.Services.Mp3;

namespace Slush.Validators.Mp3
{
    public class BrokenFrameValidator : IValidator
    {
        public event ValidationFailureEventHandler OnValidationFailure;

        public void FoundMp3FrameEventHandler(FoundMp3FrameEventArgs e)
        {
            if (e.Region.IsTruncated)
            {
                if (OnValidationFailure != null)
                {
                    OnValidationFailure(
                        new ValidationFailureEventArgs(
                            new ValidationFailure(
                                "broken frame")));
                }
            }
        }
    }
}
