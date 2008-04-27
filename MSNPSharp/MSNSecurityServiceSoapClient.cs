using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.MSNSecurityTokenService;
using System.Web.Services.Protocols;
using System.Net;
using System.Security.Authentication;

namespace MSNPSharp.SOAP
{
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
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch
            {
                throw new AuthenticationException("Request error");
            }
            //If we don't do this,we will always encount an error. This line of code just cost me 2 days.
            response.Headers[HttpResponseHeader.ContentType] = "text/xml; charset=utf-8";
            return response;
        }
    }
}
