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
using System.Collections.Generic;
using System.Threading;
using Org.Mentalis.Network.ProxySocket;
using MSNPSharp;
using MSNPSharp.DataTransfer;

namespace MSNPSharp.Core
{	

	public class SocketMessageProcessor : IMessageProcessor
	{

		ConnectivitySettings connectivitySettings = new ConnectivitySettings();
		byte[] socketBuffer = new byte[1500];
		IPEndPoint proxyEndPoint = null;
		ProxySocket socket = null;
		MessagePool messagePool	= null;
		List<IMessageHandler> messageHandlers = new List<IMessageHandler> ();

		public event EventHandler ConnectionEstablished;
		public event EventHandler ConnectionClosed;
		public event ProcessorExceptionEventHandler ConnectingException;
		public event ProcessorExceptionEventHandler ConnectionException;
		
		public SocketMessageProcessor()
		{
			
		}
		
		protected IPEndPoint ProxyEndPoint
		{
			get { 
				return proxyEndPoint; 
			}
			set { 
				proxyEndPoint = value;
			}
		}

		protected MessagePool MessagePool
		{
			get { 
				return messagePool; 
			}
			set { 
				messagePool = value;
			}
		}

		protected virtual ProxySocket GetPreparedSocket()
		{
			//Creates the Socket for sending data over TCP.
			ProxySocket socket = new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			
			// incorporate the connection settings like proxy's						
			// Note: ProxyType is in MSNPSharp namespace, ProxyTypes in ProxySocket namespace.
			if(ConnectivitySettings.ProxyType != ProxyType.None)
			{
				// set the proxy type
				socket.ProxyType = (ConnectivitySettings.ProxyType == ProxyType.Socks4) 
					? Org.Mentalis.Network.ProxySocket.ProxyTypes.Socks4 
					: Org.Mentalis.Network.ProxySocket.ProxyTypes.Socks5;
				
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
			//Send operations will timeout of confirmation is not received within 3000 milliseconds.
			socket.SendTimeout = 3000;
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, new LingerOption (true, 0));
			
			return socket;
		}

		protected virtual void EndSendCallback(IAsyncResult ar)
		{
			ProxySocket socket = (ProxySocket)ar.AsyncState;
			socket.EndSend(ar);
		}

		protected void SendSocketData(byte[] data)
		{
			/*if (socket == null || !socket.Connected)
			{
				// the connection is closed
				OnDisconnected();
				return;
			}*/
			
			SendSocketData(socket, data);
		}

		protected static void SendSocketData(Socket psocket, byte[] data)
		{			
			try
			{
				lock(psocket)
				{
					psocket.Send (data);
				}
			}
			catch(Exception e)
			{
				throw new MSNPSharpException("Error while sending network message. See the inner exception for more details.", e);
			}
		}

		protected virtual void OnMessageReceived(byte[] data)
		{
			// do nothing since this is a base class
		}		

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

		protected virtual void BeginDataReceive(Socket socket)
		{
			// now go retrieve data	
			socketBuffer = new byte[socketBuffer.Length];
			socket.BeginReceive(socketBuffer, 0, socketBuffer.Length, SocketFlags.None, new AsyncCallback(EndReceiveCallback), socket);
		}

		protected virtual void OnConnected()
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Connected", "SocketMessageProcessor");
			if(ConnectionEstablished != null)
				ConnectionEstablished(this, new EventArgs());			
		}

		protected virtual void OnDisconnected()
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Disconnected", "SocketMessageProcessor");
			if(ConnectionClosed != null)
				ConnectionClosed(this, new EventArgs());			
		}

		public  ConnectivitySettings ConnectivitySettings
		{
			get { 
				return connectivitySettings; 
			}
			set { 
				connectivitySettings = value;
			}
		}

		public bool Connected
		{
			get { return socket != null && socket.Connected; }
		}


		public EndPoint LocalEndPoint
		{
			get 
			{
				return socket.LocalEndPoint;
			}
		}

		protected List<IMessageHandler> MessageHandlers
		{
			get { 
				return messageHandlers; 
			}
		}

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

		public virtual void	UnregisterHandler(IMessageHandler handler)
		{
			lock(messageHandlers)
			{
				// remove from the collection
				messageHandlers.Remove(handler);
			}
		}

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

		public virtual void SendMessage(NetworkMessage message)
		{
			throw new NotImplementedException("SendMessage() on the base class SocketMessageProcessor is invalid.");
		}
	}
}
