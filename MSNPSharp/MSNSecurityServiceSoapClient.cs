#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Security.Authentication;
using System.Security.Permissions;

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
        WebResponse resp;
        MemoryStream innerstream;

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

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return resp.InitializeLifetimeService();
        }

        #endregion
    }
};
