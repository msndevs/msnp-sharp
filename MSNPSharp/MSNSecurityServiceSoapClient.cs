using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Security.Authentication;

namespace MSNPSharp.SOAP
{
    using MSNPSharp.MSNWS.MSNSecurityTokenService;

    /// <summary>
    /// Just go and ask M$ why they just send a ContentType header as "text/html" instead of "text/xml"!
    /// </summary>
    public class MSNSecurityServiceSoapClient : SecurityTokenService
    {
        /// <summary>
        /// Override GetWebResponse is just enough. If you want to know why...use Reflector to see
        /// how SoapHttpClientProtocol class is implemented in the framework.
        /// <remarks>This class migth not needed in Linux with mono.</remarks>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;
            try
            {
                response = new FakeWebResponse(base.GetWebResponse(request));
            }
            catch (Exception ex)
            {
                throw new AuthenticationException("Request error: " + ex.Message);
            }
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = null;
            try
            {
                response = new FakeWebResponse(base.GetWebResponse(request, result));
            }
            catch (Exception ex)
            {
                throw new AuthenticationException("Request error: " + ex.Message);
            }
            return response;
        }
    }

    public class FakeWebResponse : WebResponse
    {
        WebResponse resp = null;
        MemoryStream innerstream = null;

        protected FakeWebResponse()
        {
        }

        public FakeWebResponse(WebResponse originalResponse)
        {
            resp = originalResponse;
        }

        public override Stream GetResponseStream()
        {
            // If we don't do this,we will always encount an error when calling RequestMultipleSecurityTokens. 
            // This line of code just cost me 2 days.
            Headers[HttpResponseHeader.ContentType] = "text/xml; charset=utf-8";

            if (innerstream == null)
            {
                innerstream = new MemoryStream();

                using (Stream s = resp.GetResponseStream())
                {
                    int read;
                    byte[] buffer = new byte[8192];
                    while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        innerstream.Write(buffer, 0, read);
                    }
                }

                innerstream.Position = 0;

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(innerstream);

                XmlNodeList envlist = xmldoc.GetElementsByTagName("S:Envelope");
                XmlNodeList bodylist = xmldoc.GetElementsByTagName("S:Body");
                XmlNodeList faultlist = xmldoc.GetElementsByTagName("S:Fault");

                if (bodylist.Count == 0 && faultlist.Count > 0)
                {
                    // RequestMultipleSecurityTokens will never fail but return a reply without <S:Body> tag!!
                    XmlElement bodyElement = xmldoc.CreateElement("Body", xmldoc.DocumentElement.NamespaceURI);
                    bodyElement.AppendChild(faultlist[0]);  //Add the fault to body so RequestMultipleSecurityTokens just throw an exception.
                    envlist[0].AppendChild(bodyElement);
                    innerstream.Position = 0;
                }
                
                xmldoc.Save(innerstream);
                innerstream.Position = 0;
            }
            return innerstream;
        }

        #region Other overrides

        public override void Close()
        {
            resp.Close();
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                return resp.Headers;
            }
        }

        public override string ContentType
        {
            get
            {
                return resp.ContentType;
            }
            set
            {
                resp.ContentType = value;
            }
        }

        public override long ContentLength
        {
            get
            {
                return resp.ContentLength;
            }
            set
            {
                resp.ContentLength = value;
            }
        }

        public override bool IsFromCache
        {
            get
            {
                return resp.IsFromCache;
            }
        }

        public override Uri ResponseUri
        {
            get
            {
                return resp.ResponseUri;
            }
        }

        public override bool IsMutuallyAuthenticated
        {
            get
            {
                return resp.IsMutuallyAuthenticated;
            }
        }

        public override object InitializeLifetimeService()
        {
            return resp.InitializeLifetimeService();
        }

        #endregion
    }
};
