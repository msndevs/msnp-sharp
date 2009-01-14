#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
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

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    /// <summary>
    /// Specifies the user credentials. These settings are used when authentication
    /// is required on the network.
    /// </summary>
    /// <remarks>
    /// The client identifier, together with the client code, represents
    /// a unique way of identifying the client connected to the network.
    /// 
    /// Third party softwarehouses can request their own identifier/code combination
    /// for their software. These values have to be stored in the properties before connecting
    /// to the network.
    /// When you want to emulate the Microsoft MSN Messenger client, you can use any of the following
    /// values:
    /// <c>
    /// ClientID			ClientCode
    /// msmsgs@msnmsgr.com	Q1P7W2E4J9R8U3S5 
    /// PROD0038W!61ZTF9	VT6PX?UQTM4WM%YR 
    /// PROD0058#7IL2{QD	QHDCY@7R1TB6W?5B 
    /// PROD0061VRRZH@4F	JXQ6J@TUOGYV@N0M
    /// PROD0119GSJUC$18    ILTXC!4IXB5FB*PX
    /// </c>
    /// 
    /// Note that officially you must use an obtained license (client id and client code) from Microsoft in order to access the network legally!
    /// After you have received your own license you can set the client id and client code in this class.
    /// </remarks>
    [Serializable()]
    public class Credentials
    {
        private string clientID = Properties.Resources.ProductID;
        private string clientCode = Properties.Resources.ProductKey;
        private string password;
        private string account;

        /// <summary>
        /// The client identifier used to identify the clientsoftware.
        /// </summary>
        public string ClientID
        {
            get
            {
                return clientID;
            }
        }
        /// <summary>
        /// The client code used to identify the clientsoftware.
        /// </summary>
        public string ClientCode
        {
            get
            {
                return clientCode;
            }
        }

        /// <summary>
        /// Password for the account. Used when logging into the network.
        /// </summary>
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        /// <summary>
        /// The account the identity uses. A typical messenger account is specified as name@hotmail.com.
        /// </summary>
        public string Account
        {
            get
            {
                return account;
            }
            set
            {
                account = value;
            }
        }

        /// <summary>
        /// Constructor to instantiate a Credentials object.
        /// </summary>
        public Credentials()
        {
        }

        /// <summary>
        /// Constructor to instantiate a Credentials object with the specified values.
        /// </summary>
        public Credentials(string account, string password, string clientID, string clientCode)
        {
            this.account = account;
            this.password = password;
            this.clientCode = clientCode;
            this.clientID = clientID;
        }
    }
};
