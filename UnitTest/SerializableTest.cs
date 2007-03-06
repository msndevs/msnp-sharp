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

namespace MSNPSharp.Test
{
	/// <summary>
	/// Serializes all serializable objects into memory.
	/// </summary>
	[TestFixture]
	public class SerializableTest : TestBase
	{
		public SerializableTest()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		[Test]
		public void SerializeContactlist()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			formatter.Serialize(stream, this.Client1.ContactList);
			stream.Close();
		}

		[Test]
		public void SerializeContactgrouplist()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			formatter.Serialize(stream, this.Client1.ContactGroups);
			stream.Close();
		}

		[Test]
		public void SerializeOwner()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			formatter.Serialize(stream, this.Client1.Owner);
			stream.Close();
		}
	}
}
