using System;
using System.Collections.Generic;
using System.Text;

namespace Slush
{
    public class ValidationFailureEventArgs
    {
        private ValidationFailure failure;

        public ValidationFailureEventArgs(ValidationFailure failure)
        {
            this.failure = failure;
        }

        public ValidationFailure Failure
        {
            get
            {
                return failure;
            }
        }
    }

    public delegate void ValidationFailureEventHandler(ValidationFailureEventArgs e);

    public class ValidationFailure
    {
        private string validationType;
        private string details;

        public ValidationFailure(string validationType, string details)
        {
            this.validationType = validationType;
            this.details = details;
        }

        public ValidationFailure(string validationType)
            : this(validationType, string.Empty)
        {
        }

        public string ValidationType
        {
            get
            {
                return validationType;
            }
        }

        public string Details
        {
            get
            {
                return details;
            }
        }
    }
}
