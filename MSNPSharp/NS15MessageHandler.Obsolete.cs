using System;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;

namespace MSNPSharp
{    
    using MSNPSharp.Core;

    partial class NSMessageHandler
    {
        /// <summary>
        /// Send the synchronize command to the server. This will rebuild the contactlist with the most recent data.
        /// </summary>
        /// <remarks>
        /// Synchronizing is the most complete way to retrieve data about groups, contacts, privacy settings, etc.
        /// You <b>must</b> call this function before setting your initial status, otherwise you won't received online notifications of other clients.
        /// Please note that you can only synchronize a single time per session! (this is limited by the the msn-servers)
        /// </remarks>
        [Obsolete("Use NSMessageHandler.ContactService.SynchronizeContactList")]
        public virtual void SynchronizeContactList()
        {
            contactService.SynchronizeContactList();
        }

        /// <summary>
        /// Creates a new contact and sends a request to the server to add this contact to the forward and allowed list.
        /// </summary>
        /// <param name="account">An e-mail adress to add</param>
        [Obsolete("Use NSMessageHandler.ContactService.AddNewContact")]
        public virtual void AddNewContact(string account)
        {
            ContactService.AddNewContact(account);
        }

        /// <summary>
        /// Remove the specified contact from your forward and allow list. Note that remote contacts that are blocked remain blocked.
        /// </summary>
        /// <param name="contact">Contact to remove</param>
        [Obsolete("Use NSMessageHandler.ContactService.RemoveContact")]
        public virtual void RemoveContact(Contact contact)
        {
            ContactService.RemoveContact(contact);
        }

        [Obsolete("Use NSMessageHandler.ContactService.AddContactToGroup")]
        public virtual void AddContactToGroup(Contact contact, ContactGroup group)
        {
            ContactService.AddContactToGroup(contact, group);
        }

        [Obsolete("Use NSMessageHandler.ContactService.RemoveContactFromGroup")]
        public virtual void RemoveContactFromGroup(Contact contact, ContactGroup group)
        {
            ContactService.RemoveContactFromGroup(contact, group);
        }

        /// <summary>
        /// Block this contact. This way you don't receive any messages anymore. This contact
        /// will be removed from your allow list and placed in your blocked list.
        /// </summary>
        /// <param name="contact">Contact to block</param>
        [Obsolete("Use NSMessageHandler.ContactService.BlockContact")]
        public virtual void BlockContact(Contact contact)
        {
            ContactService.BlockContact(contact);
        }

        /// <summary>
        /// Unblock this contact. After this you are able to receive messages from this contact. This contact
        /// will be removed from your blocked list and placed in your allowed list.
        /// </summary>
        /// <param name="contact">Contact to unblock</param>
        [Obsolete("Use NSMessageHandler.ContactService.UnBlockContact")]
        public virtual void UnBlockContact(Contact contact)
        {
            ContactService.UnBlockContact(contact);
        }

        /// <summary>
        /// Send an offline message to a contact.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="msg"></param>
        [Obsolete("Use NSMessageHandler.OIMService.SendOIMMessage")]
        public virtual void SendOIMMessage(string account, string msg)
        {
            oimService.SendOIMMessage(account, msg);
        }

        /// <summary>
        /// Tracks the last contact object which has been synchronized. Used for BPR commands.
        /// </summary>
        [Obsolete]
        private Contact lastContactSynced = null;

        /// <summary>
        /// Tracks the number of LST commands to expect
        /// </summary>
        [Obsolete]
        private int syncContactsCount = 0;

        /// <summary>
        /// Keeps track of the last received synchronization identifier
        /// </summary>
        [Obsolete]
        private int lastSync = -1;

        /// <summary>
        /// Called when a LST command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send either a forward, reverse, blocked or access list.
        /// <code>LST [Contact Guid] [List Bit] [Display Name] [Group IDs]</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete]
        protected virtual void OnLSTReceived(NSMessage message)
        {
            // decrease the number of LST commands following the SYN command we can expect
            syncContactsCount--;

            int indexer = 0;

            //contact email					
            string _contact = message.CommandValues[indexer].ToString();
            Contact contact = ContactList.GetContact(_contact.Remove(0, 2));
            contact.NSMessageHandler = this;
            indexer++;

            // store this contact for the upcoming BPR commands			
            lastContactSynced = contact;

            //contact name
            if (message.CommandValues.Count > 3)
            {
                string name = message.CommandValues[indexer].ToString();
                contact.SetName(name.Remove(0, 2));

                indexer++;
            }

            if (message.CommandValues.Count > indexer && message.CommandValues[indexer].ToString().Length > 2)
            {
                string guid = message.CommandValues[indexer].ToString().Remove(0, 2);
                contact.SetGuid(new Guid(guid));

                indexer++;
            }

            //contact list				
            if (message.CommandValues.Count > indexer)
            {
                try
                {
                    int lstnum = int.Parse(message.CommandValues[indexer].ToString());

                    contact.SetLists((MSNLists)lstnum);
                    indexer++;

                }
                catch (System.FormatException)
                {
                }
            }

            if (message.CommandValues.Count > indexer)
            {
                string[] groupids = message.CommandValues[indexer].ToString().Split(new char[] { ',' });

                foreach (string groupid in groupids)
                    if (groupid.Length > 0 && contactGroups[groupid] != null)
                        contact.ContactGroups.Add(contactGroups[groupid]); //we add this way so the event doesn't get fired
            }

            // if all LST commands are send in this synchronization cyclus we call the callback
            if (syncContactsCount <= 0)
            {
                // set this so the user can set initial presence
                abSynchronized = true;

                if (AutoSynchronize == true)
                    OnSignedIn();

                // call the event
                if (SynchronizationCompleted != null)
                {
                    SynchronizationCompleted(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Called when a SYN command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send an answer to a request for a list synchronization from the client. It stores the 
        /// parameters of the command internally for future use with other commands.
        /// Raises the SynchronizationCompleted event when there are no contacts on any list.
        /// <code>SYN [Transaction] [Cache] [Cache] [Contact count] [Group count]</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete]
        protected virtual void OnSYNReceived(NSMessage message)
        {
            int syncNr = int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture);

            syncContactsCount = int.Parse((string)message.CommandValues[3], System.Globalization.CultureInfo.InvariantCulture);

            // check whether there is a new list version or no contacts are on the list
            if (lastSync == syncNr || syncContactsCount == 0)
            {
                syncContactsCount = 0;
                // set this so the user can set initial presence
                abSynchronized = true;

                if (AutoSynchronize == true)
                    OnSignedIn();

                // no contacts are sent so we are done synchronizing
                // call the callback
                // MSNP8: New callback defined, SynchronizationCompleted.
                if (SynchronizationCompleted != null)
                {
                    SynchronizationCompleted(this, new EventArgs());
                    abSynchronized = true;
                }
            }
            else
            {
                lastSync = syncNr;
                lastContactSynced = null;
            }
        }

        [Obsolete("Sending GTC command to NS causes disconnect", true)]
        protected virtual void OnGTCReceived(NSMessage message)
        {
        }

        #region Passport authentication
        /// <summary>
        /// Authenticates the contactlist owner with the Passport (Nexus) service.		
        /// </summary>
        /// <remarks>
        /// The passport uri in the ConnectivitySettings is used to determine the service location. Also if a WebProxy is specified the resource will be accessed
        /// through the webproxy.
        /// </remarks>
        /// <param name="twnString">The string passed as last parameter of the USR command</param>
        /// <returns>The ticket string</returns>
        [Obsolete("AuthenticatePassport", true)]
        private string AuthenticatePassport(string twnString)
        {
            // check whether we have settings to follow
            if (ConnectivitySettings == null)
                throw new MSNPSharpException("No ConnectivitySettings specified in the NSMessageHandler");

            try
            {
                // first login to nexus			
                /*
                Bas Geertsema: as of 14 march 2006 this is not done anymore since it returned always the same URI, and only increased login time.
				
                WebRequest request = HttpWebRequest.Create(ConnectivitySettings.PassportUri);
                if(ConnectivitySettings.WebProxy != null)
                    request.Proxy = ConnectivitySettings.WebProxy;

                Uri uri = null;
                // create the header				
                using(WebResponse response = request.GetResponse())
                {
                    string urls = response.Headers.Get("PassportURLs");			

                    // get everything from DALogin= till the next ,
                    // example string: PassportURLs: DARealm=Passport.Net,DALogin=login.passport.com/login2.srf,DAReg=http://register.passport.net/uixpwiz.srf,Properties=https://register.passport.net/editprof.srf,Privacy=http://www.passport.com/consumer/privacypolicy.asp,GeneralRedir=http://nexusrdr.passport.com/redir.asp,Help=http://memberservices.passport.net/memberservice.srf,ConfigVersion=11
                    Regex re = new Regex("DALogin=([^,]+)");
                    Match m  = re.Match(urls);
                    if(m.Success == false)
                    {
                        throw new MSNPSharpException("Regular expression failed; no DALogin (messenger login server) could be extracted");
                    }											
                    string loginServer = m.Groups[1].ToString();

                    uri = new Uri("https://"+loginServer);
                    response.Close();
                }
                */

                string ticket = null;
                // login at the passport server
                using (WebResponse response = PassportServerLogin(ConnectivitySettings.PassportUri, twnString, 0))
                {
                    // at this point the login has succeeded, otherwise an exception is thrown
                    ticket = response.Headers.Get("Authentication-Info");
                    Regex re = new Regex("from-PP='([^']+)'");
                    Match m = re.Match(ticket);
                    if (m.Success == false)
                    {
                        throw new MSNPSharpException("Regular expression failed; no ticket could be extracted");
                    }
                    // get the ticket (kind of challenge string
                    ticket = m.Groups[1].ToString();

                    response.Close();
                }

                return ticket;
            }
            catch (Exception e)
            {
                // call this event
                OnAuthenticationErrorOccurred(e);

                // rethrow to client programmer
                throw new MSNPSharpException("Authenticating with the Nexus service failed : " + e.ToString(), e);
            }
        }


        /// <summary>
        /// Login at the Passport server.		
        /// </summary>
        /// <exception cref="UnauthorizedException">Thrown when the credentials are invalid.</exception>
        /// <param name="serverUri"></param>
        /// <param name="twnString"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        [Obsolete("PassportServerLogin", true)]
        private WebResponse PassportServerLogin(Uri serverUri, string twnString, int retries)
        {
            // create the header to login
            // example header:
            // >>> GET /login2.srf HTTP/1.1\r\n
            // >>> Authorization: Passport1.4 OrgVerb=GET,OrgURL=http%3A%2F%2Fmessenger%2Emsn%2Ecom,example%40passport.com,pwd=password,lc=1033,id=507,tw=40,fs=1,ru=http%3A%2F%2Fmessenger%2Emsn%2Ecom,ct=1062764229,kpp=1,kv=5,ver=2.1.0173.1,tpf=43f8a4c8ed940c04e3740be46c4d1619\r\n
            // >>> Host: login.passport.com\r\n
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(serverUri);
            if (ConnectivitySettings.WebProxy != null)
                request.Proxy = ConnectivitySettings.WebProxy;

            request.Headers.Clear();
            string authorizationHeader = "Authorization: Passport1.4 OrgVerb=GET,OrgURL=http%3A%2F%2Fmessenger%2Emsn%2Ecom,sign-in=" + HttpUtility.UrlEncode(Credentials.Account) + ",pwd=" + HttpUtility.UrlEncode(Credentials.Password) + "," + twnString;
            request.Headers.Add(authorizationHeader);
            //string headersstr = request.Headers.ToString();

            // auto redirect does not work correctly in this case! (we get HTML pages that way)
            request.AllowAutoRedirect = false;
            request.PreAuthenticate = false;
            request.Timeout = 60000;

            if (Settings.TraceSwitch.TraceVerbose)
                System.Diagnostics.Trace.WriteLine("Making connection with Passport. URI=" + request.RequestUri, "NS11MessgeHandler");


            // now do the transaction
            try
            {
                // get response
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (Settings.TraceSwitch.TraceVerbose)
                    System.Diagnostics.Trace.WriteLine("Response received from Passport service: " + response.StatusCode.ToString() + ".", "NS11MessgeHandler");

                // check for responses						
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 200 OK response
                    return response;
                }
                else if (response.StatusCode == HttpStatusCode.Found)
                {
                    // 302 Found (this means redirection)
                    string newUri = response.Headers.Get("Location");
                    response.Close();

                    // call ourselfs again to try a new login					
                    return PassportServerLogin(new Uri(newUri), twnString, retries);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // 401 Unauthorized 
                    throw new UnauthorizedException("Failed to login. Response of passport server: " + response.Headers.Get(0));
                }
                else
                {
                    throw new MSNPSharpException("Passport server responded with an unknown header");
                }
            }
            catch (HttpException e)
            {
                if (retries < 3)
                {
                    return PassportServerLogin(serverUri, twnString, retries + 1);
                }
                else
                    throw e;
            }
        }
        #endregion
    }
};
