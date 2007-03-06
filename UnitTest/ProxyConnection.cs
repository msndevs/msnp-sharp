using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
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
using Org.Mentalis.Proxy;
using Org.Mentalis.Proxy.Socks;
using Org.Mentalis.Proxy.Socks.Authentication;

namespace MSNPSharp.Test
{
	/// <summary>
	/// Connecting through proxy
	/// </summary>
	[TestFixture]
	public class ProxyConnection : TestBase
	{
		private Proxy _proxyServer;
		private Listener _listener;
		private Listener _httpListener;

		public ProxyConnection()
		{
			// setup proxy server
			_proxyServer = new Proxy("");
			_listener = new Org.Mentalis.Proxy.Socks.SocksListener(1234);
			_httpListener = new Org.Mentalis.Proxy.Http.HttpListener(8080);

			ConnectivitySettings settings = new ConnectivitySettings();
			settings.ProxyType = ProxyType.Socks4;
			settings.ProxyPort = 1234;
			settings.ProxyHost = "127.0.0.1";
			settings.WebProxy  = new System.Net.WebProxy("127.0.0.1",8080);

			Client1.ConnectivitySettings = settings;
			Client2.ConnectivitySettings = settings;
			_listener.Start();
			_httpListener.Start();
			_proxyServer.AddListener(_listener);
		}

		[Test]
		public void Connected()
		{
			Assert.IsTrue(Client1.Connected && Client2.Connected);
		}
	}
}
