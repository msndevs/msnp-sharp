#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Xml;
using System.Text;

namespace MSNPSharp.Services
{

    #region LogExtensionAttribute

    [AttributeUsage(AttributeTargets.Method)]
    public class LogExtensionAttribute : SoapExtensionAttribute
    {
        int _priority;

        public override Type ExtensionType
        {
            get
            {
                return typeof(SoapLogExtension);
            }
        }

        public override int Priority
        {
            get
            {
                return _priority;
            }
            set
            {
                _priority = value;
            }
        }
    }

    #endregion

    #region SoapLogExtension

    public class SoapLogExtension : SoapExtension
    {
        private Stream oldStream;
        private Stream newStream;

        public override void Initialize(object initializer)
        {
        }

        public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
        {
            return null;
        }

        public override object GetInitializer(Type serviceType)
        {
            return null;
        }

        public override Stream ChainStream(Stream stream)
        {
            oldStream = stream;
            newStream = new MemoryStream();
            return newStream;
        }

        public override void ProcessMessage(SoapMessage message)
        {
            switch (message.Stage)
            {
                case SoapMessageStage.BeforeSerialize:
                    break;

                case SoapMessageStage.AfterSerialize:
                    WriteOutput(message); //OUTPUT
                    break;

                case SoapMessageStage.BeforeDeserialize:
                    WriteInput(message); //INPUT
                    break;

                case SoapMessageStage.AfterDeserialize:
                    break;

                default:
                    throw new Exception("invalid stage");
            }
        }

        public void WriteOutput(SoapMessage message)
        {
            newStream.Position = 0;

            StreamReader reader = new StreamReader(newStream);
   
            if(!Settings.IsMono)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose && Settings.TraceSoap,
                    FormatXML(reader.ReadToEnd()),
                    GetType().Name + ":SOAP-REQUEST(" + message.Url + ")");
            }else{
                if(Settings.TraceSwitch.TraceVerbose && Settings.TraceSoap){
                    Console.WriteLine(GetType().Name + ":SOAP-REQUEST(" + message.Url + ") " + FormatXML(reader.ReadToEnd()));
                }
            }

            newStream.Position = 0;
            Copy(newStream, oldStream);
        }

        public void WriteInput(SoapMessage message)
        {
            Copy(oldStream, newStream);

            newStream.Position = 0;

            StreamReader reader = new StreamReader(newStream);
   
            if(!Settings.IsMono)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose && Settings.TraceSoap,
                    FormatXML(reader.ReadToEnd()),
                    GetType().Name + ":SOAP-RESPONSE(" + message.Url + ")");
            }else{
                if(Settings.TraceSwitch.TraceVerbose && Settings.TraceSoap){
                    Console.WriteLine(GetType().Name + ":SOAP-RESPONSE(" + message.Url + ") " + FormatXML(reader.ReadToEnd()));
                }
            }

            newStream.Position = 0;
        }

        private void Copy(Stream from, Stream to)
        {
            TextReader reader = new StreamReader(from);
            TextWriter writer = new StreamWriter(to);
            writer.WriteLine(reader.ReadToEnd());
            writer.Flush();
        }

        public static string FormatXML(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                using (StringWriter sw = new StringWriter())
                {
                    XmlTextWriter xtw = new XmlTextWriter(sw);
                    xtw.Formatting = System.Xml.Formatting.Indented;
                    doc.WriteTo(xtw);

                    return sw.ToString();
                }
            }
            catch
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Unable to format XML");
                return xml;
            }
        }
    }
    #endregion
};
