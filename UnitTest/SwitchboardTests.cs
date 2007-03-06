using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using NUnit.Framework;
using NUnit.Core;
using NUnit;
using MSNPSharp;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp.Test
{
	/// <summary>
	/// Switchboard tests
	/// </summary>
	[TestFixture]
	public class SwitchboardTests : TestBase
	{
		public SwitchboardTests()
		{
		}

		private string TextString = "D ist nicht d";

		private Conversation client2Conversation = null;

		[Test]
		public void CreateConversation()
		{
			ContactChangedEventHandler contactJoinedHandler = new ContactChangedEventHandler(Switchboard_ContactJoined);
			ConversationCreatedEventHandler conversationCreatedHandler = new ConversationCreatedEventHandler(Client2_ConversationCreated);

			// create conversation
			CreateWait(2);
				Conversation conversation = Client1.CreateConversation();
				Client2.ConversationCreated += conversationCreatedHandler;	
				conversation.Switchboard.ContactJoined += contactJoinedHandler;				
				conversation.Invite(Client1.ContactList[Client2.Owner.Mail]);
			Wait(16000);
				conversation.Switchboard.ContactJoined -= contactJoinedHandler;
				Client2.ConversationCreated -= conversationCreatedHandler;

			Assert.IsTrue(conversation.Switchboard.Contacts.Count == 1, "Contacts.Count = " + conversation.Switchboard.Contacts.Count.ToString());
			Assert.IsTrue(conversation.Switchboard.Contacts[Client2.Owner.Mail] != null);
			Assert.IsTrue(conversation.Switchboard.Invited == false);			
			Assert.IsTrue(conversation.Switchboard.IsSessionEstablished == true);
			Assert.IsTrue(conversation.SwitchboardProcessor.Connected == true);
	
			// now send messages
			TextMessage messageObject = new TextMessage(TextString);
			messageObject.Color = Color.Red;
			messageObject.CharSet = MessageCharSet.Russian;
			messageObject.Decorations = TextDecorations.Bold | TextDecorations.Italic;
			messageObject.Font = "Comic Sans MS";

			CreateWait(1);				
				client2Conversation.Switchboard.TextMessageReceived += new TextMessageReceivedEventHandler(Switchboard_TextMessageReceived);
				conversation.Switchboard.SendTextMessage(messageObject);
			Wait(8000);
				
			
		}
		

		private void Switchboard_ContactJoined(object sender, ContactEventArgs e)
		{
			Pulse();
		}

		private void Client2_ConversationCreated(object sender, ConversationCreatedEventArgs e)
		{
			Assert.IsTrue(e.Initiator == null);
			Assert.IsTrue(e.Conversation != null);
			Assert.IsTrue(e.Conversation.Switchboard.Invited == true);
			Assert.IsTrue(e.Conversation.Switchboard.Contacts.Count == 0);			

			client2Conversation = e.Conversation;

			Pulse();			
		}

		private void Switchboard_TextMessageReceived(object sender, TextMessageEventArgs e)
		{
			Assert.IsTrue(e.Message.Text.Equals(TextString));
			Assert.IsTrue(e.Message.Color.ToArgb() == Color.Red.ToArgb(), "Color != " + Color.Red.ToString() + ", but " + e.Message.Color.ToString());
			Assert.IsTrue(e.Message.CharSet == MessageCharSet.Russian);
			Assert.IsTrue((e.Message.Decorations & TextDecorations.Bold) > 0);
			Assert.IsTrue((e.Message.Decorations & TextDecorations.Italic) > 0);
			Assert.IsTrue(e.Message.Font ==  "Comic Sans MS");						
			Pulse();
		}
	}	
}
