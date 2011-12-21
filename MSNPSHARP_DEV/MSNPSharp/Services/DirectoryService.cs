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
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNDirectoryService;

    public class MSNDirectoryService : MSNService
    {
        public MSNDirectoryService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
        }

        /// <summary>
        /// Gets core profile from directory service and fires <see cref="Contact.CoreProfileUpdated"/> event
        /// after async request completed.
        /// </summary>
        /// <param name="cid"></param>
        public void Get(long cid)
        {
            Get(cid, null, null);
        }

        /// <summary>
        /// Gets core profiles from directory service and fires <see cref="Contact.CoreProfileUpdated"/> event
        /// for each contact after async request completed.
        /// </summary>
        /// <param name="cids"></param>
        public void GetMany(long[] cids)
        {
            GetMany(cids, null, null);
        }

        public void Get(long cid, EventHandler<EventArgs> onSuccess, EventHandler<ExceptionEventArgs> onError)
        {
            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                MsnServiceState getObject = new MsnServiceState(PartnerScenario.Initial, "Get", true);
                DirectoryService dirService = (DirectoryService)CreateService(MsnServiceType.Directory, getObject);
                dirService.GetCompleted += delegate(object sender, GetCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(dirService, MsnServiceType.Directory, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    if (e.Cancelled)
                        return;

                    if (e.Error != null)
                    {
                        OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("Get", e.Error));

                        if (onError != null)
                            onError(sender, new ExceptionEventArgs(e.Error));
                        return;
                    }
                    
                    if (e.Result.GetResult != null)
                    {
                        if (e.Result.GetResult.View != null)
                        {
                            FindContactByCidAndFireCoreProfileUpdated(cid, e.Result.GetResult.View);
                        }
                        else
                        {
                            //No profile yet.

                        }
                        
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(sender, e);
                    }
                };

                IdType id = new IdType();
                id.Ns1 = "Cid";
                id.V1 = cid;

                id.Ns2 = "Unspecified";
                id.V2 = null;


                GetRequestType request = new GetRequestType();
                request.request = new GetRequestTypeRequest();
                request.request.ViewName = "WLX.DC.CoreProfile";
                request.request.Id = id;

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(dirService, MsnServiceType.Directory, getObject, request));
            }
        }

        public void GetMany(long[] cids, EventHandler<EventArgs> onSuccess, EventHandler<ExceptionEventArgs> onError)
        {
            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                MsnServiceState getManyObject = new MsnServiceState(PartnerScenario.Initial, "GetMany", true);
                DirectoryService dirService = (DirectoryService)CreateService(MsnServiceType.Directory, getManyObject);
                dirService.GetManyCompleted += delegate(object sender, GetManyCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(dirService, MsnServiceType.Directory, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    if (e.Cancelled)
                        return;

                    if (e.Error != null)
                    {
                        OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("Get", e.Error));
                        if (onError != null)
                            onError(sender, new ExceptionEventArgs(e.Error));
                        return;
                    }
                    
                    if (e.Result.GetManyResult != null)
                    {
                        GetManyResultType r = e.Result.GetManyResult;
                        for (int i = 0; i < r.Ids.Length; i++)
                        {
                            IdType idType = r.Ids[i];
                            if (idType != null && idType.Ns1 == "Cid")
                            {
                                long cid;
                                if (long.TryParse(idType.V1.ToString(), out cid) && cid != 0)
                                {
                                    FindContactByCidAndFireCoreProfileUpdated(cid, r.Views[i]);
                                }
                            }
                        }
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(sender, e);
                    }
                };

                List<IdType> ids = new List<IdType>(cids.Length);

                foreach (long cid in cids)
                {
                    IdType id = new IdType();
                    id.Ns1 = "Cid";
                    id.V1 = cid;

                    id.Ns2 = "Unspecified";
                    id.V2 = null;

                    ids.Add(id);
                }

                GetManyRequestType request = new GetManyRequestType();
                request.request = new GetManyRequestTypeRequest();
                request.request.ViewName = "WLX.DC.CoreProfile";
                request.request.Ids = ids.ToArray();

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(dirService, MsnServiceType.Directory, getManyObject, request));
            }
        }


        private void FindContactByCidAndFireCoreProfileUpdated(long cid, ViewType view)
        {
            Contact contact = (cid == NSMessageHandler.Owner.CID) ?
                NSMessageHandler.Owner : NSMessageHandler.ContactList.GetContactByCID(cid);

            if (contact != null)
            {
                lock (contact.SyncObject)
                {
                    foreach (AttributeType at in view.Attributes)
                    {
                        if (!String.IsNullOrEmpty(at.N))
                        {
                            contact.CoreProfile[at.N] = at.V;
                        }
                    }
                }

                contact.OnCoreProfileUpdated(EventArgs.Empty);
            }
        }
    }
};
