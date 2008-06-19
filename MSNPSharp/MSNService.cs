using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp
{
    public delegate void ServiceOperationFailedEventHandler(object sender, ServiceOperationFailedEventArgs e);

    public class ServiceOperationFailedEventArgs : EventArgs
    {
        private string method;
        private Exception exc;

        public ServiceOperationFailedEventArgs(string methodname, Exception ex)
        {
            method = methodname;
            exc = ex;
        }

        public string Method
        {
            get { return method; }
        }
        public Exception Exception
        {
            get { return exc; }
        }
    }

    /// <summary>
    /// Base class of webservice-related classes
    /// </summary>
    public abstract class MSNService
    {
        /// <summary>
        /// Fired when request to an async webservice method failed.
        /// </summary>
        public event ServiceOperationFailedEventHandler ServiceOperationFailed;
        protected virtual void OnServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            if (ServiceOperationFailed != null)
                ServiceOperationFailed(sender, e);
        }
    }
}
