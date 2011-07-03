using System;
using System.IO;
using System.Data;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Net;
using System.Text;

using MSNPSharp.Services;

namespace MSNPSharpClient
{
    using MSNPSharp;
    using MSNPSharp.Apps;
    using MSNPSharp.Core;
    using MSNPSharp.P2P;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.IO;

    /// <summary>
    /// MSNPSharp Client example.
    /// </summary>
    public partial class ClientForm : System.Windows.Forms.Form
    {
        // Create a Messenger object to use MSNPSharp.
        private Messenger messenger = new Messenger();
        private List<ConversationForm> convforms = new List<ConversationForm>(0);
        private TraceForm traceform = new TraceForm();
        private bool syncContactListCompleted = false;

        public List<ConversationForm> ConversationForms
        {
            get
            {
                return convforms;
            }
        }

        public Messenger Messenger
        {
            get
            {
                return messenger;
            }
        }

        public ClientForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.Icon = Properties.Resources.MSNPSharp_logo_small_ico;

            // You can set proxy settings here
            // for example: messenger.ConnectivitySettings.ProxyHost = "10.0.0.2";

            Settings.TraceSwitch.Level = System.Diagnostics.TraceLevel.Verbose;

            if (Settings.IsMono) //I am running on Mono.
            {
                // Don't enable this on mono, because mono raises NotImplementedException.
                Settings.EnableGzipCompressionForWebServices = false;
            }

#if DEBUG

            //How to save your personal addressbook.
            //If you want your addressbook have a better reading/writting performance, use MclSerialization.None
            //In this case, your addressbook will be save as a xml file, everyone can read it.
            //If you want your addressbook has a smaller size, use MclSerialization.Compression.
            //In this case, your addressbook file will be save in gzip format, none can read it, but the performance is not so good.
            Settings.SerializationType = MSNPSharp.IO.MclSerialization.None;
#elif TRACE
            Settings.SerializationType = MSNPSharp.IO.MclSerialization.Compression | MSNPSharp.IO.MclSerialization.Cryptography;
#endif

            // The following line is very IMPOTANT.
            // Keep the messenger sending PNG to the server in a proper frequency, or it will be kicked offline.
            this.tmrKeepOnLine.Tick += new EventHandler(tmrKeepOnLine_Tick);

            // If you want to use it in an environment that does not have write permission, set NoSave to true.
            //Settings.NoSave = true;

            // Disable P2P direct connections for transfers if direct connections fails or
            // the machine connecting internet is behind NAT.
            //Settings.DisableP2PDirectConnections = true;

            // set the events that we will handle
            // remember that the nameserver is the server that sends contact lists, notifies you of contact status changes, etc.
            // a switchboard server handles the individual conversation sessions.
            messenger.NameserverProcessor.ConnectionEstablished += new EventHandler<EventArgs>(NameserverProcessor_ConnectionEstablished);
            messenger.Nameserver.SignedIn += new EventHandler<EventArgs>(Nameserver_SignedIn);
            messenger.Nameserver.SignedOff += new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff);
            messenger.NameserverProcessor.ConnectingException += new EventHandler<ExceptionEventArgs>(NameserverProcessor_ConnectingException);
            messenger.Nameserver.ExceptionOccurred += new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred);
            messenger.Nameserver.AuthenticationError += new EventHandler<ExceptionEventArgs>(Nameserver_AuthenticationError);
            messenger.Nameserver.ServerErrorReceived += new EventHandler<MSNErrorEventArgs>(Nameserver_ServerErrorReceived);

            // Receive messages send by contacts.
            messenger.MessageManager.NudgeReceived += new EventHandler<NudgeArrivedEventArgs>(Nameserver_NudgeReceived);
            messenger.MessageManager.TypingMessageReceived += new EventHandler<TypingArrivedEventArgs>(Nameserver_TypingMessageReceived);
            messenger.MessageManager.TextMessageReceived += new EventHandler<TextMessageArrivedEventArgs>(Nameserver_TextMessageReceived);
            messenger.MessageManager.EmoticonReceived += new EventHandler<EmoticonArrivedEventArgs>(Nameserver_EmoticonReceived);
            messenger.MessageManager.WinkReceived += new EventHandler<WinkEventArgs>(Nameserver_WinkReceived);


            // Listen for the data transfer events (i.e. file transfer invitation, activity invitation)
            messenger.P2PHandler.InvitationReceived += new EventHandler<P2PSessionEventArgs>(p2pHandler_InvitationReceived);

            // Listen to ping answer event. In each ping answer, MSN will give you a number. That is the interval to send the next Ping.
            // You can send a Ping by using Messenger.Nameserver.SendPing().
            messenger.Nameserver.PingAnswer += new EventHandler<PingAnswerEventArgs>(Nameserver_PingAnswer);

            messenger.Nameserver.OwnerVerified += new EventHandler<EventArgs>(Nameserver_OwnerVerified);
            messenger.Nameserver.ContactOnline += new EventHandler<ContactStatusChangedEventArgs>(Nameserver_ContactOnline);
            messenger.Nameserver.ContactOffline += new EventHandler<ContactStatusChangedEventArgs>(Nameserver_ContactOffline);

            messenger.ContactService.ContactBlockedStatusChanged += new EventHandler<ContactBlockedStatusChangedEventArgs>(Nameserver_ContactBlockedStatusChanged);

            // SynchronizationCompleted will fired after the updated operation for your contact list has completed.
            messenger.ContactService.SynchronizationCompleted += new EventHandler<EventArgs>(ContactService_SynchronizationCompleted);
            // ReverseAdded will fired after a contact adds you to his/her contact list.
            messenger.ContactService.ReverseAdded += new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded);

            messenger.ContactService.ReverseRemoved += new EventHandler<ContactEventArgs>(ContactService_ReverseRemoved);
            // ContactAdded will fired after a contact added to any role list.
            messenger.ContactService.ContactAdded += new EventHandler<ListMutateEventArgs>(ContactService_ContactAdded);
            // ContactRemoved will fired after a contact removed from any role list.
            messenger.ContactService.ContactRemoved += new EventHandler<ListMutateEventArgs>(ContactService_ContactRemoved);

            #region Circle events

            // These are circle events. They will be fired after corresponding circle operation completed.
            messenger.ContactService.CreateCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_CircleCreated);
            messenger.ContactService.JoinedCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_JoinedCircle);
            messenger.ContactService.JoinCircleInvitationReceived += new EventHandler<CircleEventArgs>(ContactService_JoinCircleInvitationReceived);
            messenger.ContactService.ExitCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_ExitCircle);
            messenger.ContactService.CircleMemberJoined += new EventHandler<CircleMemberEventArgs>(ContactService_CircleMemberJoined);
            messenger.ContactService.CircleMemberLeft += new EventHandler<CircleMemberEventArgs>(ContactService_CircleMemberLeft);
            #endregion

            #region MPOP related events

            // This event will be fired after a chat window has been closed on a different login end point.
            // You will get this notification to decide whether to close the local chat window as well.
            messenger.Nameserver.RemoteEndPointCloseIMWindow += new EventHandler<CloseIMWindowEventArgs>(Nameserver_RemoteEndPointCloseIMWindow);
            
            #endregion


            #region Offline Message Operation events

            // OIMReceived will be triggered after receved an Offline Message.
            messenger.OIMService.OIMReceived += new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived);

            // Triggered after the send operation for an Offline Message has been completed.
            // If the operation failed, there will contains an error in the event args.
            messenger.OIMService.OIMSendCompleted += new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted); 

            #endregion
            // This event will be triggered after finished getting your contacts recent updates.
            messenger.WhatsUpService.GetWhatsUpCompleted += new EventHandler<GetWhatsUpCompletedEventArgs>(WhatsUpService_GetWhatsUpCompleted);

            #region Webservice Error handler

            // Handle Service Operation Errors
            //In most cases, these error are not so important.
            messenger.ContactService.ServiceOperationFailed += new EventHandler<ServiceOperationFailedEventArgs>(ServiceOperationFailed);
            messenger.OIMService.ServiceOperationFailed += new EventHandler<ServiceOperationFailedEventArgs>(ServiceOperationFailed);
            messenger.StorageService.ServiceOperationFailed += new EventHandler<ServiceOperationFailedEventArgs>(ServiceOperationFailed);
            messenger.WhatsUpService.ServiceOperationFailed += new EventHandler<ServiceOperationFailedEventArgs>(ServiceOperationFailed); 

            #endregion
        }


        public static class ImageIndexes
        {
            public const int Closed = 0;
            public const int Open = 1;
            public const int CircleOnline = 2;
            public const int CircleOffline = 3;

            public const int Online = 4;
            public const int Busy = 5;
            public const int Away = 6;
            public const int Idle = 7;
            public const int Hidden = 8;
            public const int Offline = 9;

            // Show always (0/0)
            public const string FavoritesNodeKey = "__10F";
            public const string CircleNodeKey = "__20C";
            public const string FacebookNodeKey = "__25F";
            // Sort by status (0)
            public const string MobileNodeKey = "__30M";
            public const string OnlineNodeKey = "__32N";
            public const string OfflineNodeKey = "__34F";
            // Sort by categories (0/0)
            public const string NoGroupNodeKey = "ZZZZZ";

            public static int GetCircleStatusImageIndex(PresenceStatus status)
            {
                switch (status)
                {
                    case PresenceStatus.Online:
                        return CircleOnline;
                    case PresenceStatus.Offline:
                        return CircleOffline;
                }

                return CircleOffline;
            }
            
            public static int GetContactStatusImageIndex(PresenceStatus status)
            {
                switch (status)
                {
                    case PresenceStatus.Online:
                        return Online;

                    case PresenceStatus.Busy:
                    case PresenceStatus.Phone:
                        return Busy;

                    case PresenceStatus.BRB:
                    case PresenceStatus.Away:
                    case PresenceStatus.Lunch:
                        return Away;

                    case PresenceStatus.Idle:
                        return Idle;
                    case PresenceStatus.Hidden:
                        return Hidden;

                    case PresenceStatus.Offline:
                        return Offline;

                    default:
                        return Offline;
                }
            }
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {




            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.closed);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.open);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.circle);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.circleoffline);

            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.online);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.busy);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.away);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.idle);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.hidden);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.offline);

            Version dllVersion = messenger.GetType().Assembly.GetName().Version;
            Text += " (v" + dllVersion.Major + "." + dllVersion.Minor + "." + dllVersion.Build + " r" + dllVersion.Revision + ")";
            treeViewFavoriteList.TreeViewNodeSorter = StatusSorter.Default;

            comboStatus.SelectedIndex = 0;

            if (toolStripSortByStatus.Checked)
                SortByStatus(null);
            else
                SortByGroup(null);

            // ******* Listen traces *****
            traceform.Show();
        }


        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Messenger.Connected)
            {
                Messenger.Nameserver.SignedOff -= Nameserver_SignedOff;
                
                ResetAll();
                Messenger.Disconnect();
            }

            traceform.Close();
        }

        private void AutoGroupMessageReply(Contact circle)
        {
            if (Messenger.Owner.Status != PresenceStatus.Hidden || Messenger.Owner.Status != PresenceStatus.Offline)
                circle.SendMessage(new TextMessage("MSNPSharp example client auto reply."));
        }

        void ContactService_ExitCircle(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void ContactService_JoinedCircle(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
            messenger.ContactService.ExitCircle(e.Circle); //Demostrate how to leave a circle.
        }


        void ContactService_CircleMemberLeft(object sender, CircleMemberEventArgs e)
        {
            //RefreshCircleList(sender, e);
        }

        void ContactService_CircleMemberJoined(object sender, CircleMemberEventArgs e)
        {
            //RefreshCircleList(sender, e);
        }

        void ContactService_JoinCircleInvitationReceived(object sender, CircleEventArgs e)
        {
            messenger.ContactService.AcceptCircleInvitation(e.Circle);
        }

        void ContactService_CircleCreated(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void RefreshCircleList(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<EventArgs>(RefreshCircleList), new object[] { sender, e });
                return;
            }

            if (toolStripSortByStatus.Checked)
            {
                SortByStatus(null);
            }
            else
            {
                SortByGroup(null);
            }
        }

        void ServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Method + ": " + e.Exception.ToString(), sender.GetType().Name); 
        }

        void ContactService_SynchronizationCompleted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(ContactService_SynchronizationCompleted), sender, e);
                return;
            }

            syncContactListCompleted = true;
            lblNews.Text = "Getting your friends' news...";
            messenger.WhatsUpService.GetWhatsUp(200);
        }

        List<ActivityDetailsType> activities = new List<ActivityDetailsType>();
        void WhatsUpService_GetWhatsUpCompleted(object sender, GetWhatsUpCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<GetWhatsUpCompletedEventArgs>(WhatsUpService_GetWhatsUpCompleted), new object[] { sender, e });
                return;
            }

            if (e.Error != null)
            {
                lblNews.Text = "ERROR: " + e.Error.ToString();
            }
            else
            {
                activities.Clear();

                foreach (ActivityDetailsType activityDetails in e.Response.Activities)
                {
                    // Show status news
                    if (activityDetails.ApplicationId == "6262816084389410")
                    {
                        activities.Add(activityDetails);
                    }

                    Contact c = messenger.ContactList.GetContactByCID(long.Parse(activityDetails.OwnerCID));

                    if (c != null)
                    {                        
                        c.Activities.Add(activityDetails);                        
                    }
                }

                if (activities.Count == 0)
                {
                    lblNews.Text = "No news";
                    return;
                }

                lblNewsLink.Text = "Get Feeds";
                lblNewsLink.Tag = e.Response.FeedUrl;
                
                ShowNextNews();
            }
        }

        private int currentActivity = 0;
        private bool activityForward = true;
        
        private void ShowNextNews()
        {
            if (currentActivity >= activities.Count || currentActivity < 0)
            {
                currentActivity = 0;
            }

            ActivityDetailsType activitiy = activities[currentActivity];
            if (activitiy.ApplicationId == "6262816084389410")
            {
                string name = string.Empty;
                string status = string.Empty;

                foreach (TemplateVariableBaseType t in activitiy.TemplateVariables)
                {
                    if (t is PublisherIdTemplateVariable)
                    {
                        name = ((PublisherIdTemplateVariable)t).NameHint;
                    }
                    else if (t is TextTemplateVariable)
                    {
                        status = ((TextTemplateVariable)t).Value;
                    }
                }

                lblNews.Text = name + ": " + status;

                Contact c = messenger.ContactList.GetContactByCID(long.Parse(activitiy.OwnerCID));

                if (c != null)
                {
                    if (c.DisplayImage != null && c.DisplayImage.Image != null)
                    {
                        pbNewsPicture.Image = c.DisplayImage.Image;
                    }
                    else if (c.UserTileURL != null)
                    {
                        // Everytime we call this, a new thread is created, in *nix system
                        // this is really harmful for the performance and is easily cause deadlock
                        // in Mac (I think Mono's threading implementation in Mac is really bad).
                        // Calling this LoadAsync in Mono sometimes will cause ThreadInterupt exception,
                        // which might be a bug of Mono's implementation.
                        // pbNewsPicture.LoadAsync(c.UserTileURL.AbsoluteUri);

                        HttpAsyncDataDownloader.BeginDownload(c.UserTileURL.AbsoluteUri + "?t=" + System.Web.HttpUtility.UrlEncode(Messenger.StorageTicket), 
                            new EventHandler<ObjectEventArgs>(SetUserTileToPictureBox), 
                            Messenger.ConnectivitySettings.WebProxy);
                    }
                    else
                    {
                        pbNewsPicture.Image = null;
                    }
                }
            }
            if (activityForward)
                currentActivity++;
            else
                currentActivity--;
        }
        
        private void SetUserTileToPictureBox(object sender, ObjectEventArgs e)
        {
            if(pbNewsPicture.InvokeRequired)
            {
                pbNewsPicture.Invoke(new EventHandler<ObjectEventArgs>(SetUserTileToPictureBox), new object[]{ sender, e});
            }
            else
            {
                try
                {
                    Image img = Image.FromStream(new MemoryStream((byte[])e.Object));
                    pbNewsPicture.Image = img;
                }
                catch(Exception ex)
                {
                    Trace.WriteLine("Get UserTile error: " + ex.Message);
                }
            }
        }

        private void cmdPrev_Click(object sender, EventArgs e)
        {
            if (activities.Count > 0)
            {
                activityForward = false;

                if (currentActivity > 0)
                    currentActivity--;
                else
                    currentActivity = activities.Count - 1;

                ShowNextNews();
            }
        }

        private void cmdNext_Click(object sender, EventArgs e)
        {
            if (activities.Count > 0)
            {
                activityForward = true;

                if (currentActivity < activities.Count)
                    currentActivity++;

                ShowNextNews();
            }
        }

        private void lblNewsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (lblNewsLink.Tag != null)
            {
                Process.Start(lblNewsLink.Tag.ToString());
            }
        }

        void Owner_PersonalMessageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<EventArgs>(Owner_PersonalMessageChanged), new object[] { sender, e });
                return;
            }

            lblName.Text = Messenger.Owner.Name;

            if (Messenger.Owner.PersonalMessage != null && Messenger.Owner.PersonalMessage.Message != null)
            {
                lblPM.Text = System.Web.HttpUtility.HtmlDecode(Messenger.Owner.PersonalMessage.Message);
            }
        }

        void Owner_DisplayImageChanged(object sender, DisplayImageChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                displayImageBox.BeginInvoke(new EventHandler<DisplayImageChangedEventArgs>(Owner_DisplayImageChanged), new object[] { sender, e });
                return;
            }

            displayImageBox.Image = e.NewDisplayImage.Image;
        }
        

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClientForm());
        }

        void Nameserver_OwnerVerified(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(Nameserver_OwnerVerified), sender, e);
                return;
            }

            Messenger.Owner.CoreProfileUpdated += new EventHandler<EventArgs>(Owner_CoreProfileUpdated);
            Messenger.Owner.SceneImageChanged += new EventHandler<SceneImageChangedEventArgs>(Owner_SceneImageChanged);
            Messenger.Owner.DisplayImageChanged += new EventHandler<DisplayImageChangedEventArgs>(Owner_DisplayImageChanged);
            Messenger.Owner.PersonalMessageChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            Messenger.Owner.ScreenNameChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            Messenger.Owner.PlacesChanged += new EventHandler<PlaceChangedEventArgs>(Owner_PlacesChanged);
            Messenger.Owner.StatusChanged += new EventHandler<StatusChangedEventArgs>(Owner_StatusChanged);
        }

        void Owner_CoreProfileUpdated(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Owner profile updated: ");

            lock (Messenger.Owner.CoreProfile)
            {
                foreach (KeyValuePair<string, object> kv in Messenger.Owner.CoreProfile)
                {
                    sb.AppendLine(kv.Key + " = " + kv.Value);
                }
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, sb.ToString(), GetType().Name);
        }

        void Owner_SceneImageChanged(object sender, SceneImageChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<SceneImageChangedEventArgs>(Owner_SceneImageChanged), sender, e);
                return;
            }

            tableLayoutPanel3.BackgroundImage = e.NewSceneImage.Image;

        }

        void Nameserver_ContactBlockedStatusChanged(object sender, ContactBlockedStatusChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ContactBlockedStatusChangedEventArgs>(Nameserver_ContactBlockedStatusChanged), new object[] { sender, e });
            }
            else
            {
                UpdateContactlist(sender, e);
            }
        }

        void Nameserver_ContactOnline(object sender, ContactStatusChangedEventArgs e)
        {
            Invoke(new EventHandler<ContactStatusChangedEventArgs>(ContactOnlineOffline), new object[] { sender, e });
        }

        void Nameserver_ContactOffline(object sender, ContactStatusChangedEventArgs e)
        {
            Invoke(new EventHandler<ContactStatusChangedEventArgs>(ContactOnlineOffline), new object[] { sender, e });
        }

        void ContactOnlineOffline(object sender, ContactStatusChangedEventArgs e)
        {
            if (toolStripSortByStatus.Checked)
                SortByStatus(e);
            else
                SortByGroup(e);
        }

        void Nameserver_PingAnswer(object sender, PingAnswerEventArgs e)
        {
            nextPing = e.SecondsToWait;
        }

        void Nameserver_OIMReceived(object sender, OIMReceivedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived), sender, e);
                return;
            }

            if (DialogResult.Yes == MessageBox.Show(
                "OIM received at : " + e.ReceivedTime + "\r\nFrom : " + e.NickName + " (" + e.Email + ") " + ":\r\n"
                + e.Message + "\r\n\r\n\r\nClick yes, if you want to receive this message next time you login.",
                "Offline Message from " + e.Email, MessageBoxButtons.YesNoCancel))
            {
                e.IsRead = false;
            }
        }

        void Nameserver_TextMessageReceived(object sender, TextMessageArrivedEventArgs e)
        {
            MessageManager_MessageArrived(sender, e);
        }

        void Nameserver_TypingMessageReceived(object sender, TypingArrivedEventArgs e)
        {
            MessageManager_MessageArrived(sender, e);
        }

        void Nameserver_NudgeReceived(object sender, NudgeArrivedEventArgs e)
        {
            MessageManager_MessageArrived(sender, e);
        }

        void Nameserver_EmoticonReceived(object sender, EmoticonArrivedEventArgs e)
        {
            MessageManager_MessageArrived(sender, e);
        }

        void Nameserver_WinkReceived(object sender, WinkEventArgs e)
        {
            MessageManager_MessageArrived(sender, e);
        }

        void Nameserver_RemoteEndPointCloseIMWindow(object sender, CloseIMWindowEventArgs e)
        {
            if (e.Sender != null && e.SenderEndPoint != null)
            {
                string partiesString = string.Empty;
                foreach (Contact party in e.Parties)
                {
                    partiesString += party.ToString() + "\r\n";
                }

                Trace.WriteLine("[Output by Client] User at End Point: " + e.SenderEndPoint.ToString() + " has closed the IM window.\r\n" +
                                "Parties in the conversation: \r\n" +
                                partiesString);
            }
        }

        void MessageManager_MessageArrived(object sender, MessageArrivedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<MessageArrivedEventArgs>(MessageManager_MessageArrived), new object[] { sender, e });
            }
            else
            {
                foreach (ConversationForm cform in ConversationForms)
                {
                    if (cform.RemoteContact.IsSibling(e.Sender))
                    {
                        cform.OnMessageReceived(sender, e);
                        return;
                    }
                }

                if (!(e is TypingArrivedEventArgs))
                {
                    CreateConversationForm(e.Sender).OnMessageReceived(sender, e);
                }
            }
        }

        void OIMService_OIMSendCompleted(object sender, OIMSendCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted), sender, e);
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "OIM Send Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ContactService_ContactRemoved(object sender, ListMutateEventArgs e)
        {
            Trace.WriteLine(e.Contact.Hash + " removed from the " + e.AffectedList + " role list.");

            if (!syncContactListCompleted)  //This add/remove was caused by initial contact list sync, don't process it.
                return;

            if (toolStripSortByStatus.Checked)
                SortByStatus(e.Contact, e.Contact.Via);
            else
                SortByGroup(e.Contact, e.Contact.Via);
        }

        void ContactService_ContactAdded(object sender, ListMutateEventArgs e)
        {
            Trace.WriteLine(e.Contact.Hash + " added to the " + e.AffectedList + " role list.");

            if (!syncContactListCompleted)  //This add/remove was caused by initial contact list sync, don't process it.
                return;

            if (toolStripSortByStatus.Checked)
                SortByStatus(e.Contact, e.Contact.Via);
            else
                SortByGroup(e.Contact, e.Contact.Via);
        }

        void ContactService_ReverseRemoved(object sender, ContactEventArgs e)
        {
            Trace.WriteLine(e.Contact.Hash + " removed you their contact (forward) list.");
        }

        void Nameserver_ReverseAdded(object sender, ContactEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded), sender, e);
                return;
            }

            Contact contact = e.Contact;
            if (true /* || messenger.Nameserver.BotMode */)  //If you want your provisioned account in botmode to fire ReverseAdded event, uncomment this.
            {
                // Show pending window if it is necessary.
                if (contact.OnPendingList)
                {
                    ReverseAddedForm form = new ReverseAddedForm(contact);
                    form.FormClosed += delegate(object f, FormClosedEventArgs fce)
                    {
                        form = f as ReverseAddedForm;
                        if (DialogResult.OK == form.DialogResult)
                        {
                            if (form.AddAsFriend)
                            {
                                messenger.ContactService.AddNewContact(contact.Account);
                                System.Threading.Thread.Sleep(200);
                            }
                            else
                            {
                                messenger.ContactService.RemoveContact(contact, form.Block);
                            }                           


                            System.Threading.Thread.Sleep(200);
                            contact.OnPendingList = false;
                        }
                        return;
                    };
                    form.Show(this);
                }
                else
                {
                    MessageBox.Show(contact.Account + " accepted your invitation and added you their contact list.");
                }
            }
        }


        /// <summary>
        /// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
        /// </summary>
        private delegate void SetStatusDelegate(string status);

        private void SetStatusSynchronized(string status)
        {
            statusBar.Text = status;
        }

        private void SetStatus(string status)
        {
            if (InvokeRequired)
            {
                this.Invoke(new SetStatusDelegate(SetStatusSynchronized), new object[] { status });
            }
            else
            {
                SetStatusSynchronized(status);
            }
        }

        /// <summary>
        /// Sign into the messenger network. Disconnect first if a connection has already been established.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loginButton_Click(object sender, System.EventArgs e)
        {
            switch (Convert.ToInt32(loginButton.Tag))
            {
                case 0: // not connected -> connect
                    {
                        if (messenger.Connected)
                        {
                            SetStatus("Disconnecting from server");
                            messenger.Disconnect();
                        }

                        // set the credentials, this is ofcourse something every MSNPSharp program will need to implement.
                        messenger.Credentials = new Credentials(accountTextBox.Text, passwordTextBox.Text);
                       
                        // inform the user what is happening and try to connecto to the messenger network.
                        SetStatus("Connecting to server");
                        messenger.Connect();

                        displayImageBox.Image = global::MSNPSharpClient.Properties.Resources.loading;

                        loginButton.Tag = 1;
                        loginButton.Text = "Cancel";
                        initialExpand = true;

                        // note that Messenger.Connect() will run in a seperate thread and return immediately.
                        // it will fire events that informs you about the status of the connection attempt. 
                        // these events are registered in the constructor.

                    }
                    break;

                case 1: // connecting -> cancel
                    {
                        if (messenger.Connected)
                            messenger.Disconnect();

                        if (toolStripSortByStatus.Checked)
                            SortByStatus(null);
                        else
                            SortByGroup(null);

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = false;
                        comboPlaces.Visible = false;
                        initialExpand = true;

                    }
                    break;

                case 2: // connected -> disconnect
                    {
                        if (messenger.Connected)
                            messenger.Disconnect();

                        if (toolStripSortByStatus.Checked)
                            SortByStatus(null);
                        else
                            SortByGroup(null);

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = true;
                        comboPlaces.Visible = true;
                        initialExpand = true;

                    }
                    break;
            }
        }

        private void login_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == '\r') || (e.KeyChar == '\r'))
            {
                loginButton.PerformClick();
            }
        }

        void Owner_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<StatusChangedEventArgs>(Owner_StatusChanged), new object[] { sender, e });
                return;
            }

            if (messenger.Nameserver.IsSignedIn)
            {
                comboStatus.SelectedIndex = comboStatus.FindString(GetStatusString(Messenger.Owner.Status));
            }
        }

        private string GetStatusString(PresenceStatus status)
        {
            switch (status)
            {
                case PresenceStatus.Away:
                case PresenceStatus.BRB:
                case PresenceStatus.Lunch:
                case PresenceStatus.Idle:
                    return "Away";
                case PresenceStatus.Online:
                    return "Online";
                case PresenceStatus.Offline:
                    return "Offline";
                case PresenceStatus.Hidden:
                    return "Hidden";
                case PresenceStatus.Busy:
                case PresenceStatus.Phone:
                    return "Busy";

            }

            return "Offline";
        }

        private void comboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(comboStatus_SelectedIndexChanged), sender, e);
                return;
            }

            PresenceStatus newstatus = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Text);

            if (messenger.Connected)
            {
                if (newstatus == PresenceStatus.Offline)
                {
                    PresenceStatus old = Messenger.Owner.Status;

                    foreach (ConversationForm convform in ConversationForms)
                    {
                        if (convform.Visible == true)
                        {
                            if (MessageBox.Show("You are signing out from example client. All windows will be closed.", "Sign out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                            {
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    Messenger.Disconnect();
                    comboStatus.SelectedIndex = 0;
                }
                else
                {
                    Messenger.Owner.Status = newstatus;
                }
            }
            else if (newstatus == PresenceStatus.Offline)
            {
                comboStatus.SelectedIndex = 0;
            }
        }

        private void comboStatus_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;

            e.Graphics.FillRectangle(Brushes.White, e.Bounds);
            if ((e.State & DrawItemState.Selected) != DrawItemState.None)
                e.DrawBackground();

            PresenceStatus newstatus = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Items[e.Index].ToString());
            Brush brush = Brushes.Green;

            switch (newstatus)
            {
                case PresenceStatus.Online:
                    brush = Brushes.Green;
                    break;

                case PresenceStatus.Busy:
                    brush = Brushes.Red;
                    break;

                case PresenceStatus.Away:
                    brush = Brushes.Orange;
                    break;

                case PresenceStatus.Hidden:
                    brush = Brushes.Gray;
                    break;

                case PresenceStatus.Offline:
                    brush = Brushes.Black;
                    break;
            }

            Point imageLocation = new Point(e.Bounds.X + 2, e.Bounds.Y + 2);
            e.Graphics.FillRectangle(brush, new Rectangle(imageLocation, new Size(12, 12)));

            PointF textLocation = new PointF(imageLocation.X + 16, imageLocation.Y);
            e.Graphics.DrawString(newstatus.ToString(), PARENT_NODE_FONT, Brushes.Black, textLocation);
        }

        private void comboPlaces_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboPlaces.SelectedIndex > 0)
            {
                string place = comboPlaces.Text.Split(' ')[comboPlaces.Text.Split(' ').Length - 1];

                if (comboPlaces.SelectedIndex == 1)
                {
                    comboPlaces.Visible = false;
                    Messenger.Owner.SignoutFrom(NSMessageHandler.MachineGuid);
                }
                else if (comboPlaces.SelectedIndex == 2)
                {
                    comboPlaces.Visible = false;
                    Messenger.Owner.SignoutFromEverywhere();
                }
                else if (comboPlaces.SelectedIndex > 2)
                {
                    Guid placeId = new Guid(place);
                    Messenger.Owner.SignoutFrom(placeId);
                }
            }
        }

        void cbRobotMode_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox cbBotMode = sender as ComboBox;
            messenger.Nameserver.BotMode = cbRobotMode.Checked;
        }

        private void Owner_PlacesChanged(object sender, PlaceChangedEventArgs e)
        {
            if (comboPlaces.InvokeRequired)
            {
                comboPlaces.BeginInvoke(new EventHandler<PlaceChangedEventArgs>(Owner_PlacesChanged), sender, e);
                return;
            }

            // if (Messenger.Owner.Places.Count > 1)
            {
                comboPlaces.BeginUpdate();
                comboPlaces.Items.Clear();
                comboPlaces.Items.Add("(" + Messenger.Owner.PlaceCount + ") Places");

                comboPlaces.Items.Add("Signout from here (" + Messenger.Owner.EpName + ")");
                comboPlaces.Items.Add("Signout from everywhere");

                Dictionary<Guid, EndPointData> copyPlaces = new Dictionary<Guid, EndPointData>(Messenger.Owner.EndPointData);

                foreach (KeyValuePair<Guid, EndPointData> keyvalue in copyPlaces)
                {
                    PrivateEndPointData ipep = keyvalue.Value as PrivateEndPointData;
                    if (ipep.Id != NSMessageHandler.MachineGuid)
                        comboPlaces.Items.Add(ipep.Name + " " + ipep.Id);
                }

                comboPlaces.SelectedIndex = 0;
                comboPlaces.Visible = true;
                comboPlaces.EndUpdate();
            }
        }

        private void NameserverProcessor_ConnectionEstablished(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(NameserverProcessor_ConnectionEstablished), sender, e);
                return;
            }

            messenger.Nameserver.AutoSynchronize = !cbRobotMode.Checked;

            SetStatus("Connected to server");
        }

        private void Nameserver_SignedIn(object sender, EventArgs e)
        {

            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler(Nameserver_SignedIn), sender, e);
                return;
            }

            SetStatus("Signed into the messenger network as " + Messenger.Owner.Name);

            // set our presence status
            loginButton.Tag = 2;
            loginButton.Text = "Sign off";
            pnlNameAndPM.Visible = true;
            comboPlaces.Visible = true;

            Messenger.Owner.Status = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Text);

            propertyGrid.SelectedObject = Messenger.Owner;
            displayImageBox.Image = Messenger.Owner.DisplayImage.Image;
            displayImageBox.SizeMode = PictureBoxSizeMode.Zoom;

            if (Messenger.Owner.SceneImage != null && Messenger.Owner.SceneImage.Image != null)
            {
                tableLayoutPanel3.BackgroundImage = Messenger.Owner.SceneImage.Image;
            }

            UpdateContactlist(sender, e);
        }

        private void Nameserver_SignedOff(object sender, SignedOffEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff), sender, e);
                return;
            }

            SetStatus("Signed off from the messenger network");
            ResetAll();
        }

        private void ResetAll()
        {
            syncContactListCompleted = false;
            tmrKeepOnLine.Enabled = false;

            displayImageBox.Image = null;
            displayImageBox.SizeMode = PictureBoxSizeMode.CenterImage;

            loginButton.Tag = 0;
            loginButton.Text = "> Sign in";
            pnlNameAndPM.Visible = false;
            comboPlaces.Visible = false;
            propertyGrid.SelectedObject = null;
            tableLayoutPanel3.BackgroundImage = Properties.Resources.my_scene;

            treeViewFavoriteList.Nodes.Clear();
            treeViewFilterList.Nodes.Clear();

            if (toolStripSortByStatus.Checked)
                SortByStatus(null);
            else
                SortByGroup(null);

            comboPlaces.Visible = false;
            comboPlaces.Items.Clear();

            List<ConversationForm> convFormsClone = new List<ConversationForm>(ConversationForms);
            foreach(ConversationForm convForm in convFormsClone)
            {
                convForm.Close();
            }
        }

        private void Nameserver_ExceptionOccurred(object sender, ExceptionEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred), new object[] { sender, e });
            }
            else
            {

                // ignore the unauthorized exception, since we're handling that error in another method.
                if (e.Exception is UnauthorizedException)
                    return;

                MessageBox.Show(e.Exception.ToString(), "Nameserver exception");
            }
        }

        private void NameserverProcessor_ConnectingException(object sender, ExceptionEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred), new object[] { sender, e });
            }
            else
            {
                MessageBox.Show(e.Exception.ToString(), "Connecting exception");
                SetStatus("Connecting failed");
            }
        }

        private void Nameserver_AuthenticationError(object sender, ExceptionEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred), new object[] { sender, e });
            }
            else
            {
                MessageBox.Show("Authentication failed, check your account or password.\r\n Error detail:\r\n " + e.Exception.InnerException.Message + "\r\n"
                    + " StackTrace:\r\n " + e.Exception.InnerException.StackTrace
                    , "Authentication Error");
                SetStatus("Authentication failed");
            }
        }

        /// <summary>
        /// Updates the treeView.
        /// </summary>
        private void UpdateContactlist(object sender, EventArgs e)
        {
            if (messenger.Connected == false)
                return;

            if (toolStripSortByStatus.Checked)
                SortByStatus(null);
            else
                SortByGroup(null);

            tmrKeepOnLine.Enabled = true;
        }


        /// <summary>
        /// Notifies the user of errors which are send by the MSN server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Nameserver_ServerErrorReceived(object sender, MSNErrorEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<MSNErrorEventArgs>(Nameserver_ServerErrorReceived), new object[] { sender, e });
            }
            else
            {
                // when the MSN server sends an error code we want to be notified.
                MessageBox.Show("Error code: " + e.MSNError.ToString() + " (" + ((int)e.MSNError) + ")" +
                    "\r\n\r\nDescription: " + e.Description, "Server error received");
                SetStatus("Server error received");
            }
        }

        /// <summary>
        /// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
        /// </summary>
        private delegate ConversationForm CreateConversationDelegate(Contact remote);

        private ConversationForm CreateConversationForm(Contact remote)
        {

            // create a new conversation. However do not show the window untill a message is received.
            // for example, a conversation will be created when the remote client sends wants to send
            // you a file. You don't want to show the conversation form in that case.
            ConversationForm conversationForm = new ConversationForm(Messenger, remote);
            // do this to create the window handle. Otherwise we are not able to call Invoke() on the
            // conversation form later.
            conversationForm.Handle.ToInt32();
            ConversationForms.Add(conversationForm);

            conversationForm.FormClosing += delegate
            {
                ConversationForms.Remove(conversationForm);
            };

            return conversationForm;
        }

        /// <summary>
        /// Asks the user to accept or deny the incoming filetransfer/activity invitation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p2pHandler_InvitationReceived(object sender, P2PSessionEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<P2PSessionEventArgs>(p2pHandler_InvitationReceived), sender, e);
                return;
            }

            if (e.P2PSession.Application is MSNPSharp.Apps.FileTransfer)
            {
                FileTransferForm ftf = new FileTransferForm(e.P2PSession);
                ftf.Show(this);
            }
            else if (e.P2PSession.Application is P2PActivity)
            {
                P2PActivity p2pActivity = e.P2PSession.Application as P2PActivity;

                if (MessageBox.Show(
                     e.P2PSession.Remote.Name +
                    " wants to invite you to join an activity.\r\n\r\nActivity name: " +
                    p2pActivity.ActivityName + "\r\nAppID: " + 
                    p2pActivity.ApplicationId + "\r\nEufGuid: " +
                    p2pActivity.ApplicationEufGuid,
                    "Activity invitation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    e.P2PSession.Accept();
                }
            }
            else if (e.P2PSession.Application is P2PApplication)
            {
                if (MessageBox.Show(
                     e.P2PSession.Remote.Name +
                    " wants to invite you to join an activity.\r\nActivity name: " +
                    e.P2PSession.Application.ApplicationEufGuid + "\r\nAppID: " + e.P2PSession.Application.ApplicationId,
                    "Activity invitation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    e.P2PSession.Accept();
                }
            }
        }

        private int nextPing = 50;
        private void tmrKeepOnLine_Tick(object sender, EventArgs e)
        {
            if (nextPing > 0)
                nextPing--;
            if (nextPing == 0)
            {
                nextPing--;
                messenger.Nameserver.SendPing();
            }
        }



        private static Font PARENT_NODE_FONT = new Font("Tahoma", 8f, FontStyle.Bold);
        private static Font PARENT_NODE_FONT_BANNED = new Font("Tahoma", 8f, FontStyle.Bold | FontStyle.Strikeout);
        private static Font USER_NODE_FONT = new Font("Tahoma", 8f);
        private static Font USER_NODE_FONT_BANNED = new Font("Tahoma", 8f, FontStyle.Strikeout);
        public class StatusSorter : IComparer
        {
            public static StatusSorter Default = new StatusSorter();
            private StatusSorter()
            {
            }
            public int Compare(object x, object y)
            {
                TreeNode node = x as TreeNode;
                TreeNode node2 = y as TreeNode;

                if (node.Tag is string && node2.Tag is string)
                {
                    // Online (0), Offline (1)
                    return node.Tag.ToString().CompareTo(node2.Tag.ToString());
                }
                else if (node.Tag is Contact && (node.Tag as Contact).ClientType == IMAddressInfoType.Circle &&
                    node2.Tag is Contact && (node2.Tag as Contact).ClientType == IMAddressInfoType.Circle)
                {

                    return ((Contact)node.Tag).AddressBookId.CompareTo(((Contact)node2.Tag).AddressBookId);

                }
                else if (node.Tag is Contact && node2.Tag is Contact)
                {
                    if (((Contact)node.Tag).Online == ((Contact)node2.Tag).Online)
                    {
                        return string.Compare(((Contact)node.Tag).Name, ((Contact)node2.Tag).Name, StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (((Contact)node.Tag).Online)
                        return -1;
                    else if (((Contact)node2.Tag).Online)
                        return 1;
                }
                else if (node.Tag is ContactGroup && node2.Tag is ContactGroup)
                {
                    return string.Compare(((ContactGroup)node.Tag).Name, ((ContactGroup)node2.Tag).Name, StringComparison.CurrentCultureIgnoreCase);
                }
                return 0;
            }
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if ((e.Button == MouseButtons.Left) && (e.Node.Level != 0))
            {
                Contact selectedContact = treeViewFavoriteList.SelectedNode.Tag as Contact;

                if (selectedContact != null)
                {
                    propertyGrid.SelectedObject = selectedContact;

                    if (selectedContact.Online && (!(selectedContact.ClientType == IMAddressInfoType.Circle)))
                    {
                        sendIMMenuItem.PerformClick();
                    }
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.Node.Level == 0)
                {
                    if (e.Node.IsExpanded || (e.Node.ImageIndex == ImageIndexes.Open))
                    {
                        e.Node.Collapse();
                        e.Node.ImageIndex = e.Node.SelectedImageIndex = ((e.Node.Tag is Contact) && (e.Node.Tag as Contact).ClientType == IMAddressInfoType.Circle) ? ((e.Node.Tag as Contact).Online ? ImageIndexes.CircleOnline : ImageIndexes.CircleOffline) : ImageIndexes.Closed;
                    }
                    else if (e.Node.Nodes.Count > 0)
                    {
                        e.Node.Expand();
                        e.Node.ImageIndex = e.Node.SelectedImageIndex = ((e.Node.Tag is Contact) && (e.Node.Tag as Contact).ClientType == IMAddressInfoType.Circle) ? ((e.Node.Tag as Contact).Online ? ImageIndexes.CircleOnline : ImageIndexes.CircleOffline) : ImageIndexes.Open;
                    }
                    if (e.Node.Tag is ContactGroup || ((e.Node.Tag is Contact) && (e.Node.Tag as Contact).ClientType == IMAddressInfoType.Circle))
                    {
                        propertyGrid.SelectedObject = e.Node.Tag;
                    }
                }
                else
                {
                    Contact selectedContact = (Contact)e.Node.Tag;

                    if (selectedContact.ClientType == IMAddressInfoType.Circle)
                    {
                        if (e.Node.IsExpanded)
                        {
                            e.Node.Collapse();
                        }
                        else
                        {
                            e.Node.Expand();
                        }
                    }

                    propertyGrid.SelectedObject = selectedContact;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (e.Node.Tag is Contact && e.Node.Level > 0)
                {
                    treeViewFavoriteList.SelectedNode = e.Node;
                    Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;

                    if (contact.AppearOffline)
                    {
                        appearOfflineMenuItem.Visible = false;
                        appearOnlineMenuItem.Visible = true;
                    }
                    else
                    {
                        appearOfflineMenuItem.Visible = true;
                        appearOnlineMenuItem.Visible = false;
                    }

                    if (contact.Online)
                    {
                        sendIMMenuItem.Visible = true;
                        sendOIMMenuItem.Visible = false;

                        toolStripMenuItem2.Visible = true;
                    }
                    else
                    {
                        sendIMMenuItem.Visible = false;
                        sendOIMMenuItem.Visible = true;

                        toolStripMenuItem2.Visible = false;
                    }

                    deleteMenuItem.Visible = contact.Guid != Guid.Empty;

                    Point point = treeViewFavoriteList.PointToScreen(new Point(e.X, e.Y));
                    userMenuStrip.Show(point.X - userMenuStrip.Width, point.Y);
                }
                else if (e.Node.Tag is ContactGroup)
                {
                    treeViewFavoriteList.SelectedNode = e.Node;

                    Point point = treeViewFavoriteList.PointToScreen(new Point(e.X, e.Y));
                    groupContextMenu.Show(point.X - groupContextMenu.Width, point.Y);
                }
            }
        }

        private void blockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeViewFavoriteList.SelectedNode;

            ((Contact)selectedNode.Tag).AppearOffline = true;
            selectedNode.NodeFont = USER_NODE_FONT_BANNED;

        }

        private void unblockMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeViewFavoriteList.SelectedNode;

            ((Contact)selectedNode.Tag).AppearOffline = false;
            selectedNode.NodeFont = USER_NODE_FONT_BANNED;
        }

        private void deleteMenuItem_Click(object sender, EventArgs e)
        {
            Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            RemoveContactForm form = new RemoveContactForm();
            form.FormClosed += delegate(object f, FormClosedEventArgs fce)
            {
                form = f as RemoveContactForm;
                if (DialogResult.OK == form.DialogResult)
                {
                    messenger.ContactService.RemoveContact(contact, form.Block);
                }
            };
            form.ShowDialog(this);
        }

        private void sendMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact contact = treeViewFavoriteList.SelectedNode.Tag as Contact;

            foreach (ConversationForm conv in ConversationForms)
            {
                if (contact.IsSibling(conv.RemoteContact))
                {
                    if (conv.WindowState == FormWindowState.Minimized || conv.Visible == false)
                        conv.Show();

                    conv.Activate();
                    return;
                }
            }

            ConversationForm form = CreateConversationForm(contact);
            form.Show();
        }

        private void sendOfflineMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;
            messenger.OIMService.SendOIMMessage(selectedContact, new TextMessage("MSNPSharp offline message test."));

        }

        private void sendMIMMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            if (selectedContact.MobileAccess || selectedContact.ClientType == IMAddressInfoType.Telephone)
            {
                selectedContact.SendMobileMessage("MSNP mobile message");
            }
            else
                MessageBox.Show("This contact is not able to receive mobile messages");
        }


        private void btnSortBy_Click(object sender, EventArgs e)
        {
            Button sortByButton = sender as Button;
            int x = ((base.Location.X + splitContainer1.Panel1.Width + sortByButton.Left)) + 15;
            int y = (base.Location.Y + tableLayoutPanel3.Height + sortByButton.Top) + 3 * btnSortBy.Height;
            sortContextMenu.Show(x, y);
            sortContextMenu.Focus();
        }

        private void toolStripSortByStatus_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortByStatus.Checked)
            {
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.NoGroupNodeKey);
                foreach (ContactGroup cg in messenger.ContactGroups)
                {
                    treeViewFavoriteList.Nodes.RemoveByKey(cg.Guid);
                }

                SortByStatus(null);
            }
            else
            {
                this.toolStripSortByStatus.Checked = true;
            }
        }

        private bool initialExpand = true;

        private string GetCircleDisplayName(Contact circle, int count)
        {
            if (circle == null)
                return string.Empty;

            return circle.Name + " (" + count + " members)";
        }


        private void SortByFavAndCircle(ContactStatusChangedEventArgs e)
        {
            Contact contactToUpdate = (e != null) ? e.Contact : null;
            Contact via = (e != null) ? e.Via : null;

            TreeNode favoritesNode = null; // (0/0)
            TreeNode circlesNode = null; // (0/0)
            TreeNode fbNode = null; // (0/0)

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.FavoritesNodeKey))
            {
                favoritesNode = treeViewFavoriteList.Nodes[ImageIndexes.FavoritesNodeKey];
            }
            else
            {
                favoritesNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.FavoritesNodeKey, "Favorites", ImageIndexes.Closed, ImageIndexes.Closed);
                favoritesNode.NodeFont = PARENT_NODE_FONT;
                favoritesNode.Tag = ImageIndexes.FavoritesNodeKey;
            }

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.CircleNodeKey))
            {
                circlesNode = treeViewFavoriteList.Nodes[ImageIndexes.CircleNodeKey];
            }
            else
            {
                circlesNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.CircleNodeKey, "Circles", ImageIndexes.Closed, ImageIndexes.Closed);
                circlesNode.NodeFont = PARENT_NODE_FONT;
                circlesNode.Tag = ImageIndexes.CircleNodeKey;
            }


            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.FacebookNodeKey))
            {
                fbNode = treeViewFavoriteList.Nodes[ImageIndexes.FacebookNodeKey];
            }
            else
            {
                fbNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.FacebookNodeKey, "Facebook", ImageIndexes.Closed, ImageIndexes.Closed);
                fbNode.NodeFont = PARENT_NODE_FONT;
                fbNode.Tag = ImageIndexes.FacebookNodeKey;
            }
            

            if (contactToUpdate == null)
            {
                // Initial sort
                favoritesNode.Nodes.Clear();
                circlesNode.Nodes.Clear();
                fbNode.Nodes.Clear();

                #region Circles and circle members

                foreach (Contact circle in Messenger.CircleList.Values)
                {
                    if (circle.ContactList != null)
                    {
                        int contactCount = circle.ContactList[IMAddressInfoType.None].Count;
                        TreeNode circleNode = circlesNode.Nodes.Add(circle.Hash, GetCircleDisplayName(circle, contactCount), circle.Online ? ImageIndexes.CircleOnline : ImageIndexes.CircleOffline, circle.Online ? ImageIndexes.CircleOnline : ImageIndexes.CircleOffline);
                        circleNode.NodeFont = circle.AppearOffline ? PARENT_NODE_FONT_BANNED : PARENT_NODE_FONT;
                        circleNode.Tag = circle;

                        foreach (Contact contact in circle.ContactList.All)
                        {
                            // Get real passport contact to chat with... If this contact isn't on our forward list, show add contact form...
                            string text = contact.Name;
                            if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                            {
                                text += " - " + contact.PersonalMessage.Message;
                            }
                            if (contact.Name != contact.Account)
                            {
                                text += " (" + contact.Account + ")";
                            }

                            TreeNode newnode = circleNode.Nodes.Add(contact.Hash, text);
                            newnode.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                            newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contact.Status);
                            newnode.Tag = contact;
                        }
                    }
                } 

                #endregion

                #region Contacts

                ContactGroup favGroup = messenger.ContactGroups.FavoriteGroup;
                if (favGroup != null)
                {
                    foreach (Contact c in messenger.ContactList.Forward)
                    {
                        if (c.HasGroup(favGroup))
                        {
                            string text = c.Name;
                            if (c.PersonalMessage != null && !String.IsNullOrEmpty(c.PersonalMessage.Message))
                            {
                                text += " - " + c.PersonalMessage.Message;
                            }
                            if (c.Name != c.Account)
                            {
                                text += " (" + c.Account + ")";
                            }

                            TreeNode newnode = favoritesNode.Nodes.ContainsKey(c.Hash) ?
                                favoritesNode.Nodes[c.Hash] : favoritesNode.Nodes.Add(c.Hash, text);

                            newnode.NodeFont = c.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                            newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(c.Status);
                            newnode.Tag = c;
                        }
                    }
                } 

                #endregion

                #region Facebook contacts.

                Contact fbNetwork = Messenger.ContactList.GetContact(RemoteNetworkGateways.FaceBookGatewayAccount, IMAddressInfoType.RemoteNetwork);

                if (fbNetwork != null && fbNetwork.ContactList != null)
                {
                    int onlineCountFB = 0;
                    int contactCount = fbNetwork.ContactList[IMAddressInfoType.None].Count;

                    foreach (Contact fbContact in fbNetwork.ContactList.All)
                    {
                        string text = fbContact.Name;

                        if (fbContact.Online)
                            onlineCountFB++;

                        TreeNode newnode = fbNode.Nodes.ContainsKey(fbContact.Hash) ?
                                fbNode.Nodes[fbContact.Hash] : fbNode.Nodes.Add(fbContact.Hash, text);

                        newnode.NodeFont = fbContact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(fbContact.Status);
                        newnode.Tag = fbContact;
                    }

                    string fbText = "Facebook (" + onlineCountFB + "/" + fbNode.Nodes.Count + ")";
                    if (fbNode.Text != fbText)
                        fbNode.Text = fbText;

                } 

                #endregion
            }
            else if (contactToUpdate.ClientType == IMAddressInfoType.Circle)
            {
                // Circle event
                Contact circle = contactToUpdate;

                bool isDeleted = (!Messenger.CircleList.ContainsKey(circle.Hash));

                if (!isDeleted)
                {
                    int contactCount = circle.ContactList[IMAddressInfoType.None].Count;

                    TreeNode circleNode = circlesNode.Nodes.ContainsKey(circle.Hash) ?
                        circlesNode.Nodes[circle.Hash] : circlesNode.Nodes.Add(circle.Hash, GetCircleDisplayName(circle, contactCount), circle.Online ? ImageIndexes.CircleOnline : ImageIndexes.CircleOffline, circle.Online ? ImageIndexes.CircleOnline : ImageIndexes.CircleOffline);

                    circleNode.NodeFont = circle.AppearOffline ? PARENT_NODE_FONT_BANNED : PARENT_NODE_FONT;
                    circleNode.Tag = circle;

                    int count = 0;
                    foreach (Contact contact in circle.ContactList.All)
                    {
                        count++;
                        // Get real passport contact to chat with... If this contact isn't on our forward list, show add contact form...
                        string text2 = contact.Name;
                        if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                        {
                            text2 += " - " + contact.PersonalMessage.Message;
                        }
                        if (contact.Name != contact.Account)
                        {
                            text2 += " (" + contact.Account + ")";
                        }

                        TreeNode newnode = circleNode.Nodes.ContainsKey(contact.Hash) ?
                            circleNode.Nodes[contact.Hash] : circleNode.Nodes.Add(contact.Hash, text2);

                        newnode.Text = text2;
                        newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contact.Status);
                        newnode.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.Tag = contact;
                    }

                    circleNode.Text = GetCircleDisplayName(circle, circleNode.Nodes.Count);
                }
                else
                {
                    circlesNode.Nodes.RemoveByKey(circle.Hash);
                }
            }
            else
            {
                // Contact event... Contact is not null.
                // Favorite
                ContactGroup favGroup = messenger.ContactGroups.FavoriteGroup;
                if (favGroup != null && contactToUpdate.HasGroup(favGroup))
                {
                    Contact contact = messenger.ContactList.GetContact(contactToUpdate.Account, contactToUpdate.ClientType);
                    string text = contact.Name;
                    if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                    {
                        text += " - " + contact.PersonalMessage.Message;
                    }
                    if (contact.Name != contact.Account)
                    {
                        text += " (" + contact.Account + ")";
                    }

                    TreeNode contactNode = favoritesNode.Nodes.ContainsKey(contactToUpdate.Hash) ?
                        favoritesNode.Nodes[contactToUpdate.Hash] : favoritesNode.Nodes.Add(contactToUpdate.Hash, text);

                    contactNode.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    contactNode.ImageIndex = contactNode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contactToUpdate.Status);
                    contactNode.Tag = contactToUpdate;
                }
            }

            int onlineCount = 0;
            foreach (TreeNode nodeFav in favoritesNode.Nodes)
            {
                if (nodeFav.Tag is Contact && (((Contact)nodeFav.Tag).Online))
                {
                    onlineCount++;
                }
            }
            string newText = "Favorites (" + onlineCount + "/" + favoritesNode.Nodes.Count + ")";
            if (favoritesNode.Text != newText)
                favoritesNode.Text = newText;

            onlineCount = 0;
            foreach (TreeNode nodeCircle in circlesNode.Nodes)
            {
                if (nodeCircle.Tag is Contact && (((Contact)nodeCircle.Tag).Online))
                {
                    onlineCount++;
                }
            }

            newText = "Circles (" + onlineCount + "/" + circlesNode.Nodes.Count + ")";
            if (circlesNode.Text != newText)
                circlesNode.Text = newText;


            onlineCount = 0;
            foreach (TreeNode fbContactNode in fbNode.Nodes)
            {
                if ((((Contact)fbContactNode.Tag).Online))
                {
                    onlineCount++;
                }
            }

            newText = "Facebook (" + onlineCount + "/" + fbNode.Nodes.Count + ")";
            if (fbNode.Text != newText)
                fbNode.Text = newText;
        }

        private void UpdateCircleMember(Contact circle, Contact circleMember)
        {
            string text2 = circleMember.Name;
            text2 += " - " + circleMember.PersonalMessage.Message;

            if (circleMember.Name != circleMember.Account)
            {
                text2 += " (" + circleMember.Account + ")";
            }

            TreeNode circlesNode = treeViewFavoriteList.Nodes[ImageIndexes.CircleNodeKey];
            TreeNode circleNode = circlesNode.Nodes.ContainsKey(circle.Hash) ?
                circlesNode.Nodes[circle.Hash] : circlesNode.Nodes.Add(circle.Hash, circle.Name);

            circleNode.ImageIndex = circleNode.SelectedImageIndex = ImageIndexes.GetCircleStatusImageIndex(circle.Status);
            circleNode.Text = GetCircleDisplayName(circle, circle.ContactList[IMAddressInfoType.None].Count);

            TreeNode newnode = circleNode.Nodes.ContainsKey(circleMember.Hash) ?
                circleNode.Nodes[circleMember.Hash] : circleNode.Nodes.Add(circleMember.Hash, text2);

            
            newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(circleMember.Status);
            newnode.NodeFont = circleMember.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
            newnode.Tag = circleMember;

            if (newnode.Text != text2)
                newnode.Text = text2;
        }

        private void SortByStatus(Contact contact, Contact via)
        {
            SortByStatus(new ContactStatusChangedEventArgs(contact, via, contact.Status, contact.Status));
        }
        
        private void SortByStatus(ContactStatusChangedEventArgs e)
        {
            Contact contactToUpdate = (e != null) ? e.Contact : null;
            Contact via = (e != null) ? e.Via : null;

            TreeNode selectedNode = treeViewFavoriteList.SelectedNode;
            bool isExpanded = (selectedNode != null && selectedNode.IsExpanded);

            //treeViewFavoriteList.BeginUpdate();

            if (toolStripSortBygroup.Checked)
                toolStripSortBygroup.Checked = false;

            SortByFavAndCircle(e);

            TreeNode onlineNode = null; // (0)
            TreeNode mobileNode = null; // (0)
            TreeNode offlineNode = null; // (0)

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.OnlineNodeKey))
            {
                onlineNode = treeViewFavoriteList.Nodes[ImageIndexes.OnlineNodeKey];
            }
            else
            {
                onlineNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.OnlineNodeKey, "Online", ImageIndexes.Closed, ImageIndexes.Closed);
                onlineNode.NodeFont = contactToUpdate == null ? PARENT_NODE_FONT : (contactToUpdate.AppearOffline ? PARENT_NODE_FONT_BANNED : PARENT_NODE_FONT);
                onlineNode.Tag = ImageIndexes.OnlineNodeKey;
            }

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.MobileNodeKey))
            {
                mobileNode = treeViewFavoriteList.Nodes[ImageIndexes.MobileNodeKey];
            }
            else
            {
                mobileNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.MobileNodeKey, "Mobile", ImageIndexes.Closed, ImageIndexes.Closed);
                mobileNode.NodeFont = PARENT_NODE_FONT;
                mobileNode.Tag = ImageIndexes.MobileNodeKey;
            }

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.OfflineNodeKey))
            {
                offlineNode = treeViewFavoriteList.Nodes[ImageIndexes.OfflineNodeKey];
            }
            else
            {
                offlineNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.OfflineNodeKey, "Offline", ImageIndexes.Closed, ImageIndexes.Closed);
                offlineNode.NodeFont = PARENT_NODE_FONT;
                offlineNode.Tag = ImageIndexes.OfflineNodeKey;
            }

            // Re-sort all
            if (contactToUpdate == null)
            {
                #region Update whole list

                mobileNode.Nodes.Clear();
                onlineNode.Nodes.Clear();
                offlineNode.Nodes.Clear();

                foreach (Contact contact in messenger.ContactList.All)
                {
                    if (contact.IsHiddenContact)
                        continue;

                    string text = contact.Name;
                    if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                    {
                        text += " - " + contact.PersonalMessage.Message;
                    }
                    if (contact.Name != contact.Account)
                    {
                        text += " (" + contact.Account + ")";
                    }

                    TreeNode newnode = contact.Online ? onlineNode.Nodes.Add(contact.Hash, text) : offlineNode.Nodes.Add(contact.Hash, text);
                    newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contact.Status);
                    newnode.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = contact;

                    if (contact.MobileAccess || contact.ClientType == IMAddressInfoType.Telephone)
                    {
                        TreeNode newnode2 = mobileNode.Nodes.Add(contact.Hash, text);
                        newnode2.ImageIndex = newnode2.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contact.Status);
                        newnode2.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode2.Tag = contact;
                    }
                } 

                #endregion
            }
            else
            {
                if (contactToUpdate.IsHiddenContact)
                    return;

                if (contactToUpdate.Via != null)
                {
                    
                    #region Circle Members
		
                    if (contactToUpdate.Via.ClientType == IMAddressInfoType.Circle)
                    {
                        UpdateCircleMember(contactToUpdate.Via, contactToUpdate);
                    } 
	                #endregion

                    #region Facebook members

                    if (contactToUpdate.Via.ClientType == IMAddressInfoType.RemoteNetwork)
                    {
                        Contact fbNetwork = Messenger.ContactList.GetContact(contactToUpdate.Via.Account, contactToUpdate.Via.ClientType);

                        if (fbNetwork != null && fbNetwork.ContactList != null)
                        {
                            TreeNode fbNode = treeViewFavoriteList.Nodes[ImageIndexes.FacebookNodeKey];
                            string text2 = contactToUpdate.Name;

                            TreeNode newnode = fbNode.Nodes.ContainsKey(contactToUpdate.Hash) ?
                            fbNode.Nodes[contactToUpdate.Hash] : fbNode.Nodes.Add(contactToUpdate.Hash, text2);

                            newnode.Text = text2;
                            newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contactToUpdate.Status);
                            newnode.NodeFont = contactToUpdate.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                            newnode.Tag = contactToUpdate;

                        }
                    } 

                    #endregion

                    return;
                }

                #region Update Circles

                if (contactToUpdate.ClientType == IMAddressInfoType.Circle)
                {
                    foreach (Contact contact in contactToUpdate.ContactList.All)
                    {
                        UpdateCircleMember(contactToUpdate, contact);
                    }

                    return;
                } 

                #endregion
                

                TreeNode contactNode = null;

                if (contactToUpdate.Online)
                {
                    if (offlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        offlineNode.Nodes.RemoveByKey(contactToUpdate.Hash);
                    }
                    if (onlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        contactNode = onlineNode.Nodes[contactToUpdate.Hash];
                    }
                }
                else
                {
                    if (onlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        onlineNode.Nodes.RemoveByKey(contactToUpdate.Hash);
                    }
                    if (offlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        contactNode = offlineNode.Nodes[contactToUpdate.Hash];
                    }
                }

                string text = contactToUpdate.Name;
                if (contactToUpdate.PersonalMessage != null && !String.IsNullOrEmpty(contactToUpdate.PersonalMessage.Message))
                {
                    text += " - " + contactToUpdate.PersonalMessage.Message;
                }
                if (contactToUpdate.Name != contactToUpdate.Account)
                {
                    text += " (" + contactToUpdate.Account + ")";
                }

                if (contactNode == null)
                {
                    contactNode = contactToUpdate.Online ? onlineNode.Nodes.Add(contactToUpdate.Hash, text) : offlineNode.Nodes.Add(contactToUpdate.Hash, text);
                }

                if (contactNode.Text != text)
                    contactNode.Text = text;

                contactNode.ImageIndex = contactNode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contactToUpdate.Status);
                contactNode.NodeFont = contactToUpdate.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                contactNode.Tag = contactToUpdate;

                if (contactToUpdate.MobileAccess || contactToUpdate.ClientType == IMAddressInfoType.Telephone)
                {
                    TreeNode newnode2 = mobileNode.Nodes.ContainsKey(contactToUpdate.Hash) ?
                        mobileNode.Nodes[contactToUpdate.Hash] : mobileNode.Nodes.Add(contactToUpdate.Hash, text);

                    newnode2.ImageIndex = newnode2.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contactToUpdate.Status);
                    newnode2.NodeFont = contactToUpdate.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode2.Tag = contactToUpdate;
                }
            }

            string newText = "Online (" + onlineNode.Nodes.Count.ToString() + ")";
            if (onlineNode.Text != newText)
                onlineNode.Text = newText;

            newText = "Offline (" + offlineNode.Nodes.Count.ToString() + ")";
            if (offlineNode.Text != newText)
                offlineNode.Text = newText;

            newText = "Mobile (" + mobileNode.Nodes.Count.ToString() + ")";
            if (mobileNode.Text != newText)
                mobileNode.Text = newText;

            treeViewFavoriteList.Sort();

            if (selectedNode != null)
            {
                treeViewFavoriteList.SelectedNode = selectedNode;

                if (isExpanded && treeViewFavoriteList.SelectedNode != null)
                {
                    treeViewFavoriteList.SelectedNode.Expand();
                }
            }
            else
            {
                if (initialExpand && onlineNode.Nodes.Count > 0)
                {
                    onlineNode.Expand();
                    onlineNode.ImageIndex = ImageIndexes.Open;

                    initialExpand = false;
                }
            }

            //treeViewFavoriteList.EndUpdate();
            treeViewFavoriteList.AllowDrop = false;
        }

        private void toolStripSortBygroup_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortBygroup.Checked)
            {
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.OnlineNodeKey);
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.MobileNodeKey);
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.OfflineNodeKey);

                SortByGroup(null);
            }
            else
            {
                this.toolStripSortBygroup.Checked = true;
            }
        }

        private void SortByGroup(Contact contact, Contact via)
        {
            SortByGroup(new ContactStatusChangedEventArgs(contact, via, contact.Status, contact.Status));
        }
        
        private void SortByGroup(ContactStatusChangedEventArgs e)
        {
            Contact contactToUpdate = (e != null) ? e.Contact : null;
            Contact via = (e != null) ? e.Via : null;

            this.treeViewFavoriteList.BeginUpdate();
            this.toolStripSortByStatus.Checked = false;

            SortByFavAndCircle(e);

            foreach (ContactGroup group in this.messenger.ContactGroups)
            {
                if (group.IsFavorite == false)
                {
                    TreeNode node = treeViewFavoriteList.Nodes.ContainsKey(group.Guid) ?
                        treeViewFavoriteList.Nodes[group.Guid] : treeViewFavoriteList.Nodes.Add(group.Guid, group.Name, ImageIndexes.Closed, ImageIndexes.Closed);

                    node.ImageIndex = ImageIndexes.Closed;
                    node.NodeFont = PARENT_NODE_FONT;
                    node.Tag = group;
                    node.Text = "0";
                }
            }

            TreeNode common = treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.NoGroupNodeKey) ?
                treeViewFavoriteList.Nodes[ImageIndexes.NoGroupNodeKey] : treeViewFavoriteList.Nodes.Add(ImageIndexes.NoGroupNodeKey, "Others", 0, 0);

            common.ImageIndex = ImageIndexes.Closed;
            common.NodeFont = PARENT_NODE_FONT;
            common.Tag = ImageIndexes.NoGroupNodeKey;
            common.Text = "0";

            foreach (Contact contact in messenger.ContactList.All)
            {
                string text = contact.Name;
                if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                {
                    text += " - " + contact.PersonalMessage.Message;
                }
                if (contact.Name != contact.Account)
                {
                    text += " (" + contact.Account + ")";
                }

                if (contact.ContactGroups.Count == 0)
                {
                    TreeNode newnode = common.Nodes.ContainsKey(contact.Hash) ? 
                        common.Nodes[contact.Hash] : common.Nodes.Add(contact.Hash, text);

                    newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contact.Status);
                    newnode.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = contact;
                    newnode.Text = text;

                    if (contact.Online)
                        common.Text = (Convert.ToInt32(common.Text) + 1).ToString();
                }
                else
                {
                    foreach (ContactGroup group in contact.ContactGroups)
                    {
                        if (group.IsFavorite == false)
                        {
                            TreeNode found = treeViewFavoriteList.Nodes[group.Guid];
                            TreeNode newnode = found.Nodes.Add(contact.Hash, contact.Name);
                            newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetContactStatusImageIndex(contact.Status);
                            newnode.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                            newnode.Tag = contact;

                            if (contact.Online)
                                found.Text = (Convert.ToInt32(found.Text) + 1).ToString();
                        }
                    }
                }
            }

            foreach (TreeNode nodeGroup in treeViewFavoriteList.Nodes)
            {
                if (nodeGroup.Tag is ContactGroup)
                {
                    nodeGroup.Text = ((ContactGroup)nodeGroup.Tag).Name + "(" + nodeGroup.Text + "/" + nodeGroup.Nodes.Count + ")";
                }
            }

            common.Text = "Others (" + common.Text + "/" + common.Nodes.Count + ")";

            treeViewFavoriteList.Sort();
            treeViewFavoriteList.EndUpdate();
            treeViewFavoriteList.AllowDrop = true;
        }

        private void treeViewFavoriteList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void treeViewFavoriteList_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if ((e.Item is TreeNode)/* && (((TreeNode)e.Item).Level > 0)*/)
            {
                base.DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void treeViewFavoriteList_DragOver(object sender, DragEventArgs e)
        {
            Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
            TreeNode nodeAt = ((TreeView)sender).GetNodeAt(pt);
            TreeNode data = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
            if (((data.Level == 0) || (data.Parent == nodeAt)) || (nodeAt.Parent == data.Parent))
            {
                e.Effect = DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void treeViewFavoriteList_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeNode contactNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (contactNode.Level != 0)
                {
                    Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
                    TreeNode newgroupNode = (((TreeView)sender).GetNodeAt(pt).Level == 0) ? ((TreeView)sender).GetNodeAt(pt) : ((TreeView)sender).GetNodeAt(pt).Parent;
                    TreeNode oldgroupNode = contactNode.Parent;
                    Contact contact = (Contact)contactNode.Tag;
                    bool flag = true;
                    try
                    {
                        if (newgroupNode.Tag is ContactGroup)
                        {
                            messenger.ContactService.AddContactToGroup(contact, (ContactGroup)newgroupNode.Tag);
                        }
                        if (oldgroupNode.Tag is ContactGroup)
                        {
                            messenger.ContactService.RemoveContactFromGroup(contact, (ContactGroup)oldgroupNode.Tag);
                        }
                    }
                    catch (Exception)
                    {
                        flag = false;
                    }

                    if (flag)
                    {
                        treeViewFavoriteList.BeginUpdate();
                        TreeNode node3 = (TreeNode)contactNode.Clone();
                        newgroupNode.Nodes.Add(node3);
                        contactNode.Remove();
                        treeViewFavoriteList.EndUpdate();

                        newgroupNode.Text = newgroupNode.Text.Split(new char[] { '/' })[0] + "/" + newgroupNode.Nodes.Count + ")";
                        oldgroupNode.Text = oldgroupNode.Text.Split(new char[] { '/' })[0] + "/" + oldgroupNode.Nodes.Count + ")";
                    }
                }
            }
        }

        private void toolStripDeleteGroup_Click(object sender, EventArgs e)
        {
            ContactGroup selectedGroup = (ContactGroup)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedGroup;

            messenger.ContactGroups.Remove(selectedGroup);

            System.Threading.Thread.Sleep(500);
            Application.DoEvents();
            System.Threading.Thread.Sleep(500);

            SortByGroup(null);
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            if (this.loginButton.Tag.ToString() != "2")
            {
                MessageBox.Show("Please sign in first.");
                return;
            }

            AddContactForm acf = new AddContactForm(String.Empty);
            if (DialogResult.OK == acf.ShowDialog(this) && acf.Account != String.Empty)
            {
                messenger.ContactService.AddNewContact(acf.Account, acf.InvitationMessage);
            }
        }

        private void createCircleMenuItem_Click(object sender, EventArgs e)
        {
            //This is a demostration to tell you how to use MSNPSharp to create, block, and unblock Circle.
            messenger.ContactService.CreateCircle("test wp circle");
            messenger.ContactService.CreateCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_TestingCircleAdded);
        }

        void ContactService_TestingCircleAdded(object sender, CircleEventArgs e)
        {
            //Circle created, then show you how to block.
            ////if (!e.Circle.OnBlockedList)
            ////{
            ////    messenger.ContactService.BlockCircle(e.Circle);
            ////    e.Circle.ContactBlocked += new EventHandler<EventArgs>(Circle_ContactBlocked);
            ////    Trace.WriteLine("Circle blocked: " + e.Circle.ToString());
            ////}

            ////Trace.WriteLine("Circle created: " + e.Circle.ToString());
        }

        void Circle_ContactBlocked(object sender, EventArgs e)
        {
            //Circle blocked, show you how to unblock.
            Contact circle = sender as Contact;
            if (circle != null)
            {
                circle.AppearOnline = true;
                circle.ContactUnBlocked += new EventHandler<ContactBlockedStatusChangedEventArgs>(circle_ContactUnBlocked);
                Trace.WriteLine("Circle unblocked: " + circle.ToString());
            }
        }

        void circle_ContactUnBlocked(object sender, EventArgs e)
        {
            //This demo shows you how to invite a contact to your circle.
            if (messenger.ContactList.HasContact("freezingsoft@hotmail.com", IMAddressInfoType.WindowsLive))
            {
                messenger.ContactService.InviteCircleMember(sender as Contact, messenger.ContactList.GetContact("freezingsoft@hotmail.com", IMAddressInfoType.WindowsLive), "hello");
                messenger.ContactService.InviteCircleMemberCompleted += new EventHandler<CircleMemberEventArgs>(ContactService_CircleMemberInvited);
            }
        }

        void ContactService_CircleMemberInvited(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Invited: " + e.Member.Hash);
        }

        private void importContactsMenuItem_Click(object sender, EventArgs e)
        {
            ImportContacts ic = new ImportContacts();
            if (ic.ShowDialog(this) == DialogResult.OK)
            {
                string invitation = ic.InvitationMessage;
                foreach (String account in ic.Contacts)
                {
                    messenger.ContactService.AddNewContact(account, invitation);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtSearch.Text == String.Empty || txtSearch.Text == "Search contacts")
            {
                treeViewFilterList.Nodes.Clear();
                treeViewFavoriteList.Visible = true;
                treeViewFilterList.Visible = false;
            }
            else
            {
                treeViewFilterList.Nodes.Clear();
                treeViewFavoriteList.Visible = false;
                treeViewFilterList.Visible = true;
                TreeNode foundnode = treeViewFilterList.Nodes.Add("0", "Search Results:");

                foreach (Contact contact in messenger.ContactList.All)
                {
                    if (contact.Account.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1
                        ||
                        contact.Name.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        TreeNode newnode = foundnode.Nodes.Add(contact.Hash, contact.Name);
                        newnode.NodeFont = contact.AppearOffline ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.Tag = contact;
                    }
                }
                foundnode.Text = "Search Results: " + foundnode.Nodes.Count;
                foundnode.Expand();
            }
        }

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            if (txtSearch.Text == "Search contacts")
            {
                txtSearch.Text = String.Empty;
            }
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            if (txtSearch.Text == String.Empty)
            {
                txtSearch.Text = "Search contacts";
            }
        }

        private void lblName_Leave(object sender, EventArgs e)
        {
            string displayName = lblName.Text;
            string personalStatusMessage = lblPM.Text;
            bool shouldChange = false;

            if (displayName != messenger.Owner.Name)
            {
                shouldChange = true;
            }

            if (messenger.Owner.PersonalMessage == null || 
                personalStatusMessage != messenger.Owner.PersonalMessage.Message)
            {
                shouldChange = true;
            }

            if (shouldChange)  //This might trigger when you close the window.
            {
                // Here you set your online profile
                PersonalMessage personalMessageToUpdate = messenger.Owner.PersonalMessage;
                personalMessageToUpdate.Message = personalStatusMessage;
                personalMessageToUpdate.FriendlyName = displayName;
                messenger.Owner.PersonalMessage = personalMessageToUpdate;

                // Here you update your roaming profile to make it the same with your online profile.
                messenger.Owner.UpdateRoamingProfileSync();
            }
        }

        private void comboStatus_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!messenger.Connected)
            {
                login_KeyPress(sender, e);
            }
        }

        private void displayImageBox_Click(object sender, EventArgs e)
        {
            if (messenger.Connected)
            {
                if (openImageDialog.ShowDialog() == DialogResult.OK)
                {
                    Image newImage = Image.FromFile(openImageDialog.FileName, true);

                    // Update the online profile.
                    messenger.Owner.UpdateDisplayImage(newImage);

                    // Update the roaming profile
                    messenger.Owner.UpdateRoamingProfileSync(newImage);
                   
                }
            }
        }


        private void btnSetMusic_Click(object sender, EventArgs e)
        {
            MusicForm musicForm = new MusicForm();
            if (musicForm.ShowDialog() == DialogResult.OK)
            {
                PersonalMessage pm = Messenger.Owner.PersonalMessage;
                pm.SetListeningAlbum(musicForm.Artist, musicForm.Song, musicForm.Album);

                Messenger.Owner.PersonalMessage = pm;
            }
        }

        private void btnSetTheme_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)
            {
                Image newImage = Image.FromFile(openImageDialog.FileName, true);
                messenger.Owner.SetScene(newImage, Color.White);
            }
        }

    }
}
