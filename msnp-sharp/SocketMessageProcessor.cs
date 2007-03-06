#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion

using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Threading;
using Org.Mentalis.Network.ProxySocket;
using MSNPSharp;
using MSNPSharp.DataTransfer;

namespace MSNPSharp.Core
{	

	/// <summary>
	/// Processes I/O of network message through a network connection.
	/// </summary>
	/// <remarks>
	/// SocketMessageProcessor is a message processor which sends over and receives messages
	/// from a socket connection. This is usually across the internet, but for file transfers
	/// or alternative messenger servers this can also be a LAN connection.
	/// A SocketMessageProcess object uses the connection settings in the <see cref="Connection"/> class. 
	/// Proxyservers and Webproxies are supported.
	/// </remarks>
	public class SocketMessageProcessor : IMessageProcessor
	{

		#region Private
		/// <summary>
		/// </summary>
		private ConnectivitySettings connectivitySettings = new ConnectivitySettings();

		/// <summary>
		/// The buffer in which the socket writes the data.
		/// Default buffer size is 1500 bytes.
		/// We can use a buffer at class level because data is received synchronized.
		/// E.g.: there are no multiple calls to BeginReceive().
		/// </summary>
		private byte[] socketBuffer = new byte[1500];

		#endregion
		
		#region Protected
		/// <summary>
		/// Set when a socket is prepared with proxy server enabled. This caches the ip adress of the proxyserver and eliminates resolving it everytime a socket is prepared.
		/// </summary>
		protected IPEndPoint ProxyEndPoint
		{
			get { return proxyEndPoint; }
			set { proxyEndPoint = value;}
		}

		/// <summary>
		/// </summary>
		private IPEndPoint proxyEndPoint = null;

		/// <summary>
		/// The socket used for exchanging messages.
		/// </summary>
        private ProxySocket socket = null;

		/// <summary>
		/// The messagepool used to buffer messages. 
		/// </summary>
		protected MessagePool MessagePool
		{
			get { return messagePool; }
			set { messagePool = value;}
		}

		/// <summary>
		/// </summary>
		private MessagePool messagePool	= null;

		/// <summary>
		/// Returns a socket which is setup using the settings in the ConnectivitySettings field.
		/// Always use this method when you want to use sockets.
		/// </summary>
		/// <exception cref="ConnectivityException">Raised when the proxy server can not be resolved</exception>
		/// <returns></returns>
		protected virtual ProxySocket GetPreparedSocket()
		{
            //Creates the Socket for sending data over TCP.
			ProxySocket socket = new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
			// incorporate the connection settings like proxy's						
			// Note: ProxyType is in MSNPSharp namespace, ProxyTypes in ProxySocket namespace.
			if(ConnectivitySettings.ProxyType != ProxyType.None)
			{
				// set the proxy type
				socket.ProxyType = (ConnectivitySettings.ProxyType == ProxyType.Socks4) ? Org.Mentalis.Network.ProxySocket.ProxyTypes.Socks4 : Org.Mentalis.Network.ProxySocket.ProxyTypes.Socks5;
				socket.ProxyUser = ConnectivitySettings.ProxyUsername;
				socket.ProxyPass = ConnectivitySettings.ProxyPassword;

				// resolve the proxy host
				if(proxyEndPoint == null)
				{
					bool worked = false;
					int retries = 0;
					Exception exp = null;
					
					//we retry a few times, because dns resolve failure is quite common
					do
					{
						try
						{
							System.Net.IPHostEntry ipHostEntry = System.Net.Dns.GetHostEntry(ConnectivitySettings.ProxyHost);
							System.Net.IPAddress ipAddress = ipHostEntry.AddressList[0];

							// assign to the connection object so other sockets can make use of it quickly
							proxyEndPoint = new IPEndPoint(ipAddress, ConnectivitySettings.ProxyPort);
							
							worked = true;
						}
						catch(Exception e)
						{
							retries++;
							exp = e;
						}
					} while (!worked && retries < 3);
					
					if (!worked)
						throw new ConnectivityException("DNS Resolve for the proxy server failed: " + ConnectivitySettings.ProxyHost + " failed.", exp);
				}				

				socket.ProxyEndPoint = proxyEndPoint;
			}
			else
				socket.ProxyType = ProxyTypes.None;

			// set socket options

			//Send operations will timeout of confirmation is not received within 60000 milliseconds.
			socket.SendTimeout = 60000;
			socket.ReceiveTimeout = 60000;
			
			LingerOption lingerOption = new LingerOption(false, 0);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, lingerOption);
			
			return socket;
		}

		/// <summary>
		/// The callback used by the Socket.BeginReceive method.
		/// </summary> 
		/// <param name="ar">The used socket.</param>
		protected virtual void EndSendCallback(IAsyncResult ar)
		{
			ProxySocket socket = (ProxySocket)ar.AsyncState;
			socket.EndSend(ar);
		}

		/// <summary>
		/// Used by descendants classes to send raw byte data over the socket connection.
		/// This function is at the moment blocking. This method uses the default socket in the SocketMessageProcessor class.
		/// </summary>
		/// <param name="data"></param>
		protected void SendSocketData(byte[] data)
		{
			if (socket == null || !socket.Connected)
			{
				// the connection is closed
				OnDisconnected();
				return;
			}
			
			SendSocketData(socket, data);
		}

		/// <summary>
		/// Used by descendants classes to send raw byte data over the socket connection.
		/// This function is at the moment blocking. The data is send over the specified socket connection.
		/// </summary>
		protected void SendSocketData(Socket psocket, byte[] data)
		{			
			try
			{
				lock(psocket)
				{
					psocket.Send(data, 0, data.Length, SocketFlags.None);
				}
			}
			catch(Exception e)
			{
				throw new MSNPSharpException("Error while sending network message. See the inner exception for more details.", e);
			}
		}


		/// <summary>
		/// This methods is called when data is retreived from the message pool.
		/// It represents a single message. The processor has to convert this to
		/// a NetworkMessage object and pass it on to a MessageHandler.
		/// </summary>
		/// <param name="data"></param>
		protected virtual void OnMessageReceived(byte[] data)
		{
			// do nothing since this is a base class
		}		

		/// <summary>
		/// The callback used by the Socket.BeginReceive method.
		/// </summary> 
		/// <param name="ar">The used socket.</param>
		protected virtual void EndReceiveCallback(IAsyncResult ar)
		{
			int cnt = 0;
			try
			{
				System.Diagnostics.Debug.Assert(messagePool != null, "Field messagepool must be defined in derived class of SocketMessageProcessor.");

                Socket socket = (Socket)ar.AsyncState;
				cnt = socket.EndReceive(ar);
				if(cnt == 0)
				{
					// No data is received. We are disconnected.
					OnDisconnected();
					return;
				}			

				// read the messages and dispatch to handlers
				using(BinaryReader reader = new BinaryReader(new MemoryStream(socketBuffer, 0, cnt)))
				{                    
					messagePool.BufferData(reader);
				}
				while(messagePool.MessageAvailable)
				{
					// retrieve the message
					byte[] incomingMessage = messagePool.GetNextMessageData();

					
					// call the virtual method to perform polymorphism, descendant classes can take care of it
					OnMessageReceived(incomingMessage);
				}

				// start a new read				
				BeginDataReceive(socket);
			}			
			catch(SocketException e)
			{				
				// close the socket upon a exception
				if(socket != null && socket.Connected) socket.Close();

				OnDisconnected();

				// an exception Occurred, pass it through
				if(ConnectionException != null)
					ConnectionException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor encountered a socket exception while retrieving data. See the inner exception for more information.",e)));				
			}
			catch(ObjectDisposedException)
			{				
				// the connection is closed
				OnDisconnected();				
			}
			catch(Exception e)
			{				
				// close the socket upon a exception
				if(socket != null && socket.Connected) socket.Close();	
			
				if(Settings.TraceSwitch.TraceError)
					System.Diagnostics.Trace.WriteLine(e.ToString() + "\r\n" + e.StackTrace + "\r\n", "SocketMessageProcessor");

				OnDisconnected();

				if(ConnectionException != null)
					ConnectionException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor encountered a general exception while retrieving data. See the inner exception for more information.",e)));				
			}						
		}


		/// <summary>
		/// The callback used by the Socket.BeginConnect() method.
		/// The ProxySocket class behaves different from the standard Socket class.
		/// The callback is called after a connection has already been established.
		/// </summary>
		/// <param name="ar">The used socket.</param>
		protected virtual void EndConnectCallback(IAsyncResult ar)
		{
			try
			{
                if (Settings.TraceSwitch.TraceVerbose)
                    System.Diagnostics.Trace.WriteLine("End Connect Callback", "SocketMessageProcessor");

                ((ProxySocket)socket).EndConnect(ar);

                if (Settings.TraceSwitch.TraceVerbose)
                    System.Diagnostics.Trace.WriteLine("End Connect Callback Daarna", "SocketMessageProcessor");
				OnConnected();				

				// Begin receiving data
				BeginDataReceive(socket);
			}
			catch(Exception e)
			{
                if (Settings.TraceSwitch.TraceError)
                    System.Diagnostics.Trace.WriteLine("** EndConnectCallback exception **" + e.ToString(), "SocketMessageProessor");

                // an exception was raised while connecting to the endpoint
				// fire the event to notify the client programmer
				if(ConnectingException != null)
					ConnectingException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor failed to connect to the specified endpoint. See the inner exception for more information.",e)));
			}
		}


		/// <summary>
		/// Starts an a-synchronous receive.
		/// </summary>
		/// <param name="socket"></param>
		protected virtual void BeginDataReceive(Socket socket)
		{
			// now go retrieve data	
			socketBuffer = new byte[socketBuffer.Length];
			socket.BeginReceive(socketBuffer, 0, socketBuffer.Length, SocketFlags.None, new AsyncCallback(EndReceiveCallback), socket);
		}


		/// <summary>
		/// Fires the Connected event.
		/// </summary>
		protected virtual void OnConnected()
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Connected", "SocketMessageProcessor");
			if(ConnectionEstablished != null)
				ConnectionEstablished(this, new EventArgs());			
		}


		/// <summary>
		/// Fires the Disconnected event.
		/// </summary>
		protected virtual void OnDisconnected()
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Disconnected", "SocketMessageProcessor");
			if(ConnectionClosed != null)
				ConnectionClosed(this, new EventArgs());			
		}

		#endregion

		#region Public
		/// <summary>
		/// Specifies the connection configuration used to set up the socket connection.
		/// By default the basic constructor is called.
		/// </summary>
		public  ConnectivitySettings ConnectivitySettings
		{
			get { return connectivitySettings; }
			set { connectivitySettings = value;}
		}

		/// <summary>
		/// Determines whether the socket is connected
		/// </summary>
		public bool Connected
		{
			get { return socket != null && socket.Connected; }
		}


		/// <summary>
		/// The local end point of the connection
		/// </summary>
		public EndPoint LocalEndPoint
		{
			get 
			{
				return socket.LocalEndPoint;
			}
		}

		/// <summary>
		/// Constructor to instantiate a SocketMessageProcessor object.
		/// </summary>
		public SocketMessageProcessor()
		{
			
		}

		/// <summary>
		/// Holds all messagehandlers for this socket processor
		/// </summary>
		protected ArrayList	MessageHandlers
		{
			get { return messageHandlers; }
			//set { messageHandlers = value;}
		}

		/// <summary>
		/// </summary>
		private ArrayList	messageHandlers	= new ArrayList();

		/// <summary>
		/// Registers a message handler with this processor.
		/// </summary>
		/// <param name="handler"></param>
		public virtual void	RegisterHandler(IMessageHandler handler)
		{
			lock(messageHandlers)
			{
				if(messageHandlers.Contains(handler))
					return;			

				// add the handler
				messageHandlers.Add(handler);
			}
		}

		/// <summary>
		/// Unregisters the message handler from this processor.
		/// </summary>
		/// <param name="handler"></param>
		public virtual void	UnregisterHandler(IMessageHandler handler)
		{
			lock(messageHandlers)
			{
				// remove from the collection
				messageHandlers.Remove(handler);
			}
		}

		/// <summary>
		/// Connect to the endpoint specified in the ConnectivitySettings field.
		/// If the socket is already connected this method will return immediately and leave the current connection open.
		/// </summary>
		public virtual void Connect()
		{
            if (socket != null && socket.Connected)
            {
                if (Settings.TraceSwitch.TraceWarning)
                    System.Diagnostics.Trace.WriteLine("SocketMessageProcess.Connect() called, but already a socket available.", "NS9MessageHandler");
                return;
            }

			try
			{
				// create a socket
                socket = GetPreparedSocket(); //

                IPAddress hostIP = null;
                if (IPAddress.TryParse(ConnectivitySettings.Host, out hostIP))
                {
                    // start connecting				
                    ((ProxySocket)socket).BeginConnect(new System.Net.IPEndPoint(IPAddress.Parse(ConnectivitySettings.Host), ConnectivitySettings.Port), new AsyncCallback(EndConnectCallback), socket);
                }
                else
                {
                    ((ProxySocket)socket).BeginConnect(ConnectivitySettings.Host, ConnectivitySettings.Port, new AsyncCallback(EndConnectCallback), socket);
                }
			}
			catch(Exception e)
			{
				if(Settings.TraceSwitch.TraceVerbose)
					System.Diagnostics.Trace.WriteLine("Connecting exception: " + e.ToString(), "SocketMessageProcessor");

				// an exception was raised while connecting to the endpoint
				// fire the event to notify the client programmer
				if(ConnectingException != null)
					ConnectingException(this, new ExceptionEventArgs(e));

				// re-throw the exception since the exception is thrown while in a blocking call
				throw e;
			}
		}

		/// <summary>
		/// Disconnect the current connection by sending the OUT command and closing the socket. If the connection is already closed
		/// nothing happens (<i>no</i> exception is thrown).
		/// </summary>
		public virtual void Disconnect()
		{						
			// clean up the socket properly
			if(socket != null && socket.Connected)
			{				
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
				socket = null;

				OnDisconnected();
			}
		}


		/// <summary>
		/// The base class does nothing here. Descendant classes should implement this function
		/// by encoding the message in a byte array and send it using the <see cref="MSNPSharp.Core.SocketMessageProcessor.SendSocketData(byte[])"/> method.
		/// </summary>
		/// <param name="message"></param>
		public virtual void SendMessage(NetworkMessage message)
		{
			throw new NotImplementedException("SendMessage() on the base class SocketMessageProcessor is invalid.");
		}

		/// <summary>
		/// Occurs when a connection is established with the remote endpoint.
		/// </summary>
		public event EventHandler	ConnectionEstablished;

		/// <summary>
		/// Occurs when a connection is closed with the remote endpoint.
		/// </summary>
		public event EventHandler	ConnectionClosed;

		/// <summary>
		/// Occurs when an exception was raised while <i>connecting</i> to the endpoint.
		/// </summary>
		public event ProcessorExceptionEventHandler	ConnectingException;

		/// <summary>
		/// Occurs when an exception was raised which caused the open connection to become invalid.
		/// </summary>
		public event ProcessorExceptionEventHandler	ConnectionException;
	
		#endregion
	}
}
