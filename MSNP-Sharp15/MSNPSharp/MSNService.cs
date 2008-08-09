using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace MSNPSharp
{
    #region ServiceOperationFailedEvent

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
            get
            {
                return method;
            }
        }
        public Exception Exception
        {
            get
            {
                return exc;
            }
        }
    }

    #endregion

    /// <summary>
    /// Base class of webservice-related classes
    /// </summary>
    public abstract class MSNService
    {
        private NSMessageHandler nsMessageHandler = null;
        private WebProxy webProxy = null;

        private MSNService()
        {
        }

        protected MSNService(NSMessageHandler nsHandler)
        {
            nsMessageHandler = nsHandler;
            if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
            {
                webProxy = nsMessageHandler.ConnectivitySettings.WebProxy;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        public WebProxy WebProxy
        {
            get
            {
                return webProxy;
            }
        }

        /// <summary>
        /// Fired when request to an async webservice method failed.
        /// </summary>
        public event ServiceOperationFailedEventHandler ServiceOperationFailed;

        /// <summary>
        /// Fires ServiceOperationFailed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            if (ServiceOperationFailed != null)
                ServiceOperationFailed(sender, e);
        }
    }
};
