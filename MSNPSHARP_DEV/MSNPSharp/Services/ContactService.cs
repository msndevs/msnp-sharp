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
using System.Net;
using System.Xml;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.MSNWS.MSNDirectoryService;

    /// <summary>
    /// Provide webservice operations for contacts. This class cannot be inherited.
    /// </summary>
    public sealed partial class ContactService : MSNService
    {
        #region Fields

        private int recursiveCall;
        private string applicationId = String.Empty;
        private Dictionary<int, NSPayLoadMessage> initialADLs = new Dictionary<int, NSPayLoadMessage>();
        private bool abSynchronized;
        private int serviceADL = 0;

        internal XMLContactList AddressBook;
        internal DeltasList Deltas;

        #endregion

        public ContactService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
            applicationId = nsHandler.Credentials.ClientInfo.ApplicationId;

        }

        #region Events
        /// <summary>
        /// Occurs when a contact is added to any list (including reverse list)
        /// </summary>
        public event EventHandler<ListMutateEventArgs> ContactAdded;

        /// <summary>
        /// Occurs when a contact is removed from any list (including reverse list)
        /// </summary>
        public event EventHandler<ListMutateEventArgs> ContactRemoved;

        /// <summary>
        /// Occurs when another user adds us to their contactlist. A ContactAdded event with the reverse list as parameter will also be raised.
        /// </summary>
        public event EventHandler<ContactEventArgs> ReverseAdded;

        /// <summary>
        /// Occurs when another user removes us from their contactlist. A ContactRemoved event with the reverse list as parameter will also be raised.
        /// </summary>
        public event EventHandler<ContactEventArgs> ReverseRemoved;

        /// <summary>
        /// Occurs when a new contactgroup is created
        /// </summary>
        public event EventHandler<ContactGroupEventArgs> ContactGroupAdded;

        /// <summary>
        /// Occurs when a contactgroup is removed
        /// </summary>
        public event EventHandler<ContactGroupEventArgs> ContactGroupRemoved;

        /// <summary>
        /// Occurs when a new <see cref="Circle"/> is created.
        /// </summary>
        public event EventHandler<CircleEventArgs> CreateCircleCompleted;

        /// <summary>
        /// Occurs when the owner has left a specific <see cref="Circle"/>.
        /// </summary>
        public event EventHandler<CircleEventArgs> ExitCircleCompleted;

        /// <summary>
        /// Occurs when a call to SynchronizeList() has been made and the synchronization process is completed.
        /// This means all contact-updates are received from the server and processed.
        /// </summary>
        public event EventHandler<EventArgs> SynchronizationCompleted;

        /// <summary>
        /// Fired after the InviteContactToCircle succeeded.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> InviteCircleMemberCompleted;

        /// <summary>
        /// Fired after a circle member has left the circle.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> CircleMemberLeft;

        /// <summary>
        /// Fired after a circle member has joined the circle.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> CircleMemberJoined;

        /// <summary>
        /// Fired after a remote user invite us to join a circle.
        /// </summary>
        public event EventHandler<CircleEventArgs> JoinCircleInvitationReceived;

        /// <summary>
        /// Fired after the owner join a circle successfully.
        /// </summary>
        public event EventHandler<CircleEventArgs> JoinedCircleCompleted;
        #endregion

        #region Public members

        /// <summary>
        /// Fires the <see cref="ReverseRemoved"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnReverseRemoved(ContactEventArgs e)
        {
            if (ReverseRemoved != null)
                ReverseRemoved(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ReverseAdded"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnReverseAdded(ContactEventArgs e)
        {
            if (ReverseAdded != null)
                ReverseAdded(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ContactAdded"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactAdded(ListMutateEventArgs e)
        {
            if (ContactAdded != null)
            {
                ContactAdded(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactRemoved"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactRemoved(ListMutateEventArgs e)
        {
            if (ContactRemoved != null)
            {
                ContactRemoved(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactGroupAdded"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactGroupAdded(ContactGroupEventArgs e)
        {
            if (ContactGroupAdded != null)
            {
                ContactGroupAdded(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="JoinCircleInvitationReceived"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnJoinCircleInvitationReceived(CircleEventArgs e)
        {
            //if (e.Inviter != null)
            //{
            //    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
            //        e.Inviter.Name + "(" + e.Inviter.Account + ") invite you to join circle: "
            //        + e.Circle.ToString() + "\r\nMessage: " + e.Inviter.Message);
            //}

            if (JoinCircleInvitationReceived != null)
            {
                JoinCircleInvitationReceived(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="JoinedCircleCompleted"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnJoinedCircleCompleted(CircleEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Circle invitation accepted: " + e.Circle.ToString());

            if (JoinedCircleCompleted != null)
            {
                JoinedCircleCompleted(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactGroupRemoved"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactGroupRemoved(ContactGroupEventArgs e)
        {
            if (ContactGroupRemoved != null)
            {
                ContactGroupRemoved(this, e);
            }
        }


        /// <summary>
        /// Fires the <see cref="CreateCircleCompleted"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnCreateCircleCompleted(CircleEventArgs e)
        {
            if (CreateCircleCompleted != null)
            {
                CreateCircleCompleted(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="ExitCircleCompleted"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnExitCircleCompleted(CircleEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Exit circle completed: " + e.Circle.ToString());

            if (ExitCircleCompleted != null)
            {
                ExitCircleCompleted(this, e);
            }
        }

        /// <summary> 
        /// Fires the <see cref="CircleMemberLeft"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnCircleMemberLeft(CircleMemberEventArgs e)
        {
            if (CircleMemberLeft != null)
            {
                CircleMemberLeft(this, e);
            }
        }

        /// <summary> 
        /// Fires the <see cref="CircleMemberJoined"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnCircleMemberJoined(CircleMemberEventArgs e)
        {
            if (CircleMemberJoined != null)
            {
                CircleMemberJoined(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="SynchronizationCompleted"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnSynchronizationCompleted(EventArgs e)
        {
            if (SynchronizationCompleted != null)
                SynchronizationCompleted(this, e);
        }

        /// <summary>
        /// Fires the <see cref="InviteCircleMemberCompleted"/>
        /// </summary>
        /// <param name="e"></param>
        private void OnInviteCircleMemberCompleted(CircleMemberEventArgs e)
        {
            if (InviteCircleMemberCompleted != null)
                InviteCircleMemberCompleted(this, e);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Keep track whether a address book synchronization has been completed.
        /// </summary>
        public bool AddressBookSynchronized
        {
            get
            {
                return abSynchronized;
            }
        }

        #endregion

        #region Synchronize

        /// <summary>
        /// Rebuild the contactlist with the most recent data.
        /// </summary>
        /// <remarks>
        /// Synchronizing is the most complete way to retrieve data about groups, contacts, privacy settings, etc.
        /// This method is called automatically after owner profile received and then the addressbook is merged with deltas file.
        /// After that, SignedIn event occurs and the client programmer must set it's initial status by SetPresenceStatus(). 
        /// Otherwise you won't receive online notifications from other clients or the connection is closed by the server.
        /// If you have an external contact list, you must track ProfileReceived, SignedIn and SynchronizationCompleted events.
        /// Between ProfileReceived and SignedIn: the internal addressbook is merged with deltas file.
        /// Between SignedIn and SynchronizationCompleted: the internal addressbook is merged with most recent data by soap request.
        /// All contact changes will be fired between ProfileReceived, SignedIn and SynchronizationCompleted events. 
        /// e.g: ContactAdded, ContactRemoved, ReverseAdded, ReverseRemoved.
        /// </remarks>
        internal void SynchronizeContactList()
        {
            if (AddressBookSynchronized)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "SynchronizeContactList() was called, but the list has already been synchronized.", GetType().Name);
                return;
            }

            if (recursiveCall != 0)
            {
                DeleteRecordFile();
            }

            MclSerialization st = Settings.SerializationType;
            string addressbookFile = Path.Combine(Settings.SavePath, NSMessageHandler.ContactList.Owner.Account.GetHashCode() + ".mcl");
            string deltasResultsFile = Path.Combine(Settings.SavePath, NSMessageHandler.ContactList.Owner.Account.GetHashCode() + "d" + ".mcl");

            try
            {
                AddressBook = XMLContactList.LoadFromFile(addressbookFile, st, NSMessageHandler, false);
                Deltas = DeltasList.LoadFromFile(deltasResultsFile, st, NSMessageHandler, true);

                NSMessageHandler.MSNTicket.CacheKeys = Deltas.CacheKeys;

                if (NSMessageHandler.AutoSynchronize &&
                    recursiveCall == 0 &&
                    (AddressBook.Version != Properties.Resources.XMLContactListVersion || Deltas.Version != Properties.Resources.DeltasListVersion))
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "AddressBook Version not match:\r\nMCL AddressBook Version: " +
                        AddressBook.Version.ToString() + "\r\nAddressBook Version Required:\r\nContactListVersion " +
                        Properties.Resources.XMLContactListVersion + ", DeltasList Version " +
                        Properties.Resources.DeltasListVersion +
                        "\r\nThe old mcl files for this account will be deleted and a new request for getting addressbook list will be post.");

                    recursiveCall++;
                    SynchronizeContactList();
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "An error occured while getting addressbook: " + ex.Message +
                    "\r\nA new request for getting addressbook list will be post again.", GetType().Name);

                recursiveCall++;
                SynchronizeContactList();
                return;
            }

            if (NSMessageHandler.AutoSynchronize)
            {
                AddressBook.Initialize();

                if (WebServiceDateTimeConverter.ConvertToDateTime(AddressBook.GetAddressBookLastChange(WebServiceConstants.MessengerIndividualAddressBookId)) == DateTime.MinValue)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Getting your membership list for the first time. If you have a lot of contacts, please be patient!", GetType().Name);
                }
                msRequest(
                    PartnerScenario.Initial,
                    delegate
                    {
                        if (WebServiceDateTimeConverter.ConvertToDateTime(AddressBook.GetAddressBookLastChange(WebServiceConstants.MessengerIndividualAddressBookId)) == DateTime.MinValue)
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Getting your address book for the first time. If you have a lot of contacts, please be patient!", GetType().Name);
                        }
                        abRequest(PartnerScenario.Initial,
                            delegate
                            {
                                SetDefaults();
                            }
                        );
                    }
                );
            }
            else
            {
                // Set lastchanged and roaming profile last change to get display picture and personal message
                NSMessageHandler.ContactService.AddressBook.MyProperties[AnnotationNames.Live_Profile_Expression_LastChanged] = XmlConvert.ToString(DateTime.MinValue, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz");                SetDefaults();
                NSMessageHandler.OnSignedIn(EventArgs.Empty);
            }
        }

        private void SetDefaults()
        {
            // Reset
            recursiveCall = 0;

            if (NSMessageHandler.AutoSynchronize)
            {
                AddressBook.InitializeMyProperties();
            }

            Deltas.Profile = NSMessageHandler.StorageService.GetProfile();

            // Set display name, personal status and photo
            string mydispName = String.IsNullOrEmpty(Deltas.Profile.DisplayName) ? NSMessageHandler.ContactList.Owner.NickName : Deltas.Profile.DisplayName;
            PersonalMessage pm = new PersonalMessage(Deltas.Profile.PersonalMessage, MediaType.None, null, NSMessageHandler.MachineGuid);

            NSMessageHandler.ContactList.Owner.SetName(mydispName);
            NSMessageHandler.ContactList.Owner.SetPersonalMessage(pm);
            NSMessageHandler.ContactList.Owner.CreateDefaultDisplayImage(Deltas.Profile.Photo.DisplayImage);
            NSMessageHandler.ContactList.Owner.SetColorScheme(System.Drawing.ColorTranslator.FromOle(Deltas.Profile.ColorScheme));

            if (NSMessageHandler.AutoSynchronize)
            {
                #region Initial ADL

                SendInitialADL(Scenario.SendServiceADL | Scenario.SendInitialContactsADL | Scenario.SendInitialCirclesADL);

                #endregion
            }

            // Save addressbook and then truncate deltas file.
            AddressBook.Save();
            Deltas.Truncate();
        }

        /// <summary>
        /// The indicator of whether the initial contact ADL has been processed. <br/>
        /// If the contact ADL was not processed, ignore the circle ADL.
        /// </summary>
        bool contactADLProcessed = false;
        Scenario ignoredSenario = Scenario.None;

        /// <summary>
        /// Send the initial ADL command to NS server. 
        /// </summary>
        /// <param name="scene">
        /// A <see cref="Scenario"/>
        /// </param>
        /// <remarks>
        /// The first ADL command MUST be a contact ADL. If you send a circle ADL instead,
        /// you will receive 201 server error for the following circle PUT command.
        /// </remarks>
        internal void SendInitialADL(Scenario scene)
        {
            if (scene == Scenario.None)
                return;

            NSMessageProcessor nsmp = (NSMessageProcessor)NSMessageHandler.MessageProcessor;

            if (nsmp == null)
                return;

            int firstADLKey = 0;
            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>();

            #region Service ADL

            if ((scene & Scenario.SendServiceADL) != Scenario.None)
            {
                string[] ownerAccount = NSMessageHandler.ContactList.Owner.Account.Split('@');
                string payload = "<ml><d n=\"" + ownerAccount[1] + "\"><c n=\"" + ownerAccount[0] + "\" t=\"1\"><s l=\"3\" n=\"IM\" /><s l=\"3\" n=\"PE\" /><s l=\"3\" n=\"PD\" /><s l=\"3\" n=\"PF\"/></c></d></ml>";

                serviceADL = nsmp.IncreaseTransactionID();
                NSPayLoadMessage nsPayload = new NSPayLoadMessage("ADL", payload);
                nsPayload.TransactionID = serviceADL;
                nsmp.SendMessage(nsPayload, serviceADL);

                ignoredSenario |= Scenario.SendServiceADL;
            }

            #endregion
            
            #region Process Contacts

            if ((scene & Scenario.SendInitialContactsADL) != Scenario.None)
            {
                // Combine initial ADL for Contacts
                hashlist = new Dictionary<string, RoleLists>(NSMessageHandler.ContactList.Count);
                lock (NSMessageHandler.ContactList.SyncRoot)
                {
                    foreach (Contact contact in NSMessageHandler.ContactList.All)
                    {
                        if (contact.ADLCount == 0)
                            continue;

                        contact.ADLCount--;

                        string ch = contact.Hash;
                        RoleLists l = RoleLists.None;
                        if (contact.IsMessengerUser)
                            l |= RoleLists.Forward;
                        if (contact.OnAllowedList)
                            l |= RoleLists.Allow;
                        if (contact.AppearOffline)
                            l |= RoleLists.Hide;

                        if (l != RoleLists.None && !hashlist.ContainsKey(ch))
                            hashlist.Add(ch, l);
                    }
                }
                string[] adls = ConstructLists(hashlist, true);

                if (adls.Length > 0)
                {
                    foreach (string payload in adls)
                    {
                        NSPayLoadMessage message = new NSPayLoadMessage("ADL", payload);
                        message.TransactionID = nsmp.IncreaseTransactionID();
                        initialADLs.Add(message.TransactionID, message);

                        if (firstADLKey == 0)
                        {
                            firstADLKey = message.TransactionID;
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "#################### first contact ADL trasID: " +
                                firstADLKey + " ############################");
                        }
                    }
                }
                contactADLProcessed = true;
                scene |= ignoredSenario;
            }

            #endregion

            #region Process Circles

            if ((scene & Scenario.SendInitialCirclesADL) != Scenario.None)
            {
                if (contactADLProcessed)
                {
                    // Combine initial ADL for Circles
                    if (NSMessageHandler.CircleList.Count > 0)
                    {
                        hashlist = new Dictionary<string, RoleLists>(NSMessageHandler.CircleList.Count);
                        lock (NSMessageHandler.ContactList.SyncRoot)
                        {
                            foreach (Circle circle in NSMessageHandler.CircleList.Values)
                            {
                                if (circle.ADLCount == 0)
                                    continue;

                                circle.ADLCount--;
                                string ch = circle.Hash;
                                RoleLists l = circle.Lists;
                                hashlist.Add(ch, l);
                            }
                        }

                        string[] circleadls = ConstructLists(hashlist, true);

                        if (circleadls.Length > 0)
                        {
                            foreach (string payload in circleadls)
                            {
                                NSPayLoadMessage message = new NSPayLoadMessage("ADL", payload);
                                message.TransactionID = nsmp.IncreaseTransactionID();
                                initialADLs.Add(message.TransactionID, message);

                                if (firstADLKey == 0)
                                {
                                    firstADLKey = message.TransactionID;
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                    "#################### first circle ADL trasID: " +
                                    firstADLKey + " ############################");
                                }
                            }
                        }
                    }
                }
                else
                {
                    ignoredSenario |= Scenario.SendInitialCirclesADL;
                }
            }

            #endregion

            // Send First Initial ADL,.
            // NSHandler doesn't accept more than 3 ADLs at the same time... So we must wait OK response.

            if (initialADLs.ContainsKey(firstADLKey))
            {
                NSPayLoadMessage firstADL = initialADLs[firstADLKey];
                nsmp.SendMessage(firstADL, firstADL.TransactionID);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "#################### ADL trasID choosen: " +
                                firstADLKey + " ############################");
            }
        }

        internal bool ProcessADL(int transid)
        {
            if (transid == serviceADL)
            {
                return true;
            }
            else if (initialADLs.ContainsKey(transid))
            {
                lock (this)
                {
                    initialADLs.Remove(transid);
                }

                if (initialADLs.Count <= 0)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "All initial ADLs have processed.", GetType().Name);

                    if (NSMessageHandler.AutoSynchronize)
                    {
                        NSMessageHandler.OnSignedIn(EventArgs.Empty);
                    }

                    if (!AddressBookSynchronized)
                    {
                        abSynchronized = true;
                        OnSynchronizationCompleted(EventArgs.Empty);

                        if (NSMessageHandler.AutoSynchronize)
                        {
                            lock (NSMessageHandler.ContactList.SyncRoot)
                            {
                                foreach (Contact contact in NSMessageHandler.ContactList.All)
                                {
                                    if (contact.OnPendingList)
                                    {
                                        // Added by other place, this place hasn't synchronized this contact yet.
                                        if (contact.OnForwardList && contact.OnPendingList)
                                        {
                                            contact.OnPendingList = false;
                                        }
                                        else
                                        {
                                            NSMessageHandler.ContactService.OnReverseAdded(new ContactEventArgs(contact));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Send next ADL...
                    foreach (NSPayLoadMessage nsPayload in initialADLs.Values)
                    {
                        ((NSMessageProcessor)NSMessageHandler.MessageProcessor).SendMessage(nsPayload, nsPayload.TransactionID);
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Async membership request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="onSuccess">The delegate to be executed after async membership request completed successfuly</param>
        internal void msRequest(PartnerScenario partnerScenario, FindMembershipCompletedEventHandler onSuccess)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindMembership", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                bool msdeltasOnly = false;
                DateTime serviceLastChange = WebServiceDateTimeConverter.ConvertToDateTime(WebServiceConstants.ZeroTime);
                DateTime msLastChange = WebServiceDateTimeConverter.ConvertToDateTime(AddressBook.MembershipLastChange);

                string strLastChange = WebServiceConstants.ZeroTime;

                if (msLastChange != serviceLastChange)
                {
                    msdeltasOnly = true;

                    strLastChange = AddressBook.MembershipLastChange;
                }

                MsnServiceState FindMembershipObject = new MsnServiceState(partnerScenario, "FindMembership", true);
                SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, FindMembershipObject);
                FindMembershipRequestType request = new FindMembershipRequestType();
                request.View = "Full";  // NO default!
                request.deltasOnly = msdeltasOnly;
                request.lastChange = strLastChange;
                request.serviceFilter = new FindMembershipRequestTypeServiceFilter();
                request.serviceFilter.Types = new string[]
                {
                    ServiceFilterType.Messenger,
                    ServiceFilterType.IMAvailability
                    /*,ServiceFilterType.Profile,
                    ServiceFilterType.SocialNetwork,
                    ServiceFilterType.Invitation,
                    ServiceFilterType.Folder,
                    ServiceFilterType.OfficeLiveWebNotification*/
                };

                sharingService.FindMembershipCompleted += delegate(object sender, FindMembershipCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    if (!e.Cancelled)
                    {
                        if (e.Error != null)
                        {
                            if (e.Error.Message.Contains("Address Book Does Not Exist"))
                            {
                                if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
                                {
                                    OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindMembership", new MSNPSharpException("You don't have access right on this action anymore.")));
                                }
                                else
                                {
                                    MsnServiceState ABAddObject = new MsnServiceState(partnerScenario, "ABAdd", true);
                                    ABServiceBinding abservice = (ABServiceBinding)CreateService(MsnServiceType.AB, ABAddObject);
                                    abservice.ABAddCompleted += delegate(object srv, ABAddCompletedEventArgs abadd_e)
                                    {
                                        OnAfterCompleted(new ServiceOperationEventArgs(abservice, MsnServiceType.AB, abadd_e));

                                        if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                                            return;

                                        if (abadd_e.Error == null)
                                        {
                                            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "A new addressbook has been added, addressbook list will be request again.");
                                            recursiveCall++;
                                            SynchronizeContactList();
                                        }
                                    };
                                    ABAddRequestType abAddRequest = new ABAddRequestType();
                                    abAddRequest.abInfo = new abInfoType();
                                    abAddRequest.abInfo.ownerEmail = NSMessageHandler.ContactList.Owner.Account;
                                    abAddRequest.abInfo.ownerPuid = "0";
                                    abAddRequest.abInfo.fDefault = true;

                                    RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abservice, MsnServiceType.AB, ABAddObject, abAddRequest));
                                }
                            }
                            else if ((recursiveCall == 0 && partnerScenario == PartnerScenario.Initial)
                                || (e.Error.Message.Contains("Need to do full sync")))
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                    "Need to do full sync of current addressbook list, addressbook list will be request again. Method: FindMemberShip");
                                recursiveCall++;
                                SynchronizeContactList();
                            }
                            else
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.ToString(), GetType().Name);
                            }
                        }
                        else
                        {
                            if (null != e.Result.FindMembershipResult)
                            {
                                AddressBook.Merge(e.Result.FindMembershipResult);
                                AddressBook.Save();
                            }
                            if (onSuccess != null)
                            {
                                onSuccess(sharingService, e);
                            }
                        }
                    }
                };

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, FindMembershipObject, request));
            }
        }

        /// <summary>
        /// Async Address book request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="onSuccess">The delegate to be executed after async ab request completed successfuly</param>
        internal void abRequest(PartnerScenario partnerScenario, ABFindContactsPagedCompletedEventHandler onSuccess)
        {
            abRequest(partnerScenario, null, onSuccess);
        }

        /// <summary>
        /// Async Address book request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="abHandle">The specified addressbook to retrieve.</param>
        /// <param name="onSuccess">The delegate to be executed after async ab request completed successfuly</param>
        internal void abRequest(PartnerScenario partnerScenario, abHandleType abHandle, ABFindContactsPagedCompletedEventHandler onSuccess)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABFindContactsPaged", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                bool deltasOnly = false;

                MsnServiceState ABFindContactsPagedObject = new MsnServiceState(partnerScenario, "ABFindContactsPaged", true);
                ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABFindContactsPagedObject);
                ABFindContactsPagedRequestType request = new ABFindContactsPagedRequestType();
                request.abView = "MessengerClient8";  //NO default!

                if (abHandle == null || abHandle.ABId == WebServiceConstants.MessengerIndividualAddressBookId)
                {
                    request.extendedContent = "AB AllGroups CircleResult";

                    request.filterOptions = new filterOptionsType();
                    request.filterOptions.ContactFilter = new ContactFilterType();

                    if (WebServiceDateTimeConverter.ConvertToDateTime(AddressBook.GetAddressBookLastChange(WebServiceConstants.MessengerIndividualAddressBookId)) != DateTime.MinValue)
                    {
                        deltasOnly = true;
                        request.filterOptions.LastChanged = AddressBook.GetAddressBookLastChange(WebServiceConstants.MessengerIndividualAddressBookId);
                    }

                    request.filterOptions.DeltasOnly = deltasOnly;
                    request.filterOptions.ContactFilter.IncludeHiddenContacts = true;
                }
                else
                {
                    request.extendedContent = "AB";
                    request.abHandle = abHandle;
                }

                abService.ABFindContactsPagedCompleted += delegate(object sender, ABFindContactsPagedCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    if (!e.Cancelled)
                    {
                        if (e.Error != null)
                        {
                            if ((recursiveCall == 0 && ((MsnServiceState)e.UserState).PartnerScenario == PartnerScenario.Initial
                                && (abHandle == null || abHandle.ABId == WebServiceConstants.MessengerIndividualAddressBookId))
                                || (e.Error.Message.Contains("Need to do full sync")))
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                    "Need to do full sync of current addressbook list, addressbook list will be request again. Method: ABFindContactsPaged");

                                recursiveCall++;

                                AddressBook.ClearCircleInfos();

                                SynchronizeContactList();
                            }
                            else
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.ToString(), GetType().Name);
                            }
                        }
                        else
                        {
                            if (null != e.Result.ABFindContactsPagedResult)
                            {
                                string lowerId = WebServiceConstants.MessengerIndividualAddressBookId;

                                if (e.Result.ABFindContactsPagedResult.Ab != null)
                                    lowerId = e.Result.ABFindContactsPagedResult.Ab.abId.ToLowerInvariant();

                                if (lowerId == WebServiceConstants.MessengerIndividualAddressBookId)
                                {
                                    AddressBook.MergeIndividualAddressBook(e.Result.ABFindContactsPagedResult);
                                }
                                else
                                {
                                    AddressBook.MergeGroupAddressBook(e.Result.ABFindContactsPagedResult);
                                }

                                AddressBook.Save();

                                if (e.Result.ABFindContactsPagedResult.CircleResult != null)
                                    NSMessageHandler.SendSHAAMessage(e.Result.ABFindContactsPagedResult.CircleResult.CircleTicket);
                            }

                            if (onSuccess != null)
                            {
                                onSuccess(abService, e);
                            }
                        }
                    }
                };

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABFindContactsPagedObject, request));
            }
        }

        public static string[] ConstructLists(Dictionary<string, RoleLists> contacts, bool initial)
        {

            List<string> mls = new List<string>();
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement mlElement = xmlDoc.CreateElement("ml");
            if (initial)
                mlElement.SetAttribute("l", "1");

            if (contacts == null || contacts.Count == 0)
            {
                mls.Add(mlElement.OuterXml);
                return mls.ToArray();
            }

            List<string> sortedContacts = new List<string>(contacts.Keys);
            sortedContacts.Sort(CompareContactsHash);

            int domaincontactcount = 0;
            string currentDomain = null;
            XmlElement domtelElement = null;

            foreach (string contact_hash in sortedContacts)
            {
                String name;
                String domain;
                string[] arr = contact_hash.Split(new string[] { ":", ";via=" }, StringSplitOptions.RemoveEmptyEntries);
                String type = IMAddressInfoType.Yahoo.ToString();
                if (arr.Length > 0)
                    type = arr[0];

                IMAddressInfoType clitype = (IMAddressInfoType)Enum.Parse(typeof(IMAddressInfoType), type);
                type = ((int)clitype).ToString();
                RoleLists sendlist = Contact.GetListForADL(contacts[contact_hash]);

                if (clitype == IMAddressInfoType.Telephone)
                {
                    domain = String.Empty;
                    name = "tel:" + arr[1];
                }
                else if (clitype == IMAddressInfoType.RemoteNetwork)
                {
                    domain = String.Empty;
                    name = arr[1];
                }
                else
                {
                    String[] usernameanddomain = arr[1].Split('@');
                    domain = usernameanddomain[1];
                    name = usernameanddomain[0];
                }

                if (sendlist != RoleLists.None)
                {
                    if (currentDomain != domain)
                    {
                        currentDomain = domain;
                        domaincontactcount = 0;

                        if (clitype == IMAddressInfoType.Telephone)
                        {
                            domtelElement = xmlDoc.CreateElement("t");
                        }
                        else if (clitype == IMAddressInfoType.RemoteNetwork)
                        {
                            domtelElement = xmlDoc.CreateElement("n");
                        }
                        else
                        {
                            domtelElement = xmlDoc.CreateElement("d");
                            domtelElement.SetAttribute("n", currentDomain);
                        }
                        mlElement.AppendChild(domtelElement);
                    }

                    XmlElement contactElement = xmlDoc.CreateElement("c");
                    contactElement.SetAttribute("n", name);

                    if (clitype != IMAddressInfoType.Telephone && clitype != IMAddressInfoType.RemoteNetwork)
                        contactElement.SetAttribute("t", type);

                    foreach (string s in new string[] { ServiceShortNames.IM.ToString() })
                    {
                        XmlElement service = xmlDoc.CreateElement("s");
                        service.SetAttribute("l", ((int)sendlist).ToString());
                        service.SetAttribute("n", s);

                        contactElement.AppendChild(service);
                    }

                    domtelElement.AppendChild(contactElement);
                    domaincontactcount++;

                }

                if (mlElement.OuterXml.Length > 7300)
                {
                    mlElement.AppendChild(domtelElement);
                    mls.Add(mlElement.OuterXml);

                    mlElement = xmlDoc.CreateElement("ml");
                    if (initial)
                        mlElement.SetAttribute("l", "1");

                    currentDomain = null;
                    domaincontactcount = 0;
                }
            }

            if (domaincontactcount > 0 && domtelElement != null)
                mlElement.AppendChild(domtelElement);

            mls.Add(mlElement.OuterXml);
            return mls.ToArray();
        }

        private static int CompareContactsHash(string hash1, string hash2)
        {
            string[] str_arr1 = hash1.Split(new string[] { ":", ";via=" }, StringSplitOptions.RemoveEmptyEntries);
            string[] str_arr2 = hash2.Split(new string[] { ":", ";via=" }, StringSplitOptions.RemoveEmptyEntries);

            if (str_arr1.Length == 0)
                return 1;

            else if (str_arr2.Length == 0)
                return -1;

            string xContact, yContact;

            if (str_arr1[1].IndexOf("@") == -1)
                xContact = str_arr1[1];
            else
                xContact = str_arr1[1].Substring(str_arr1[1].IndexOf("@") + 1);

            if (str_arr2[1].IndexOf("@") == -1)
                yContact = str_arr2[1];
            else
                yContact = str_arr2[1].Substring(str_arr2[1].IndexOf("@") + 1);

            return String.Compare(xContact, yContact, true, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Contact & Group Operations

        #region Add Contact

        private void CreateContactAndManageWLConnection(string account, IMAddressInfoType network, string invitation)
        {
            CreateContactAsync(
                account,
                network,
                Guid.Empty,
                delegate(object service, CreateContactCompletedEventArgs cce)
                {
                    // Get windows live contact (yes)
                    Contact contact = NSMessageHandler.ContactList.GetContactWithCreate(account, IMAddressInfoType.WindowsLive);
                    contact.Guid = new Guid(cce.Result.CreateContactResult.contactId);

                    if (network == IMAddressInfoType.Telephone)
                        return;

                    ManageWLConnectionAsync(
                        contact.Guid, Guid.Empty, invitation,
                        true, true, 1, RelationshipTypes.IndividualAddressBook, (int)RelationshipState.None,
                        delegate(object wlcSender, ManageWLConnectionCompletedEventArgs mwlce)
                        {
                            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>(2);
                            hashlist.Add(contact.Hash, RoleLists.Allow | RoleLists.Forward);
                            string payload = ConstructLists(hashlist, false)[0];
                            NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));

                            // Get all contacts and send ADL for each contact... Yahoo, Facebook etc.
                            abRequest(PartnerScenario.ContactSave,
                                delegate
                                {
                                    msRequest(PartnerScenario.ContactSave,
                                        delegate
                                        {
                                            List<IMAddressInfoType> typesFound = new List<IMAddressInfoType>();
                                            IMAddressInfoType[] addressTypes = (IMAddressInfoType[])Enum.GetValues(typeof(IMAddressInfoType));

                                            foreach (IMAddressInfoType at in addressTypes)
                                            {
                                                if (NSMessageHandler.ContactList.HasContact(account, at))
                                                {
                                                    typesFound.Add(at);
                                                }
                                            }

                                            if (typesFound.Count > 0)
                                            {
                                                hashlist = new Dictionary<string, RoleLists>(2);

                                                foreach (IMAddressInfoType im in typesFound)
                                                {
                                                    hashlist.Add(Contact.MakeHash(account, im), RoleLists.Allow | RoleLists.Forward);
                                                }

                                                payload = ConstructLists(hashlist, false)[0];
                                                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                                            }
                                        });
                                });
                        });
                });
        }        

        /// <summary>
        /// Creates a new contact on your address book and adds to allowed list if not blocked before.
        /// </summary>
        /// <param name="account">An email address or phone number to add. The email address can be yahoo account.</param>
        /// <remarks>The phone format is +CC1234567890 for phone contact, CC is Country Code</remarks>
        public void AddNewContact(string account)
        {
            AddNewContact(account, String.Empty);
        }

        /// <summary>
        /// Creates a new contact on your address book and adds to allowed list if not blocked before.
        /// </summary>
        /// <param name="account">An email address or phone number to add. The email address can be yahoo account.</param>
        /// <param name="invitation">The reason of the adding contact</param>
        /// <remarks>The phone format is +CC1234567890, CC is Country Code</remarks>
        public void AddNewContact(string account, string invitation)
        {
            long test;
            if (long.TryParse(account, out test) ||
                (account.StartsWith("+") && long.TryParse(account.Substring(1), out test)))
            {
                if (account.StartsWith("00"))
                {
                    account = "+" + account.Substring(2);
                }
                AddNewContact(account, IMAddressInfoType.Telephone, invitation);
            }
            else
            {
                AddNewContact(account, IMAddressInfoType.WindowsLive, invitation);
            }
        }

        internal void AddNewContact(string account, IMAddressInfoType network, string invitation)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddContact", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            CreateContactAndManageWLConnection(account, network, invitation);
        }

        #endregion

        #region RemoveContact

        public void RemoveContact(Contact contact, bool block)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABContactDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            contact.OnAllowedList = false;

            if (contact.Guid == Guid.Empty)
                return;

            BreakConnectionAsync(contact.Guid, Guid.Empty, block, true,
                delegate(object sender, BreakConnectionCompletedEventArgs e)
                {
                    if (e.Error != null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Break connection: " + contact.Guid.ToString("D") + " failed, error: " + e.Error.Message);
                        return;
                    }

                    abRequest(PartnerScenario.ContactSave,
                        delegate
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Delete contact :" + contact.Hash + " completed.");
                        });
                });
        }

        /// <summary>
        /// Remove the specified contact from your forward list.
        /// </summary>
        /// <param name="contact">Contact to remove</param>
        public void RemoveContact(Contact contact)
        {
            RemoveContact(contact, false);
        }

        #endregion

        #region UpdateContact

        internal void UpdateContact(Contact contact, Guid abId, ABContactUpdateCompletedEventHandler onSuccess)
        {
            UpdateContact(contact, abId.ToString("D"), onSuccess);
        }

        internal void UpdateContact(Contact contact, string abId, ABContactUpdateCompletedEventHandler onSuccess)
        {
            string lowerId = abId.ToLowerInvariant();

            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABContactUpdate", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            if (!AddressBook.HasContact(lowerId, contact.Guid))
                return;

            ContactType abContactType = AddressBook.SelectContactFromAddressBook(lowerId, contact.Guid);
            ContactType contactToChange = new ContactType();

            List<string> propertiesChanged = new List<string>();

            contactToChange.contactId = contact.Guid.ToString();
            contactToChange.contactInfo = new contactInfoType();

            // Comment
            if (abContactType.contactInfo.comment != contact.Comment)
            {
                propertiesChanged.Add(PropertyString.Comment);
                contactToChange.contactInfo.comment = contact.Comment;
            }

            // DisplayName
            if (abContactType.contactInfo.displayName != contact.Name)
            {
                propertiesChanged.Add(PropertyString.DisplayName);
                contactToChange.contactInfo.displayName = contact.Name;
            }

            //HasSpace
            if (abContactType.contactInfo.hasSpace != contact.HasSpace && abContactType.contactInfo.hasSpaceSpecified)
            {
                propertiesChanged.Add(PropertyString.HasSpace);
                contactToChange.contactInfo.hasSpace = contact.HasSpace;
            }

            // Annotations
            List<Annotation> annotationsChanged = new List<Annotation>();
            Dictionary<string, string> oldAnnotations = new Dictionary<string, string>();
            if (abContactType.contactInfo.annotations != null)
            {
                foreach (Annotation anno in abContactType.contactInfo.annotations)
                {
                    oldAnnotations[anno.Name] = anno.Value;
                }
            }

            // Annotations: AB.NickName
            string oldNickName = oldAnnotations.ContainsKey(AnnotationNames.AB_NickName) ? oldAnnotations[AnnotationNames.AB_NickName] : String.Empty;
            if (oldNickName != contact.NickName)
            {
                Annotation anno = new Annotation();
                anno.Name = AnnotationNames.AB_NickName;
                anno.Value = contact.NickName;
                annotationsChanged.Add(anno);
            }

            if (annotationsChanged.Count > 0)
            {
                propertiesChanged.Add(PropertyString.Annotation);
                contactToChange.contactInfo.annotations = annotationsChanged.ToArray();
            }


            // ClientType changes
            switch (contact.ClientType)
            {
                case IMAddressInfoType.WindowsLive:
                    {
                        // IsMessengerUser
                        if (abContactType.contactInfo.isMessengerUser != contact.IsMessengerUser)
                        {
                            propertiesChanged.Add(PropertyString.IsMessengerUser);
                            contactToChange.contactInfo.isMessengerUser = contact.IsMessengerUser;
                            contactToChange.contactInfo.isMessengerUserSpecified = true;
                            propertiesChanged.Add(PropertyString.MessengerMemberInfo); // Pang found WLM2009 add this.
                            contactToChange.contactInfo.MessengerMemberInfo = new MessengerMemberInfo(); // But forgot to add this...
                            contactToChange.contactInfo.MessengerMemberInfo.DisplayName = NSMessageHandler.ContactList.Owner.Name; // and also this :)
                        }

                        // ContactType
                        if (abContactType.contactInfo.contactType != contact.ContactType)
                        {
                            propertiesChanged.Add(PropertyString.ContactType);
                            contactToChange.contactInfo.contactType = contact.ContactType;
                        }
                    }
                    break;

                case IMAddressInfoType.Yahoo:
                    {
                        if (abContactType.contactInfo.emails != null)
                        {
                            foreach (contactEmailType em in abContactType.contactInfo.emails)
                            {
                                if (em.email.ToLowerInvariant() == contact.Account.ToLowerInvariant() && em.isMessengerEnabled != contact.IsMessengerUser)
                                {
                                    propertiesChanged.Add(PropertyString.ContactEmail);
                                    contactToChange.contactInfo.emails = new contactEmailType[] { new contactEmailType() };
                                    contactToChange.contactInfo.emails[0].contactEmailType1 = ContactEmailTypeType.Messenger2;
                                    contactToChange.contactInfo.emails[0].isMessengerEnabled = contact.IsMessengerUser;
                                    contactToChange.contactInfo.emails[0].propertiesChanged = PropertyString.IsMessengerEnabled; //"IsMessengerEnabled";
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case IMAddressInfoType.Telephone:
                    {
                        if (abContactType.contactInfo.phones != null)
                        {
                            foreach (contactPhoneType ph in abContactType.contactInfo.phones)
                            {
                                if (ph.number == contact.Account && ph.isMessengerEnabled != contact.IsMessengerUser)
                                {
                                    propertiesChanged.Add(PropertyString.ContactPhone);
                                    contactToChange.contactInfo.phones = new contactPhoneType[] { new contactPhoneType() };
                                    contactToChange.contactInfo.phones[0].contactPhoneType1 = ContactPhoneTypes.ContactPhoneMobile;
                                    contactToChange.contactInfo.phones[0].isMessengerEnabled = contact.IsMessengerUser;
                                    contactToChange.contactInfo.phones[0].propertiesChanged = PropertyString.IsMessengerEnabled; //"IsMessengerEnabled";
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }

            if (propertiesChanged.Count > 0)
            {
                contactToChange.propertiesChanged = String.Join(PropertyString.propertySeparator, propertiesChanged.ToArray());
                UpdateContact(contactToChange, WebServiceConstants.MessengerIndividualAddressBookId, onSuccess);
            }
        }

        private void UpdateContact(ContactType contact, string abId, ABContactUpdateCompletedEventHandler onSuccess)
        {
            ABContactUpdateRequestType request = new ABContactUpdateRequestType();
            request.abId = abId;
            request.contacts = new ContactType[] { contact };
            request.options = new ABContactUpdateRequestTypeOptions();
            request.options.EnableAllowListManagementSpecified = true;
            request.options.EnableAllowListManagement = true;

            MsnServiceState ABContactUpdateObject = new MsnServiceState(contact.contactInfo.isMessengerUser ? PartnerScenario.ContactSave : PartnerScenario.Timer, "ABContactUpdate", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABContactUpdateObject);
            abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled && e.Error == null)
                {
                    abRequest(PartnerScenario.ContactSave, delegate
                    {
                        if (onSuccess != null)
                            onSuccess(service, e);
                    }
                    );
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABContactUpdateObject, request));

        }

        #endregion

        #region AddContactGroup & RemoveContactGroup & RenameGroup

        /// <summary>
        /// Send a request to the server to add a new contactgroup.
        /// </summary>
        /// <param name="groupName">The name of the group to add</param>
        public void AddContactGroup(string groupName)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupAdd", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState ABGroupAddObject = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupAdd", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupAddObject);
            abService.ABGroupAddCompleted += delegate(object service, ABGroupAddCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled && e.Error == null)
                {
                    NSMessageHandler.ContactGroups.AddGroup(new ContactGroup(groupName, e.Result.ABGroupAddResult.guid, NSMessageHandler, false));
                    NSMessageHandler.ContactService.OnContactGroupAdded(new ContactGroupEventArgs((ContactGroup)NSMessageHandler.ContactGroups[e.Result.ABGroupAddResult.guid]));
                }
            };

            ABGroupAddRequestType request = new ABGroupAddRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupAddOptions = new ABGroupAddRequestTypeGroupAddOptions();
            request.groupAddOptions.fRenameOnMsgrConflict = false;
            request.groupAddOptions.fRenameOnMsgrConflictSpecified = true;
            request.groupInfo = new ABGroupAddRequestTypeGroupInfo();
            request.groupInfo.GroupInfo = new groupInfoType();
            request.groupInfo.GroupInfo.name = groupName;
            request.groupInfo.GroupInfo.fMessenger = false;
            request.groupInfo.GroupInfo.fMessengerSpecified = true;
            request.groupInfo.GroupInfo.groupType = WebServiceConstants.MessengerGroupType;
            request.groupInfo.GroupInfo.annotations = new Annotation[] { new Annotation() };
            request.groupInfo.GroupInfo.annotations[0].Name = AnnotationNames.MSN_IM_Display;
            request.groupInfo.GroupInfo.annotations[0].Value = "1";

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupAddObject, request));
        }

        /// <summary>
        /// Send a request to the server to remove a contactgroup. Any contacts in the group will also be removed from the forward list.
        /// </summary>
        /// <param name="contactGroup">The group to remove</param>
        public void RemoveContactGroup(ContactGroup contactGroup)
        {
            foreach (Contact cnt in NSMessageHandler.ContactList.All)
            {
                if (cnt.ContactGroups.Contains(contactGroup))
                {
                    throw new InvalidOperationException("Target group not empty, please remove all contacts form the group first.");
                }
            }

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState ABGroupDeleteObject = new MsnServiceState(PartnerScenario.Timer, "ABGroupDelete", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupDeleteObject);
            abService.ABGroupDeleteCompleted += delegate(object service, ABGroupDeleteCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled && e.Error == null)
                {
                    NSMessageHandler.ContactGroups.RemoveGroup(contactGroup);
                    AddressBook.Groups.Remove(new Guid(contactGroup.Guid));
                    NSMessageHandler.ContactService.OnContactGroupRemoved(new ContactGroupEventArgs(contactGroup));
                }
            };

            ABGroupDeleteRequestType request = new ABGroupDeleteRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { contactGroup.Guid };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupDeleteObject, request));
        }


        /// <summary>
        /// Set the name of a contact group
        /// </summary>
        /// <param name="group">The contactgroup which name will be set</param>
        /// <param name="newGroupName">The new name</param>
        public void RenameGroup(ContactGroup group, string newGroupName)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupUpdate", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState ABGroupUpdateObject = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupUpdate", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupUpdateObject);
            abService.ABGroupUpdateCompleted += delegate(object service, ABGroupUpdateCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled && e.Error == null)
                {
                    group.SetName(newGroupName);
                }
            };

            ABGroupUpdateRequestType request = new ABGroupUpdateRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groups = new GroupType[1] { new GroupType() };
            request.groups[0].groupId = group.Guid;
            request.groups[0].propertiesChanged = PropertyString.GroupName; //"GroupName";
            request.groups[0].groupInfo = new groupInfoType();
            request.groups[0].groupInfo.name = newGroupName;

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupUpdateObject, request));
        }

        #endregion

        #region AddContactToGroup & RemoveContactFromGroup

        public void AddContactToFavoriteGroup(Contact contact)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            ContactGroup favGroup = NSMessageHandler.ContactGroups.FavoriteGroup;

            if (favGroup != null && contact.HasGroup(favGroup) == false)
                AddContactToGroup(contact, favGroup);
            else
                throw new InvalidOperationException("No favorite group");
        }

        public void AddContactToGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactAdd", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState ABGroupContactAddObject = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupContactAdd", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupContactAddObject);
            abService.ABGroupContactAddCompleted += delegate(object service, ABGroupContactAddCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled && e.Error == null)
                {
                    contact.AddContactToGroup(group);
                }
            };

            ABGroupContactAddRequestType request = new ABGroupContactAddRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupContactAddObject, request));
        }

        public void RemoveContactFromFavoriteGroup(Contact contact)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            ContactGroup favGroup = NSMessageHandler.ContactGroups.FavoriteGroup;

            if (favGroup != null && contact.HasGroup(favGroup))
                RemoveContactFromGroup(contact, favGroup);
            else
                throw new InvalidOperationException("No favorite group");
        }

        public void RemoveContactFromGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState ABGroupContactDelete = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupContactDelete", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupContactDelete);
            abService.ABGroupContactDeleteCompleted += delegate(object service, ABGroupContactDeleteCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled && e.Error == null)
                {
                    contact.RemoveContactFromGroup(group);
                }
            };

            ABGroupContactDeleteRequestType request = new ABGroupContactDeleteRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupContactDelete, request));
        }
        #endregion

        #region AddContactToList

        /// <summary>
        /// Send a request to the server to add this contact to a specific list.
        /// </summary>
        /// <param name="contact">The affected contact</param>
        /// <param name="list">The list to place the contact in</param>
        /// <param name="onSuccess"></param>
        internal void AddContactToList(Contact contact, RoleLists list, EventHandler onSuccess)
        {
            if (list == RoleLists.Pending) //this causes disconnect 
                return;

            // check whether the update is necessary
            if (contact.HasLists(list))
                return;

            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>(2);
            hashlist.Add(contact.Hash, list);
            string payload = ConstructLists(hashlist, false)[0];

            if (list == RoleLists.Forward)
            {
                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                contact.AddToList(list);

                if (onSuccess != null)
                {
                    onSuccess(this, EventArgs.Empty);
                }

                return;
            }

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState AddMemberObject = new MsnServiceState((list == RoleLists.Reverse) ? PartnerScenario.ContactMsgrAPI : PartnerScenario.BlockUnblock, "AddMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, AddMemberObject);

            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.SelectTargetService(ServiceFilterType.Messenger);
            addMemberRequest.serviceHandle.Id = messengerService.Id.ToString();
            addMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = GetMemberRole(list);
            BaseMember member = new BaseMember();

            if (contact.ClientType == IMAddressInfoType.WindowsLive)
            {
                member = new PassportMember();
                PassportMember passportMember = member as PassportMember;
                passportMember.PassportName = contact.Account;
                passportMember.State = MemberState.Accepted;
                passportMember.Type = MembershipType.Passport;
            }
            else if (contact.ClientType == IMAddressInfoType.Yahoo)
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.State = MemberState.Accepted;
                emailMember.Type = MembershipType.Email;
                emailMember.Email = contact.Account;
                emailMember.Annotations = new Annotation[] { new Annotation() };
                emailMember.Annotations[0].Name = AnnotationNames.MSN_IM_BuddyType;
                emailMember.Annotations[0].Value = "32:";
            }
            else if (contact.ClientType == IMAddressInfoType.Telephone)
            {
                member = new PhoneMember();
                PhoneMember phoneMember = member as PhoneMember;
                phoneMember.State = MemberState.Accepted;
                phoneMember.Type = MembershipType.Phone;
                phoneMember.PhoneNumber = contact.Account;
            }
            else if (contact.ClientType == IMAddressInfoType.Circle)
            {
                member = new CircleMember();
                CircleMember circleMember = member as CircleMember;
                circleMember.Type =  MembershipType.Circle;
                circleMember.State = MemberState.Accepted;
                circleMember.CircleId = (contact as Circle).AddressBookId.ToString("D").ToLowerInvariant();
            }

            memberShip.Members = new BaseMember[] { member };
            addMemberRequest.memberships = new Membership[] { memberShip };

            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member already exists"))
                    {
                        return;
                    }

                    contact.AddToList(list);
                    AddressBook.AddMemberhip(ServiceFilterType.Messenger, contact.Account, contact.ClientType, GetMemberRole(list), member, Scenario.ContactServeAPI);
                    NSMessageHandler.ContactService.OnContactAdded(new ListMutateEventArgs(contact, list));

                    if ((list & RoleLists.Allow) == RoleLists.Allow || (list & RoleLists.Block) == RoleLists.Block)
                    {
                        NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "AddMember completed: " + list, GetType().Name);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, AddMemberObject, addMemberRequest));
        }

        #endregion

        #region RemoveContactFromList

        /// <summary>
        /// Send a request to the server to remove a contact from a specific list.
        /// </summary> 
        /// <param name="contact">The affected contact</param>
        /// <param name="list">The list to remove the contact from</param>
        /// <param name="onSuccess"></param>
        internal void RemoveContactFromList(Contact contact, RoleLists list, EventHandler onSuccess)
        {
            if (list == RoleLists.Reverse) //this causes disconnect
                return;

            // check whether the update is necessary
            if (!contact.HasLists(list))
                return;

            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>(2);
            hashlist.Add(contact.Hash, list);
            string payload = ConstructLists(hashlist, false)[0];

            if (list == RoleLists.Forward)
            {
                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("RML", payload));
                contact.RemoveFromList(list);

                if (onSuccess != null)
                {
                    onSuccess(this, EventArgs.Empty);
                }
                return;
            }

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("DeleteMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState DeleteMemberObject = new MsnServiceState((list == RoleLists.Pending) ? PartnerScenario.ContactMsgrAPI : PartnerScenario.BlockUnblock, "DeleteMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, DeleteMemberObject);
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member does not exist"))
                    {
                        return;
                    }

                    contact.RemoveFromList(list);
                    AddressBook.RemoveMemberhip(ServiceFilterType.Messenger, contact.Account, contact.ClientType, GetMemberRole(list), Scenario.ContactServeAPI);
                    NSMessageHandler.ContactService.OnContactRemoved(new ListMutateEventArgs(contact, list));

                    if ((list & RoleLists.Allow) == RoleLists.Allow || (list & RoleLists.Block) == RoleLists.Block)
                    {
                        NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("RML", payload));
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "DeleteMember completed: " + list, GetType().Name);
                }
            };

            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.SelectTargetService(ServiceFilterType.Messenger);
            deleteMemberRequest.serviceHandle.Id = messengerService.Id.ToString();   //Always set to 0 ??
            deleteMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = GetMemberRole(list);

            BaseMember deleteMember = null; // BaseMember is an abstract type, so we cannot create a new instance.
            // If we have a MembershipId different from 0, just use it. Otherwise, use email or phone number. 
            BaseMember baseMember = AddressBook.SelectBaseMember(ServiceFilterType.Messenger, contact.Account, contact.ClientType, GetMemberRole(list));
            int membershipId = (baseMember == null || String.IsNullOrEmpty(baseMember.MembershipId)) ? 0 : int.Parse(baseMember.MembershipId);

            switch (contact.ClientType)
            {
                case IMAddressInfoType.WindowsLive:

                    deleteMember = new PassportMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Passport : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as PassportMember).PassportName = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Yahoo:

                    deleteMember = new EmailMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Email : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as EmailMember).Email = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Telephone:

                    deleteMember = new PhoneMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Phone : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as PhoneMember).PhoneNumber = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Circle:
                    deleteMember = new CircleMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Circle : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    (deleteMember as CircleMember).CircleId = (contact as Circle).AddressBookId.ToString("D").ToLowerInvariant();
                    break;
            }

            deleteMember.MembershipId = membershipId.ToString();

            memberShip.Members = new BaseMember[] { deleteMember };
            deleteMemberRequest.memberships = new Membership[] { memberShip };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, DeleteMemberObject, deleteMemberRequest));
        }

        #endregion

        #region AppearOnline

        internal void AppearOnline(Contact contact, EventHandler onSuccess)
        {
            // check whether the update is necessary
            if (!contact.HasLists(RoleLists.Hide))
                return;

            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>(2);
            hashlist.Add(contact.Hash, RoleLists.Hide);
            string payload = ConstructLists(hashlist, false)[0];

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AppearOnline", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState DeleteMemberObject = new MsnServiceState(PartnerScenario.BlockUnblock, "DeleteMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, DeleteMemberObject);
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member does not exist"))
                    {
                        return;
                    }

                    contact.RemoveFromList(RoleLists.Hide);
                    AddressBook.RemoveMemberhip(ServiceFilterType.IMAvailability, contact.Account, contact.ClientType, GetMemberRole(RoleLists.Hide), Scenario.ContactServeAPI);
                    NSMessageHandler.ContactService.OnContactRemoved(new ListMutateEventArgs(contact, RoleLists.Hide));

                    NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("RML", payload));

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "DeleteMember completed: " + RoleLists.Hide, GetType().Name);
                }
            };

            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();

            Service imAvailabilityService = AddressBook.SelectTargetService(ServiceFilterType.IMAvailability);

            if (imAvailabilityService == null)
            {
                AddServiceAsync(ServiceFilterType.IMAvailability,
                    delegate
                    {
                        // RESURSIVE CALL
                        AppearOnline(contact, onSuccess);
                    });
                return;
            }

            deleteMemberRequest.serviceHandle.Id = imAvailabilityService.Id.ToString();
            deleteMemberRequest.serviceHandle.Type = ServiceFilterType.IMAvailability;

            Membership memberShip = new Membership();
            memberShip.MemberRole = GetMemberRole(RoleLists.Hide);

            BaseMember deleteMember = null; // BaseMember is an abstract type, so we cannot create a new instance.
            // If we have a MembershipId different from 0, just use it. Otherwise, use email or phone number. 
            BaseMember baseMember = AddressBook.SelectBaseMember(ServiceFilterType.IMAvailability, contact.Account, contact.ClientType, GetMemberRole(RoleLists.Hide));
            int membershipId = (baseMember == null || String.IsNullOrEmpty(baseMember.MembershipId)) ? 0 : int.Parse(baseMember.MembershipId);

            switch (contact.ClientType)
            {
                case IMAddressInfoType.WindowsLive:

                    deleteMember = new PassportMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Passport : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as PassportMember).PassportName = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Yahoo:

                    deleteMember = new EmailMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Email : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as EmailMember).Email = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Telephone:

                    deleteMember = new PhoneMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Phone : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as PhoneMember).PhoneNumber = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Circle:

                    deleteMember = new CircleMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Circle : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as CircleMember).CircleId = contact.AddressBookId.ToString("D");
                    }
                    break;
            }

            deleteMember.MembershipId = membershipId.ToString();

            memberShip.Members = new BaseMember[] { deleteMember };
            deleteMemberRequest.memberships = new Membership[] { memberShip };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, DeleteMemberObject, deleteMemberRequest));
        }


        internal void AppearOffline(Contact contact, EventHandler onSuccess)
        {
            // check whether the update is necessary
            if (contact.HasLists(RoleLists.Hide))
                return;

            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>(2);
            hashlist.Add(contact.Hash, RoleLists.Hide);
            string payload = ConstructLists(hashlist, false)[0];

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AppearOffline", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState AddMemberObject = new MsnServiceState(PartnerScenario.BlockUnblock, "AddMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, AddMemberObject);

            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();

            Service imAvailabilityService = AddressBook.SelectTargetService(ServiceFilterType.IMAvailability);

            if (imAvailabilityService == null)
            {
                AddServiceAsync(ServiceFilterType.IMAvailability,
                    delegate
                    {
                        // RESURSIVE CALL
                        AppearOffline(contact, onSuccess);
                    });
                return;
            }

            addMemberRequest.serviceHandle.Id = imAvailabilityService.Id.ToString();
            addMemberRequest.serviceHandle.Type = ServiceFilterType.IMAvailability;

            Membership memberShip = new Membership();
            memberShip.MemberRole = RoleLists.Hide.ToString();
            BaseMember member = new BaseMember();

            if (contact.ClientType == IMAddressInfoType.WindowsLive)
            {
                member = new PassportMember();
                PassportMember passportMember = member as PassportMember;
                passportMember.PassportName = contact.Account;
                passportMember.State = MemberState.Accepted;
                passportMember.Type = MembershipType.Passport;
            }
            else if (contact.ClientType == IMAddressInfoType.Yahoo)
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.State = MemberState.Accepted;
                emailMember.Type = MembershipType.Email;
                emailMember.Email = contact.Account;
                emailMember.Annotations = new Annotation[] { new Annotation() };
                emailMember.Annotations[0].Name = AnnotationNames.MSN_IM_BuddyType;
                emailMember.Annotations[0].Value = "32:";
            }
            else if (contact.ClientType == IMAddressInfoType.Telephone)
            {
                member = new PhoneMember();
                PhoneMember phoneMember = member as PhoneMember;
                phoneMember.State = MemberState.Accepted;
                phoneMember.Type = MembershipType.Phone;
                phoneMember.PhoneNumber = contact.Account;
            }
            else if (contact.ClientType == IMAddressInfoType.Circle)
            {
                member = new CircleMember();
                CircleMember circleMember = member as CircleMember;
                circleMember.State = MemberState.Accepted;
                circleMember.Type = MembershipType.Circle;
                circleMember.CircleId = contact.AddressBookId.ToString("D");
            }

            memberShip.Members = new BaseMember[] { member };
            addMemberRequest.memberships = new Membership[] { memberShip };

            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member already exists"))
                    {
                        return;
                    }

                    contact.AddToList(RoleLists.Hide);
                    AddressBook.AddMemberhip(ServiceFilterType.IMAvailability, contact.Account, contact.ClientType, GetMemberRole(RoleLists.Hide), member, Scenario.ContactServeAPI);
                    NSMessageHandler.ContactService.OnContactAdded(new ListMutateEventArgs(contact, RoleLists.Hide));

                    NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "AddMember completed: " + RoleLists.Hide, GetType().Name);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, AddMemberObject, addMemberRequest));
        }

        #endregion

        #region Create Circle

        /// <summary>
        /// Use specific name to create a new <see cref="Circle"/>. <see cref="CreateCircleCompleted"/> event will be fired after creation succeeded.
        /// </summary>
        /// <param name="circleName">New circle name.</param>
        public void CreateCircle(string circleName)
        {
            MsnServiceState createCircleObject = new MsnServiceState(PartnerScenario.CircleSave, "CreateCircle", true);
            string addressBookId = string.Empty;

            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, createCircleObject);
            CreateCircleRequestType request = new CreateCircleRequestType();
            request.callerInfo = new callerInfoType();
            request.callerInfo.PublicDisplayName = NSMessageHandler.ContactList.Owner.Name == string.Empty ? NSMessageHandler.ContactList.Owner.Account : NSMessageHandler.ContactList.Owner.Name;

            //This is M$ style, you will never guess out the meaning of these numbers.
            ContentInfoType properties = new ContentInfoType();
            properties.Domain = 1;
            properties.HostedDomain = Circle.DefaultHostDomain;
            properties.Type = 2;
            properties.MembershipAccess = 0;
            properties.IsPresenceEnabled = true;
            properties.RequestMembershipOption = 2;
            properties.DisplayName = circleName;

            request.properties = properties;
            sharingService.CreateCircleCompleted += delegate(object sender, CreateCircleCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (e.Error != null)
                    {
                        return;
                    }

                    abRequest(PartnerScenario.JoinedCircleDuringPush,
                        delegate
                        {
                            lock (AddressBook.PendingCreateCircleList)
                            {
                                AddressBook.PendingCreateCircleList[new Guid(e.Result.CreateCircleResult.Id)] = circleName;
                            }
                        }
                     );
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, createCircleObject, request));
        }

        #endregion

        #region Invite/Reject/Accept/Leave circle

        /// <summary>
        /// Send and invitition to a specific contact to invite it join a <see cref="Circle"/>.
        /// </summary>
        /// <param name="circle">Circle to join.</param>
        /// <param name="contact">Contact being invited.</param>
        public void InviteCircleMember(Circle circle, Contact contact)
        {
            InviteCircleMember(circle, contact, string.Empty);
        }

        /// <summary>
        /// Send and invitition to a specific contact to invite it join a <see cref="Circle"/>. A message will send with the invitition.
        /// </summary>
        /// <param name="circle">Circle to join.</param>
        /// <param name="contact">Contact being invited.</param>
        /// <param name="message">Message send with the invitition email.</param>
        /// <exception cref="ArgumentNullException">One or more parameter(s) is/are null.</exception>
        /// <exception cref="InvalidOperationException">The owner is not the circle admin or the circle is blocked.</exception>
        public void InviteCircleMember(Circle circle, Contact contact, string message)
        {
            if (circle == null || contact == null || message == null)
            {
                throw new ArgumentNullException();
            }

            if (circle.AppearOffline)
            {
                throw new InvalidOperationException("Circle is on your block list.");
            }

            if (circle.CircleRole != CirclePersonalMembershipRole.Admin &&
                circle.CircleRole != CirclePersonalMembershipRole.AssistantAdmin)
            {
                throw new InvalidOperationException("The owner is not the administrator of this circle.");
            }

            if (contact == NSMessageHandler.ContactList.Owner)
                return;

            CreateContactAsync(contact.Account, circle.ClientType, circle.AddressBookId,
                delegate(object sender, CreateContactCompletedEventArgs createContactCompletedArg)
                {
                    if (createContactCompletedArg.Error != null)
                        return;

                    ManageWLConnectionAsync(
                        new Guid(createContactCompletedArg.Result.CreateContactResult.contactId),
                        circle.AddressBookId,
                        message,
                        true,
                        false,
                        1,
                        RelationshipTypes.CircleGroup,
                        (int)CirclePersonalMembershipRole.Member,
                        delegate(object wlcSender, ManageWLConnectionCompletedEventArgs e)
                        {
                            if (e.Error != null)
                                return;

                            if (e.Result.ManageWLConnectionResult.contactInfo.clientErrorData != null &&
                                e.Result.ManageWLConnectionResult.contactInfo.clientErrorData != string.Empty)
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Invite circle member encounted a servier side error: " +
                                    e.Result.ManageWLConnectionResult.contactInfo.clientErrorData);
                            }

                            OnInviteCircleMemberCompleted(new CircleMemberEventArgs(circle, contact));
                        });
                }
            );
        }

        /// <summary>
        /// Reject a join circle invitation.
        /// </summary>
        /// <param name="circle">Circle to  join.</param>
        public void RejectCircleInvitation(Circle circle)
        {
            if (circle == null)
                throw new ArgumentNullException("circle");

            ManageWLConnectionAsync(circle.Guid, Guid.Empty, String.Empty, true, false, 2,
                RelationshipTypes.CircleGroup, (int)CirclePersonalMembershipRole.None,
                delegate(object wlcSender, ManageWLConnectionCompletedEventArgs e)
                {
                    if (e.Error != null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[RejectCircleInvitation] Reject circle invitation failed: " + circle.ToString());
                    }
                });
        }

        internal void FireJoinCircleEvent(JoinCircleInvitationEventArgs arg)
        {
            if (arg == null)
                return;

            Circle circle = arg.Circle;

            if (NSMessageHandler.IsSignedIn)
            {
                abHandleType abHandle = new abHandleType();
                abHandle.ABId = circle.AddressBookId.ToString().ToLowerInvariant();
                abHandle.Cid = 0;
                abHandle.Puid = 0;

                abRequest(PartnerScenario.NewCircleDuringPull,
                    abHandle,
                    delegate
                    {
                        if (circle.CircleRole == CirclePersonalMembershipRole.StatePendingOutbound)
                        {
                            OnJoinCircleInvitationReceived(arg);
                        }
                    }
                );
            }
            else
            {
                OnJoinCircleInvitationReceived(arg);
            }
        }

        internal void ServerNotificationRequest(PartnerScenario scene, object[] parameters, ABFindContactsPagedCompletedEventHandler onSuccess)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Executing notify addressbook request, PartnerScenario: " + scene);

            switch (scene)
            {
                case PartnerScenario.ABChangeNotifyAlert:
                    msRequest(scene,
                        delegate
                        {
                            ABNotifyChangedSaveReuqest(scene, onSuccess);
                        }
                    );
                    break;

                case PartnerScenario.CircleIdAlert:
                    abHandleType abHandler = new abHandleType();
                    abHandler.Puid = 0;
                    abHandler.Cid = 0;
                    abHandler.ABId = parameters[0].ToString();

                    msRequest(PartnerScenario.MessengerPendingList,
                        delegate
                        {
                            ABNotifyChangedSaveReuqest(scene, onSuccess);
                        }
                    );
                    break;
            }

        }

        private void ABNotifyChangedSaveReuqest(PartnerScenario scene, ABFindContactsPagedCompletedEventHandler onSuccess)
        {
            abRequest(scene,
                      delegate(object sender, ABFindContactsPagedCompletedEventArgs e)
                      {
                          if (e.Cancelled || e.Error != null)
                          {
                              return;
                          }

                          if (e.Result.ABFindContactsPagedResult.Contacts == null
                              && e.Result.ABFindContactsPagedResult.Groups == null)
                          {
                              if (e.Result.ABFindContactsPagedResult.CircleResult == null
                                  || e.Result.ABFindContactsPagedResult.CircleResult.Circles == null)
                              {
                                  return;
                              }
                          }

                          AddressBook.Save();

                          if (onSuccess != null)
                              onSuccess(sender, e);
                      }
                      );
        }

        /// <summary>
        /// Accept the circle invitation.
        /// </summary>
        /// <param name="circle"></param>
        /// <exception cref="ArgumentNullException">The circle parameter is null.</exception>
        /// <exception cref="InvalidOperationException">The circle specified is not a pending circle.</exception>
        public void AcceptCircleInvitation(Circle circle)
        {
            if (circle == null)
                throw new ArgumentNullException("circle");

            if (circle.CircleRole != CirclePersonalMembershipRole.StatePendingOutbound)
                throw new InvalidOperationException("This is not a pending circle.");

            ManageWLConnectionAsync(circle.Guid, Guid.Empty, String.Empty, true, false, 1,
                RelationshipTypes.CircleGroup, (int)CirclePersonalMembershipRole.None,
                delegate(object wlcSender, ManageWLConnectionCompletedEventArgs e)
                {
                    if (e.Error != null)
                    {
                        return;
                    }

                    abRequest(PartnerScenario.JoinedCircleDuringPush, null);
                });
        }

        /// <summary>
        /// Leave the specific circle.
        /// </summary>
        /// <param name="circle"></param>
        /// <exception cref="ArgumentNullException">The circle parameter is null.</exception>
        public void ExitCircle(Circle circle)
        {
            if (circle == null)
                throw new ArgumentNullException("circle");

            Guid selfGuid = AddressBook.SelectSelfContactGuid(circle.AddressBookId.ToString("D"));
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Circle contactId: " + selfGuid);

            BreakConnectionAsync(selfGuid, circle.AddressBookId, false, true,
                delegate(object sender, BreakConnectionCompletedEventArgs e)
                {
                    if (e.Cancelled || e.Error != null)
                    {
                        if (e.Error != null)
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Exit circle: " + circle.AddressBookId.ToString("D") + " failed, error: " + e.Error.Message);
                        }
                        return;
                    }

                    NSMessageHandler.SendCircleNotifyRML(circle.AddressBookId, circle.HostDomain, circle.Lists, true);
                    AddressBook.RemoveCircle(circle.AddressBookId.ToString("D").ToLowerInvariant());
                    AddressBook.Save();
                });
        }

        #endregion

        #endregion

        #region Private

        private void CreateContactAsync(string account, IMAddressInfoType network, Guid abId,
            CreateContactCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("CreateContact", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState createContactObject = new MsnServiceState(abId == Guid.Empty ? PartnerScenario.ContactSave : PartnerScenario.CircleSave, "CreateContact", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, createContactObject);
            abService.CreateContactCompleted += delegate(object service, CreateContactCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    // No error, creates a new contact or returns existing.
                    if (callback != null)
                    {
                        callback(service, e);
                    }
                }
            };

            CreateContactType request = new CreateContactType();

            if (network == IMAddressInfoType.Telephone)
            {
                contactInfoType contactInfo = new contactInfoType();

                contactPhoneType cpt = new contactPhoneType();
                cpt.contactPhoneType1 = ContactPhoneTypes.ContactPhoneMobile;
                cpt.number = account;

                contactInfo.phones = new contactPhoneType[] { cpt };
                request.contactInfo = contactInfo;
            }
            else
            {
                contactHandleType contactHandle = new contactHandleType();
                contactHandle.Email = account;
                contactHandle.Cid = 0;
                contactHandle.Puid = 0;
                contactHandle.CircleId = WebServiceConstants.MessengerIndividualAddressBookId;
                request.contactHandle = contactHandle;
            }

            if (abId != Guid.Empty)
            {
                abHandleType abHandle = new abHandleType();
                abHandle.ABId = abId.ToString("D").ToLowerInvariant();
                abHandle.Puid = 0;
                abHandle.Cid = 0;

                request.abHandle = abHandle;
            }

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, createContactObject, request));
        }

        private void BreakConnectionAsync(Guid contactGuid, Guid abID, bool block, bool delete,
            BreakConnectionCompletedEventHandler callback)
        {
            MsnServiceState breakConnectionObject = new MsnServiceState(PartnerScenario.BlockUnblock, "BreakConnection", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, breakConnectionObject);

            abService.BreakConnectionCompleted += delegate(object sender, BreakConnectionCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (callback != null)
                    {
                        callback(sender, e);
                    }
                }
            };

            BreakConnectionRequestType breakconnRequest = new BreakConnectionRequestType();
            breakconnRequest.contactId = contactGuid.ToString("D");
            breakconnRequest.blockContact = block;
            breakconnRequest.deleteContact = delete;

            if (abID != Guid.Empty)
            {
                abHandleType handler = new abHandleType();
                handler.ABId = abID.ToString("D");
                handler.Cid = 0;
                handler.Puid = 0;

                breakconnRequest.abHandle = handler;
            }

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, breakConnectionObject, breakconnRequest));
        }

        private void ManageWLConnectionAsync(Guid contactGuid, Guid abID, string inviteMessage,
           bool connection, bool presence, int action, int relType, int relRole,
           ManageWLConnectionCompletedEventHandler callback)
        {
            MsnServiceState manageWLConnectionObject = new MsnServiceState(abID == Guid.Empty ? PartnerScenario.ContactSave : PartnerScenario.CircleInvite, "ManageWLConnection", true);
            ABServiceBinding abServiceBinding = (ABServiceBinding)CreateService(MsnServiceType.AB, manageWLConnectionObject);

            abServiceBinding.ManageWLConnectionCompleted += delegate(object wlcSender, ManageWLConnectionCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abServiceBinding, MsnServiceType.AB, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (callback != null)
                    {
                        callback(wlcSender, e);
                    }
                }
            };

            ManageWLConnectionRequestType wlconnectionRequest = new ManageWLConnectionRequestType();

            wlconnectionRequest.contactId = contactGuid.ToString("D");
            wlconnectionRequest.connection = connection;
            wlconnectionRequest.presence = presence;
            wlconnectionRequest.action = action;

            wlconnectionRequest.relationshipType = relType;
            wlconnectionRequest.relationshipRole = relRole;

            if (!String.IsNullOrEmpty(inviteMessage))
            {
                Annotation anno = new Annotation();
                anno.Name = AnnotationNames.MSN_IM_InviteMessage;
                anno.Value = inviteMessage;

                wlconnectionRequest.annotations = new Annotation[] { anno };
            }

            if (abID != Guid.Empty)
            {
                abHandleType abHandle = new abHandleType();
                abHandle.ABId = abID.ToString("D").ToLowerInvariant();
                abHandle.Puid = 0;
                abHandle.Cid = 0;

                wlconnectionRequest.abHandle = abHandle;
            }

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abServiceBinding, MsnServiceType.AB, manageWLConnectionObject, wlconnectionRequest));
        }

        private void AddServiceAsync(string serviceName, AddServiceCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddService", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                MsnServiceState addServiceObject = new MsnServiceState(PartnerScenario.CircleSave, "AddService", true);
                SharingServiceBinding abService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, addServiceObject);
                abService.AddServiceCompleted += delegate(object service, AddServiceCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.Sharing, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, 
                        serviceName + "=" +  e.Result.AddServiceResult + " created...");

                    // Update service membership...
                    msRequest(PartnerScenario.BlockUnblock,
                        delegate
                        {
                            if (callback != null)
                                callback(abService, e);
                        });
                };

                AddServiceRequestType r = new AddServiceRequestType();
                r.serviceInfo = new InfoType();
                r.serviceInfo.Handle = new HandleType();
                r.serviceInfo.Handle.Type = serviceName;
                r.serviceInfo.Handle.ForeignId = String.Empty;
                r.serviceInfo.InverseRequired = true;

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.Sharing, addServiceObject, r));
            }
        }

        #endregion 

        #region DeleteRecordFile

        /// <summary>
        /// Delete the record file that contains the contactlist of owner.
        /// </summary>
        public void DeleteRecordFile()
        {
            if (NSMessageHandler.ContactList.Owner != null && NSMessageHandler.ContactList.Owner.Account != null)
            {
                string addressbookFile = Path.Combine(Settings.SavePath, NSMessageHandler.ContactList.Owner.Account.GetHashCode() + ".mcl");
                if (File.Exists(addressbookFile))
                {
                    File.SetAttributes(addressbookFile, FileAttributes.Normal);  //By default, the file is hidden.
                    File.Delete(addressbookFile);
                }

                string deltasResultFile = Path.Combine(Settings.SavePath, NSMessageHandler.ContactList.Owner.Account.GetHashCode() + "d" + ".mcl");
                if (File.Exists(deltasResultFile))
                {
                    if (Deltas != null)
                    {
                        Deltas.Truncate();  //If we saved cachekey and preferred host in it, deltas can't be deleted.
                    }
                    else
                    {
                        File.SetAttributes(deltasResultFile, FileAttributes.Normal);  //By default, the file is hidden.
                        File.Delete(deltasResultFile);
                    }
                }
                abSynchronized = false;
            }
        }

        #endregion

        public string GetMemberRole(RoleLists list)
        {
            switch (list)
            {
                case RoleLists.Allow:
                    return MemberRole.Allow;

                case RoleLists.Block:
                    return MemberRole.Block;

                case RoleLists.Pending:
                    return MemberRole.Pending;

                case RoleLists.Reverse:
                    return MemberRole.Reverse;

                case RoleLists.Hide:
                    return MemberRole.Hide;
            }
            return MemberRole.ProfilePersonalContact;
        }

        public RoleLists GetMSNList(string memberRole)
        {
            switch (memberRole)
            {
                case MemberRole.Allow:
                    return RoleLists.Allow;
                case MemberRole.Block:
                    return RoleLists.Block;
                case MemberRole.Reverse:
                    return RoleLists.Reverse;
                case MemberRole.Pending:
                    return RoleLists.Pending;
                case MemberRole.Hide:
                    return RoleLists.Hide;
            }
            throw new MSNPSharpException("Unknown MemberRole type");
        }

        public override void Clear()
        {
            base.Clear();

            lock (this)
            {
                recursiveCall = 0;
                serviceADL = 0;
                initialADLs.Clear();

                // Last save for contact list files
                if (NSMessageHandler.IsSignedIn && AddressBook != null && Deltas != null)
                {
                    try
                    {
                        AddressBook.Save();
                        Deltas.Truncate();
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, error.Message, GetType().Name);
                    }
                }

                AddressBook = null;
                Deltas = null;
                abSynchronized = false;
            }
        }
    }
};