using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MSNPSharp;
using System.Diagnostics;

namespace MSNPSharpClient
{
    public partial class ContactCardForm : Form
    {
        private ContactCard card = null;
        public ContactCardForm(ContactCard cc)
        {
            InitializeComponent();
            card = cc;
            picDisplayImage.Image = cc.DisplayImage;
            lblDisplayName.Text = cc.DisplayName;
            lblSpaceTitle.Text = cc.Space.Title;

            if (cc.Album != null)
            {
                lnkAlbumName.Text = cc.Album.Title;
                int col = 0;
                foreach (ThumbnailImage img in cc.Album.Photos)  //Setting the thumbnail pictures.
                {
                    PictureBox lnkPic = new PictureBox();
                    lnkPic.Dock = DockStyle.Fill;
                    lnkPic.Margin = new Padding(6);
                    lnkPic.BorderStyle = BorderStyle.FixedSingle;
                    lnkPic.Image = img.Image;
                    lnkPic.SizeMode = PictureBoxSizeMode.Zoom;
                    lnkPic.BackColor = Color.White;

                    tlpnlAlbum.Controls.Add(lnkPic, col, 0);
                    lnkPic.Tag = col;
                    lnkPic.Visible = true;
                    lnkPic.Click += new EventHandler(lnkPic_Click);
                    ttips.SetToolTip(lnkPic, img.ToolTip);
                    col++;
                }
            }
            else
            {
                pnlAlbum.Visible = false;
            }

            if (cc.NewPost != null)
            {
                lnkBlogTitle.Text = cc.NewPost.Title;
                ttips.SetToolTip(lnkBlogTitle, cc.NewPost.Description);
                lnkBlogContent.Text = cc.NewPost.Description;
                ttips.SetToolTip(lnkBlogContent, cc.NewPost.Description);
            }
            else
            {
                pnlBlog.Visible = false;
            }

            Text = cc.DisplayName + "'s ContactCard";
        }

        void lnkPic_Click(object sender, EventArgs e)
        {
            Process.Start(card.Album.Photos[(int)((PictureBox)sender).Tag].Url);
        }

        void lnkPic_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(card.Album.Photos[(int)((LinkLabel)sender).Tag].Url);
        }

        private void lblSpaceTitle_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(card.Space.Url);
        }

        private void ContactCardForm_Load(object sender, EventArgs e)
        {

        }

        private void lnkAlbumName_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(card.Album.Url);
        }

        private void lnkBlogTitle_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(card.NewPost.Url);
        }

        private void lnkBlogContent_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(card.NewPost.Url);
        }
    }
}