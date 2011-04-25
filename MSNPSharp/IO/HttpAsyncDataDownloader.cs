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
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MSNPSharp.IO
{
    using MSNPSharp.Core;


    public class HttpAsyncDataDownloader
    {
        public static void BeginDownload(string URL, EventHandler<ObjectEventArgs> completeCallback, WebProxy proxy)
        {
            Thread httpRequestThread = new Thread(new ParameterizedThreadStart(DoDownload));
            httpRequestThread.Start(new object[] { URL, completeCallback, proxy });
        }

        private static void DoDownload(object param)
        {
            object[] paramlist = param as object[];

            string usertileURL = paramlist[0].ToString();
            EventHandler<ObjectEventArgs> callBack = paramlist[1] as EventHandler<ObjectEventArgs>;
            WebProxy proxy = paramlist[2] as WebProxy;

            try
            {
                Uri uri = new Uri(usertileURL);

                HttpWebRequest fwr = (HttpWebRequest)WebRequest.Create(uri);

                // Don't override existing system wide proxy settings.
                if (proxy != null)
                {
                    fwr.Proxy = proxy;
                }

                fwr.Timeout = 10000;

                fwr.BeginGetResponse(delegate(IAsyncResult result)
                {
                    try
                    {
                        Stream stream = ((WebRequest)result.AsyncState).EndGetResponse(result).GetResponseStream();
                        MemoryStream ms = new MemoryStream();
                        byte[] data = new byte[8192];
                        int read;
                        while ((read = stream.Read(data, 0, data.Length)) > 0)
                        {
                            ms.Write(data, 0, read);
                        }
                        stream.Close();

                        if (callBack != null)
                        {
                            callBack(fwr, new ObjectEventArgs(ms.ToArray()));
                        }

                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[HttpDataDownloader] Error: " + ex.Message);
                    }

                }, fwr);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[HttpDataDownloader] Error: " + ex.Message);
            }
        }
    }
}
