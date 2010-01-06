using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Validators.Mp3
{
    public class AverageBitrateValidator : IValidator
    {
        public event ValidationFailureEventHandler OnValidationFailure;
    }
}
