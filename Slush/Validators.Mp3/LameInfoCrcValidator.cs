using System;
using System.Collections.Generic;
using System.Text;
using Slush.DomainObjects.Mp3;
using Slush.Services.Mp3;

namespace Slush.Validators.Mp3
{
    public class LameInfoCrcValidator : IValidator
    {
        public event ValidationFailureEventHandler OnValidationFailure;

        public void FoundLameHeaderEventHandler(FoundLameHeaderEventArgs e)
        {
            if (!LameHeaderRules.IsInfoCrcCorrect(e.Region))
            {
                if (OnValidationFailure != null)
                {
                    OnValidationFailure(
                        new ValidationFailureEventArgs(
                            new ValidationFailure(
                                "Lame info CRC",
                                "Expected " 
                                + e.Region.InfoCrc + ", "
                                + "found "
                                + LameHeaderRules.CalculateInfoCrc(e.Region))));
                }
            }
        }
    }
}
