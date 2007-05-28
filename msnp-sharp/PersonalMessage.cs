using System;
using System.Xml;
using System.IO;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using MSNPSharp.Core;

namespace MSNPSharp
{
	[Serializable()]
	public class PersonalMessage
	{
		string personalmessage;
		string machineguid;
		string appname;
		string format;
		MediaType mediatype;
		
		[NonSerialized]
		NSMessage message;
		
		string[] content;
		
		public PersonalMessage (string personalmsg, MediaType mediatype, string[] currentmediacontent)
		{
			Message = personalmsg;
			this.mediatype = mediatype;
			this.content = currentmediacontent;
		}
		
		internal PersonalMessage (NSMessage message)
		{
			this.message = message;
			mediatype = MediaType.None;
			Handle ();		
		}
		
		public NSMessage NSMessage
		{
			get {
				return message;
			}
		}
		
		public string Payload
		{
			get {
				string currentmedia = String.Empty;
				
				if (mediatype != MediaType.None)
					currentmedia = System.Web.HttpUtility.HtmlEncode (String.Join (@"\0", content));
				
				personalmessage = System.Web.HttpUtility.HtmlEncode (personalmessage);
				
				string pload = String.Format ("<Data><PSM>{0}</PSM><CurrentMedia>{1}</CurrentMedia></Data>", 
				                              personalmessage, 
				                              currentmedia);
				
				return pload; 
			}
		}
		
		public string Message
		{
			get {
				return personalmessage;
			}
			set {
				personalmessage = value;
			}
		}
		
		public string MachineGuid
		{
			get {
				return machineguid;
			}
		}
		
		public MediaType MediaType
		{
			get {
				return mediatype;
			}
		}
		
		public string AppName
		{
			get {
				return appname;
			}
		}
		
		public string Format
		{
			get {
				return format;
			}
			set {
				format = value;	
			}
		}
		
		//This is used in conjunction with format
		public string[] CurrentMediaContent
		{
			get {
				return content;
			}
			set {
				content = value;	
			}
		}
		
		public string ToDebugString ()
		{
			return System.Text.UTF8Encoding.ASCII.GetString (message.InnerBody);
		}
		
		void Handle ()
		{
			XmlDocument xmlDoc = new XmlDocument();
			TextReader	reader = new StreamReader(new MemoryStream(message.InnerBody), new System.Text.UTF8Encoding(false));
			
			xmlDoc.Load (reader);
			
			XmlNode node = xmlDoc.SelectSingleNode("//Data/PSM");
			
			if (node != null)
			{
				personalmessage = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
			}
			
			node = xmlDoc.SelectSingleNode("//Data/CurrentMedia");
			
			if (node != null)
			{
				string cmedia = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
				
				if (cmedia.Length > 0)
				{
					string[] vals = cmedia.Split (new string[] {@"\0"}, StringSplitOptions.None);
					
					if (vals[0] != "")
						appname = vals[0];
					
					switch (vals[1])
					{
						case "Music":
							mediatype = MediaType.Music;
							break;
						case "Games":
							mediatype = MediaType.Games;
							break;
						case "Office":
							mediatype = MediaType.Office;
							break;
					}
					
					/*
					0 
					1 Music
					2 1
					3 {0} - {1}
					4 Evanescence
					5 Call Me When You're Sober
					6 Album
					7 WMContentID
					8 
					*/
					//vals[2] = Enabled/Disabled
					
					format = vals[3];
					
					int size = vals.Length-4;
					
					content = new String[size];
					
					for (int i=0; i<size; i++)
						content[i] = vals[i+4];
				}
			}
			
			node = xmlDoc.SelectSingleNode("//Data/MachineGuid");
			
			if (node != null)
				machineguid = node.InnerText;
		}
	}
}