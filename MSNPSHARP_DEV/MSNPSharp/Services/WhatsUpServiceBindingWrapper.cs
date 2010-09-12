using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MSNPSharp.Services
{
    using MSNPSharp.MSNWS.MSNABSharingService;

    internal sealed class WhatsUpServiceBindingWrapper : WhatsUpServiceBinding
    {
        private IPEndPoint localEndPoint = null;

        public WhatsUpServiceBindingWrapper()
            : base()
        {
        }

        public WhatsUpServiceBindingWrapper(IPEndPoint localEndPoint)
            : base()
        {
            this.localEndPoint = localEndPoint;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request =  base.GetWebRequest(uri);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(BindIPEndPointCallback);
            }

            return request;
        }


        private IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
        {
            if (remoteEndPoint.AddressFamily == AddressFamily.InterNetwork)
            {
                if (localEndPoint == null)
                    return new IPEndPoint(IPAddress.Any, 0);
                return localEndPoint;
            }
            else
            {
                if (localEndPoint == null)
                    return new IPEndPoint(IPAddress.IPv6Any, 0);
                return localEndPoint;
            }
        }
    }
}

