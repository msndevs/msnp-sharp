#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

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
        private string dataTypeGuid = string.Empty;

        internal string DataTypeGuid
        {
            get { return dataTypeGuid; }
            set { dataTypeGuid = value; }
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
        private bool remoteInvited;

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
        private uint sessionId;

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
        private uint dataSize;

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
        private int lastCSeq;

        /// <summary>
        /// The account of the local contact
        /// </summary>
        public string LocalContact
        {
            get
            {
                return localContact;
            }
            set
            {
                localContact = value;
            }
        }

        /// <summary>
        /// </summary>
        private string localContact = "";

        /// <summary>
        /// The account of the remote contact
        /// </summary>
        public string RemoteContact
        {
            get
            {
                return remoteContact;
            }
            set
            {
                remoteContact = value;
            }
        }

        /// <summary>
        /// </summary>
        private string remoteContact = "";
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
        private MSNSLPMessage invitationMessage;

        /// <summary>
        /// The affected transfer session
        /// </summary>
        public MSNSLPMessage InvitationMessage
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
            MSNSLPMessage invitationMessage,
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
                messageProcessor = value;
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
            set
            {
                externalEndPoint = value;
            }
        }

        /// <summary>
        /// </summary>
        private IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any,0);

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

        #endregion

        #region Public

        /// <summary>
        /// Constructor.
        /// </summary>
        public MSNSLPHandler()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);
        }

        ~MSNSLPHandler()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
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
        public P2PTransferSession SendInvitation(string localContact, string remoteContact, MSNObject msnObject)
        {
            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties();
            properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));

            properties.LocalContact = localContact;
            properties.RemoteContact = remoteContact;
            string AppID = string.Empty;
            P2PMessage p2pMessage = new P2PMessage();
            P2PTransferSession session = new P2PTransferSession();

            if (msnObject.ObjectType == MSNObjectType.Emoticon)
            {
                properties.DataType = DataTransferType.Emoticon;
                AppID = P2PConst.CustomEmoticonFooter11.ToString();
                session.MessageFooter = P2PConst.CustomEmoticonFooter11;
            }
            else if (msnObject.ObjectType == MSNObjectType.UserDisplay)
            {
                properties.DataType = DataTransferType.DisplayImage;
                AppID = P2PConst.DisplayImageFooter12.ToString();
                session.MessageFooter = P2PConst.DisplayImageFooter12;
            }

            p2pMessage.Flags = P2PFlag.Normal;

            session.MessageFlag = (uint)P2PFlag.MSNObjectData;

            MSNSLPMessage slpMessage = new MSNSLPMessage();

            byte[] contextArray = System.Text.Encoding.UTF8.GetBytes(MSNObject.GetDecodeString(msnObject.OriginalContext));//GetEncodedString());

            string base64Context = Convert.ToBase64String(contextArray, 0, contextArray.Length);
            properties.Context = base64Context;

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            slpMessage.StartLine = "INVITE MSNMSGR:" + remoteContact + " MSNSLP/1.0";
            slpMessage.To = "<msnmsgr:" + remoteContact + ">";
            slpMessage.From = "<msnmsgr:" + localContact + ">";
            slpMessage.Via = "MSNSLP/1.0/TLP ;branch=" + properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";

            slpMessage.BodyValues["EUF-GUID"] = P2PConst.UserDisplayGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();
            slpMessage.BodyValues["AppID"] = AppID;
            slpMessage.BodyValues["Context"] = base64Context;

            p2pMessage.InnerMessage = slpMessage;

            // set the size, it could be more than 1202 bytes. This will make sure it is split in multiple messages by the processor.
            p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

            // store the transferproperties
            TransferProperties[properties.CallId] = properties;

            // create a transfer session to handle the actual data transfer
            session.MessageSession = (P2PMessageSession)MessageProcessor;
            session.SessionId = properties.SessionId;

            MessageSession.AddTransferSession(session);

            // set the data stream to write to
            session.DataStream = msnObject.OpenStream();
            session.IsSender = false;
            session.CallId = properties.CallId;

            OnTransferSessionCreated(session);

            // send the invitation
            MessageSession.SendMessage(p2pMessage);

            return session;
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
        /// P2PMessageSession session =  Conversation.Messenger.P2PHandler.GetSession(Conversation.Messenger.Owner.Mail, remoteAccount);
        /// MSNSLPHandler slpHandler =  session.GetHandler(typeof(MSNSLPHandler)) as MSNSLPHandler ;
        /// slpHandler.SendInvitation(Conversation.Messenger.Owner.Mail, remoteaccount, activityID, activityName);
        /// </code>
        /// </example>
        public P2PTransferSession SendInvitation(string localContact, string remoteContact, string applicationID, string activityName)
        {
            return SendInvitation(localContact, remoteContact, applicationID, activityName, string.Empty);
        }

        /// <summary>
        /// Sends the remote contact a invitation for the activity. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="applicationID">The ID of activity, that was register by Microsoft.</param>
        /// <param name="activityName">The name of Activity.</param>
        /// <param name="activityURL">The URL you want the side window open.</param>
        /// <returns></returns>
        /// <example>
        /// <code language="C#">
        /// //An example that invites a remote user to attend the "Music Mix" activity.
        /// 
        /// String remoteAccount = @"remoteUser@hotmail.com";
        /// 
        /// String activityID = "99995868";                         //The applicationID of some existing activity, or you can apply one from M$.
        /// String activityName = "MSNPSharp Testing Activity";     //The name of acticvity
        /// String activityURL = @"http://www.ohloh.net/p/msnp-sharp/widgets/project_basic_stats.html";       //The URL open in the side box.
        /// 
        /// P2PMessageSession session =  Conversation.Messenger.P2PHandler.GetSession(Conversation.Messenger.Owner.Mail, remoteAccount);
        /// MSNSLPHandler slpHandler =  session.GetHandler(typeof(MSNSLPHandler)) as MSNSLPHandler ;
        /// slpHandler.SendInvitation(Conversation.Messenger.Owner.Mail, remoteaccount, activityID, activityName, activityURL);
        /// </code>
        /// </example>
        public P2PTransferSession SendInvitation(string localContact, string remoteContact, string applicationID, string activityName, string activityURL)
        {
            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties();
            properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));

            properties.LocalContact = localContact;
            properties.RemoteContact = remoteContact;

            properties.DataType = DataTransferType.Activity;

            MSNSLPMessage slpMessage = new MSNSLPMessage();

            string activityIdentifier = applicationID + ";1;" + activityName;
            byte[] contextData = System.Text.UnicodeEncoding.Unicode.GetBytes(activityIdentifier);
            string base64Context = Convert.ToBase64String(contextData, 0, contextData.Length);

            properties.Context = base64Context;

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            slpMessage.StartLine = "INVITE MSNMSGR:" + remoteContact + " MSNSLP/1.0";
            slpMessage.To = "<msnmsgr:" + remoteContact + ">";
            slpMessage.From = "<msnmsgr:" + localContact + ">";
            slpMessage.Via = "MSNSLP/1.0/TLP ;branch=" + properties.LastBranch;
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

            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.InnerMessage = slpMessage;
            p2pMessage.Flags = P2PFlag.Normal;

            // set the size, it could be more than 1202 bytes. This will make sure it is split in multiple messages by the processor.
            p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;
            // store the transferproperties
            TransferProperties[properties.CallId] = properties;

            // create a transfer session to handle the actual data transfer
            P2PTransferSession session = new P2PTransferSession();
            session.MessageSession = (P2PMessageSession)MessageProcessor;
            session.SessionId = properties.SessionId;
            MessageSession.AddTransferSession(session);

            if (activityURL != null && activityURL != string.Empty)
            {
                activityURL += "\0";
                int urlLength = System.Text.Encoding.Unicode.GetByteCount(activityURL);

                MemoryStream urlDataStream = new MemoryStream();

                byte[] header = new byte[] { 0x80, 0x00, 0x00, 0x00 };

                urlDataStream.Write(header, 0, header.Length);
                urlDataStream.Write(new byte[] { 0x08, 0x00 }, 0, 2);  //data type: 0x08: string
                urlDataStream.Write(BitConverter.GetBytes(P2PMessage.ToLittleEndian((uint)urlLength)), 0, sizeof(int));
                urlDataStream.Write(System.Text.Encoding.Unicode.GetBytes(activityURL), 0, urlLength);

                urlDataStream.Seek(0, SeekOrigin.Begin);
                session.DataStream = urlDataStream;
                session.IsSender = true;
            }


            session.CallId = properties.CallId;

            OnTransferSessionCreated(session);

            MessageSession.SendMessage(p2pMessage);

            return session;
        }

        /// <summary>
        /// Sends the remote contact a request for the filetransfer. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="filename"></param>
        /// <param name="file"></param>
        public P2PTransferSession SendInvitation(string localContact, string remoteContact, string filename, Stream file)
        {
            //0-7: location of the FT preview
            //8-15: file size
            //16-19: I don't know or need it so...
            //20-540: location I use to get the file name
            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties();
            do
            {
                properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));
            }
            while (MessageSession.GetTransferSession(properties.SessionId) != null);

            properties.LocalContact = localContact;
            properties.RemoteContact = remoteContact;

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            properties.DataType = DataTransferType.File;

            MSNSLPMessage slpMessage = new MSNSLPMessage();

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
            TransferProperties[properties.CallId] = properties;

            // create the message
            slpMessage.StartLine = "INVITE MSNMSGR:" + remoteContact + " MSNSLP/1.0";
            slpMessage.To = "<msnmsgr:" + remoteContact + ">";
            slpMessage.From = "<msnmsgr:" + localContact + ">";
            slpMessage.Via = "MSNSLP/1.0/TLP ;branch=" + properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["EUF-GUID"] = P2PConst.FileTransferGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();
            slpMessage.BodyValues["AppID"] = P2PConst.FileTransFooter2.ToString();
            slpMessage.BodyValues["Context"] = base64Context;

            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.InnerMessage = slpMessage;

            // set the size, it could be more than 1202 bytes. This will make sure it is split in multiple messages by the processor.
            p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

            // create a transfer session to handle the actual data transfer
            P2PTransferSession session = new P2PTransferSession();
            session.MessageSession = (P2PMessageSession)MessageProcessor;
            session.SessionId = properties.SessionId;

            MessageSession.AddTransferSession(session);
            session.MessageFlag = (uint)P2PFlag.FileData;
            session.MessageFooter = P2PConst.FileTransFooter2;

            // set the data stream to read from
            session.DataStream = file;
            session.IsSender = true;
            session.CallId = properties.CallId;

            OnTransferSessionCreated(session);

            MessageSession.SendMessage(p2pMessage);

            return session;
        }

        /// <summary>
        /// The client programmer should call this if he wants to reject the transfer or an activity invotation.
        /// </summary>
        public void RejectTransfer(MSNSLPInvitationEventArgs invitationArgs)
        {
            MSNSLPTransferProperties properties = invitationArgs.TransferProperties;

            P2PMessage replyMessage = new P2PMessage();
            replyMessage.InnerMessage = CreateDeclineMessage(properties);
            replyMessage.Flags = P2PFlag.Normal;
            MessageProcessor.SendMessage(replyMessage);

            replyMessage.SessionId = 0;
            replyMessage.InnerMessage = CreateClosingMessage(properties);
            replyMessage.Flags = P2PFlag.Normal;
            replyMessage.Footer = 0;

            MessageProcessor.SendMessage(replyMessage);
        }


        /// <summary>
        /// The client programmer should call this if he wants to accept the file transfer or activity invitation.
        /// </summary>
        public void AcceptTransfer(MSNSLPInvitationEventArgs invitationArgs)
        {
            MSNSLPTransferProperties properties = invitationArgs.TransferProperties;
            P2PTransferSession p2pTransfer = invitationArgs.TransferSession;
            MSNSLPMessage message = invitationArgs.InvitationMessage;
            P2PMessage replyMessage = new P2PMessage();

            // check for a valid datastream
            if (invitationArgs.TransferSession.DataStream == null)
                throw new MSNPSharpException("The invitation was accepted, but no datastream to read to/write from has been specified.");

            // the client programmer has accepted, continue !
            TransferProperties.Add(properties.CallId, properties);

            ((P2PMessageSession)MessageProcessor).AddTransferSession(p2pTransfer);

            // we want to be notified of messages through this session
            p2pTransfer.RegisterHandler(this);

            p2pTransfer.DataStream = invitationArgs.TransferSession.DataStream;
            replyMessage.InnerMessage = CreateAcceptanceMessage(properties);
            replyMessage.Flags = P2PFlag.Normal;

            switch (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture))
            {
                case P2PConst.UserDisplayGuid:
                    {
                        p2pTransfer.IsSender = true;
                        p2pTransfer.MessageFlag = (uint)P2PFlag.MSNObjectData;
                        p2pTransfer.MessageFooter = P2PConst.DisplayImageFooter12;

                        break;
                    }

                case P2PConst.FileTransferGuid:
                    {
                        p2pTransfer.MessageFlag = (uint)P2PFlag.FileData;
                        p2pTransfer.MessageFooter = P2PConst.FileTransFooter2;
                        p2pTransfer.IsSender = false;
                        break;
                    }

                case P2PConst.ActivityGuid:
                    {
                        p2pTransfer.MessageFlag = (uint)P2PFlag.Normal;
                        if (message.BodyValues.ContainsKey("AppID"))
                        {
                            uint appID = 0;
                            uint.TryParse(message.BodyValues["AppID"].Value, out appID);
                            p2pTransfer.MessageFooter = appID;
                        }
                        p2pTransfer.IsSender = false;
                        break;
                    }
            }


            p2pTransfer.CallId = properties.CallId;

            OnTransferSessionCreated(p2pTransfer);

            MessageProcessor.SendMessage(replyMessage);
            //After sending this message, we can get the data prepare message ackId.
            p2pTransfer.DataPreparationAck = replyMessage.AckSessionId;

            if (p2pTransfer.IsSender)
                p2pTransfer.StartDataTransfer(false);
        }

        /// <summary>
        /// Closes all sessions by sending the remote client a closing message for each session available.
        /// </summary>
        public void CloseAllSessions()
        {
            foreach (MSNSLPTransferProperties properties in TransferProperties.Values)
            {
                P2PMessageSession session = (P2PMessageSession)MessageProcessor;
                P2PTransferSession transferSession = session.GetTransferSession(properties.SessionId);
                P2PMessage closeMessage = new P2PMessage();

                closeMessage.SessionId = 0;
                closeMessage.Flags = P2PFlag.Normal;
                closeMessage.InnerMessage = CreateClosingMessage(properties);
                closeMessage.Footer = 0;

                MessageProcessor.SendMessage(closeMessage);
                if (transferSession != null)
                {
                    transferSession.AbortTransfer();
                }
            }
        }

        /// <summary>
        /// Close the specified session
        /// </summary>
        /// <param name="session">Session to close</param>
        public void CloseSession(P2PTransferSession session)
        {
            MSNSLPTransferProperties property = GetTransferProperties(session.CallId);
            P2PMessage closeMessage = new P2PMessage();

            closeMessage.SessionId = 0;
            closeMessage.InnerMessage = CreateClosingMessage(property);
            closeMessage.Flags = P2PFlag.Normal;
            closeMessage.Footer = 0;

            MessageProcessor.SendMessage(closeMessage);
            if (session != null)
            {
                session.AbortTransfer();
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
            return (MSNSLPTransferProperties)TransferProperties[callId];
        }

        /// <summary>
        /// Creates the handshake message to send in a direct connection.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected virtual P2PDCHandshakeMessage CreateHandshakeMessage(MSNSLPTransferProperties properties)
        {
            P2PDCHandshakeMessage dcMessage = new P2PDCHandshakeMessage();
            dcMessage.SessionId = 0;

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
        protected virtual MSNSLPMessage CreateClosingMessage(MSNSLPTransferProperties transferProperties)
        {
            MSNSLPMessage slpMessage = new MSNSLPMessage();

            slpMessage.StartLine = "BYE MSNMSGR:" + transferProperties.RemoteContact + " MSNSLP/1.0";
            slpMessage.To = "<msnmsgr:" + transferProperties.RemoteContact + ">";
            slpMessage.From = "<msnmsgr:" + transferProperties.LocalContact + ">";
            slpMessage.Via = "MSNSLP/1.0/TLP ;branch=" + transferProperties.LastBranch;
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
        protected virtual MSNSLPMessage CreateInternalErrorMessage(MSNSLPTransferProperties transferProperties)
        {
            MSNSLPMessage slpMessage = new MSNSLPMessage();
            slpMessage.StartLine = "MSNSLP/1.0 500 Internal Error";
            slpMessage.To = "<msnmsgr:" + transferProperties.RemoteContact + ">";
            slpMessage.From = "<msnmsgr:" + transferProperties.LocalContact + ">";
            slpMessage.Via = "MSNSLP/1.0/TLP ;branch=" + transferProperties.LastBranch;
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
        protected virtual MSNSLPTransferProperties ParseInvitationMessage(MSNSLPMessage message)
        {
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties();

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
                        msnObject.ParseContext(message.BodyValues["Context"].ToString(), true);

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
                properties.LastBranch = String.Copy(message.Branch);
                properties.LastCSeq = message.CSeq;
                properties.CallId = message.CallId;
                properties.SessionId = (uint)(uint.Parse(message.BodyValues["SessionID"].ToString(), System.Globalization.CultureInfo.InvariantCulture));

                // set the contacts who send and receive it				
                properties.LocalContact = ExtractNameFromTag(message.To);
                properties.RemoteContact = ExtractNameFromTag(message.From);
            }

            return properties;
        }


        /// <summary>
        /// Sends the invitation request for a direct connection
        /// </summary>
        protected virtual void SendDCInvitation(MSNSLPTransferProperties transferProperties)
        {
            //0-7: location of the FT preview
            //8-15: file size
            //16-19: I don't know or need it so...
            //20-540: location I use to get the file name
            // set class variables							
            MSNSLPMessage slpMessage = new MSNSLPMessage();

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
            slpMessage.StartLine = "INVITE MSNMSGR:" + transferProperties.RemoteContact + " MSNSLP/1.0";
            slpMessage.To = "<msnmsgr:" + transferProperties.RemoteContact + ">";
            slpMessage.From = "<msnmsgr:" + transferProperties.LocalContact + ">";
            slpMessage.Via = "MSNSLP/1.0/TLP ;branch=" + transferProperties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = transferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-transreqbody";
            slpMessage.BodyValues["Bridges"] = "TRUDPV1 TCPv1";
            slpMessage.BodyValues["NetID"] = "2042264281"; // unknown variable
            slpMessage.BodyValues["Conn-Type"] = connectionType;
            slpMessage.BodyValues["UPnPNat"] = "false"; // UPNP Enabled
            slpMessage.BodyValues["ICF"] = "false"; // Firewall enabled		

            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.InnerMessage = slpMessage;
            p2pMessage.Flags = P2PFlag.Normal;
            p2pMessage.Footer = 0;


            MessageProcessor.SendMessage(p2pMessage);
        }


        /// <summary>
        /// Creates a 200 OK message. This is called by the handler after the client-programmer
        /// has accepted the invitation.
        /// </summary>
        /// <returns></returns>
        protected static MSNSLPMessage CreateAcceptanceMessage(MSNSLPTransferProperties properties)
        {
            MSNSLPMessage newMessage = new MSNSLPMessage();
            newMessage.StartLine = "MSNSLP/1.0 200 OK";
            newMessage.To = "<msnmsgr:" + properties.RemoteContact + ">";
            newMessage.From = "<msnmsgr:" + properties.LocalContact + ">";
            newMessage.Via = "MSNSLP/1.0/TLP ;branch=" + properties.LastBranch;
            newMessage.CSeq = 1;
            newMessage.CallId = properties.CallId;
            newMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            newMessage.BodyValues["SessionID"] = properties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            newMessage.BodyValues["SChannelState"] = "0";
            newMessage.BodyValues["Capabilities-Flags"] = "1";

            return newMessage;
        }


        /// <summary>
        /// Creates a 603 Decline message.
        /// </summary>
        /// <returns></returns>
        protected static MSNSLPMessage CreateDeclineMessage(MSNSLPTransferProperties properties)
        {
            // create 603 Decline message			
            MSNSLPMessage newMessage = new MSNSLPMessage();
            newMessage.StartLine = "MSNSLP/1.0 603 Decline";
            newMessage.To = "<msnmsgr:" + properties.RemoteContact + ">";
            newMessage.From = "<msnmsgr:" + properties.LocalContact + ">";
            newMessage.Via = "MSNSLP/1.0/TLP ;branch=" + properties.LastBranch;
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
            MessageSession.RemoveTransferSession(session);
            OnTransferSessionClosed(session);
            if (session.AutoCloseStream)
                session.DataStream.Close();
        }

        #endregion

        #region Private
        /// <summary>
        /// A collection of all transfer. This collection holds MSNSLPTransferProperties objects. Indexed by call-id.
        /// </summary>
        private Hashtable transfers = new Hashtable();

        /// <summary>
        /// A hashtable containing MSNSLPTransferProperties objects. Indexed by session id;
        /// </summary>
        private Hashtable TransferProperties = new Hashtable();

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
        /// Helper method to extract the e-mail adress from a msnmsgr:name@hotmail.com tag.
        /// </summary>
        /// <param name="taggedText"></param>
        /// <returns></returns>
        private static string ExtractNameFromTag(string taggedText)
        {
            int start = taggedText.IndexOf(":");
            int end = taggedText.IndexOf(">");
            if (start < 0 || end < 0)
                throw new MSNPSharpException("Could not extract name from tag");

            return taggedText.Substring(start + 1, end - start - 1);
        }

        /// <summary>
        /// Closes the datastream and sends the closing message, if the local client is the receiver. 
        /// Afterwards the associated <see cref="P2PTransferSession"/> object is removed from the class's <see cref="P2PMessageSession"/> object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MSNSLPHandler_TransferFinished(object sender, EventArgs e)
        {
            P2PTransferSession session = (P2PTransferSession)sender;

            MSNSLPTransferProperties properties = this.GetTransferProperties(session.CallId);

            if (properties.RemoteInvited == false)
            {
                // we are the receiver. send close message back
                P2PMessage p2pMessage = new P2PMessage();

                p2pMessage.SessionId = 0;
                p2pMessage.InnerMessage = CreateClosingMessage(GetTransferProperties(session.CallId));
                p2pMessage.Flags = P2PFlag.Normal;
                p2pMessage.Footer = 0;

                session.SendMessage(p2pMessage);

                // close it
                RemoveTransferSession(session);
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
        /// Handles incoming P2P Messages by extracting the inner contents and converting it to a MSNSLP Message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            Debug.Assert(p2pMessage != null, "Message is not a P2P message in MSNSLP handler", "");            

            if (!(p2pMessage.Footer == 0 && p2pMessage.SessionId == 0 && p2pMessage.Flags != P2PFlag.Acknowledgement && p2pMessage.MessageSize > 0))
            {
                //We don't process any p2p message because this is a SIP message handler.
                return;
            }

            MSNSLPMessage slpMessage = new MSNSLPMessage();
            slpMessage.CreateFromMessage(message);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "MSNSLPMessage incoming:\r\n" + slpMessage.ToDebugString(), GetType().Name);

            // call the methods belonging to the content-type to handle the message
            switch (slpMessage.ContentType)
            {
                case "application/x-msnmsgr-sessionreqbody":
                    OnSessionRequest(slpMessage);
                    break;
                case "application/x-msnmsgr-transreqbody":
                    //OnDCRequest(slpMessage);
                    break;
                case "application/x-msnmsgr-transrespbody":
                    OnDCResponse(slpMessage);
                    break;
                case "application/x-msnmsgr-sessionclosebody":
                    OnSessionCloseRequest(slpMessage);
                    break;
                default:
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Content-type not supported: " + slpMessage.ContentType, GetType().Name);
                        break;
                    }
            }
        }


        #endregion

        #region Protected message handling methods

        /// <summary>
        /// Called when a remote client closes a session.
        /// </summary>
        protected virtual void OnSessionCloseRequest(MSNSLPMessage message)
        {
            Guid callGuid = message.CallId;
            MSNSLPTransferProperties properties = this.GetTransferProperties(callGuid);
            if (properties != null) // Closed before or never accepted?
            {
                P2PMessageSession msgSession = MessageProcessor as P2PMessageSession;
                if (msgSession != null)
                {
                    P2PTransferSession session = msgSession.GetTransferSession(properties.SessionId);

                    if (session == null)
                        return;

                    // remove the resources
                    RemoveTransferSession(session);
                    // and close the connection
                    if (session.MessageSession.DirectConnected)
                        session.MessageSession.CloseDirectConnection();

                }
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Warning: a session with the call-id " + callGuid + " not exists.", GetType().Name);
            }
        }

        /// <summary>
        /// Called when a remote client request a session
        /// </summary>
        protected virtual void OnSessionRequest(MSNSLPMessage message)
        {
            if (transfers.ContainsKey(message.CallId))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Warning: a session with the call-id " + message.CallId.ToString() + " already exists.", GetType().Name);
                return;
            }

            if (message.CSeq == 1 && message.StartLine.IndexOf("603 Decline") >= 0)
            {
                OnSessionCloseRequest(message);
            }
            else if (message.CSeq == 1 && message.StartLine.IndexOf("200 OK") >= 0)
            {
                Guid callGuid = message.CallId;
                MSNSLPTransferProperties properties = this.GetTransferProperties(callGuid);
                P2PTransferSession session = ((P2PMessageSession)MessageProcessor).GetTransferSession(properties.SessionId);
                if (properties.DataType == DataTransferType.File)
                {
                    // ok we can receive the OK message. we can now send the invitation to setup a transfer connection
                    if (MessageSession.DirectConnected == false && MessageSession.DirectConnectionAttempt == false)
                        SendDCInvitation(properties);
                    session.StartDataTransfer(true);
                }
                else if (properties.DataType == DataTransferType.Activity)
                {
                    if (session.IsSender && session.DataStream != null)  //We have URL to be sent here.
                    {
                        session.StartDataTransfer(false);
                    }
                }

            }
            else if (message.StartLine.IndexOf("INVITE ") >= 0)
            {
                // create a properties object and add it to the transfer collection
                MSNSLPTransferProperties properties = ParseInvitationMessage(message);

                // create a p2p transfer
                P2PTransferSession p2pTransfer = new P2PTransferSession();
                p2pTransfer.MessageSession = (P2PMessageSession)MessageProcessor;
                p2pTransfer.SessionId = properties.SessionId;

                // hold a reference to this argument object because we need the accept property later
                MSNSLPInvitationEventArgs invitationArgs =
                    new MSNSLPInvitationEventArgs(properties, message, p2pTransfer, this);

                if (properties.DataType == DataTransferType.Unknown)  // If type is unknown, we reply an internal error.
                {
                    P2PMessage replyMessage = new P2PMessage();
                    replyMessage.Flags = P2PFlag.Normal;
                    replyMessage.InnerMessage = CreateInternalErrorMessage(properties);
                    MessageProcessor.SendMessage(replyMessage);
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
                    reader.ReadInt32();	// previewDataLength, 4
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
                    invitationArgs.MSNObject.ParseContext(properties.Context, false);
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
        /// <param name="message"></param>
        protected virtual void OnDCRequest(MSNSLPMessage message)
        {
            // let's listen			
            MSNSLPMessage slpMessage = new MSNSLPMessage();

            // Find host by name
            IPAddress iphostentry = LocalEndPoint.Address;

            MSNSLPTransferProperties properties = GetTransferProperties(message.CallId);
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




            // create the message
            slpMessage.StartLine = "MSNSLP/1.0 200 OK";
            slpMessage.To = "<msnmsgr:" + properties.RemoteContact + ">";
            slpMessage.From = "<msnmsgr:" + properties.LocalContact + ">";
            slpMessage.Via = "MSNSLP/1.0/TLP ;branch=" + properties.LastBranch;
            slpMessage.CSeq = 1;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-transrespbody";
            slpMessage.BodyValues["Bridge"] = "TCPv1";
            



            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.InnerMessage = slpMessage;
            p2pMessage.Footer = 0;
            p2pMessage.Flags = P2PFlag.Normal;


            ((P2PMessageSession)MessageProcessor).GetTransferSession(properties.SessionId);
            P2PDCHandshakeMessage hsMessage = new P2PDCHandshakeMessage();
            hsMessage.Guid = properties.Nonce;
            MessageSession.HandshakeMessage = hsMessage;



            // and notify the remote client that he can connect
            MessageProcessor.SendMessage(p2pMessage);
        }


        /// <summary>
        /// Called when the remote client send us it's direct-connect capabilities
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnDCResponse(MSNSLPMessage message)
        {
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

                    P2PTransferSession transferSession = ((P2PMessageSession)MessageProcessor).GetTransferSession(properties.SessionId);

                    properties.Nonce = new Guid(bodyValues["Nonce"].ToString());

                    // create the handshake message to send upon connection                    
                    P2PDCHandshakeMessage hsMessage = new P2PDCHandshakeMessage();
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
