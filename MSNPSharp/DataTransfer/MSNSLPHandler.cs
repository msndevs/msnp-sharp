#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
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
using System.Text;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using System.Drawing;

    /// <summary>
    /// Defines the type of datatransfer for a MSNSLPHandler
    /// </summary>
    public enum DataTransferType
    {
        /// <summary>
        /// Unknown datatransfer type.
        /// </summary>
        Unknown,
        /// <summary>
        /// Filetransfer.
        /// </summary>
        File,
        /// <summary>
        /// Emoticon transfer.
        /// </summary>
        Emoticon,
        /// <summary>
        /// Displayimage transfer.
        /// </summary>
        DisplayImage,
        /// <summary>
        /// Activity invitation.
        /// </summary>
        Activity
    }

    /// <summary>
    /// Holds the property of activity such as AppID and activity name.
    /// </summary>
    public class ActivityInfo
    {
        private uint appID = 0;

        /// <summary>
        /// The AppID of activity.
        /// </summary>
        public uint AppID
        {
            get { return appID; }
        }

        private string activityName = string.Empty;

        /// <summary>
        /// The name of activity.
        /// </summary>
        public string ActivityName
        {
            get { return activityName; }
        }

        protected ActivityInfo()
        {
        }

        public ActivityInfo(string contextString)
        {
            try
            {
                byte[] byts = Convert.FromBase64String(contextString);
                string activityUrl = System.Text.Encoding.Unicode.GetString(byts);
                string[] activityProperties = activityUrl.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (activityProperties.Length >= 3)
                {
                    uint.TryParse(activityProperties[0], out appID);
                    activityName = activityProperties[2];
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "An error occurs while parsing activity context, error info: " +
                    ex.Message);
            }
        }

        public override string ToString()
        {
            return "Activity info: " + appID.ToString() + " name: " + activityName;
        }

    }

    /// <summary>
    /// Holds all properties for a single data transfer.
    /// </summary>
    [Serializable()]
    public class MSNSLPTransferProperties
    {
        protected MSNSLPTransferProperties()
        {
        }

        public MSNSLPTransferProperties(Contact local, Guid localEndPointID, Contact remote, Guid remoteEndPointID)
        {
            remoteContact = remote;
            localContact = local;
            localContactEndPointID = localEndPointID;
            remoteContactEndPointID = remoteEndPointID;

            transferStackVersion = JudgeP2PStackVersion(local, localContactEndPointID, remote, remoteContactEndPointID, false);
        }

        private P2PVersion transferStackVersion = P2PVersion.P2PV1;

        /// <summary>
        /// The transfer stack that transfer layer (P2PMessageSession) used for this data transfer.
        /// </summary>
        public P2PVersion TransferStackVersion
        {
            get 
            { 
                return transferStackVersion; 
            }
        }

        private string dataTypeGuid = string.Empty;

        internal string DataTypeGuid
        {
            get
            {
                return dataTypeGuid;
            }
            set
            {
                dataTypeGuid = value;
            }
        }

        private int sessionCloseState = (int)SessionCloseState.None;

        /// <summary>
        /// Indicates whether we should remove this transfer session from its transferlayer (P2PMessageSession).
        /// </summary>
        internal SessionCloseState SessionCloseState
        {
            get 
            { 
                return (SessionCloseState)sessionCloseState; 
            }

            set 
            { 
                sessionCloseState = (int)value; 
            }
        }

        /// <summary>
        /// The kind of data that will be transferred
        /// </summary>
        public DataTransferType DataType
        {
            get
            {
                return dataType;
            }
            set
            {
                dataType = value;
            }
        }

        /// <summary>
        /// </summary>
        private DataTransferType dataType = DataTransferType.Unknown;

        /// <summary>
        /// </summary>
        private bool remoteInvited = false;

        /// <summary>
        /// Defines whether the remote client has invited the transfer (true) or the local client has initiated the transfer (false).
        /// </summary>
        public bool RemoteInvited
        {
            get
            {
                return remoteInvited;
            }
            set
            {
                remoteInvited = value;
            }
        }

        /// <summary>
        /// The GUID used in the handshake message for direct connections
        /// </summary>
        public Guid Nonce
        {
            get
            {
                return nonce;
            }
            set
            {
                nonce = value;
            }
        }

        /// <summary>
        /// </summary>
        private Guid nonce = Guid.Empty;

        /// <summary>
        /// The branch last received in the message session
        /// </summary>
        public string LastBranch
        {
            get
            {
                return lastBranch;
            }
            set
            {
                lastBranch = value;
            }
        }

        /// <summary>
        /// </summary>
        private string lastBranch = Guid.Empty.ToString("B").ToUpper(CultureInfo.InvariantCulture);

        /// <summary>
        /// The unique call id for this transfer
        /// </summary>
        public Guid CallId
        {
            get
            {
                return callId;
            }
            set
            {
                callId = value;
            }
        }

        /// <summary>
        /// </summary>
        private Guid callId = Guid.Empty;

        /// <summary>
        /// The unique session id for the transfer
        /// </summary>
        public uint SessionId
        {
            get
            {
                return sessionId;
            }
            set
            {
                sessionId = value;
            }
        }

        /// <summary>
        /// </summary>
        private uint sessionId = 0;

        /// <summary>
        /// The total length of the data, in bytes
        /// </summary>
        public uint DataSize
        {
            get
            {
                return dataSize;
            }
            set
            {
                dataSize = value;
            }
        }

        /// <summary>
        /// </summary>
        private uint dataSize = 0;

        /// <summary>
        /// The context send in the invitation. This informs the client about the type of transfer, filename, file-hash, msn object settings, etc.
        /// </summary>
        public string Context
        {
            get
            {
                return context;
            }
            set
            {
                context = value;
            }
        }

        /// <summary>
        /// </summary>
        private string context = "";

        /// <summary>
        /// The checksum of the fields used in the context
        /// </summary>
        public string Checksum
        {
            get
            {
                return checksum;
            }
            set
            {
                checksum = value;
            }
        }

        /// <summary>
        /// </summary>
        private string checksum = "";

        /// <summary>
        /// CSeq identifier
        /// </summary>
        public int LastCSeq
        {
            get
            {
                return lastCSeq;
            }
            set
            {
                lastCSeq = value;
            }
        }

        /// <summary>
        /// </summary>
        private int lastCSeq = 0;

        private Guid localContactEndPointID = Guid.Empty;

        /// <summary>
        /// The <see cref="EndPointData"/> id of local contact that involved in the transfer.
        /// </summary>
        public Guid LocalContactEndPointID
        {
            get { return localContactEndPointID; }
        }

        private Guid remoteContactEndPointID = Guid.Empty;

        /// <summary>
        /// The <see cref="EndPointData"/> id of remote contact that involved in the transfer.
        /// </summary>
        public Guid RemoteContactEndPointID
        {
            get { return remoteContactEndPointID; }
        }

        private Contact localContact = null;

        /// <summary>
        /// The the local contact in the transfer session.
        /// </summary>
        public Contact LocalContact
        {
            get { return localContact; }
        }

        private Contact remoteContact = null;

        /// <summary>
        /// The the remote contact in the transfer session.
        /// </summary>
        public Contact RemoteContact
        {
            get { return remoteContact; }
        }

        internal static P2PVersion JudgeP2PStackVersion(Contact local, Guid localEPID, Contact remote, Guid remoteEPID, bool dumpJudgeProcedure)
        {
            P2PVersion result = P2PVersion.P2PV1;

            if (!local.EndPointData.ContainsKey(localEPID))
            {
                string errorMessage = "Invalid parameter localEndPointID, EndPointData with id = " +
                    localEPID.ToString("B") + " not exists in contact: " + local;
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError && dumpJudgeProcedure, "[JudgeP2PStackVersion] " + errorMessage);

            }

            if (!remote.EndPointData.ContainsKey(remoteEPID))
            {
                string errorMessage = "Invalid parameter remoteEndPointID, EndPointData with id = " +
                    remoteEPID.ToString("B") + " not exists in contact: " + remote;
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError && dumpJudgeProcedure, "[JudgeP2PStackVersion] " + errorMessage);
            }

            bool supportMPOP = (localEPID != Guid.Empty && remoteEPID != Guid.Empty);

            if (local.EndPointData.ContainsKey(localEPID) && remote.EndPointData.ContainsKey(remoteEPID))
            {
                bool supportMSNC10 = ((local.EndPointData[localEPID].ClientCapacities & ClientCapacities.CanHandleMSNC10) > 0 &&
                                      (remote.EndPointData[remoteEPID].ClientCapacities & ClientCapacities.CanHandleMSNC10) > 0);
                bool supportP2Pv2 = ((local.EndPointData[localEPID].ClientCapacitiesEx & ClientCapacitiesEx.CanP2PV2) > 0 &&
                                     (remote.EndPointData[remoteEPID].ClientCapacitiesEx & ClientCapacitiesEx.CanP2PV2) > 0);



                if (supportMPOP /*&&  supportP2Pv2 &&  supportMSNC10 */) //It seems that supportP2Pv2 is not a consideration.
                    result = P2PVersion.P2PV2;
                else
                    result = P2PVersion.P2PV1;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose && dumpJudgeProcedure,
                    "Version Triggers: supportMPOP = " + supportMPOP + ", supportMSNC10 = " + supportMSNC10 + ", supportP2Pv2 = " + supportP2Pv2 + ", Result = " + result);
            }
            else
            {

                if (localEPID != Guid.Empty && remoteEPID != Guid.Empty)
                {
                    result = P2PVersion.P2PV2;
                }

                result = P2PVersion.P2PV1;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceError && dumpJudgeProcedure, "[JudgeP2PStackVersion] Judge only based on EPIDs, result:" + result);
            }

            return result;
        }

        internal string LocalContactEPIDString
        {
            get
            {
                if (TransferStackVersion == P2PVersion.P2PV1)
                {
                    return LocalContact.Mail.ToLowerInvariant();
                }

                return LocalContact.Mail.ToLowerInvariant() + ";" + LocalContactEndPointID.ToString("B").ToLowerInvariant();
            }
        }

        internal string RemoteContactEPIDString
        {
            get
            {
                if (TransferStackVersion == P2PVersion.P2PV1)
                {
                    return RemoteContact.Mail.ToLowerInvariant();
                }

                return RemoteContact.Mail.ToLowerInvariant() + ";" + RemoteContactEndPointID.ToString("B").ToLowerInvariant();
            }

        }

        
    }

    /// <summary>
    /// Used as event argument when a P2PTransferSession is affected.
    /// </summary>
    public class P2PTransferSessionEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private P2PTransferSession transferSession;

        /// <summary>
        /// The affected transfer session
        /// </summary>
        public P2PTransferSession TransferSession
        {
            get
            {
                return transferSession;
            }
            set
            {
                transferSession = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="transferSession"></param>
        public P2PTransferSessionEventArgs(P2PTransferSession transferSession)
        {
            this.transferSession = transferSession;
        }
    }


    /// <summary>
    /// Used as event argument when an invitation is received.
    /// </summary>
    /// <remarks>
    /// The client programmer must set the Accept property to true (accept) or false (reject) to response to the invitation. By default the invitation is rejected.
    /// </remarks>
    [Serializable()]
    public class MSNSLPInvitationEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private MSNSLPTransferProperties transferProperties;

        /// <summary>
        /// The affected transfer session
        /// </summary>
        public MSNSLPTransferProperties TransferProperties
        {
            get
            {
                return transferProperties;
            }
            set
            {
                transferProperties = value;
            }
        }

        /// <summary>
        /// </summary>
        private MSNObject msnObject;

        /// <summary>
        /// The corresponding msnobject defined in the invitation. Only available in case of an msn object transfer (image display, emoticons).
        /// </summary>
        /// <remarks>
        /// Created from the Context property of the <see cref="MSNSLPTransferProperties"/> object.
        /// </remarks>
        public MSNObject MSNObject
        {
            get
            {
                return msnObject;
            }
            set
            {
                msnObject = value;
            }
        }

        /// <summary>
        /// </summary>
        private string filename;

        /// <summary>
        /// Name of the file the remote contact wants to send. Only available in case of a filetransfer session.
        /// </summary>
        public string Filename
        {
            get
            {
                return filename;
            }
            set
            {
                filename = value;
            }
        }

        /// <summary>
        /// </summary>
        private long fileSize;

        /// <summary>
        /// The total size of the file in bytes. Only available in case of a filetransfer session.
        /// </summary>
        public long FileSize
        {
            get
            {
                return fileSize;
            }
            set
            {
                fileSize = value;
            }
        }

        private ActivityInfo activity = null;

        /// <summary>
        /// The activity properties.
        /// </summary>
        public ActivityInfo Activity
        {
            get { return activity; }
            set { activity = value; }
        }

        /// <summary>
        /// </summary>
        private SLPMessage invitationMessage;

        /// <summary>
        /// The affected transfer session
        /// </summary>
        public SLPMessage InvitationMessage
        {
            get
            {
                return invitationMessage;
            }
            set
            {
                invitationMessage = value;
            }
        }

        /// <summary>
        /// </summary>
        [NonSerialized]
        private P2PTransferSession transferSession = null;

        /// <summary>
        /// The p2p transfer session that will transfer the session data
        /// </summary>
        public P2PTransferSession TransferSession
        {
            get
            {
                return transferSession;
            }
            set
            {
                transferSession = value;
            }
        }

        [NonSerialized]
        private MSNSLPHandler transferhandler = null;

        public MSNSLPHandler TransferHandler
        {
            get
            {
                return transferhandler;
            }
        }

        /// <summary>
        /// </summary>
        private bool accept;

        /// <summary>
        /// Defines if the transfer is accepted. This must be set by the client programmer in a event handler. By default this property is set to false, which means the invitation is rejected. If this property is set to true, the invitation is accepted.
        /// </summary>
        public bool Accept
        {
            get
            {
                return accept;
            }
            set
            {
                accept = value;
            }
        }

        private bool delayprocess;

        /// <summary>
        /// Whether process the invitation request right after the event was fired.
        /// </summary>
        public bool DelayProcess
        {
            get
            {
                return delayprocess;
            }
            set
            {
                delayprocess = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="transferProperties"></param>
        /// <param name="invitationMessage"></param>
        /// <param name="transferSession"></param>
        /// <param name="handler"></param>
        public MSNSLPInvitationEventArgs
            (MSNSLPTransferProperties transferProperties,
            SLPMessage invitationMessage,
            P2PTransferSession transferSession,
            MSNSLPHandler handler)
        {
            this.transferProperties = transferProperties;
            this.invitationMessage = invitationMessage;
            this.transferSession = transferSession;
            this.transferhandler = handler;
        }
    }


    /// <summary>
    /// Handles invitations and requests for file transfers, emoticons, user displays and other msn objects.
    /// </summary>
    /// <remarks>
    /// MSNSLPHandler is responsible for communicating with the remote client about the transfer properties.
    /// This means receiving and sending details about filelength, filename, user display context, etc.
    /// When an invitation request is received the client programmer is asked to accept or decline the invitation. This is done
    /// through the TransferInvitationReceived event. The client programmer must handle this event and set the Accept and DataStream property in the event argument, see <see cref="MSNSLPInvitationEventArgs"/>.
    /// When the receiver of the invitation has accepted a <see cref="P2PTransferSession"/> is created and used to actually send the data. In the case
    /// of user displays or other msn objects the data transfer always goes over the switchboard. In case of a file transfer there will be negotiating about the direct connection to setup.
    /// Depending on the connectivity of both clients, a request for a direct connection is send to associated the <see cref="P2PTransferSession"/> object.
    /// </remarks>
    public class MSNSLPHandler : IMessageHandler, IDisposable
    {
        #region Properties
        /// <summary>
        /// </summary>
        private IMessageProcessor messageProcessor;

        /// <summary>
        /// The message processor to send outgoing p2p messages to.
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                if (value != null && object.ReferenceEquals(MessageProcessor, value))
                    return;

                if (value == null && MessageProcessor != null)
                {
                    MessageProcessor.UnregisterHandler(this);
                    messageProcessor = value;
                    return;
                }

                if (MessageProcessor != null)
                {
                    MessageProcessor.UnregisterHandler(this);
                }

                messageProcessor = value;
                messageProcessor.RegisterHandler(this);
            }
        }

        /// <summary>
        /// </summary>
        private IPEndPoint externalEndPoint;

        /// <summary>
        /// The client end-point as perceived by the server. This can differ from the actual local endpoint through the use of routers.
        /// This value is used to determine how to set-up a direct connection.
        /// </summary>
        public IPEndPoint ExternalEndPoint
        {
            get
            {
                return externalEndPoint;
            }

            private set
            {
                externalEndPoint = value;
            }
        }

        /// <summary>
        /// </summary>
        private IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// The client's local end-point. This can differ from the external endpoint through the use of routers.
        /// This value is used to determine how to set-up a direct connection.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                IPAddress iphostentry = IPAddress.Any;
                IPAddress[] addrList = Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;

                for (int IPC = 0; IPC < addrList.Length; IPC++)
                {
                    if (addrList[IPC].AddressFamily == AddressFamily.InterNetwork)
                    {
                        localEndPoint.Address = addrList[IPC];
                        break;
                    }
                }
                return localEndPoint;
            }
        }

        private Guid schedulerID = Guid.Empty;

        /// <summary>
        /// The P2P invitation scheduler guid.
        /// </summary>
        protected Guid SchedulerID
        {
            get { return schedulerID; }
        }



        #endregion

        #region Public

        /// <summary>
        /// Constructor.
        /// </summary>
        protected MSNSLPHandler()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);
        }

        public MSNSLPHandler(P2PVersion ver, Guid invitationSchedulerId, IPAddress externalAddress)
        {
            version = ver;
            schedulerID = invitationSchedulerId;
            ExternalEndPoint = new IPEndPoint(externalAddress, 0);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                "Constructing object, version = " + ver.ToString() + "\r\n" +
                "A new " + GetType().Name + " created, with scheduler id = " 
                + SchedulerID.ToString("B")
                , GetType().Name);
        }

        ~MSNSLPHandler()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        private P2PVersion version = P2PVersion.P2PV1;

        public P2PVersion Version
        {
            get { return version; }
        }

        /// <summary>
        /// The message session to send message to. This is simply the MessageProcessor property, but explicitly casted as a P2PMessageSession.
        /// </summary>
        public P2PMessageSession MessageSession
        {
            get
            {
                return (P2PMessageSession)MessageProcessor;
            }
        }


        /// <summary>
        /// Occurs when a transfer session is created.
        /// </summary>
        public event EventHandler<P2PTransferSessionEventArgs> TransferSessionCreated;

        /// <summary>
        /// Occurs when a transfer session is closed. Either because the transfer has finished or aborted.
        /// </summary>
        public event EventHandler<P2PTransferSessionEventArgs> TransferSessionClosed;

        /// <summary>
        /// Occurs when a remote client has send an invitation for a transfer session.
        /// </summary>
        public event EventHandler<MSNSLPInvitationEventArgs> TransferInvitationReceived;

        /// <summary>
        /// Sends the remote contact a request for the given context. The invitation message is send over the current MessageProcessor.
        /// </summary>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, MSNObject msnObject)
        {
            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(localContact, MessageSession.LocalContactEndPointID, 
                remoteContact, MessageSession.RemoteContactEndPointID);
            properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));


            P2PMessage p2pMessage = new P2PMessage(Version);
            P2PTransferSession transferSession = new P2PTransferSession(p2pMessage.Version, properties, MessageSession);
            transferSession.TransferFinished += delegate { transferSession.SendDisconnectMessage(CreateClosingMessage(properties)); };

            Contact remote = MessageSession.RemoteContact;
            string AppID = "1";

            string msnObjectContext = msnObject.ContextPlain;

            if (msnObject.ObjectType == MSNObjectType.Emoticon)
            {
                properties.DataType = DataTransferType.Emoticon;
                transferSession.MessageFooter = P2PConst.CustomEmoticonFooter11;
                transferSession.DataStream = msnObject.OpenStream();
                AppID = transferSession.MessageFooter.ToString();

            }
            else if (msnObject.ObjectType == MSNObjectType.UserDisplay)
            {
                if (remoteContact.DisplayImage == remoteContact.UserTileLocation)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "The current display image of remote contact: " + remoteContact + "is the same with it's displayImage context, invitation will not be processed.");
                    return null;  //If the display image is the same as current user tile string, do not process the invitation.
                }

                properties.DataType = DataTransferType.DisplayImage;
                transferSession.MessageFooter = P2PConst.DisplayImageFooter12;

                AppID = transferSession.MessageFooter.ToString();
                DisplayImage displayImage = msnObject as DisplayImage;

                if (displayImage == null)
                    throw new ArgumentNullException("msnObject is not a DisplayImage object.");

                msnObjectContext = remoteContact.UserTileLocation;


                transferSession.TransferFinished += delegate(object sender, EventArgs ea)
                {
                    //User display image is a special case, we use the default DataStream of a TransferSession.
                    //After the transfer finished, we create a new display image and fire the DisplayImageChanged event.
                    DisplayImage newDisplayImage = new DisplayImage(remoteContact.Mail.ToLowerInvariant(), transferSession.DataStream as MemoryStream);
                    remoteContact.SetDisplayImageAndFireDisplayImageChangedEvent(newDisplayImage);
                };
            }

            if (string.IsNullOrEmpty(msnObjectContext))
            {
                //This is a default displayImage or any object created by the client programmer.
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "[SendInvitation] msnObject does not have OriginalContext.");
                throw new InvalidOperationException("Parameter msnObject does not have valid OriginalContext property.");
            }

            byte[] contextArray = System.Text.Encoding.UTF8.GetBytes(msnObjectContext);

            string base64Context = Convert.ToBase64String(contextArray, 0, contextArray.Length);
            properties.Context = base64Context;

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            SLPRequestMessage slpMessage = new SLPRequestMessage(properties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = properties.RemoteContactEPIDString;
            slpMessage.FromMail = properties.LocalContactEPIDString;
            slpMessage.Branch = properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";

            slpMessage.BodyValues["EUF-GUID"] = P2PConst.UserDisplayGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();

            if (version == P2PVersion.P2PV1)
            {
                slpMessage.BodyValues["SChannelState"] = "0";
                slpMessage.BodyValues["Capabilities-Flags"] = "1";

                p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
                transferSession.MessageFlag = (uint)P2PFlag.MSNObjectData;
            }

            if (version == P2PVersion.P2PV2)
            {
                slpMessage.BodyValues["RequestFlags"] = "18";
            }

            slpMessage.BodyValues["AppID"] = AppID;
            slpMessage.BodyValues["Context"] = base64Context;

            p2pMessage.InnerMessage = slpMessage;

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.OperationCode = (byte)(OperationCode.SYN | OperationCode.RAK);
                p2pMessage.V2Header.AppendPeerInfoTLV();

                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    p2pMessage.V2Header.PackageNumber = (ushort)((p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) / 1202 + 1);
                }
                else
                {
                    p2pMessage.V2Header.PackageNumber = 0;
                }
                transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                p2pMessage.V2Header.TFCombination = TFCombination.First;

            }



            // store the transferproperties
            lock (transferProperties)
                transferProperties[properties.CallId] = properties;

 
            transferSession.IsSender = false;

            OnTransferSessionCreated(transferSession);

            // send the invitation
            Schedulers.P2PInvitationScheduler.Enqueue(transferSession, p2pMessage, SchedulerID);

            return transferSession;
        }

        /// <summary>
        /// Sends the remote contact a invitation for the activity. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="applicationID">The ID of activity, that was register by Microsoft.</param>
        /// <param name="activityName">The name of Activity.</param>
        /// <returns></returns>
        /// <example>
        /// <code language="C#">
        /// //An example that invites a remote user to attend the "Music Mix" activity.
        /// 
        /// String remoteAccount = @"remoteUser@hotmail.com";
        /// 
        /// String activityID = "20521364";        //The activityID of Music Mix activity.
        /// String activityName = "Music Mix";     //Th name of acticvity
        /// 
        /// P2PMessageSession session =  Conversation.Messenger.P2PHandler.GetSession(Conversation.Messenger.ContactList.Owner.Mail, remoteAccount);
        /// MSNSLPHandler slpHandler =  session.GetHandler(typeof(MSNSLPHandler)) as MSNSLPHandler ;
        /// slpHandler.SendInvitation(Conversation.Messenger.ContactList.Owner.Mail, remoteaccount, activityID, activityName);
        /// </code>
        /// </example>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, string applicationID, string activityName)
        {
            // set class variables
            return SendInvitation(localContact, remoteContact, applicationID, activityName, string.Empty);
        }

        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, string applicationID, string activityName, string activityData)
        {

            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(localContact, MessageSession.LocalContactEndPointID, 
                remoteContact, MessageSession.RemoteContactEndPointID);
            properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));

            properties.DataType = DataTransferType.Activity;

            string activityID = applicationID + ";1;" + activityName;
            byte[] contextData = System.Text.UnicodeEncoding.Unicode.GetBytes(activityID);
            string base64Context = Convert.ToBase64String(contextData, 0, contextData.Length);

            properties.Context = base64Context;

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            SLPRequestMessage slpMessage = new SLPRequestMessage(properties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = properties.RemoteContactEPIDString;
            slpMessage.FromMail = properties.LocalContactEPIDString;
            slpMessage.Branch = properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["EUF-GUID"] = P2PConst.ActivityGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();
            slpMessage.BodyValues["SChannelState"] = "0";
            slpMessage.BodyValues["Capabilities-Flags"] = "1";
            slpMessage.BodyValues["AppID"] = applicationID.ToString();
            slpMessage.BodyValues["Context"] = base64Context;

            if (Version == P2PVersion.P2PV2)
            {
                slpMessage.BodyValues["RequestFlags"] = "16";
            }

            P2PMessage p2pMessage = new P2PMessage(Version);
            p2pMessage.InnerMessage = slpMessage;

            lock (transferProperties)
                transferProperties[properties.CallId] = properties;

            // create a transfer session to handle the actual data transfer
            P2PTransferSession transferSession = new P2PTransferSession(Version, properties, MessageSession);

            if (Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.OperationCode = (byte)(OperationCode.SYN | OperationCode.RAK);
                p2pMessage.V2Header.AppendPeerInfoTLV();

                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    p2pMessage.V2Header.PackageNumber = (ushort)((p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) / 1202 + 1);
                }
                else
                {
                    p2pMessage.V2Header.PackageNumber = 0;
                }
                transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                p2pMessage.V2Header.TFCombination = TFCombination.First;
            }

            if (activityData != string.Empty && activityData != null)
            {
                activityData += "\0";

                int urlLength = Encoding.Unicode.GetByteCount(activityData);

                MemoryStream urlDataStream = new MemoryStream();

                byte[] header = null; ;

                if (Version == P2PVersion.P2PV1)
                {
                    header = new byte[] { 0x80, 0x00, 0x00, 0x00 };
                }

                if (Version == P2PVersion.P2PV2)
                {
                    header = new byte[] { 0x80, 0x3f, 0x14, 0x05 };
                }

                urlDataStream.Write(header, 0, header.Length);
                urlDataStream.Write(BitUtility.GetBytes((ushort)0x08, true), 0, sizeof(ushort));  //data type: 0x08: string
                urlDataStream.Write(BitUtility.GetBytes(urlLength, true), 0, sizeof(int));
                urlDataStream.Write(Encoding.Unicode.GetBytes(activityData), 0, urlLength);

                urlDataStream.Seek(0, SeekOrigin.Begin);
                transferSession.DataStream = urlDataStream;
                transferSession.IsSender = true;
            }

            transferSession.MessageFlag = (uint)P2PFlag.Normal;
            transferSession.TransferFinished += delegate { transferSession.SendDisconnectMessage(CreateClosingMessage(properties)); };


            OnTransferSessionCreated(transferSession);
            Schedulers.P2PInvitationScheduler.Enqueue(transferSession, p2pMessage, SchedulerID);

            return transferSession;
        }

        /// <summary>
        /// Sends the remote contact a request for the filetransfer. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="filename"></param>
        /// <param name="file"></param>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, string filename, Stream file)
        {
            //0-7: location of the FT preview
            //8-15: file size
            //16-19: I don't know or need it so...
            //20-540: location I use to get the file name
            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(localContact, MessageSession.LocalContactEndPointID, 
                remoteContact, MessageSession.RemoteContactEndPointID); ;
            do
            {
                properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));
            }
            while (MessageSession.GetTransferSession(properties.SessionId) != null);


            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            properties.DataType = DataTransferType.File;

            // size: location (8) + file size (8) + unknown (4) + filename (236) = 256

            // create a new bytearray to build up the context
            byte[] contextArray = new byte[574];// + picturePreview.Length];
            BinaryWriter binWriter = new BinaryWriter(new MemoryStream(contextArray, 0, contextArray.Length, true));

            // length of preview data
            binWriter.Write((int)574);

            // don't know some sort of flag
            binWriter.Write((int)2);

            // send the total size of the file we are to transfer

            binWriter.Write((long)file.Length);

            // don't know some sort of flag
            binWriter.Write((int)1);

            // write the filename in the context
            binWriter.Write(System.Text.UnicodeEncoding.Unicode.GetBytes(filename));

            // convert to base 64 string
            //base64Context = "PgIAAAIAAAAVAAAAAAAAAAEAAABkAGMALgB0AHgAdAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            string base64Context = Convert.ToBase64String(contextArray, 0, contextArray.Length);

            // set our current context
            properties.Context = base64Context;

            // store the transferproperties
            lock (transferProperties)
                transferProperties[properties.CallId] = properties;

            // create the message
            SLPRequestMessage slpMessage = new SLPRequestMessage(properties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = properties.RemoteContactEPIDString;
            slpMessage.FromMail = properties.LocalContactEPIDString;
            slpMessage.Branch = properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["EUF-GUID"] = P2PConst.FileTransferGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();
            slpMessage.BodyValues["AppID"] = P2PConst.FileTransFooter2.ToString();
            slpMessage.BodyValues["Context"] = base64Context;

            if (Version == P2PVersion.P2PV2)
            {
                slpMessage.BodyValues["RequestFlags"] = "16";
            }

            P2PMessage p2pMessage = new P2PMessage(Version);
            p2pMessage.InnerMessage = slpMessage;

            // set the size, it could be more than 1202 bytes. This will make sure it is split in multiple messages by the processor.
            //p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

            // create a transfer session to handle the actual data transfer
            P2PTransferSession transferSession = new P2PTransferSession(Version, properties, MessageSession);

            if (Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.OperationCode = (byte)(OperationCode.RAK | OperationCode.SYN);
                p2pMessage.V2Header.AppendPeerInfoTLV();

                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    p2pMessage.V2Header.PackageNumber = (ushort)((p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) / 1202 + 1);
                }
                else
                {
                    p2pMessage.V2Header.PackageNumber = 0;
                }
                transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                p2pMessage.V2Header.TFCombination = TFCombination.First;
            }

            
            
            transferSession.MessageFlag = (uint)P2PFlag.FileData;
            transferSession.MessageFooter = P2PConst.FileTransFooter2;
            
            transferSession.TransferFinished += delegate { transferSession.SendDisconnectMessage(CreateClosingMessage(properties)); };
            // set the data stream to read from
            transferSession.DataStream = file;
            transferSession.IsSender = true;

            OnTransferSessionCreated(transferSession);

            Schedulers.P2PInvitationScheduler.Enqueue(transferSession, p2pMessage, SchedulerID);

            return transferSession;
        }

        /// <summary>
        /// The client programmer should call this if he wants to reject the transfer
        /// </summary>
        public void RejectTransfer(MSNSLPInvitationEventArgs invitationArgs)
        {
            if (invitationArgs.TransferSession != null)
            {
                invitationArgs.TransferSession.SendMessage(CreateDeclineMessage(invitationArgs.TransferProperties));
                invitationArgs.TransferSession.SendMessage(CreateClosingMessage(invitationArgs.TransferProperties));

            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[RejectTransfer Error] No transfer session attached.");
            }
        }


        /// <summary>
        /// The client programmer should call this if he wants to accept the transfer
        /// </summary>
        public void AcceptTransfer(MSNSLPInvitationEventArgs invitationArgs)
        {
            MSNSLPTransferProperties properties = invitationArgs.TransferProperties;
            P2PTransferSession transferSession = invitationArgs.TransferSession;
            SLPMessage message = invitationArgs.InvitationMessage;

            // check for a valid datastream
            if (invitationArgs.TransferSession.DataStream == null)
                throw new MSNPSharpException("The invitation was accepted, but no datastream to read to/write from has been specified.");

            // the client programmer has accepted, continue !
            lock (transferProperties)
                transferProperties[properties.CallId] = properties;

            // we want to be notified of messages through this session
            transferSession.RegisterHandler(this);
            transferSession.DataStream = invitationArgs.TransferSession.DataStream;


            switch (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture))
            {
                case P2PConst.UserDisplayGuid:
                    {
                        // for some kind of weird behavior, our local identifier must now subtract 4 ?
                        transferSession.IsSender = true;
                        transferSession.MessageFlag = (uint)P2PFlag.MSNObjectData;
                        transferSession.MessageFooter = P2PConst.DisplayImageFooter12;

                        break;
                    }

                case P2PConst.FileTransferGuid:
                    {
                        transferSession.IsSender = false;
                        transferSession.MessageFlag = (uint)P2PFlag.FileData;
                        transferSession.MessageFooter = P2PConst.FileTransFooter2;
                        break;
                    }

                case P2PConst.ActivityGuid:
                    {
                        transferSession.MessageFlag = (uint)P2PFlag.Normal;
                        if (message.BodyValues.ContainsKey("AppID"))
                        {
                            uint appID = 0;
                            uint.TryParse(message.BodyValues["AppID"].Value, out appID);
                            transferSession.MessageFooter = appID;
                        }
                        transferSession.IsSender = false;
                        break;
                    }
            }

            OnTransferSessionCreated(transferSession);
            transferSession.AcceptInviation(CreateAcceptanceMessage(properties));
        }

        /// <summary>
        /// Closes all sessions by sending the remote client a closing message for each session available.
        /// </summary>
        public void CloseAllSessions()
        {
            foreach (MSNSLPTransferProperties properties in transferProperties.Values)
            {
                P2PMessageSession session = (P2PMessageSession)MessageProcessor;
                P2PTransferSession transferSession = session.GetTransferSession(properties.SessionId);
                if (transferSession != null)
                {
                    CloseSession(transferSession);
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[CloseAllSessions Error] No transfer session attached on transfer layer.");
                }
            }
        }

        /// <summary>
        /// Close the specified session
        /// </summary>
        /// <param name="transferSession">Session to close</param>
        public void CloseSession(P2PTransferSession transferSession)
        {
            if (transferSession != null)
            {
                transferSession.SendMessage(CreateClosingMessage(transferSession.TransferProperties));
                Thread.Sleep(300);
                transferSession.AbortTransfer();
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[CloseSessions Error] No transfer session attached on transfer layer.");
            }
        }

        #endregion

        #region Protected

        /// <summary>
        /// Fires the TransferSessionCreated event and registers event handlers.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnTransferSessionCreated(P2PTransferSession session)
        {
            session.TransferFinished += new EventHandler<EventArgs>(MSNSLPHandler_TransferFinished);
            session.TransferAborted += new EventHandler<EventArgs>(MSNSLPHandler_TransferAborted);

            if (TransferSessionCreated != null)
                TransferSessionCreated(this, new P2PTransferSessionEventArgs(session));
        }

        /// <summary>
        /// Fires the TransferSessionClosed event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnTransferSessionClosed(P2PTransferSession session)
        {
            if (TransferSessionClosed != null)
                TransferSessionClosed(this, new P2PTransferSessionEventArgs(session));
        }

        /// <summary>
        /// Fires the TransferInvitationReceived event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransferInvitationReceived(MSNSLPInvitationEventArgs e)
        {
            if (TransferInvitationReceived != null)
                TransferInvitationReceived(this, e);
        }

        /// <summary>
        /// Returns the MSNSLPTransferProperties object associated with the specified call id.
        /// </summary>
        /// <param name="callId"></param>
        /// <returns></returns>
        protected MSNSLPTransferProperties GetTransferProperties(Guid callId)
        {
            return transferProperties.ContainsKey(callId) ? transferProperties[callId] : null;
        }

        /// <summary>
        /// Creates the handshake message to send in a direct connection.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected virtual P2PDCHandshakeMessage CreateHandshakeMessage(MSNSLPTransferProperties properties)
        {
            P2PDCHandshakeMessage dcMessage = new P2PDCHandshakeMessage(P2PVersion.P2PV1); // v!
            dcMessage.Header.SessionId = 0;

            System.Diagnostics.Debug.Assert(properties.Nonce != Guid.Empty, "Direct connection established, but no Nonce GUID is available.");
            System.Diagnostics.Debug.Assert(properties.SessionId != 0, "Direct connection established, but no session id is available.");

            // set the guid to use in the handshake message
            dcMessage.Guid = properties.Nonce;

            return dcMessage;
        }


        /// <summary>
        /// Creates a message which is send directly after the last data message.
        /// </summary>
        /// <returns></returns>
        protected virtual SLPRequestMessage CreateClosingMessage(MSNSLPTransferProperties transferProperties)
        {
            SLPRequestMessage slpMessage = new SLPRequestMessage(transferProperties.RemoteContactEPIDString, MSNSLPRequestMethod.BYE);
            slpMessage.ToMail = transferProperties.RemoteContactEPIDString;
            slpMessage.FromMail = transferProperties.LocalContactEPIDString;

            slpMessage.Branch = transferProperties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = transferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";

            slpMessage.BodyValues["SessionID"] = transferProperties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (transferProperties.DataType == DataTransferType.Activity)
            {
                slpMessage.BodyValues["Context"] = "dAMAgQ==";

            }

            return slpMessage;
        }


        /// <summary>
        /// Creates an 500 internal error message.
        /// </summary>
        /// <param name="transferProperties"></param>
        /// <returns></returns>
        protected virtual SLPStatusMessage CreateInternalErrorMessage(MSNSLPTransferProperties transferProperties)
        {
            SLPStatusMessage slpMessage = new SLPStatusMessage(transferProperties.RemoteContactEPIDString, 500, "Internal Error");
            slpMessage.ToMail = transferProperties.RemoteContactEPIDString;
            slpMessage.FromMail = transferProperties.LocalContactEPIDString;
            slpMessage.Branch = transferProperties.LastBranch;
            slpMessage.CSeq = transferProperties.LastCSeq;
            slpMessage.CallId = transferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["SessionID"] = transferProperties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            slpMessage.BodyValues["SChannelState"] = "0";
            return slpMessage;
        }

        /// <summary>
        /// Parses the incoming invitation message. This will set the class's properties for later retrieval in following messages.
        /// </summary>
        /// <param name="message"></param>
        protected virtual MSNSLPTransferProperties ParseInvitationMessage(SLPMessage message)
        {
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(MessageSession.LocalContact, message.ToEndPoint, MessageSession.RemoteContact, message.FromEndPoint);

            properties.RemoteInvited = true;

            if (message.BodyValues.ContainsKey("EUF-GUID"))
            {
                properties.DataTypeGuid = message.BodyValues["EUF-GUID"].ToString();
                if (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) == P2PConst.UserDisplayGuid)
                {
                    // create a temporary msn object to extract the data type
                    if (message.BodyValues.ContainsKey("Context"))
                    {
                        MSNObject msnObject = new MSNObject();
                        msnObject.SetContext(message.BodyValues["Context"].ToString(), true);

                        if (msnObject.ObjectType == MSNObjectType.UserDisplay)
                            properties.DataType = DataTransferType.DisplayImage;
                        else if (msnObject.ObjectType == MSNObjectType.Emoticon)
                            properties.DataType = DataTransferType.Emoticon;
                        else
                            properties.DataType = DataTransferType.Unknown;

                        properties.Context = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(message.BodyValues["Context"].ToString()));
                        properties.Checksum = ExtractChecksum(properties.Context);
                    }
                    else
                    {
                        properties.DataType = DataTransferType.Unknown;
                    }
                }
                else if (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) == P2PConst.FileTransferGuid)
                {
                    properties.DataType = DataTransferType.File;
                }
                else if (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) == P2PConst.ActivityGuid)
                {
                    properties.DataType = DataTransferType.Activity;
                }
                else
                {
                    properties.DataType = DataTransferType.Unknown;
                }

                // store the branch for use in the OK Message
                properties.LastBranch = message.Branch;
                properties.LastCSeq = message.CSeq;
                properties.CallId = message.CallId;
                properties.SessionId = uint.Parse(message.BodyValues["SessionID"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            }

            return properties;
        }


        /// <summary>
        /// Sends the invitation request for a direct connection
        /// </summary>
        protected virtual void SendDCInvitation(MSNSLPTransferProperties transferProperties)
        {
            if (Version == P2PVersion.P2PV2)  //In p2pv2 our library don't support any direct connection.
                return;

            //0-7: location of the FT preview
            //8-15: file size
            //16-19: I don't know or need it so...
            //20-540: location I use to get the file name
            // set class variables


            // create a new branch, but keep the same callid as the first invitation
            transferProperties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);

            string connectionType = "Unknown-Connect";

            // check and determine connectivity
            if (LocalEndPoint == null || this.ExternalEndPoint == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "LocalEndPoint or ExternalEndPoint are not set. Connection type will be set to unknown.", GetType().Name);
            }
            else
            {
                if (LocalEndPoint.Address.Equals(ExternalEndPoint.Address))
                {
                    if (LocalEndPoint.Port == ExternalEndPoint.Port)
                        connectionType = "Direct-Connect";
                    else
                        connectionType = "Port-Restrict-NAT";
                }
                else
                {
                    if (LocalEndPoint.Port == ExternalEndPoint.Port)
                        connectionType = "IP-Restrict-NAT";
                    else
                        connectionType = "Symmetric-NAT";
                }
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Connection type set to " + connectionType + " for session " + transferProperties.SessionId.ToString(CultureInfo.InvariantCulture), GetType().Name);
            }

            // create the message
            SLPRequestMessage slpMessage = new SLPRequestMessage(transferProperties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = transferProperties.RemoteContactEPIDString;
            slpMessage.FromMail = transferProperties.LocalContactEPIDString;
            slpMessage.Branch = transferProperties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = transferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-transreqbody";
            slpMessage.BodyValues["Bridges"] = "TRUDPV1 TCPv1";
            slpMessage.BodyValues["NetID"] = "2042264281"; // unknown variable
            slpMessage.BodyValues["Conn-Type"] = connectionType;
            slpMessage.BodyValues["UPnPNat"] = "false"; // UPNP Enabled
            slpMessage.BodyValues["ICF"] = "false"; // Firewall enabled

            P2PMessage p2pMessage = new P2PMessage(P2PVersion.P2PV1);
            p2pMessage.InnerMessage = slpMessage;

            MessageProcessor.SendMessage(p2pMessage);
        }


        /// <summary>
        /// Creates a 200 OK message. This is called by the handler after the client-programmer
        /// has accepted the invitation.
        /// </summary>
        /// <returns></returns>
        protected static SLPStatusMessage CreateAcceptanceMessage(MSNSLPTransferProperties properties)
        {
            SLPStatusMessage newMessage = new SLPStatusMessage(properties.RemoteContactEPIDString, 200, "OK");
            newMessage.ToMail = properties.RemoteContactEPIDString;
            newMessage.FromMail = properties.LocalContactEPIDString;
            newMessage.Branch = properties.LastBranch;
            newMessage.CSeq = 1;
            newMessage.CallId = properties.CallId;
            newMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            newMessage.BodyValues["SessionID"] = properties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return newMessage;
        }

        


        /// <summary>
        /// Creates a 603 Decline message.
        /// </summary>
        /// <returns></returns>
        protected static SLPStatusMessage CreateDeclineMessage(MSNSLPTransferProperties properties)
        {
            // create 603 Decline message
            SLPStatusMessage newMessage = new SLPStatusMessage(properties.RemoteContactEPIDString, 603, "Decline");
            newMessage.ToMail = properties.RemoteContactEPIDString;
            newMessage.FromMail = properties.LocalContactEPIDString;
            newMessage.Branch = properties.LastBranch;
            newMessage.CSeq = 1;
            newMessage.CallId = properties.CallId;
            newMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            newMessage.BodyValues["SessionID"] = properties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return newMessage;
        }


        /// <summary>
        /// Closes the session's datastream and removes the transfer sessions from the class' <see cref="P2PMessageSession"/> object (MessageProcessor property).
        /// </summary>
        /// <param name="session"></param>
        protected virtual void RemoveTransferSession(P2PTransferSession session)
        {
            // remove the session
            OnTransferSessionClosed(session);
            if (session.AutoCloseStream)
                session.DataStream.Close();

            MessageSession.RemoveTransferSession(session);
        }

        #endregion

        #region Private

        /// <summary>
        /// A dictionary containing MSNSLPTransferProperties objects. Indexed by CallId;
        /// </summary>
        private Dictionary<Guid, MSNSLPTransferProperties> transferProperties = new Dictionary<Guid, MSNSLPTransferProperties>();

        /// <summary>
        /// Extracts the checksum (SHA1C/SHA1D field) from the supplied context.
        /// </summary>
        /// <remarks>The context must be a plain string, no base-64 decoding will be done</remarks>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string ExtractChecksum(string context)
        {
            Regex shaRe = new Regex("SHA1C=\"([^\"]+)\"");
            Match match = shaRe.Match(context);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                shaRe = new Regex("SHA1D=\"([^\"]+)\"");
                match = shaRe.Match(context);
                if (match.Success)
                    return match.Groups[1].Value;
            }
            throw new MSNPSharpException("SHA field could not be extracted from the specified context: " + context);
        }



        /// <summary>
        /// Closes the datastream and sends the closing message, if the local client is the receiver. 
        /// Afterwards the associated <see cref="P2PTransferSession"/> object is removed from the class's <see cref="P2PMessageSession"/> object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MSNSLPHandler_TransferFinished(object sender, EventArgs e)
        {
            P2PTransferSession transferSession = (P2PTransferSession)sender;
            Contact remote = MessageSession.RemoteContact;

            MSNSLPTransferProperties properties = transferSession.TransferProperties;
            

            if (properties.RemoteInvited == false)
            {
                if (Version == P2PVersion.P2PV1)
                {
                    // we are the receiver. send close message back
                    P2PMessage p2pMessage = new P2PMessage(transferSession.Version);
                    p2pMessage.InnerMessage = CreateClosingMessage(properties);
                    p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
                    MessageProcessor.SendMessage(p2pMessage);
                }

                // close it
                RemoveTransferSession(transferSession);
            }

            
        }


        /// <summary>
        /// Cleans up the transfer session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MSNSLPHandler_TransferAborted(object sender, EventArgs e)
        {
            P2PTransferSession session = (P2PTransferSession)sender;

            RemoveTransferSession(session);
        }

        #endregion

        #region IMessageHandler Members


        /// <summary>
        /// Handles incoming P2P Messages by extracting the inner contents and converting it to a SLP Message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            Debug.Assert(p2pMessage != null, "Message is not a P2P message in MSNSLP handler", "");

            if (p2pMessage.Version == P2PVersion.P2PV1)
            {
                if (!(p2pMessage.Footer == 0 && p2pMessage.V1Header.SessionId == 0 && 
                    p2pMessage.V1Header.Flags != P2PFlag.Acknowledgement && p2pMessage.V1Header.MessageSize > 0))
                {
                    //We don't process any p2p message because this is a SIP message handler.
                    //Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "P2Pv1 Message incoming:\r\n" + p2pMessage.ToDebugString(), GetType().Name);
                    return;
                }
            }

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                if (!(p2pMessage.V2Header.SessionId == 0 &&
                    p2pMessage.V2Header.MessageSize > 0 && p2pMessage.V2Header.TFCombination == TFCombination.First))
                {
                    //Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "P2Pv2 Message incoming:\r\n" + p2pMessage.ToDebugString(), GetType().Name);
                    return;
                }

            }

            SLPMessage slpMessage = SLPMessage.Parse(p2pMessage.InnerBody);

            if (slpMessage != null)
            {
                // call the methods belonging to the content-type to handle the message
                switch (slpMessage.ContentType)
                {
                    case "application/x-msnmsgr-sessionreqbody":
                        OnSessionRequest(p2pMessage);
                        break;
                    case "application/x-msnmsgr-transreqbody":
                        OnDCRequest(p2pMessage);
                        break;
                    case "application/x-msnmsgr-transrespbody":
                        OnDCResponse(p2pMessage);
                        break;
                    case "application/x-msnmsgr-sessionclosebody":
                        OnSessionCloseRequest(p2pMessage);
                        break;
                    default:
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Content-type not supported: " + slpMessage.ContentType, GetType().Name);
                            break;
                        }
                }
            }
        }


        #endregion

        #region Protected message handling methods

        /// <summary>
        /// Called when a remote client closes a session.
        /// </summary>
        protected virtual void OnSessionCloseRequest(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);

            Guid callGuid = message.CallId;
            MSNSLPTransferProperties properties = GetTransferProperties(callGuid);

            if (properties != null) // Closed before or never accepted?
            {
                properties.SessionCloseState--;
                P2PTransferSession session = MessageSession.GetTransferSession(properties.SessionId);

                if (session == null)
                    return;

                // remove the resources
                RemoveTransferSession(session);
                // and close the connection
                if (session.MessageSession.DirectConnected)
                    session.MessageSession.CloseDirectConnection();
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Warning: a session with the call-id " + callGuid + " not exists.", GetType().Name);
            }
        }

        /// <summary>
        /// Called when a remote client request a session
        /// </summary>
        protected virtual void OnSessionRequest(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);
            SLPStatusMessage slpStatus = message as SLPStatusMessage;

            if (slpStatus != null)
            {
                if (slpStatus.CSeq == 1 && slpStatus.Code == 603)
                {
                    OnSessionCloseRequest(p2pMessage);
                }
                else if (slpStatus.CSeq == 1 && slpStatus.Code == 200)
                {
                    Guid callGuid = message.CallId;
                    MSNSLPTransferProperties properties = GetTransferProperties(callGuid);

                    if (properties == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Cannot find request transfer property, Guid: " + callGuid.ToString("B"));
                        return;
                    }

                    P2PTransferSession session = ((P2PMessageSession)MessageProcessor).GetTransferSession(properties.SessionId);

                    if (session == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Cannot find request transfer session, SessionId: " + properties.SessionId.ToString());
                        return;
                    }


                    if (properties.DataType == DataTransferType.File)
                    {
                        if (Version == P2PVersion.P2PV1)
                        {
                            // ok we can receive the OK message. we can now send the invitation to setup a transfer connection
                            if (MessageSession.DirectConnected == false && MessageSession.DirectConnectionAttempt == false)
                                SendDCInvitation(properties);

                            session.StartDataTransfer(true);
                        }
                        else
                        {
                            session.StartDataTransfer(false);
                        }          
                    }

                    if (properties.DataType == DataTransferType.Activity)
                    {
                        if (session.DataStream != null && session.IsSender)
                        {
                            session.StartDataTransfer(false);
                        }
                    }

                }
                return;
            }

            SLPRequestMessage slpMessage = message as SLPRequestMessage;

            if (slpMessage.Method == "INVITE")
            {
                // create a properties object and add it to the transfer collection
                MSNSLPTransferProperties properties = ParseInvitationMessage(message);

                // create a p2p transfer
                P2PTransferSession transferSession = new P2PTransferSession(Version, properties, MessageSession);
                transferSession.TransferFinished += delegate { transferSession.SendDisconnectMessage(CreateClosingMessage(properties)); };

                if (Version == P2PVersion.P2PV2)
                {
                    transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;
                }

                // hold a reference to this argument object because we need the accept property later
                MSNSLPInvitationEventArgs invitationArgs =
                    new MSNSLPInvitationEventArgs(properties, message, transferSession, this);

                if (properties.DataType == DataTransferType.Unknown)  // If type is unknown, we reply an internal error.
                {

                    transferSession.SendMessage(CreateInternalErrorMessage(properties));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Unknown p2p datatype received: " +
                        properties.DataTypeGuid + ". 500 INTERNAL ERROR send.", GetType().ToString());
                    return;
                }

                if (properties.DataType == DataTransferType.File)
                {
                    // set the filetransfer values in the EventArgs object.
                    // see the SendInvitation(..) method for more info about the layout of the context string
                    MemoryStream memStream = new MemoryStream(Convert.FromBase64String(message.BodyValues["Context"].ToString()));
                    BinaryReader reader = new BinaryReader(memStream);
                    reader.ReadInt32(); // previewDataLength, 4
                    reader.ReadInt32(); // first flag, 4
                    invitationArgs.FileSize = reader.ReadInt64(); // 8
                    reader.ReadInt32(); // 2nd flag, 4

                    MemoryStream filenameStream = new MemoryStream();
                    byte val = 0;
                    while (reader.BaseStream.Position < reader.BaseStream.Length - 2 &&
                            (val = reader.ReadByte()) > 0)
                    {
                        filenameStream.WriteByte(val);
                        filenameStream.WriteByte(reader.ReadByte());
                    }
                    invitationArgs.Filename = System.Text.UnicodeEncoding.Unicode.GetString(filenameStream.ToArray());
                }
                else if (properties.DataType == DataTransferType.DisplayImage ||
                    properties.DataType == DataTransferType.Emoticon)
                {
                    // create a MSNObject based upon the send context
                    invitationArgs.MSNObject = new MSNObject();
                    invitationArgs.MSNObject.SetContext(properties.Context, false);
                }
                else if (properties.DataType == DataTransferType.Activity)
                {
                    if (message.BodyValues.ContainsKey("Context"))
                    {
                        string activityContextString = message.BodyValues["Context"].Value;
                        ActivityInfo info = new ActivityInfo(activityContextString);
                        invitationArgs.Activity = info;
                    }
                }

                OnTransferInvitationReceived(invitationArgs);

                if (!invitationArgs.DelayProcess)
                {
                    // check whether the client programmer wants to accept or reject this message
                    if (invitationArgs.Accept == false)
                    {
                        RejectTransfer(invitationArgs);
                        return;
                    }

                    AcceptTransfer(invitationArgs);
                }
            }
        }

        /// <summary>
        /// Returns a port number which can be used to listen for a new direct connection.
        /// </summary>
        /// <remarks>Throws an SocketException when no ports can be found.</remarks>
        /// <returns></returns>
        protected virtual int GetNextDirectConnectionPort()
        {
            // Find host by name
            IPHostEntry iphostentry = new IPHostEntry();
            iphostentry.AddressList = new IPAddress[1];
            int IPC = 0;
            for (IPC = 0; IPC < Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList.Length; IPC++)
            {
                if (Dns.GetHostEntry(Dns.GetHostName()).AddressList[IPC].AddressFamily == AddressFamily.InterNetwork)
                {
                    iphostentry.AddressList[0] = Dns.GetHostEntry(Dns.GetHostName()).AddressList[IPC];

                    break;
                }
            }

            for (int p = 1119; p <= IPEndPoint.MaxPort; p++)
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    s.Bind(new IPEndPoint(iphostentry.AddressList[0], p));
                    //s.Bind(new IPEndPoint(IPAddress.Any, p));
                    s.Close();
                    return p;
                }
                catch (SocketException ex)
                {
                    // EADDRINUSE?
                    if (ex.ErrorCode == 10048)
                        continue;
                    else
                        throw;
                }
            }
            throw new SocketException(10048);
        }

        /// <summary>
        /// Called when the remote client sends a file and sends us it's direct-connect capabilities.
        /// A reply will be send with the local client's connectivity.
        /// </summary>
        /// <param name="p2pMessage"></param>
        protected virtual void OnDCRequest(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);

           // let's listen
            

           // Find host by name
            IPAddress iphostentry = LocalEndPoint.Address;

            MSNSLPTransferProperties properties = GetTransferProperties(message.CallId);

            if (properties == null)
                return;

            SLPStatusMessage slpMessage = new SLPStatusMessage(properties.RemoteContactEPIDString, 200, "OK");
            properties.Nonce = Guid.NewGuid();


            if (!iphostentry.Equals(ExternalEndPoint.Address))
            {
                slpMessage.BodyValues["Listening"] = "false";
                slpMessage.BodyValues["Nonce"] = Guid.Empty.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                int port = GetNextDirectConnectionPort();

                MessageSession.ListenForDirectConnection(iphostentry, port);
                slpMessage.BodyValues["Listening"] = "true";
                slpMessage.BodyValues["Nonce"] = properties.Nonce.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                slpMessage.BodyValues["IPv4Internal-Addrs"] = iphostentry.ToString();
                slpMessage.BodyValues["IPv4Internal-Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);


                // check if client is behind firewall (NAT-ted)
                // if so, send the public ip also the client, so it can try to connect to that ip
                if (ExternalEndPoint != null && ExternalEndPoint.Address != iphostentry)
                {
                    slpMessage.BodyValues["IPv4External-Addrs"] = ExternalEndPoint.Address.ToString();
                    slpMessage.BodyValues["IPv4External-Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }


            slpMessage.ToMail = properties.RemoteContactEPIDString;
            slpMessage.FromMail = properties.LocalContactEPIDString;
            slpMessage.Branch = properties.LastBranch;
            slpMessage.CSeq = 1;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-transrespbody";
            slpMessage.BodyValues["Bridge"] = "TCPv1";
            



            P2PMessage p2pReplyMessage = new P2PMessage(Version);
            p2pReplyMessage.InnerMessage = slpMessage;
            P2PTransferSession transferSession = ((P2PMessageSession)MessageProcessor).GetTransferSession(properties.SessionId);
            
            if (Version == P2PVersion.P2PV2)
            {
                p2pReplyMessage.V2Header.TFCombination = TFCombination.First;
                if (transferSession != null)
                {
                    p2pReplyMessage.V2Header.PackageNumber = transferSession.GetNextSLPStatusDataPacketNumber();
                }
                else
                {
                    p2pReplyMessage.V2Header.PackageNumber = P2PTransferSession.GetNextSLPStatusDataPacketNumber(p2pMessage.V2Header.PackageNumber);
                }
            }


            

            if (Version == P2PVersion.P2PV1)
            {
                P2PDCHandshakeMessage hsMessage = new P2PDCHandshakeMessage(P2PVersion.P2PV1);
                hsMessage.Guid = properties.Nonce;
                MessageSession.HandshakeMessage = hsMessage;
            }


            // and notify the remote client that he can connect
            MessageProcessor.SendMessage(p2pReplyMessage);
        }


        /// <summary>
        /// Called when the remote client send us it's direct-connect capabilities
        /// </summary>
        /// <param name="p2pMessage"></param>
        protected virtual void OnDCResponse(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);

            // read the values
            MimeDictionary bodyValues = message.BodyValues;

            // check the protocol
            if (bodyValues.ContainsKey("Bridge") && bodyValues["Bridge"].ToString().IndexOf("TCPv1") >= 0)
            {
                if (bodyValues.ContainsKey("IPv4Internal-Addrs") &&
                    bodyValues.ContainsKey("Listening") && bodyValues["Listening"].ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("true") >= 0)
                {
                    // we must connect to the remote client
                    ConnectivitySettings settings = new ConnectivitySettings();
                    settings.Host = bodyValues["IPv4Internal-Addrs"].ToString();
                    settings.Port = int.Parse(bodyValues["IPv4Internal-Port"].ToString(), System.Globalization.CultureInfo.InvariantCulture);

                    // let the message session connect
                    MSNSLPTransferProperties properties = GetTransferProperties(message.CallId);

                    properties.Nonce = new Guid(bodyValues["Nonce"].ToString());

                    // create the handshake message to send upon connection                    
                    P2PDCHandshakeMessage hsMessage = new P2PDCHandshakeMessage(P2PVersion.P2PV1); // v!
                    hsMessage.Guid = properties.Nonce;
                    MessageSession.HandshakeMessage = hsMessage;

                    MessageSession.CreateDirectConnection(settings.Host, settings.Port);
                }
            }
        }


        #endregion

        #region IDisposable Members

        /// <summary>
        /// Closes all sessions. Dispose() calls Dispose(true)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources
                CloseAllSessions();
            }

            // Free native resources if there are any.
        }



        #endregion
    }
};
