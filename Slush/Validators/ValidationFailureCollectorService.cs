using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Validators
{
    /// <summary>
    /// This is a service that relays validation failure
    /// events. It acts as a funnel to catch all of the
    /// validation failure events in an EventWeaver.
    /// </summary>
    public class ValidationFailureCollectorService : IValidator
    {
        /// <summary>
        /// Any validation failures are repeated by
        /// this event handler.
        /// </summary>
        public event ValidationFailureEventHandler OnValidationFailure;

        /// <summary>
        /// Relays the ValidationFalureEventArgs,
        /// calling the OnValidationFailure event.
        /// </summary>
        /// <notes>
        /// If the argument is null then 
        /// OnValidationFailure will not be called
        /// </notes>
        /// <param name="e"></param>
        public void ValidationFailureEventHandler(ValidationFailureEventArgs e)
        {
            if (OnValidationFailure != null)
            {
                OnValidationFailure(e);
            }
        }
    }
}
