#region
/*
Copyright (c) 2002-2012, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan, Chang Liu. 
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
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;
using MSNPSharp.LiveConnectAPI.Atom;
using System.Xml.Serialization;
using System.IO;
using System.Threading;

namespace MSNPSharp.LiveConnectAPI
{
    /// <summary>
    /// A wrapper of Windows Live API (http://api.live.net)
    /// </summary>
    public class LiveAtomAPILight
    {
        private const string liveAPIBaseURL = @"http://api.live.net";
        private const string appID = "1275182653";

        internal static void UpdatePersonalStatusAsync(string newDisplayName, long ownerCID, string authToken,
            EventHandler<AtomRequestSucceedEventArgs> onSucceed,
            EventHandler<ExceptionEventArgs> onError)
        {
            Thread workingThread = new Thread(new ParameterizedThreadStart(doUpdate));
            workingThread.Start(new object[] { newDisplayName, ownerCID, authToken, onSucceed, onError });
        }

        private static void doUpdate(object paramArray)
        {
            object[] parameters = paramArray as object[];
            entryType returnEntry = null;
            long ownerCID = 0;
            try
            {
                string newDisplayName = parameters[0].ToString();
                ownerCID = (long)parameters[1];
                string authToken = parameters[2].ToString();
                returnEntry = LiveAtomAPILight.UpdatePersonalStatus(newDisplayName, ownerCID, authToken);
            }
            catch (Exception ex)
            {
                EventHandler<ExceptionEventArgs> onErrorHandler = parameters[4] as EventHandler<ExceptionEventArgs>;
                if (onErrorHandler != null)
                    onErrorHandler(ownerCID, new ExceptionEventArgs(ex));
            }

            EventHandler<AtomRequestSucceedEventArgs> onSuccessHandler = parameters[3] as EventHandler<AtomRequestSucceedEventArgs>;
            if (onSuccessHandler != null)
                onSuccessHandler(ownerCID, new AtomRequestSucceedEventArgs(returnEntry));
        }

        internal static entryType UpdatePersonalStatus(string newDisplayName, long ownerCID, string authToken)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(liveAPIBaseURL + "/Users({0})/Status".Replace("{0}", ownerCID.ToString()));
            request.Method = "POST";
            request.Accept = @"application/atom+xml; type=entry";
            request.ContentType = @"application/atom+xml";
            request.Headers.Add(HttpRequestHeader.Authorization, "WLID1.0 " + authToken);
            request.Headers.Add("AppId", appID);
            request.ServicePoint.Expect100Continue = false;

            entryType entry = new entryType();

            if (!string.IsNullOrEmpty(newDisplayName))
            {
                entry.title = new entryTypeTitle();
                entry.title.Value = newDisplayName;
            }

            XmlSerializer ser = new XmlSerializer(typeof(entryType));
            MemoryStream memStream = new MemoryStream();
            ser.Serialize(memStream, entry);
            string xmlString = Encoding.UTF8.GetString(memStream.ToArray());
            request.ContentLength = Encoding.UTF8.GetByteCount(xmlString);

            request.GetRequestStream().Write(Encoding.UTF8.GetBytes(xmlString), 0, Encoding.UTF8.GetByteCount(xmlString));


            entryType returnEntry = null;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CloseInput = true;

            XmlReader xmlReader = XmlReader.Create(request.GetResponse().GetResponseStream(), readerSettings);


            returnEntry = (entryType)ser.Deserialize(xmlReader);


            return returnEntry;

        }

    }
}
