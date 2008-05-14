using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MSNPSharpClient
{
    public partial class AddContactForm : Form
    {
        public AddContactForm()
        {
            InitializeComponent();
        }

        private string account;
        public string Account
        {
            get
            {
                return account;
            }
        }

        private string invitationMessage;
        public string InvitationMessage
        {
            get
            {
                return invitationMessage;
            }
        }

        private bool isYahooUser;
        public bool IsYahooUser
        {
            get
            {
                return isYahooUser;
            }
        }


        private void btn_Click(object sender, EventArgs e)
        {
            account = txtAccount.Text;
            invitationMessage = txtInvitation.Text;
            isYahooUser = rbYahoo.Checked;

            Close();
        }

        private void txtAccount_TextChanged(object sender, EventArgs e)
        {
            if (-1 != txtAccount.Text.IndexOf("yahoo.", StringComparison.CurrentCultureIgnoreCase))
                rbYahoo.Checked = true;
            else
                rbLive.Checked = true;
        }

    }
}