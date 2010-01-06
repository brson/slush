using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Security;

namespace Slush
{
    /// <summary>
    /// This is a container class that automatically
    /// wires up the event handlers of the objects contained.
    /// </summary>
    public sealed class EventWeaver
    {
        #region Private Types

        private delegate MethodInfo GetEventMethodDelegate(EventInfo eventInfo);
        
        #endregion


        #region Members

        private Stack<object> services = new Stack<object>();
        private bool          disposed = false;

        #endregion


        #region Public Methods

        /// <summary>
        /// Adds a service to the container
        /// </summary>
        /// <param name="o">Service object</param>
        public void Add(object o)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
            if (null == o)
            {
                throw new ArgumentNullException();
            }
            if (services.Contains(o))
            {
                throw new ArgumentException();
            }

            try
            {
                AttachSinkToAllSources(
                    o,
                    services,
                    delegate(EventInfo eventInfo)
                    {
                        return eventInfo.GetAddMethod();
                    });
                services.Push(o);
            }
            catch
            {
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
        }

        #endregion


        #region Private Methods
        
        private void PopAndDetangle()
        {
            Debug.Assert(0 < services.Count);
            try
            {
                Object o = services.Pop();
                AttachSinkToAllSources(
                    o,
                    services,
                    delegate(EventInfo eventInfo)
                    {
                        return eventInfo.GetRemoveMethod();
                    });
            }
            catch
            {
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
        }
        
        // TODO: These aren't correctly named since they can be used to both attach and remove
        private static void AttachSinkToAllSources(
            object sinkService,
            Stack<object> sourceServices,
            GetEventMethodDelegate delGev)
        {
            Debug.Assert(null != sinkService);

            // Catch everything
            try
            {
                // Try each current service
                foreach (object source in sourceServices)
                {
                    Debug.Assert(null != source, "Shouldn't be possible for the list to contain null");

                    AttachSinkToSource(sinkService, source, delGev);
                }
            }
            catch
            {
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
        }

        private static void AttachSinkToSource(
            object sinkService,
            object sourceService,
            GetEventMethodDelegate delGev)
        {
            Debug.Assert(null != sinkService);
            Debug.Assert(null != sourceService);

            // Type of service
            Type sourceServiceType = sourceService.GetType();
            // Go through all the events in the service
            EventInfo[] sourceEventInfoArray = sourceServiceType.GetEvents();
            foreach (EventInfo sourceEventInfo in sourceEventInfoArray)
            {
                AttachSinkToEvent(sinkService, sourceService, sourceEventInfo, delGev);
            }
        }

        private static void AttachSinkToEvent(
            object sinkService,
            object sourceService,
            EventInfo sourceEventInfo,
            GetEventMethodDelegate delGev)
        {
            // Try to subscribe each method of the sink
            // to each event of the service
            Type sinkServiceType = sinkService.GetType();
            MethodInfo[] sinkMethodInfoArray = sinkServiceType.GetMethods();
            foreach (MethodInfo sinkMethodInfo in sinkMethodInfoArray)
            {
                AttachMethodToEvent(sinkService, sourceService, sinkMethodInfo, sourceEventInfo, delGev);
            }
        }

        private static void AttachMethodToEvent(
            object sinkService,
            object sourceService,
            MethodInfo sinkMethodInfo,
            EventInfo sourceEventInfo,
            GetEventMethodDelegate delGev)
        {
            Delegate del = CreateDelegate(sinkService, sinkMethodInfo, sourceEventInfo);

            // Add or remove the delegate to the event
            // using the event's method retrieved using
            // the delGev GetEventMethodDelegate
            MethodInfo delAddMethodInfo = delGev(sourceEventInfo);
            Object[] delAddMethodArgs = { del };
            try
            {
                delAddMethodInfo.Invoke(sourceService, delAddMethodArgs);
            }
            catch (TargetException)
            {
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
            catch (ArgumentException)
            {
                // Arguments should be right because the delegate is
                // the correct type for the event
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
            catch (TargetInvocationException)
            {
                // Not really sure
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
            catch (TargetParameterCountException)
            {
                // Arguments should be correct...
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
            catch (MethodAccessException)
            {
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
            catch (InvalidOperationException)
            {
                // I don't really know if this should be expected
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
        }

        private static Delegate CreateDelegate(object sinkService, MethodInfo sinkMethodInfo, EventInfo sourceEventInfo)
        {
            Type delegateType;
            // Get the type of the event
            try
            {
                delegateType = sourceEventInfo.EventHandlerType;
            }
            catch (SecurityException)
            {
                return null;
            }

            Delegate del;
            try
            {
                // Make a delegate with the service's delegate type
                // and the sink's method info, bound to the sink service
                del = Delegate.CreateDelegate(delegateType, sinkService, sinkMethodInfo, false);
            }
            catch (ArgumentNullException)
            {
                // None of the arguments should have been null
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
            catch (ArgumentException)
            {
                // The delegate and the method don't jive
                // Shouldn't happen because CreateDelegate
                // is called with throwOnBindFailure set to false
                Debug.Fail(UnexpectedException.Message);
                return null;
            }
            catch (MissingMethodException)
            {
                // Don't know if this exception should be expected
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }
            catch (MethodAccessException)
            {
                // Should be a public method
                Debug.Fail(UnexpectedException.Message);
                throw new UnexpectedException();
            }

            return del;
        }

        #endregion

    }
}
