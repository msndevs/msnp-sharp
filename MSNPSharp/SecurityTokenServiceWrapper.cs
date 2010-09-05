using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Net.Cache;
using System.Diagnostics;
using System.Web.Services;
using System.Security.Permissions;
using System.Web.Services.Protocols;

namespace MSNPSharp
{
    using MSNPSharp.MSNWS.MSNSecurityTokenService;
    using MSNPSharp.Framework;


    [System.Web.Services.WebServiceBindingAttribute(Name = "SecurityTokenServicePortBinding", Namespace = "http://schemas.microsoft.com/Passport/SoapServices/PPCRL")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(NotUnderstoodType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Fault))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Envelope))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedKeyTokenType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(SecurityContextTokenType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AttributeDesignatorType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(SignaturePropertiesType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ManifestType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EndpointReferenceType1))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ProblemActionType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(errorType))]
    public sealed class SecurityTokenServiceWrapper : SecurityTokenService
    {

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            request.ContentType = ContentType.ApplicationSoap;
            WebResponse response = base.GetWebResponse(request);
            if (!ContentType.IsSoap(response.ContentType))
                response.Headers[HttpResponseHeader.ContentType] = response.ContentType.Replace(ContentType.TextHtml, ContentType.ApplicationSoap);
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            request.ContentType = ContentType.ApplicationSoap;
            (request as HttpWebRequest).Expect = null;

            WebResponse response = base.GetWebResponse(request, result);
            if (!ContentType.IsSoap(response.ContentType))
                response.Headers[HttpResponseHeader.ContentType] = response.ContentType.Replace(ContentType.TextHtml, ContentType.ApplicationSoap);
            return response;
        }

        [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
        protected override XmlReader GetReaderForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize)
        {
            string xmlMatchSchema = "<?xml";
            string xmlSchema = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";
            int schemaLength = Encoding.UTF8.GetByteCount(xmlMatchSchema);
            Stream messageStream = message.Stream;
            byte[] schemaArray = new byte[schemaLength];

            if (messageStream.CanSeek)
            {
                long originalPosition = messageStream.Position;

                messageStream.Seek(0, SeekOrigin.Begin);
                int bytesRead = messageStream.Read(schemaArray, 0, schemaArray.Length);

                string readSchema = Encoding.UTF8.GetString(schemaArray);
                if (readSchema.ToLowerInvariant() != xmlMatchSchema.ToLowerInvariant())
                {
                    messageStream.Seek(0, SeekOrigin.Begin);
                    byte[] content = new byte[messageStream.Length];
                    messageStream.Read(content, 0, content.Length);
                    messageStream.Seek(0, SeekOrigin.Begin);

                    string strContent = Encoding.UTF8.GetString(content);

                    MemoryStream newMemStream = new MemoryStream();
                    newMemStream.Seek(0, SeekOrigin.Begin);
                    newMemStream.Write(Encoding.UTF8.GetBytes(xmlSchema), 0, Encoding.UTF8.GetByteCount(xmlSchema));
                    newMemStream.Write(content, 0, content.Length);
                    newMemStream.Seek(0, SeekOrigin.Begin);

                    XmlTextReader reader = null;
                    Encoding encoding = (message.SoapVersion == SoapProtocolVersion.Soap12) ? RequestResponseUtils.GetEncoding2(message.ContentType) : RequestResponseUtils.GetEncoding(message.ContentType);
                    if (bufferSize < 0x200)
                    {
                        bufferSize = 0x200;
                    }

                    if (encoding != null)
                    {
                        reader = new XmlTextReader(new StreamReader(message.Stream, encoding, true, bufferSize));
                    }
                    else
                    {
                        reader = new XmlTextReader(message.Stream);
                    }
                    reader.ProhibitDtd = true;
                    reader.Normalization = true;
                    reader.XmlResolver = null;
                    return reader;
                }
                else
                {
                    messageStream.Seek(originalPosition, SeekOrigin.Begin);
                    return base.GetReaderForMessage(message, bufferSize);
                }
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Unseekable stream returned with message, maybe the connection has terminated. Stream type: " + message.Stream.GetType().ToString());
                return base.GetReaderForMessage(message, bufferSize);
            }
        }

        [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
        protected override XmlWriter GetWriterForMessage(SoapClientMessage message, int bufferSize)
        {
            if (bufferSize < 0x200)
            {
                bufferSize = 0x200;
            }

            return new XmlSpecialNSPrefixTextWriter(new StreamWriter(message.Stream, (base.RequestEncoding != null) ? base.RequestEncoding : new UTF8Encoding(false), bufferSize));
        }
    }


}
