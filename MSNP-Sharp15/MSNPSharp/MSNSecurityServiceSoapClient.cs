using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using System.Net;
using System.Security.Authentication;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;

namespace MSNPSharp.SOAP
{
    using MSNPSharp.MSNWS.MSNSecurityTokenService;

    /// <summary>
    /// Why this happens??? Just go and ask M$ why they just ignore to send a ContentType header!
    /// </summary>
    public class MSNSecurityServiceSoapClient : SecurityTokenService
    {
        /// <summary>
        /// Override GetWebResponse is just enough. If you want to know why...use Reflector to see
        /// how SoapHttpClientProtocol class is implemented in the framework.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;
            try
            {
                response = new FakeWebResponse((HttpWebResponse)request.GetResponse());
            }
            catch(Exception ex)
            {
                throw new AuthenticationException("Request error: " +ex.Message );
            }
            return response;
        }
    }

    public class FakeWebResponse :WebResponse
    {
        WebResponse resp = null;

        protected FakeWebResponse()
        { }

        public FakeWebResponse(WebResponse originalResponse)
        {
            resp = originalResponse;
        }

        public override Stream GetResponseStream()
        {
            //If we don't do this,we will always encount an error when calling RequestMultipleSecurityTokens. 
            //This line of code just cost me 2 days.
            ((HttpWebResponse)resp).Headers[HttpResponseHeader.ContentType] = "text/xml; charset=utf-8";
            MemoryStream memstream = new MemoryStream();

            Stream s = resp.GetResponseStream();
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(s);
            XmlNodeList envlist = xmldoc.GetElementsByTagName("S:Envelope");
            XmlNodeList bodylist = xmldoc.GetElementsByTagName("S:Body");
            XmlNodeList faultlist = xmldoc.GetElementsByTagName("S:Fault");

            if (bodylist.Count == 0 && faultlist.Count > 0)
            {
                //RequestMultipleSecurityTokens will never fail but return a reply without <S:Body> tag!!
                XmlElement bodyElement = xmldoc.CreateElement("Body", xmldoc.DocumentElement.NamespaceURI);
                bodyElement.AppendChild(faultlist[0]);  //Add the fault to body so RequestMultipleSecurityTokens just throw an exception.
                envlist[0].AppendChild(bodyElement);
            }
            xmldoc.Save(memstream);
            memstream.Position = 0;
            return memstream;
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
}
