using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Validators.Mp3
{
    public class FrameCrcValidator : IValidator
    {
        public event ValidationFailureEventHandler OnValidationFailure;
    }
}
