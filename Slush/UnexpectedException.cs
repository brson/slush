using System;
using System.Collections.Generic;
using System.Text;

namespace Slush
{
    /// <summary>
    /// Something unexpected happened. These are for unreachable code,
    /// caught exceptions that shouldn't have been thrown, etc.
    /// </summary>
    public class UnexpectedException : ApplicationException
    {
        public UnexpectedException()
            : base(Message)
        {
        }

        public UnexpectedException(string message)
            : base(message)
        {
        }

        public UnexpectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public UnexpectedException(Exception innerException)
            : base(Message, innerException)
        {
        }

        public static string Message
        {
            get
            {
                return "An unexpected error has ocurred";
            }
        }
    }
}
