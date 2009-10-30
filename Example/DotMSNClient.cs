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

namespace MSNPSharpClient
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// MSNPSharp Client example.
    /// </summary>
    public partial class ClientForm : System.Windows.Forms.Form
    {
        // Create a Messenger object to use MSNPSharp.
        private Messenger messenger = new Messenger();
        private List<ConversationForm> convforms = new List<ConversationForm>(0);

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
            // Move PM panel to SignIn window...
            pnlNameAndPM.Location = panel1.Location;


            // You can set proxy settings here
            // for example: messenger.ConnectivitySettings.ProxyHost = "10.0.0.2";


            // ******* Listen traces *****
            TraceForm traceform = new TraceForm();
            traceform.Show();

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
            messenger.ConversationCreated += new EventHandler<ConversationCreatedEventArgs>(messenger_ConversationCreated);
            messenger.TransferInvitationReceived += new EventHandler<MSNSLPInvitationEventArgs>(messenger_TransferInvitationReceived);
            messenger.Nameserver.PingAnswer += new EventHandler<PingAnswerEventArgs>(Nameserver_PingAnswer);

            messenger.Nameserver.ContactOnline += new EventHandler<ContactEventArgs>(Nameserver_ContactOnline);
            messenger.Nameserver.ContactOffline += new EventHandler<ContactEventArgs>(Nameserver_ContactOffline);
            messenger.Nameserver.CircleOnline += new EventHandler<CircleEventArgs>(Nameserver_CircleOnline);
            messenger.Nameserver.CircleOffline += new EventHandler<CircleEventArgs>(Nameserver_CircleOffline);

            messenger.Nameserver.ContactService.ReverseAdded += new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded);
            messenger.Nameserver.ContactService.SynchronizationCompleted += new EventHandler<EventArgs>(ContactService_SynchronizationCompleted);
            messenger.Nameserver.ContactService.CreateCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_CircleCreated);
            messenger.Nameserver.ContactService.JoinedCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_JoinedCircle);
            messenger.Nameserver.ContactService.JoinCircleInvitationReceived += new EventHandler<JoinCircleInvitationEventArgs>(ContactService_JoinCircleInvitationReceived);
            messenger.Nameserver.ContactService.ExitCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_ExitCircle);
            messenger.Nameserver.CircleMemberLeft += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleMemberLeft);
            messenger.Nameserver.CircleMemberJoined += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleMemberJoined);
            messenger.Nameserver.CircleTextMessageReceived += new EventHandler<CircleTextMessageEventArgs>(Nameserver_CircleTextMessageReceived);
            messenger.Nameserver.CircleNudgeReceived += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleNudgeReceived);

            messenger.Nameserver.Owner.DisplayImageChanged += new EventHandler<EventArgs>(Owner_DisplayImageChanged);
            messenger.Nameserver.Owner.PersonalMessageChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            messenger.Nameserver.Owner.ScreenNameChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            messenger.Nameserver.Owner.PlacesChanged += new EventHandler<EventArgs>(Owner_PlacesChanged);
            messenger.Nameserver.Owner.StatusChanged += new EventHandler<StatusChangedEventArgs>(Owner_StatusChanged);
            messenger.Nameserver.OIMService.OIMReceived += new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived);
            messenger.Nameserver.OIMService.OIMSendCompleted += new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted);
            

            messenger.Nameserver.WhatsUpService.GetWhatsUpCompleted += WhatsUpService_GetWhatsUpCompleted;


            // Handle Service Operation Errors
            //In most cases, these error are not so important.
            messenger.ContactService.ServiceOperationFailed += ServiceOperationFailed;
            messenger.OIMService.ServiceOperationFailed += ServiceOperationFailed;
            messenger.StorageService.ServiceOperationFailed += ServiceOperationFailed;
            messenger.WhatsUpService.ServiceOperationFailed += ServiceOperationFailed;

            treeViewFavoriteList.TreeViewNodeSorter = StatusSorter.Default;

            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();

            comboStatus.SelectedIndex = 0;
            comboProtocol.SelectedIndex = 0;

        }

        void Nameserver_CircleNudgeReceived(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Circle " + e.Circle.ToString() + ": Member: " + e.Member.ToString() + " send you a nudge.");
            AutoGroupMessageReply(e.Circle);
        }

        private void AutoGroupMessageReply(Circle circle)
        {
            if (messenger.Owner.Status != PresenceStatus.Hidden || messenger.Owner.Status != PresenceStatus.Offline)
                circle.SendMessage(new TextMessage("MSNPSharp example client auto reply."));
        }

        void Nameserver_CircleTextMessageReceived(object sender, CircleTextMessageEventArgs e)
        {
            Trace.WriteLine("Circle " + e.Sender.ToString() + ": Member: " + e.TriggerMember.ToString() + " send you a message :" + e.Message.ToString());
            AutoGroupMessageReply(e.Sender);
        }

        void Nameserver_CircleMemberJoined(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Circle member " + e.Member.ToString() + " joined the circle conversation: " + e.Circle.ToString());
            RefreshCircleList(sender, e);
        }

        void Nameserver_CircleMemberLeft(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Circle member " + e.Member.ToString() + " has left the circle: " + e.Circle.ToString());
            RefreshCircleList(sender, e);
        }

        void Nameserver_CircleOnline(object sender, CircleEventArgs e)
        {
            Trace.WriteLine("Circle go online: " + e.Circle.ToString());
        }

        void Nameserver_CircleOffline(object sender, CircleEventArgs e)
        {
            Trace.WriteLine("Circle go offline: " + e.Circle.ToString());
        }

        void ContactService_ExitCircle(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void ContactService_JoinedCircle(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
            messenger.Nameserver.ContactService.ExitCircle(e.Circle); //Demostrate how to leave a circle.
        }

        void ContactService_JoinCircleInvitationReceived(object sender, JoinCircleInvitationEventArgs e)
        {
            messenger.Nameserver.ContactService.AcceptCircleInvitation(e.Circle);
        }

        void ContactService_CircleCreated(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void RefreshCircleList(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(RefreshCircleList), sender, e);
                return;
            }

            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();
        }

        void ServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Method + ": " + e.Exception.ToString(), sender.GetType().Name); 
        }

        void ContactService_SynchronizationCompleted(object sender, EventArgs e)
        {
            messenger.Nameserver.WhatsUpService.GetWhatsUp(200);
        }

        List<ActivityDetailsType> activities = new List<ActivityDetailsType>();
        void WhatsUpService_GetWhatsUpCompleted(object sender, GetWhatsUpCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<GetWhatsUpCompletedEventArgs>(WhatsUpService_GetWhatsUpCompleted), sender, e);
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.ToString());
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
                    return;
                
                lblNewsLink.Text = "Get Feeds";
                lblNewsLink.Tag = e.Response.FeedUrl;
                tmrNews.Enabled = true;
            }
        }

        private int currentActivity = 0;
        private bool activityForward = true;
        private void tmrNews_Tick(object sender, EventArgs e)
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
                    else if (c.UserTile != null)
                    {
                        pbNewsPicture.LoadAsync(c.UserTile.AbsoluteUri);
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

        private void cmdPrev_Click(object sender, EventArgs e)
        {
            if (activities.Count > 0)
            {
                activityForward = false;

                if (currentActivity > 0)
                    currentActivity--;
                else
                    currentActivity = activities.Count - 1;

                if (tmrNews.Enabled)
                    tmrNews_Tick(this, EventArgs.Empty);
            }
        }

        private void cmdNext_Click(object sender, EventArgs e)
        {
            if (activities.Count > 0)
            {
                activityForward = true;

                if (currentActivity < activities.Count)
                    currentActivity++;

                if (tmrNews.Enabled)
                    tmrNews_Tick(this, EventArgs.Empty);
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
                Invoke(new EventHandler<EventArgs>(Owner_PersonalMessageChanged), sender, e);
                return;
            }

            lblName.Text = messenger.Owner.Name;

            if (messenger.Owner.PersonalMessage != null && messenger.Owner.PersonalMessage.Message != null)
            {
                lblPM.Text = System.Web.HttpUtility.HtmlDecode(messenger.Owner.PersonalMessage.Message);
            }
        }

        void Owner_DisplayImageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(Owner_DisplayImageChanged), sender, e);
                return;
            }

            displayImageBox.Image = messenger.Owner.DisplayImage.Image;
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

        void Nameserver_ContactOnline(object sender, ContactEventArgs e)
        {
            Invoke(new EventHandler<ContactEventArgs>(ContactOnlineOffline), sender, e);
        }
        void Nameserver_ContactOffline(object sender, ContactEventArgs e)
        {
            Invoke(new EventHandler<ContactEventArgs>(ContactOnlineOffline), sender, e);
        }

        void ContactOnlineOffline(object sender, ContactEventArgs e)
        {
            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();
        }

        void Nameserver_PingAnswer(object sender, PingAnswerEventArgs e)
        {
            nextPing = e.SecondsToWait;
        }

        void Nameserver_OIMReceived(object sender, OIMReceivedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived), sender, e);
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

        void OIMService_OIMSendCompleted(object sender, OIMSendCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted), sender, e);
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "OIM Send Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void Nameserver_ReverseAdded(object sender, ContactEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded), sender, e);
                return;
            }

            Contact contact = e.Contact;
            if (messenger.Nameserver.Owner.NotifyPrivacy == NotifyPrivacy.PromptOnAdd
                /* || messenger.Nameserver.BotMode */)  //If you want your provisioned account in botmode to fire ReverseAdded event, uncomment this.
            {
                // Show pending window if it is necessary.
                if (contact.OnPendingList || 
                    (contact.OnReverseList && !contact.OnAllowedList && !contact.OnBlockedList && !contact.OnPendingList))
                {
                    ReverseAddedForm form = new ReverseAddedForm(contact);
                    form.FormClosed += delegate(object f, FormClosedEventArgs fce)
                    {
                        form = f as ReverseAddedForm;
                        if (DialogResult.OK == form.DialogResult)
                        {
                            if (form.AddToContactList)
                            {
                                messenger.Nameserver.ContactService.AddNewContact(contact.Mail);
                                System.Threading.Thread.Sleep(200);

                                if (form.Blocked)
                                {
                                    contact.Blocked = true;
                                }
                            }
                            else if (form.Blocked)
                            {
                                contact.Blocked = true;
                            }
                            else
                            {
                                contact.OnAllowedList = true;
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
                    MessageBox.Show(contact.Mail + " accepted your invitation and added you their contact list.");
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
                        messenger.Credentials = new Credentials(accountTextBox.Text, passwordTextBox.Text, (MsnProtocol)Enum.Parse(typeof(MsnProtocol), comboProtocol.Text));
                       
                        // inform the user what is happening and try to connecto to the messenger network.
                        SetStatus("Connecting to server");
                        messenger.Connect();

                        displayImageBox.Image = global::MSNPSharpClient.Properties.Resources.loading;

                        loginButton.Tag = 1;
                        loginButton.Text = "Cancel";

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
                            SortByStatus();
                        else
                            SortByGroup();

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = false;
                        comboPlaces.Visible = false;
                    }
                    break;

                case 2: // connected -> disconnect
                    {
                        if (messenger.Connected)
                            messenger.Disconnect();

                        if (toolStripSortByStatus.Checked)
                            SortByStatus();
                        else
                            SortByGroup();

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = true;
                        comboPlaces.Visible = true;
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
                Invoke(new EventHandler<StatusChangedEventArgs>(Owner_StatusChanged), sender, e);
                return;
            }

            if (messenger.Nameserver.IsSignedIn)
            {
                comboStatus.SelectedIndex = comboStatus.FindString(messenger.Owner.Status.ToString());
            }
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
                    PresenceStatus old = messenger.Owner.Status;
                    //close all ConversationForms
                    if (ConversationForms.Count == 0 ||
                        MessageBox.Show("You are signing out from example client. All windows will be closed.", "Sign out",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        if (ConversationForms.Count != 0)
                        {
                            for (int i = 0; i < ConversationForms.Count; i++)
                            {
                                ConversationForms[i].Close();
                            }

                        }
                        loginButton.Tag = 2;
                        loginButton.PerformClick();
                        pnlNameAndPM.Visible = false;
                        comboPlaces.Visible = false;

                    }
                    comboStatus.SelectedIndex = comboStatus.FindString(old.ToString());
                }
                else
                {
                    messenger.Owner.Status = newstatus;
                }
            }
            else if (newstatus == PresenceStatus.Offline)
            {
                MessageBox.Show("You can not login as Offline :)");
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

        private void treeViewFavoriteList_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node.Level == 0)
            {

                Point[] points = !e.Node.IsExpanded
                    ? new Point[] {
                        new Point(e.Bounds.X+4, e.Bounds.Y +4),
                        new Point(e.Bounds.X+4, e.Bounds.Y +12),
                        new Point(e.Bounds.X+12, e.Bounds.Y + 8) }
                    : new Point[] {
                        new Point(e.Bounds.X + 12, e.Bounds.Y + 12),
                        new Point(e.Bounds.X + 12, e.Bounds.Y + 4),
                        new Point(e.Bounds.X + 4, e.Bounds.Y + 12)
                   };
                Point imageLocation = new Point(e.Bounds.X + 2, e.Bounds.Y + 2);
                if (e.Node.Tag is Circle)
                {
                    e.Graphics.FillEllipse(Brushes.Blue, new Rectangle(imageLocation, new Size(12, 12)));
                    e.Graphics.FillPolygon(Brushes.Yellow, points);
                }
                else
                    e.Graphics.FillPolygon(Brushes.Black, points);

                PointF textLocation = new PointF(imageLocation.X + 16, imageLocation.Y);
                e.Graphics.DrawString(e.Node.Text, PARENT_NODE_FONT, e.Node.Tag is Circle ? Brushes.Blue : Brushes.Black, textLocation);


            }
            else if (e.Node.Level == 1 && e.Node.Tag is Contact)
            {


                Contact contact = e.Node.Tag as Contact;
                Brush brush = Brushes.Green;

                switch (contact.Status)
                {
                    case PresenceStatus.Online:
                        brush = Brushes.Green;
                        break;

                    case PresenceStatus.Busy:
                        brush = Brushes.Red;
                        break;

                    case PresenceStatus.Away:
                    case PresenceStatus.Idle:
                        brush = Brushes.Orange;
                        break;

                    default:
                        brush = Brushes.Gray;
                        break;
                }

                Point imageLocation = new Point(e.Bounds.X + 2, e.Bounds.Y + 2);
                //e.Graphics.DrawImage((Image)MSNPSharpClient.Properties.Resources.smiley, imageLocation.X, imageLocation.Y, 14, 14);
                e.Graphics.FillEllipse(brush, new Rectangle(imageLocation, new Size(12, 12)));

                string text = contact.Name;
                if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                {
                    text += " - " + contact.PersonalMessage.Message;
                }

                PointF textLocation = new PointF(imageLocation.X + 16, imageLocation.Y);
                e.Graphics.DrawString(text, contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT, Brushes.Black, textLocation);



            }





        }

        private void comboPlaces_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboPlaces.SelectedIndex > 0)
            {
                string place = comboPlaces.Text.Split(' ')[comboPlaces.Text.Split(' ').Length - 1];
                if (comboPlaces.SelectedIndex == 1)
                {
                    messenger.Owner.Status = PresenceStatus.Offline;
                    comboPlaces.Visible = false;
                }
                else if (comboPlaces.SelectedIndex == comboPlaces.Items.Count - 1)
                {
                    messenger.Owner.SignoutFromEverywhere();
                    comboPlaces.Visible = false;
                }
                else
                {
                    foreach (KeyValuePair<Guid, string> keyvalue in messenger.Owner.Places)
                    {
                        if (keyvalue.Value == place)
                        {
                            messenger.Owner.SignoutFrom(keyvalue.Key);
                            break;
                        }
                    }
                }
            }
        }

        void cbRobotMode_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox cbBotMode = sender as ComboBox;
            messenger.Nameserver.BotMode = cbRobotMode.Checked;
        }

        private void Owner_PlacesChanged(object sender, EventArgs e)
        {
            if (comboPlaces.InvokeRequired)
            {
                comboPlaces.Invoke(new EventHandler(Owner_PlacesChanged), sender, e);
                return;
            }

            // if (messenger.Owner.Places.Count > 1)
            {
                comboPlaces.BeginUpdate();
                comboPlaces.Items.Clear();
                comboPlaces.Items.Add("(" + messenger.Owner.Places.Count + ") Places");
                comboPlaces.Items.Add("Signout from here (" + messenger.Owner.EpName + ")");

                foreach (KeyValuePair<Guid, string> keyvalue in messenger.Owner.Places)
                {
                    if (keyvalue.Key != NSMessageHandler.MachineGuid)
                    {
                        comboPlaces.Items.Add("Signout from " + keyvalue.Value);
                    }
                }

                comboPlaces.Items.Add("Signout from everywhere");
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
            SetStatus("Signed into the messenger network as " + messenger.Owner.Name);

            if (InvokeRequired)
            {
                Invoke(new EventHandler(Nameserver_SignedIn), sender, e);
                return;
            }

            // set our presence status
            loginButton.Tag = 2;
            loginButton.Text = "Sign off";
            pnlNameAndPM.Visible = true;
            comboPlaces.Visible = true;

            messenger.Owner.Status = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Text);

            propertyGrid.SelectedObject = messenger.Owner;

            Invoke(new EventHandler<EventArgs>(UpdateContactlist), sender, e);
        }

        private void Nameserver_SignedOff(object sender, SignedOffEventArgs e)
        {
            SetStatus("Signed off from the messenger network");

            if (InvokeRequired)
            {
                Invoke(new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff), sender, e);
                return;
            }

            tmrKeepOnLine.Enabled = false;
            tmrNews.Enabled = false;

            displayImageBox.Image = null;
            loginButton.Tag = 0;
            loginButton.Text = "> Sign in";
            pnlNameAndPM.Visible = false;
            comboPlaces.Visible = false;
        }

        private void Nameserver_ExceptionOccurred(object sender, ExceptionEventArgs e)
        {
            // ignore the unauthorized exception, since we're handling that error in another method.
            if (e.Exception is UnauthorizedException)
                return;

            MessageBox.Show(e.Exception.ToString(), "Nameserver exception");
        }

        private void NameserverProcessor_ConnectingException(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Connecting exception");
            SetStatus("Connecting failed");
        }

        private void Nameserver_AuthenticationError(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show("Authentication failed, check your account or password.", e.Exception.InnerException.Message);
            SetStatus("Authentication failed");
        }

        /// <summary>
        /// Updates the treeView.
        /// </summary>
        private void UpdateContactlist(object sender, EventArgs e)
        {
            if (messenger.Connected == false)
                return;

            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();

            tmrKeepOnLine.Enabled = true;
        }


        /// <summary>
        /// Notifies the user of errors which are send by the MSN server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Nameserver_ServerErrorReceived(object sender, MSNErrorEventArgs e)
        {
            // when the MSN server sends an error code we want to be notified.
            MessageBox.Show(e.MSNError.ToString(), "Server error received");
            SetStatus("Server error received");
        }

        /// <summary>
        /// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
        /// </summary>
        private delegate ConversationForm CreateConversationDelegate(Conversation conversation, Contact remote);

        private ConversationForm CreateConversationForm(Conversation conversation, Contact remote)
        {
            foreach (ConversationForm cform in ConversationForms)
            {
                if (cform.CanAttach(conversation))
                {
                    cform.AttachConversation(conversation);
                    return cform;
                }
            }

            // create a new conversation. However do not show the window untill a message is received.
            // for example, a conversation will be created when the remote client sends wants to send
            // you a file. You don't want to show the conversation form in that case.
            ConversationForm conversationForm = new ConversationForm(conversation, this, remote);
            // do this to create the window handle. Otherwise we are not able to call Invoke() on the
            // conversation form later.
            conversationForm.Handle.ToInt32();
            ConversationForms.Add(conversationForm);
            return conversationForm;
        }

        private void messenger_ConversationCreated(object sender, ConversationCreatedEventArgs e)
        {
            // check if the request is initiated remote or by this object
            // if it is initiated remote then we have to create a conversation form. Otherwise the 
            // form is already created and we don't need to create another one.
            if (e.Initiator == null)
            {
                // use the invoke method to create the form in the main thread, ONLY create the form after a contact joined our conversation.
                e.Conversation.ContactJoined += new EventHandler<ContactEventArgs>(Conversation_ContactJoined);
            }
        }

        void Conversation_ContactJoined(object sender, ContactEventArgs e)
        {
            //The request is initiated by remote user, so we needn't invite anyone.
            this.Invoke(new CreateConversationDelegate(CreateConversationForm), new object[] { sender, null });
            Conversation convers = sender as Conversation;
            convers.ContactJoined -= Conversation_ContactJoined; //We don't care any further join event anymore.
        }


        private delegate DialogResult ShowFileDialogDelegate(FileDialog dialog);

        private DialogResult ShowFileDialog(FileDialog dialog)
        {
            return dialog.ShowDialog();
        }

        /// <summary>
        /// Asks the user to accept or deny the incoming filetransfer invitation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messenger_TransferInvitationReceived(object sender, MSNSLPInvitationEventArgs e)
        {
            if (e.TransferProperties.DataType == DataTransferType.File)
            {
                if (MessageBox.Show(
                    e.TransferProperties.RemoteContact.Name +
                    " wants to send you a file.\r\nFilename: " +
                    e.Filename + "\r\nLength (bytes): " + e.FileSize,
                    "Filetransfer invitation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // by setting the Accept property in the EventArgs to true we give the transfer a green light				
                    saveFileDialog.FileName = e.Filename;
                    if ((DialogResult)Invoke(new ShowFileDialogDelegate(ShowFileDialog), new object[] { saveFileDialog }) == DialogResult.OK)
                    {
                        e.TransferSession.DataStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                        //e.Handler.AcceptTransfer (e);
                        e.Accept = true;
                        e.TransferSession.AutoCloseStream = true;
                    }
                }
            }
            else if (e.TransferProperties.DataType == DataTransferType.Activity)
            {
                if (MessageBox.Show(
                    e.TransferProperties.RemoteContact.Name +
                    " wants to invite you to join an activity.\r\nActivity name: " +
                    e.Activity.ActivityName + "\r\nAppID: " + e.Activity.AppID,
                    "Activity invitation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    e.TransferSession.DataStream = new MemoryStream();
                    e.Accept = true;
                    e.TransferSession.AutoCloseStream = true;
                }
            }
        }

        private int nextPing = 50;
        private void tmrKeepOnLine_Tick(object sender, EventArgs e)
        {
            if (nextPing > 0)
                nextPing--;
            if (nextPing == 0)
                messenger.Nameserver.SendPing();
        }



        private static Font PARENT_NODE_FONT = new Font("Tahoma", 8f, FontStyle.Bold);
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
                    return Convert.ToInt32(node.Tag) - Convert.ToInt32(node2.Tag);
                }
                else if (node.Tag is Contact && node2.Tag is Contact)
                {
                    if (((Contact)node.Tag).Status == ((Contact)node2.Tag).Status)
                    {
                        return string.Compare(((Contact)node.Tag).Name, ((Contact)node2.Tag).Name, StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (((Contact)node.Tag).Online)
                        return 1;
                    else if (((Contact)node2.Tag).Online)
                        return -1;
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
                Contact selectedContact = (Contact)e.Node.Tag;
                propertyGrid.SelectedObject = selectedContact;
                if (selectedContact.Online)
                {
                    sendIMMenuItem.PerformClick();
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.Node.Level == 0)
                {
                    if (e.Node.IsExpanded || (e.Node.ImageIndex == 1))
                    {
                        e.Node.Collapse();
                        e.Node.ImageIndex = 0;
                        e.Node.SelectedImageIndex = 0;
                    }
                    else
                    {
                        e.Node.Expand();
                        e.Node.ImageIndex = 1;
                        e.Node.SelectedImageIndex = 1;
                    }
                    if (e.Node.Tag is ContactGroup || e.Node.Tag is Circle)
                    {
                        propertyGrid.SelectedObject = e.Node.Tag;
                    }
                }
                else
                {
                    Contact selectedContact = (Contact)e.Node.Tag;

                    propertyGrid.SelectedObject = selectedContact;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (e.Node.Tag is Contact && e.Node.Level > 0)
                {
                    treeViewFavoriteList.SelectedNode = e.Node;
                    Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;

                    if (contact.Blocked)
                    {
                        blockMenuItem.Visible = false;
                        unblockMenuItem.Visible = true;
                    }
                    else
                    {
                        blockMenuItem.Visible = true;
                        unblockMenuItem.Visible = false;
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
            ((Contact)treeViewFavoriteList.SelectedNode.Tag).Blocked = true;
            treeViewFavoriteList.SelectedNode.NodeFont = USER_NODE_FONT_BANNED;
        }

        private void unblockMenuItem_Click(object sender, EventArgs e)
        {
            ((Contact)treeViewFavoriteList.SelectedNode.Tag).Blocked = false;
            treeViewFavoriteList.SelectedNode.NodeFont = USER_NODE_FONT;
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
                    if (form.Block)
                    {
                        contact.Blocked = true;
                    }

                    if (form.RemoveFromAddressBook)
                    {
                        messenger.Nameserver.ContactService.RemoveContact(contact);
                    }
                    else
                    {
                        contact.IsMessengerUser = false;
                    }
                }
            };
            form.ShowDialog(this);
        }

        private void sendMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Contact contact = treeViewFavoriteList.SelectedNode.Tag as Contact;
            bool activate = false;
            ConversationForm activeForm = null;
            foreach (ConversationForm conv in ConversationForms)
            {
                if (conv.Conversation.HasContact(contact) && 
                    (conv.Conversation.Type & ConversationType.Chat) == ConversationType.Chat)
                {
                    activeForm = conv;
                    activate = true;
                }

            }

            if (activate)
            {
                if (activeForm.WindowState == FormWindowState.Minimized)
                    activeForm.Show();

                activeForm.Activate();
                return;
            }


            Conversation convers = messenger.CreateConversation();
            ConversationForm form = CreateConversationForm(convers, contact);

            form.Show();
        }

        private void sendOfflineMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;
            messenger.Nameserver.OIMService.SendOIMMessage(selectedContact, new TextMessage("MSNP offline message"));

        }

        private void sendMIMMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            if (selectedContact.MobileAccess || selectedContact.ClientType == ClientType.PhoneMember)
            {
                messenger.Nameserver.SendMobileMessage(selectedContact, "MSNP mobile message");
            }
            else
                MessageBox.Show("This contact is not able to receive mobile messages");
        }


        private void btnSortBy_Click(object sender, EventArgs e)
        {
            int x = ((base.Location.X + ListPanel.Location.X) + treeViewFavoriteList.Location.X) + 5;
            int y = ((base.Location.Y + ListPanel.Location.Y) + treeViewFavoriteList.Location.Y) + (3 * btnSortBy.Height);
            sortContextMenu.Show(x, y);
            sortContextMenu.Focus();
        }

        private void toolStripSortByStatus_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortByStatus.Checked)
            {
                SortByStatus();
            }
            else
            {
                this.toolStripSortByStatus.Checked = true;
            }
        }

        private void SortByStatus()
        {
            this.treeViewFavoriteList.BeginUpdate();
            this.toolStripSortBygroup.Checked = false;
            this.treeViewFavoriteList.Nodes.Clear();

            TreeNode onlinenode = treeViewFavoriteList.Nodes.Add("0", "Online", 0, 0);
            onlinenode.NodeFont = PARENT_NODE_FONT;
            onlinenode.Tag = "0";
            TreeNode offlinenode = treeViewFavoriteList.Nodes.Add("1", "Offline", 0, 0);
            offlinenode.NodeFont = PARENT_NODE_FONT;
            offlinenode.Tag = "1";

            foreach (Circle circle in messenger.Nameserver.CircleList)
            {
                TreeNode circlenode = treeViewFavoriteList.Nodes.Add(circle.Mail, circle.Name, 0, 0);
                circlenode.NodeFont = PARENT_NODE_FONT;
                circlenode.Tag = circle;

                foreach (Contact member in circle.Members)
                {
                    TreeNode newnode = circlenode.Nodes.Add(member.Mail, member.Name);
                    newnode.NodeFont = member.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = member;
                }
            }

            foreach (Contact contact in messenger.ContactList.All)
            {
                TreeNode newnode = contact.Online ? onlinenode.Nodes.Add(contact.Mail, contact.Name) : offlinenode.Nodes.Add(contact.Mail, contact.Name);
                newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                newnode.Tag = contact;
            }

            onlinenode.Text = "Online (" + onlinenode.Nodes.Count.ToString() + ")";
            offlinenode.Text = "Offline (" + offlinenode.Nodes.Count.ToString() + ")";

            treeViewFavoriteList.Sort();
            treeViewFavoriteList.EndUpdate();
            treeViewFavoriteList.AllowDrop = false;
        }

        private void toolStripSortBygroup_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortBygroup.Checked)
            {
                SortByGroup();
            }
            else
            {
                this.toolStripSortBygroup.Checked = true;
            }
        }

        private void SortByGroup()
        {
            this.treeViewFavoriteList.BeginUpdate();
            this.toolStripSortByStatus.Checked = false;
            this.treeViewFavoriteList.Nodes.Clear();

            foreach (ContactGroup group in this.messenger.ContactGroups)
            {
                TreeNode node = treeViewFavoriteList.Nodes.Add(group.Guid, "0", 0, 0);
                node.ImageIndex = 0;
                node.NodeFont = PARENT_NODE_FONT;
                node.Tag = group;
            }

            TreeNode common = treeViewFavoriteList.Nodes.Add("0", "0", 0, 0);
            common.ImageIndex = 0;
            common.NodeFont = PARENT_NODE_FONT;
            common.Tag = String.Empty;

            foreach (Circle circle in messenger.Nameserver.CircleList)
            {
                TreeNode circlenode = treeViewFavoriteList.Nodes.Add(circle.Mail, circle.Name, 0, 0);
                circlenode.NodeFont = PARENT_NODE_FONT;
                circlenode.Tag = circle;

                foreach (Contact member in circle.Members)
                {
                    TreeNode newnode = circlenode.Nodes.Add(member.Mail, member.Name);
                    newnode.NodeFont = member.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = member;
                }
            }

            foreach (Contact contact in messenger.ContactList.All)
            {
                if (contact.ContactGroups.Count == 0)
                {
                    TreeNode newnode = common.Nodes.Add(contact.Mail, contact.Name);
                    newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = contact;
                    if (contact.Online)
                        common.Text = (Convert.ToInt32(common.Text) + 1).ToString();
                }
                else
                {
                    foreach (ContactGroup group in contact.ContactGroups)
                    {
                        TreeNode found = treeViewFavoriteList.Nodes.Find(group.Guid, false)[0];
                        TreeNode newnode = found.Nodes.Add(contact.Mail, contact.Name);
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.Tag = contact;
                        if (contact.Online)
                            found.Text = (Convert.ToInt32(found.Text) + 1).ToString();
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

            common.Text = "No group (" + common.Text + "/" + common.Nodes.Count + ")";

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
            if ((e.Item is TreeNode) && (((TreeNode)e.Item).Level > 0))
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
                            messenger.Nameserver.ContactService.AddContactToGroup(contact, (ContactGroup)newgroupNode.Tag);
                        }
                        if (oldgroupNode.Tag is ContactGroup)
                        {
                            messenger.Nameserver.ContactService.RemoveContactFromGroup(contact, (ContactGroup)oldgroupNode.Tag);
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

            SortByGroup();
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            if (this.loginButton.Tag.ToString() != "2")
            {
                MessageBox.Show("Please sign in first.");
                return;
            }
            AddContactForm acf = new AddContactForm();
            if (DialogResult.OK == acf.ShowDialog(this) && acf.Account != String.Empty)
            {
                messenger.Nameserver.ContactService.AddNewContact(acf.Account);
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
            if (!e.Circle.OnBlockedList)
            {
                messenger.ContactService.BlockCircle(e.Circle);
                e.Circle.ContactBlocked += new EventHandler<EventArgs>(Circle_ContactBlocked);
                Trace.WriteLine("Circle blocked: " + e.Circle.ToString());
            }

            Trace.WriteLine("Circle created: " + e.Circle.ToString());
        }

        void Circle_ContactBlocked(object sender, EventArgs e)
        {
            //Circle blocked, show you how to unblock.
            Circle circle = sender as Circle;
            if (circle != null)
            {
                messenger.ContactService.UnBlockCircle(circle);
                circle.ContactUnBlocked += new EventHandler<EventArgs>(circle_ContactUnBlocked);
                Trace.WriteLine("Circle unblocked: " + circle.ToString());
            }
        }

        void circle_ContactUnBlocked(object sender, EventArgs e)
        {
            //This demo shows you how to invite a contact to your circle.
            if (messenger.ContactList.HasContact("freezingsoft@hotmail.com", ClientType.PassportMember))
            {
                messenger.ContactService.InviteCircleMember(sender as Circle, messenger.ContactList["freezingsoft@hotmail.com", ClientType.PassportMember], "hello");
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
                    messenger.Nameserver.ContactService.AddNewContact(account, invitation);
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
                    if (contact.Mail.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1
                        ||
                        contact.Name.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        TreeNode newnode = foundnode.Nodes.Add(contact.Mail, contact.Name);
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
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
            string dn = lblName.Text;
            string pm = lblPM.Text;

            List<string> lstPersonalMessage = new List<string>(new string[] { "", "" });

            if (dn != messenger.Nameserver.Owner.Name)
            {

                lstPersonalMessage[0] = dn;
            }

            if (messenger.Nameserver.Owner.PersonalMessage == null || pm != messenger.Nameserver.Owner.PersonalMessage.Message)
            {
                lstPersonalMessage[1] = pm;

            }

            Thread updateThread = new Thread(new ParameterizedThreadStart(UpdateProfile));
            updateThread.Start(lstPersonalMessage);
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
                    Thread updateThread = new Thread(new ParameterizedThreadStart(UpdateProfile));
                    updateThread.Start(newImage);
                }
            }
        }

        private void UpdateProfile(object profileObject)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Updating owner profile, please wait....");

            if (profileObject is Image)
            {
                messenger.Nameserver.StorageService.UpdateProfile(profileObject as Image, "MyPhoto");
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Update displayimage completed.");
            }

            if (profileObject is List<string>)
            {
                List<string> lstPersonalMessage = profileObject as List<string>;
                if (lstPersonalMessage[0] != "")
                {
                    messenger.Nameserver.Owner.Name = lstPersonalMessage[0];
                }

                if (lstPersonalMessage[1] != "")
                {
                    messenger.Nameserver.Owner.PersonalMessage = new PersonalMessage(lstPersonalMessage[1], MediaType.None, null, NSMessageHandler.MachineGuid);
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Update personal message completed.");
            }
        }

        private void btnSetMusic_Click(object sender, EventArgs e)
        {
            MusicForm musicForm = new MusicForm();
            if (musicForm.ShowDialog() == DialogResult.OK)
            {
                messenger.Owner.PersonalMessage = new PersonalMessage(
                    messenger.Owner.PersonalMessage.Message,
                    MediaType.Music,
                    new string[] { musicForm.Artist, musicForm.Song, musicForm.Album, "" },
                    NSMessageHandler.MachineGuid);
            }
        }       
    }
}
