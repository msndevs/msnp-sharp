using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using MSNPSharp;
using MSNPSharp.Core;
using MSNPSharp.IO;

namespace MSNPSharpClient
{

    using MSNPSharp.MSNWS.MSNABSharingService;

    public partial class ReverseAddedForm : Form
    {
        Contact pendingContact;
        Messenger messenger;

        public ReverseAddedForm(Contact contact, Messenger messenger)
        {
            InitializeComponent();

            this.pendingContact = contact;
            this.messenger = messenger;

            Text = String.Format(Text, contact.Account);
            lblAdded.Text = String.Format(lblAdded.Text, contact.PublicProfileName + " (" + contact.Account + ")");
        }

        public bool AddAsFriend
        {
            get
            {
                return rbAllow.Checked;
            }
        }

        public bool Delete
        {
            get
            {
                return rbDeleteRequest.Checked;
            }
        }

        public bool Block
        {
            get
            {
                return rbBlock2.Checked;
            }
        }

        private void btn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start("http://profile.live.com/cid-" + String.Format("{0:x}", pendingContact.CID) + "/");
        }

        private void ReverseAddedForm_Load(object sender, EventArgs e)
        {

            pictureBox1.Image = DisplayImage.DefaultImage;

            messenger.DirectoryService.Get(pendingContact.CID,
                delegate
                {
                    if (pendingContact.CoreProfile.ContainsKey(CoreProfileAttributeName.PublicProfile_DisplayLastName))
                    {
                        lblAdded.Text = pendingContact.PublicProfileName + " (" + pendingContact.Account + ")";
                    }

                    if (pendingContact.CoreProfile.ContainsKey(CoreProfileAttributeName.UserTileStaticUrl))
                    {
                        HttpAsyncDataDownloader.BeginDownload(
                            pendingContact.CoreProfile[CoreProfileAttributeName.UserTileStaticUrl] + "?t=" + System.Web.HttpUtility.UrlEncode(messenger.StorageTicket),
                            delegate(object s, ObjectEventArgs oea)
                            {
                                pictureBox1.Image = Image.FromStream(new MemoryStream(oea.Object as byte[]));
                            },

                            messenger.ConnectivitySettings.WebProxy);
                    }
                },
                null);


            messenger.ContactService.FindFriendsInCommon(pendingContact, 4,
                delegate(object service, FindFriendsInCommonCompletedEventArgs ffincea)
                {
                    FindFriendsInCommonResult result = ffincea.Result.FindFriendsInCommonResult;

                    if (result.MatchedCount > 0 && result.MatchedList != null)
                    {
                        groupBox1.Visible = true;

                        int col = 0;
                        foreach (ContactType ct in result.MatchedList)
                        {
                            Contact cont = messenger.ContactList.GetContactByCID(ct.contactInfo.CID);
                            if (cont != null)
                            {
                                PictureBox lnkPic = new PictureBox();
                                lnkPic.Dock = DockStyle.Fill;
                                lnkPic.Margin = new Padding(6);
                                lnkPic.BorderStyle = BorderStyle.FixedSingle;
                                lnkPic.SizeMode = PictureBoxSizeMode.StretchImage;
                                lnkPic.BackColor = Color.White;
                                tableLayoutPanel1.Controls.Add(lnkPic, col, 0);

                                toolTip1.SetToolTip(lnkPic, cont.Name + " - " + cont.Account);
                                lnkPic.Cursor = System.Windows.Forms.Cursors.Hand;
                                lnkPic.Tag = cont;
                                lnkPic.Visible = true;
                                lnkPic.Click +=
                                    delegate
                                    {
                                        Process.Start("http://profile.live.com/cid-" + String.Format("{0:x}", cont.CID) + "/");
                                    };

                                col++;

                                cont.CoreProfileUpdated += new EventHandler<EventArgs>(cont_CoreProfileUpdated);
                                messenger.DirectoryService.Get(cont.CID);
                            }
                        }
                    }

                });
        }

        void cont_CoreProfileUpdated(object sender, EventArgs e)
        {
            Contact c = sender as Contact;
            c.CoreProfileUpdated -= cont_CoreProfileUpdated;

            if (c.CoreProfile.ContainsKey(CoreProfileAttributeName.UserTileStaticUrl))
            {
                foreach (Control control in tableLayoutPanel1.Controls)
                {
                    if (control.Tag is Contact && ((Contact)control.Tag) == c)
                    {
                        PictureBox pb = control as PictureBox;

                        HttpAsyncDataDownloader.BeginDownload(
                            c.CoreProfile[CoreProfileAttributeName.UserTileStaticUrl] + "?t=" + System.Web.HttpUtility.UrlEncode(messenger.StorageTicket),
                            delegate(object s, ObjectEventArgs oea)
                            {
                                pb.Image = Image.FromStream(new MemoryStream(oea.Object as byte[]));
                            },

                            messenger.ConnectivitySettings.WebProxy);

                    }
                }

            }

        }
    }
};