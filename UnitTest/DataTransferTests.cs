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
	/// Summary description for NameserverTests.
	/// </summary>
	[TestFixture]
	public class DataTransferTests : TestBase
	{		
		public DataTransferTests()
		{
			Settings.TraceSwitch.Level = TraceLevel.Verbose;
		}
		
		private byte[] sendBuffer = new byte[2048];

		[Test]
		public void TransferData()
		{			
			Random rnd = new Random();
			rnd.NextBytes(sendBuffer);

			MemoryStream memStream = new MemoryStream(sendBuffer);
			MSNSLPHandler handler = Client1.GetMSNSLPHandler(Client2.Owner.Mail);
			Thread.Sleep(10000);
			Client2.ConversationCreated += new ConversationCreatedEventHandler(Client2_ConversationCreated);				
			Client2.TransferInvitationReceived += new MSNSLPInvitationReceivedEventHandler(Client2_TransferInvitationReceived);
			handler.TransferSessionClosed += new P2PTransferSessionChangedEventHandler(handler_TransferSessionClosed);

			Settings.TraceSwitch.Level = TraceLevel.Error;
			for(int i = 0; i < 40; i++)
			{
				CreateWait();
				P2PTransferSession session = handler.SendInvitation(Client1.Owner.Mail, Client2.Owner.Mail, "test.dat", memStream);
				session.TransferFinished += new EventHandler(session_TransferFinished);
				Wait(15000);
				CreateWait(3);
				Wait(15000);
			}
		}

		private void Client2_TransferInvitationReceived(object sender, MSNSLPInvitationEventArgs e)
		{			
			Pulse();
			Assert.IsTrue(e.Filename == "test.dat");
			Assert.IsTrue(e.FileSize == sendBuffer.Length);
			Assert.IsTrue(e.TransferSession.IsSender == false);			
			Assert.IsTrue(e.TransferProperties.RemoteContact == Client1.Owner.Mail);
			Assert.IsTrue(e.TransferProperties.RemoteInvited == true);
			Assert.IsTrue(e.TransferProperties.LocalContact == Client2.Owner.Mail);
			Assert.IsTrue(e.TransferSession.DataStream is MemoryStream);			
			
			e.TransferSession.TransferFinished += new EventHandler(TransferSession_TransferFinished);
			//e.Accept = true;
			e.Handler.AcceptTransfer (e);
			
		}

		private void TransferSession_TransferFinished(object sender, EventArgs e)
		{			
			Assert.IsTrue(sender is P2PTransferSession);			
			P2PTransferSession session = (P2PTransferSession)sender;
			
			Assert.IsTrue(session.DataStream != null);			
			Assert.IsTrue(session.DataStream.Length == sendBuffer.Length);			
			Assert.IsTrue(session.DataStream is MemoryStream);
			
			MemoryStream memStream = (MemoryStream)session.DataStream;
			byte[] receiveBuffer = memStream.ToArray();
			for(int i = 0; i < sendBuffer.Length; i++)
			{				
				Assert.IsTrue(sendBuffer[i] == receiveBuffer[i]);
			}
			
			Pulse();
		}

		private void session_TransferFinished(object sender, EventArgs e)
		{			
			P2PTransferSession session = (P2PTransferSession)sender;
			Assert.IsTrue(session.IsSender == true);
			Assert.IsTrue(session.AutoCloseStream == false);
			Assert.IsTrue(session.DataStream.Length == sendBuffer.Length);			
			Pulse();
		}

		private void Client2_ConversationCreated(object sender, ConversationCreatedEventArgs e)
		{
			Console.WriteLine("Conversation created for client2");			
		}

		private void handler_TransferSessionClosed(object sender, P2PTransferSessionEventArgs e)
		{			
			Pulse();
		}
	}	
}
