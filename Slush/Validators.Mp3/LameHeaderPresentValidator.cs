using System;
using System.Collections.Generic;
using System.Text;
using Slush.Services.Mp3;

namespace Slush.Validators.Mp3
{
    public class LameHeaderPresentValidator : IValidator
    {
        private static ValidationFailureEventArgs cachedArgs =
            new ValidationFailureEventArgs(
                new ValidationFailure(
                    "LAME header not present"));

        public event ValidationFailureEventHandler OnValidationFailure;

        public void MissedLameHeaderEventHandler(MissedLameHeaderEventArgs e)
        {
            if (OnValidationFailure != null)
            {
                OnValidationFailure(cachedArgs);
            }
        }
    }
}
