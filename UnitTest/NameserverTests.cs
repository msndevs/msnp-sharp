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
	/// Nameserver tests.
	/// </summary>
	[TestFixture]
	public class NameserverTests : TestBase
	{
		public NameserverTests()
		{
		}

		[Test]
		public void SetOnline()
		{
			// event handler to use
			MSNPSharp.Contact.StatusChangedEventHandler statusChangedHandler = new MSNPSharp.Contact.StatusChangedEventHandler(Owner_StatusChanged);

			// set status from busy -> online
			CreateWait();
				Client1.Owner.StatusChanged += statusChangedHandler;
				Client1.Owner.Status = PresenceStatus.Online;			
			Wait(5000);
				Client1.Owner.StatusChanged -= statusChangedHandler;
			Assert.IsTrue(Client1.Owner.Status == PresenceStatus.Online);
			Assert.IsTrue(Client1.Owner.Online);

			// set status to hidden
			CreateWait(2);					
				Client2.ContactList[Client1.Owner.Mail].ContactOffline += statusChangedHandler;
				Client1.Owner.StatusChanged += statusChangedHandler;
				Client1.Nameserver.SetPresenceStatus(PresenceStatus.Hidden);			
			Wait(2000);
				Client2.ContactList[Client1.Owner.Mail].ContactOffline -= statusChangedHandler;
				Client1.Owner.StatusChanged -= statusChangedHandler;

			
			// check whether client1 is hidden but still online 
			Assert.IsTrue(Client1.Owner.Status == PresenceStatus.Hidden);
			Assert.IsTrue(Client1.Owner.Online == true);

			// check whether client1 is offline, seen from client2
			Assert.IsTrue(((Contact)Client2.ContactList[Client1.Owner.Mail]).Status == PresenceStatus.Offline);
			Assert.IsTrue(((Contact)Client2.ContactList[Client1.Owner.Mail]).Online == false);

			// reset status to busy
			CreateWait();
				Client1.Owner.StatusChanged += statusChangedHandler;
				Client1.Nameserver.SetPresenceStatus(PresenceStatus.Busy);			
			Wait(2000);
				Client1.Owner.StatusChanged -= statusChangedHandler;

			// check the outcome
			Assert.IsTrue(Client1.Owner.Status == PresenceStatus.Busy);
		}


		[Test]
		public void SetName()
		{
			// Set a unicode name, with timestamp to always create a new name
			string name = "MSNPSharp Ludovt tr " + DateTime.Now.ToString();

			CreateWait();
				Client1.Owner.Name = name;
				Client1.Owner.ScreenNameChanged += new MSNPSharp.Contact.ContactChangedEventHandler(Owner_ScreenNameChanged);
			Wait(2000);			
			Assert.IsTrue(Client1.Owner.Name == name);
		}


		[Test]
		public void AddContacts()
		{
			ListMutatedAddedEventHandler contactAddedHandler = new ListMutatedAddedEventHandler(Nameserver_ContactAdded);

			// first add both clients from the list, if they are not present in the system
			// two wait objects, for forward and allow list
			if(Client1.ContactList[Client2.Owner.Mail] == null)
			{								
				CreateWait(3);	// fl, al, rl
					Client2.Nameserver.ContactAdded += contactAddedHandler;
					Client1.Nameserver.ContactAdded += contactAddedHandler;
					Client1.Nameserver.AddNewContact(Client2.Owner.Mail);					
				System.Console.WriteLine("Waiting 1");
				Wait(2000);
					Client1.Nameserver.ContactAdded -= contactAddedHandler;
					Client2.Nameserver.ContactAdded -= contactAddedHandler;
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail] != null);
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnForwardList == true);
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnAllowedList == true);
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnReverseList == true);
			}
			if(Client2.ContactList[Client1.Owner.Mail] == null)
			{				
				CreateWait(3);
					Client1.Nameserver.ContactAdded += contactAddedHandler;
					Client2.Nameserver.ContactAdded += contactAddedHandler;
					Client2.Nameserver.AddNewContact(Client1.Owner.Mail);
				System.Console.WriteLine("Waiting 2");
				Wait(2000);
					Client1.Nameserver.ContactAdded -= contactAddedHandler;
					Client2.Nameserver.ContactAdded -= contactAddedHandler;
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail] != null);
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnForwardList == true);
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnAllowedList == true);
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnReverseList == true);
			}
						
			// remove clients completely from both lists
			if(Client1.ContactList[Client2.Owner.Mail].OnForwardList == true)
			{
				CreateWait(2);	// fl en rl	
				if(Client1.ContactList[Client2.Owner.Mail].OnAllowedList) CreateWait();
				if(Client1.ContactList[Client2.Owner.Mail].OnBlockedList) CreateWait();
					Client2.Nameserver.ContactRemoved += contactAddedHandler;					
					Client1.Nameserver.ContactRemoved += contactAddedHandler;					
					Client1.ContactList[Client2.Owner.Mail].Blocked = false;					
					Client1.Nameserver.RemoveContact(Client1.ContactList[Client2.Owner.Mail]);					
				System.Console.WriteLine("Waiting 3");
				Wait(3000);
					Client1.Nameserver.ContactRemoved -= contactAddedHandler;		
					Client2.Nameserver.ContactRemoved -= contactAddedHandler;		
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail] != null);
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnForwardList == false);
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnBlockedList == false);
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnAllowedList == false);				
				Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].Online == false);
			}
			if(Client2.ContactList[Client1.Owner.Mail].OnForwardList == true)
			{
				CreateWait(2);	// fl en rl
				if(Client2.ContactList[Client1.Owner.Mail].OnAllowedList) CreateWait();
				if(Client2.ContactList[Client1.Owner.Mail].OnBlockedList) CreateWait();
					Client1.Nameserver.ContactRemoved += contactAddedHandler;					
					Client2.Nameserver.ContactRemoved += contactAddedHandler;					
					Client2.ContactList[Client1.Owner.Mail].Blocked = false;					
					Client2.Nameserver.RemoveContact(Client2.ContactList[Client1.Owner.Mail]);				
				System.Console.WriteLine("Waiting 4");
				Wait(3000);					
					Client2.Nameserver.ContactRemoved -= contactAddedHandler;
					Client1.Nameserver.ContactRemoved -= contactAddedHandler;
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail] != null);
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnForwardList == false);
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnBlockedList == false);
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnAllowedList == false);				
				Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].Online == false);
			}			

			// they should not be on each other's reverse list now
			Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnReverseList == false);
			Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnReverseList == false);
					
						 		
			// and add them both again			
			// 6 pulses, (al + rl + fl) * 2
			CreateWait(6);
				Client1.Nameserver.ContactAdded += contactAddedHandler;
				Client2.Nameserver.ContactAdded += contactAddedHandler;
				Client1.Nameserver.AddContactToList(Client1.ContactList[Client2.Owner.Mail], MSNLists.AllowedList);
				Client2.Nameserver.AddContactToList(Client2.ContactList[Client1.Owner.Mail], MSNLists.AllowedList);
				Client1.Nameserver.AddContactToList(Client1.ContactList[Client2.Owner.Mail], MSNLists.ForwardList);
				Client2.Nameserver.AddContactToList(Client2.ContactList[Client1.Owner.Mail], MSNLists.ForwardList);
			System.Console.WriteLine("Waiting 5");
			Wait(3000);
				Client1.Nameserver.ContactAdded -= contactAddedHandler;
				Client2.Nameserver.ContactAdded -= contactAddedHandler;
			Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnForwardList == true);
			Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnBlockedList == false);
			Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnAllowedList == true);
			Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].OnReverseList == true);
			//Assert.IsTrue(Client1.ContactList[Client2.Owner.Mail].Online == true);
			Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnForwardList == true);
			Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnBlockedList == false);
			Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnAllowedList == true);
			Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].OnReverseList == true);
			//Assert.IsTrue(Client2.ContactList[Client1.Owner.Mail].Online == true);
		}


		[Test]
		public void SetPrivacyPolicies()
		{			
			Client1.Owner.Privacy = PrivacyMode.NoneButAllowed;			
			Thread.Sleep(1000);
			Assert.IsTrue(Client1.Owner.Privacy == PrivacyMode.NoneButAllowed);

			Client1.Owner.Privacy = PrivacyMode.AllExceptBlocked;
			Thread.Sleep(1000);
			Assert.IsTrue(Client1.Owner.Privacy == PrivacyMode.AllExceptBlocked);			
		}


		[Test]
		public void BlockTest()
		{			
			MSNPSharp.Contact.ContactChangedEventHandler contactChangedHandler = new MSNPSharp.Contact.ContactChangedEventHandler(NameserverTests_ContactBlocker);
			ContactChangedEventHandler nsContactChangedHandler = new ContactChangedEventHandler(Nameserver_ContactOffline);
			
			// two waitobjects; 1 for blocking and 1 for going offline in client 2
			if(Client1.ContactList[Client2.Owner.Mail].Blocked != true)
			{
				CreateWait();
				CreateWait();
					Client1.ContactList[Client2.Owner.Mail].ContactBlocked += contactChangedHandler;
					Client2.Nameserver.ContactOffline += nsContactChangedHandler;
					Client1.ContactList[Client2.Owner.Mail].Blocked = true;	
				Wait(4000);
					Client1.ContactList[Client2.Owner.Mail].ContactBlocked -= contactChangedHandler;
					Client2.Nameserver.ContactOffline -= nsContactChangedHandler;
				Assert.IsTrue(((Contact)Client1.ContactList[Client2.Owner.Mail]).Blocked == true);
				Assert.IsTrue(((Contact)Client2.ContactList[Client1.Owner.Mail]).Status == PresenceStatus.Offline);
			}
			
			if(Client1.ContactList[Client2.Owner.Mail].Blocked != false)
			{
				CreateWait();
				CreateWait();
					Client1.ContactList[Client2.Owner.Mail].ContactUnBlocked += contactChangedHandler;
					Client2.Nameserver.ContactOnline += nsContactChangedHandler;
					Client1.ContactList[Client2.Owner.Mail].Blocked = false;
				Wait(4000);
					Client1.ContactList[Client2.Owner.Mail].ContactUnBlocked -= contactChangedHandler;
					Client2.Nameserver.ContactOnline -= nsContactChangedHandler;
				Assert.IsTrue(((Contact)Client1.ContactList[Client2.Owner.Mail]).Blocked == false);
				Assert.IsTrue(((Contact)Client2.ContactList[Client1.Owner.Mail]).Online == true);
			}
		}


		[Test]
		public void GroupCreationsTest()
		{						
			string newName = "TestGroup @ " + System.DateTime.Now.ToString();
			ContactGroupChangedEventHandler groupChangedEventHandler = new ContactGroupChangedEventHandler(Nameserver_ContactGroupAdded);

			// add group
			CreateWait();
				Client1.Nameserver.ContactGroupAdded += groupChangedEventHandler;
				Client1.Nameserver.AddContactGroup(newName);				
			Wait(2000);
				Client1.Nameserver.ContactGroupAdded -= groupChangedEventHandler;

			ContactGroup newGroup = Client1.ContactGroups[newName];				
			Assert.IsTrue(newGroup != null);			

			// change group name
			/*CreateWait();
				Client1.Nameserver.ContactGroupChanged += groupChangedEventHandler;
				newGroup.Name = newName + " changed";
			Wait(2000);
				Client1.Nameserver.ContactGroupChanged -= groupChangedEventHandler;
			System.Console.WriteLine("Contactgroup after namechange : " + newGroup.Name);
			Assert.IsTrue(newGroup.Name == newName + " changed");*/

			/*ContactGroup oldGroup = ((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroup;
		
			// set the client in the new list
			MSNPSharp.Contact.ContactChangedEventHandler contactChangedHandler = new MSNPSharp.Contact.ContactChangedEventHandler(NameserverTests_ContactGroupChanged);
			CreateWait();
				((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroupChanged += contactChangedHandler;
				((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroup = newGroup;
			Wait(2000);
				((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroupChanged -= contactChangedHandler;
			System.Console.WriteLine("Contactgroup after move : " + ((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroup.Name);
			Assert.IsTrue(((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroup == newGroup);
			*/
			
			// and revert back to group 0
			/*CreateWait();
				((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroupChanged += contactChangedHandler;
				((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroup = Client1.ContactGroups[0];
			Wait(2000);
				((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroupChanged -= contactChangedHandler;
			Assert.IsTrue(((Contact)Client1.ContactList[Client2.Owner.Mail]).ContactGroup ==  Client1.ContactGroups[0]);

			// remove the new group
			CreateWait();
				Client1.Nameserver.ContactGroupRemoved += groupChangedEventHandler;
				Client1.Nameserver.RemoveContactGroup(newGroup);			
			Wait(2000);
				Client1.Nameserver.ContactGroupRemoved -= groupChangedEventHandler;						
			Assert.IsTrue(Client1.ContactGroups[newName] == null);*/				
		}
	
		[Test]
		public void SetPhoneDetails()
		{
			Random rnd = new Random();
			string homePhone = rnd.Next(1000000, 9999999).ToString();
			string mobPhone  = rnd.Next(1000000, 9999999).ToString();			
			string workPhone = rnd.Next(1000000, 9999999).ToString();
			bool mobileAccess = !Client1.Owner.MobileAccess;
			

			Client1.Owner.HomePhone   = homePhone;
			Client1.Owner.WorkPhone   = workPhone;
			Client1.Owner.MobilePhone = mobPhone;			
			Client1.Owner.MobileAccess= mobileAccess;
			
			// wait some time for everything to be processed
			Thread.Sleep(2000);

			Console.WriteLine(Client1.Owner.HomePhone);
			Assert.IsTrue(Client1.Owner.HomePhone    == homePhone);
			Assert.IsTrue(Client1.Owner.WorkPhone    == workPhone);
			Assert.IsTrue(Client1.Owner.MobilePhone  == mobPhone);			
			Assert.IsTrue(Client1.Owner.MobileAccess == mobileAccess);

			Client1.Owner.HomePhone   = "";
			Thread.Sleep(500);
			Assert.IsTrue(Client1.Owner.HomePhone    == "");
		}


		private void Owner_ContactOnline(object sender, EventArgs e)
		{		
			Pulse();
		}

		private void Owner_StatusChanged(object sender, StatusChangeEventArgs e)
		{
			Pulse();
		}

		private void Owner_ScreenNameChanged(object sender, EventArgs e)
		{
			Pulse();
		}

		private void NameserverTests_ContactBlocker(object sender, EventArgs e)
		{
			Pulse();
		}

		private void Nameserver_ContactGroupAdded(object sender, ContactGroupEventArgs e)
		{
			Console.WriteLine("ADG event : " + e.ContactGroup.Name);
			Pulse();
		}

		private void Nameserver_ContactGroupChanged(object sender, ContactGroupEventArgs e)
		{
			Console.WriteLine("ADG change event : " + e.ContactGroup.Name);
			Pulse();
		}

		private void NameserverTests_ContactGroupChanged(object sender, EventArgs e)
		{
			Pulse();
		}

		private void Nameserver_ContactGroupRemoved(object sender, ContactGroupEventArgs e)
		{
			Pulse();
		}

		private void Owner_ContactOffline(object sender, StatusChangeEventArgs e)
		{
			Pulse();
		}

		private void Nameserver_ContactAdded(object sender, ListMutateEventArgs e)
		{
			//Console.WriteLine("Pulse: " + e.AffectedList.ToString());
			Pulse();
		}

		private void Nameserver_ContactOffline(object sender, ContactEventArgs e)
		{
			Pulse();
		}
	}	
}
