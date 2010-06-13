using System;
using System.Collections.Generic;
using System.Text;
using System.Resources;
using System.Globalization;
using System.Threading;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Collections;

namespace MSNPSharp.Framework
{
    using MSNPSharp.MSNWS.MSNSecurityTokenService;
    using System.Xml;
    using System.Diagnostics;

    internal class ContentType
    {
        // Fields
        internal const string ApplicationBase = "application";
        internal const string ApplicationOctetStream = "application/octet-stream";
        internal const string ApplicationSoap = "application/soap+xml";
        internal const string ApplicationXml = "application/xml";
        internal const string ContentEncoding = "Content-Encoding";
        internal const string TextBase = "text";
        internal const string TextHtml = "text/html";
        internal const string TextPlain = "text/plain";
        internal const string TextXml = "text/xml";

        // Methods
        private ContentType()
        {
        }

        internal static string Compose(string contentType, Encoding encoding)
        {
            return Compose(contentType, encoding, null);
        }

        internal static string Compose(string contentType, Encoding encoding, string action)
        {
            if ((encoding == null) && (action == null))
            {
                return contentType;
            }
            StringBuilder builder = new StringBuilder(contentType);
            if (encoding != null)
            {
                builder.Append("; charset=");
                builder.Append(encoding.WebName);
            }
            if (action != null)
            {
                builder.Append("; action=\"");
                builder.Append(action);
                builder.Append("\"");
            }
            return builder.ToString();
        }

        internal static string GetAction(string contentType)
        {
            return GetParameter(contentType, "action");
        }

        internal static string GetBase(string contentType)
        {
            int index = contentType.IndexOf(';');
            if (index >= 0)
            {
                return contentType.Substring(0, index);
            }
            return contentType;
        }

        internal static string GetCharset(string contentType)
        {
            return GetParameter(contentType, "charset");
        }

        internal static string GetMediaType(string contentType)
        {
            string str = GetBase(contentType);
            int index = str.IndexOf('/');
            if (index >= 0)
            {
                return str.Substring(0, index);
            }
            return str;
        }

        private static string GetParameter(string contentType, string paramName)
        {
            string[] strArray = contentType.Split(new char[] { ';' });
            for (int i = 1; i < strArray.Length; i++)
            {
                string strA = strArray[i].TrimStart(null);
                if (string.Compare(strA, 0, paramName, 0, paramName.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    int index = strA.IndexOf('=', paramName.Length);
                    if (index >= 0)
                    {
                        return strA.Substring(index + 1).Trim(new char[] { ' ', '\'', '"', '\t' });
                    }
                }
            }
            return null;
        }

        internal static bool IsApplication(string contentType)
        {
            return (string.Compare(GetMediaType(contentType), "application", StringComparison.OrdinalIgnoreCase) == 0);
        }

        internal static bool IsHtml(string contentType)
        {
            return (string.Compare(GetBase(contentType), "text/html", StringComparison.OrdinalIgnoreCase) == 0);
        }

        internal static bool IsSoap(string contentType)
        {
            string strA = GetBase(contentType);
            if (string.Compare(strA, "text/xml", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return (string.Compare(strA, "application/soap+xml", StringComparison.OrdinalIgnoreCase) == 0);
            }
            return true;
        }

        internal static bool IsXml(string contentType)
        {
            string strA = GetBase(contentType);
            if (string.Compare(strA, "text/xml", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return (string.Compare(strA, "application/xml", StringComparison.OrdinalIgnoreCase) == 0);
            }
            return true;
        }

        internal static bool MatchesBase(string contentType, string baseContentType)
        {
            return (string.Compare(GetBase(contentType), baseContentType, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }

    internal class RequestResponseUtils
    {
        // Methods
        private RequestResponseUtils()
        {
        }

        internal static int GetBufferSize(int contentLength)
        {
            if (contentLength == -1)
            {
                return 0x1f40;
            }
            if (contentLength <= 0x3e80)
            {
                return contentLength;
            }
            return 0x3e80;
        }

        internal static Encoding GetEncoding(string contentType)
        {
            string charset = ContentType.GetCharset(contentType);
            Encoding encoding = null;
            try
            {
                if ((charset != null) && (charset.Length > 0))
                {
                    encoding = Encoding.GetEncoding(charset);
                }
            }
            catch (Exception exception)
            {
                if (((exception is ThreadAbortException) || (exception is StackOverflowException)) || (exception is OutOfMemoryException))
                {
                    throw;
                }
            }


            if (encoding != null)
            {
                return encoding;
            }
            return new ASCIIEncoding();
        }

        internal static Encoding GetEncoding2(string contentType)
        {
            if (!ContentType.IsApplication(contentType))
            {
                return GetEncoding(contentType);
            }
            string charset = ContentType.GetCharset(contentType);
            Encoding encoding = null;
            try
            {
                if ((charset != null) && (charset.Length > 0))
                {
                    encoding = Encoding.GetEncoding(charset);
                }
            }
            catch (Exception exception)
            {
                if (((exception is ThreadAbortException) || (exception is StackOverflowException)) || (exception is OutOfMemoryException))
                {
                    throw;
                }
            }

            return encoding;
        }

        internal static string ReadResponse(WebResponse response)
        {
            return ReadResponse(response, response.GetResponseStream());
        }

        internal static string ReadResponse(WebResponse response, Stream stream)
        {
            string str;
            Encoding encoding = GetEncoding(response.ContentType);
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }
            StreamReader reader = new StreamReader(stream, encoding, true);
            try
            {
                str = reader.ReadToEnd();
            }
            finally
            {
                stream.Close();
            }
            return str;
        }

        internal static Stream StreamToMemoryStream(Stream stream)
        {
            int num;
            MemoryStream stream2 = new MemoryStream(0x400);
            byte[] buffer = new byte[0x400];
            while ((num = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                stream2.Write(buffer, 0, num);
            }
            stream2.Position = 0L;
            return stream2;
        }

    }

    internal static class XmlSpecialNamespaces
    {
        private static Dictionary<string, string> prefixNamespacePairs = new Dictionary<string, string>();
        private static bool initialized = false;

        public static string GetPrefix(string ns)
        {
            if (ns == null || ns.Length == 0)
            {
                return string.Empty;
            }

            if (!initialized)
            {
                prefixNamespacePairs.Add(@"http://schemas.microsoft.com/Passport/SoapServices/PPCRL", @"ps");
                prefixNamespacePairs.Add(@"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault", @"psf");

                prefixNamespacePairs.Add(@"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", @"wsse");
                prefixNamespacePairs.Add(@"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", @"wsu");

                prefixNamespacePairs.Add(@"http://schemas.xmlsoap.org/ws/2005/02/trust", @"wst");
                prefixNamespacePairs.Add(@"http://www.w3.org/2005/08/addressing", @"wsa");
                prefixNamespacePairs.Add(@"http://schemas.xmlsoap.org/ws/2004/09/policy", @"wsp");
                prefixNamespacePairs.Add(@"http://schemas.xmlsoap.org/ws/2005/02/sc", @"wsc");
                prefixNamespacePairs.Add(@"http://schemas.xmlsoap.org/ws/2004/08/addressing", @"a");

                prefixNamespacePairs.Add(@"http://www.w3.org/2001/04/xmlenc#", @"xenc");
                prefixNamespacePairs.Add(@"http://www.w3.org/2000/09/xmldsig#", @"ds");

                prefixNamespacePairs.Add(@"urn:oasis:names:tc:SAML:1.0:assertion", @"saml");
                prefixNamespacePairs.Add(@"urn:oasis:names:tc:SAML:1.0:protocol", @"samlp");

                initialized = true;
            }

            if (prefixNamespacePairs.ContainsKey(ns))
                return prefixNamespacePairs[ns];
            return string.Empty;
        }
    }


    public class XmlSpecialNSPrefixTextWriter : XmlTextWriter
    {
        private enum WriteState
        {
            None,
            EvelopeWritten,
            SpecialNSWritten,
            BeginWriteBody
        }

        private WriteState state = WriteState.None;

        public XmlSpecialNSPrefixTextWriter(TextWriter w)
            : base(w)
        {
        }

        public XmlSpecialNSPrefixTextWriter(Stream w, Encoding encoding)
            : base(w, encoding)
        {
        }

        public XmlSpecialNSPrefixTextWriter(string filename, Encoding encoding)
            : base(filename, encoding)
        {
        }

        //public override string LookupPrefix(string ns)
        //{
        //    string pref = XmlSpecialNamespaces.GetPrefix(ns);

        //    if (pref.Length == 0)
        //        return base.LookupPrefix(ns);
        //    return pref;
        //}

        //public override void WriteStartAttribute(string prefix, string localName, string ns)
        //{
        //    string pref = XmlSpecialNamespaces.GetPrefix(ns);

        //    if (pref.Length > 0)
        //    {
        //        if (prefix == "xmlns")
        //        {
        //            localName = pref;
        //        }
        //        else
        //        {
        //            prefix = pref;
        //        }
        //    }

        //    Trace.WriteLineIf(MSNPSharp.Settings.TraceSwitch.TraceVerbose, (prefix == null ? "" : pref) + ":" + (localName == null ? "" : localName) + " = " + (ns == null ? "" : ns));

        //    base.WriteStartAttribute(prefix, localName, ns);
        //}

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            base.WriteStartElement(prefix, localName, ns);

            if (localName == "Envelope" && state == WriteState.None)
            {
                state = WriteState.EvelopeWritten;

                if (state == WriteState.EvelopeWritten)
                {
                    //WriteAttributeString("xmlns", "ps", null, @"http://schemas.microsoft.com/Passport/SoapServices/PPCRL");
                    //WriteAttributeString("xmlns", "psf", null, @"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault");

                    WriteAttributeString("xmlns", "wsse", null, @"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
                    WriteAttributeString("xmlns", "wssc", null, @"http://schemas.xmlsoap.org/ws/2005/02/sc");
                    WriteAttributeString("xmlns", "wsu", null, @"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
                    WriteAttributeString("xmlns", "wst", null, @"http://schemas.xmlsoap.org/ws/2005/02/trust");
                    WriteAttributeString("xmlns", "wsp", null, @"http://schemas.xmlsoap.org/ws/2004/09/policy");
                    WriteAttributeString("xmlns", "wsa", null, @"http://www.w3.org/2005/08/addressing");

                    WriteAttributeString("xmlns", "saml", null, @"urn:oasis:names:tc:SAML:1.0:assertion");

                    state = WriteState.SpecialNSWritten;
                }
            }

            if (localName == "Assertion" && state == WriteState.SpecialNSWritten)
            {
                WriteAttributeString("xmlns", "saml", null, @"urn:oasis:names:tc:SAML:1.0:assertion");
            }

            if (localName == "Body" && state != WriteState.BeginWriteBody)
            {
                state = WriteState.BeginWriteBody;
            }
        }
    }
}
