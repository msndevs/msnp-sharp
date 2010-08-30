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

        // Nested Types
        private static class HttpUtility
        {
            // Fields
            private static char[] s_entityEndingChars = new char[] { ';', '&' };

            // Methods
            internal static string HtmlDecode(string s)
            {
                if (s == null)
                {
                    return null;
                }
                if (s.IndexOf('&') < 0)
                {
                    return s;
                }
                StringBuilder sb = new StringBuilder();
                StringWriter output = new StringWriter(sb, CultureInfo.InvariantCulture);
                HtmlDecode(s, output);
                return sb.ToString();
            }

            public static void HtmlDecode(string s, TextWriter output)
            {
                if (s != null)
                {
                    if (s.IndexOf('&') < 0)
                    {
                        output.Write(s);
                    }
                    else
                    {
                        int length = s.Length;
                        for (int i = 0; i < length; i++)
                        {
                            char ch = s[i];
                            if (ch == '&')
                            {
                                int num3 = s.IndexOfAny(s_entityEndingChars, i + 1);
                                if ((num3 > 0) && (s[num3] == ';'))
                                {
                                    string entity = s.Substring(i + 1, (num3 - i) - 1);
                                    if ((entity.Length > 1) && (entity[0] == '#'))
                                    {
                                        try
                                        {
                                            if ((entity[1] == 'x') || (entity[1] == 'X'))
                                            {
                                                ch = (char)int.Parse(entity.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                                            }
                                            else
                                            {
                                                ch = (char)int.Parse(entity.Substring(1), CultureInfo.InvariantCulture);
                                            }
                                            i = num3;
                                        }
                                        catch (FormatException)
                                        {
                                            i++;
                                        }
                                        catch (ArgumentException)
                                        {
                                            i++;
                                        }
                                    }
                                    else
                                    {
                                        i = num3;
                                        char ch2 = HtmlEntities.Lookup(entity);
                                        if (ch2 != '\0')
                                        {
                                            ch = ch2;
                                        }
                                        else
                                        {
                                            output.Write('&');
                                            output.Write(entity);
                                            output.Write(';');
                                            goto Label_0153;
                                        }
                                    }
                                }
                            }
                            output.Write(ch);
                        Label_0153: ;
                        }
                    }
                }
            }

            // Nested Types
            private static class HtmlEntities
            {
                // Fields
                private static string[] _entitiesList = new string[] { 
                "\"-quot", "&-amp", "<-lt", ">-gt", "\x00a0-nbsp", "\x00a1-iexcl", "\x00a2-cent", "\x00a3-pound", "\x00a4-curren", "\x00a5-yen", "\x00a6-brvbar", "\x00a7-sect", "\x00a8-uml", "\x00a9-copy", "\x00aa-ordf", "\x00ab-laquo", 
                "\x00ac-not", "\x00ad-shy", "\x00ae-reg", "\x00af-macr", "\x00b0-deg", "\x00b1-plusmn", "\x00b2-sup2", "\x00b3-sup3", "\x00b4-acute", "\x00b5-micro", "\x00b6-para", "\x00b7-middot", "\x00b8-cedil", "\x00b9-sup1", "\x00ba-ordm", "\x00bb-raquo", 
                "\x00bc-frac14", "\x00bd-frac12", "\x00be-frac34", "\x00bf-iquest", "\x00c0-Agrave", "\x00c1-Aacute", "\x00c2-Acirc", "\x00c3-Atilde", "\x00c4-Auml", "\x00c5-Aring", "\x00c6-AElig", "\x00c7-Ccedil", "\x00c8-Egrave", "\x00c9-Eacute", "\x00ca-Ecirc", "\x00cb-Euml", 
                "\x00cc-Igrave", "\x00cd-Iacute", "\x00ce-Icirc", "\x00cf-Iuml", "\x00d0-ETH", "\x00d1-Ntilde", "\x00d2-Ograve", "\x00d3-Oacute", "\x00d4-Ocirc", "\x00d5-Otilde", "\x00d6-Ouml", "\x00d7-times", "\x00d8-Oslash", "\x00d9-Ugrave", "\x00da-Uacute", "\x00db-Ucirc", 
                "\x00dc-Uuml", "\x00dd-Yacute", "\x00de-THORN", "\x00df-szlig", "\x00e0-agrave", "\x00e1-aacute", "\x00e2-acirc", "\x00e3-atilde", "\x00e4-auml", "\x00e5-aring", "\x00e6-aelig", "\x00e7-ccedil", "\x00e8-egrave", "\x00e9-eacute", "\x00ea-ecirc", "\x00eb-euml", 
                "\x00ec-igrave", "\x00ed-iacute", "\x00ee-icirc", "\x00ef-iuml", "\x00f0-eth", "\x00f1-ntilde", "\x00f2-ograve", "\x00f3-oacute", "\x00f4-ocirc", "\x00f5-otilde", "\x00f6-ouml", "\x00f7-divide", "\x00f8-oslash", "\x00f9-ugrave", "\x00fa-uacute", "\x00fb-ucirc", 
                "\x00fc-uuml", "\x00fd-yacute", "\x00fe-thorn", "\x00ff-yuml", "≈í-OElig", "≈ì-oelig", "≈†-Scaron", "≈°-scaron", "≈∏-Yuml", "∆í-fnof", "ÀÜ-circ", "Àú-tilde", "Œë-Alpha", "Œí-Beta", "Œì-Gamma", "Œî-Delta", 
                "Œï-Epsilon", "Œñ-Zeta", "Œó-Eta", "Œò-Theta", "Œô-Iota", "Œö-Kappa", "Œõ-Lambda", "Œú-Mu", "Œù-Nu", "Œû-Xi", "Œü-Omicron", "Œ†-Pi", "Œ°-Rho", "Œ£-Sigma", "Œ§-Tau", "Œ•-Upsilon", 
                "Œ¶-Phi", "Œß-Chi", "Œ®-Psi", "Œ©-Omega", "Œ±-alpha", "Œ≤-beta", "Œ≥-gamma", "Œ¥-delta", "Œµ-epsilon", "Œ∂-zeta", "Œ∑-eta", "Œ∏-theta", "Œπ-iota", "Œ∫-kappa", "Œª-lambda", "Œº-mu", 
                "ŒΩ-nu", "Œæ-xi", "Œø-omicron", "œÄ-pi", "œÅ-rho", "œÇ-sigmaf", "œÉ-sigma", "œÑ-tau", "œÖ-upsilon", "œÜ-phi", "œá-chi", "œà-psi", "œâ-omega", "œë-thetasym", "œí-upsih", "œñ-piv", 
                "‚Ä?ensp", "‚Ä?emsp", "‚Ä?thinsp", "‚Ä?zwnj", "‚Ä?zwj", "‚Ä?lrm", "‚Ä?rlm", "‚Ä?ndash", "‚Ä?mdash", "‚Ä?lsquo", "‚Ä?rsquo", "‚Ä?sbquo", "‚Ä?ldquo", "‚Ä?rdquo", "‚Ä?bdquo", "‚Ä?dagger", 
                "‚Ä?Dagger", "‚Ä?bull", "‚Ä?hellip", "‚Ä?permil", "‚Ä?prime", "‚Ä?Prime", "‚Ä?lsaquo", "‚Ä?rsaquo", "‚Ä?oline", "‚Å?frasl", "‚Ç?euro", "‚Ñ?image", "‚Ñ?weierp", "‚Ñ?real", "‚Ñ?trade", "‚Ñ?alefsym", 
                "‚Ü?larr", "‚Ü?uarr", "‚Ü?rarr", "‚Ü?darr", "‚Ü?harr", "‚Ü?crarr", "‚á?lArr", "‚á?uArr", "‚á?rArr", "‚á?dArr", "‚á?hArr", "‚àÄ-forall", "‚à?part", "‚à?exist", "‚à?empty", "‚à?nabla", 
                "‚à?isin", "‚à?notin", "‚à?ni", "‚à?prod", "‚à?sum", "‚à?minus", "‚à?lowast", "‚à?radic", "‚à?prop", "‚à?infin", "‚à?ang", "‚à?and", "‚à?or", "‚à?cap", "‚à?cup", "‚à?int", 
                "‚à?there4", "‚à?sim", "‚â?cong", "‚â?asymp", "‚â?ne", "‚â?equiv", "‚â?le", "‚â?ge", "‚ä?sub", "‚ä?sup", "‚ä?nsub", "‚ä?sube", "‚ä?supe", "‚ä?oplus", "‚ä?otimes", "‚ä?perp", 
                "‚ã?sdot", "‚å?lceil", "‚å?rceil", "‚å?lfloor", "‚å?rfloor", "‚å?lang", "‚å?rang", "‚ó?loz", "‚ô?spades", "‚ô?clubs", "‚ô?hearts", "‚ô?diams"
             };
                private static Hashtable _entitiesLookupTable;
                private static object _lookupLockObject = new object();

                // Methods
                internal static char Lookup(string entity)
                {
                    if (_entitiesLookupTable == null)
                    {
                        lock (_lookupLockObject)
                        {
                            if (_entitiesLookupTable == null)
                            {
                                Hashtable hashtable = new Hashtable();
                                foreach (string str in _entitiesList)
                                {
                                    hashtable[str.Substring(2)] = str[0];
                                }
                                _entitiesLookupTable = hashtable;
                            }
                        }
                    }
                    object obj2 = _entitiesLookupTable[entity];
                    if (obj2 != null)
                    {
                        return (char)obj2;
                    }
                    return '\0';
                }
            }
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
        private enum XmlWriteState
        {
            None,
            EvelopeWritten,
            SpecialNSWritten,
            BeginWriteBody
        }

        private XmlWriteState state = XmlWriteState.None;

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

            if (localName == "Envelope" && state == XmlWriteState.None)
            {
                state = XmlWriteState.EvelopeWritten;

                if (state == XmlWriteState.EvelopeWritten)
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

                    state = XmlWriteState.SpecialNSWritten;
                }
            }

            if (localName == "Assertion" && state == XmlWriteState.SpecialNSWritten)
            {
                WriteAttributeString("xmlns", "saml", null, @"urn:oasis:names:tc:SAML:1.0:assertion");
            }

            if (localName == "Body" && state != XmlWriteState.BeginWriteBody)
            {
                state = XmlWriteState.BeginWriteBody;
            }
        }
    }
}
