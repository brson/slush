using System;
using System.Collections.Generic;
using System.Text;

namespace Slush
{
    public interface IValidator
    {
        event ValidationFailureEventHandler OnValidationFailure;
    }
}
