using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

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

    public class MSNServiceCertificatePolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint sp, X509Certificate certificate, WebRequest request, int error)
        {
            return true;
        }
    }

    /// <summary>
    /// Base class of webservice-related classes
    /// </summary>
    public abstract class MSNService
    {
        static MSNService()
        {
            if (null != Type.GetType("Mono.Runtime"))
            {
                ServicePointManager.CertificatePolicy = new MSNServiceCertificatePolicy();
            }
            else
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate
                {
                    return true;
                };
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
