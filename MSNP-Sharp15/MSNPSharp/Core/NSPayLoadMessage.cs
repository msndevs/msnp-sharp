using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace MSNPSharp.Core
{
    /// <summary>
    /// NS payload message class, such as ADL and FQY
    /// <para>The format of these mseeages is: COMMAND TRANSID [PARAM1] [PARAM2] .. PAYLOADLENGTH\r\nPAYLOAD</para>
    /// <remarks>
    /// DONOT pass the payload length as command value, the payload length will be calculated automatically
    /// <para>
    /// <list type="bullet">
    /// List of NS payload commands:
    /// <item>
    /// RML
    /// <description>Remove contact </description>
    /// </item>
    /// <item>
    /// ADL
    /// <description>Add users to your contact lists.</description>
    /// </item>
    /// <item>
    /// FQY
    /// <description>Query client's online status </description>
    /// </item>
    /// <item>
    /// QRY
    /// <description>Response to CHL by client </description>
    /// </item>
    /// <item>NOT</item>
    /// <item>UBX</item>
    /// <item>GCF</item>
    /// <item>
    /// UBM
    /// <description>Yahoo messenger message command. You can also send the command as <see cref="YIMMessage"/></description>
    /// </item>
    /// <item>IPG</item>
    /// <item>UUX</item>
    /// <item>MSG</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// </summary>
    [Serializable()]
    public class NSPayLoadMessage:NSMessage
    {
        private string payLoad = string.Empty;

        public string PayLoad
        {
            get { return payLoad; }
            set { payLoad = value; }
        }

        public NSPayLoadMessage()
            : base()
        {

        }

        public NSPayLoadMessage(string command, ArrayList commandValues,string payload)
            : base(command, commandValues)
        {
            payLoad = payload;
        }

        public NSPayLoadMessage(string command, string[] commandValues, string payload)
            : base(command, new ArrayList(commandValues))
        {
            payLoad = payload;
        }

        public NSPayLoadMessage(string command,string payload)
            : base(command)
        {
            payLoad = payload;
        }

        public override byte[] GetBytes()
        {
            StringBuilder cmdBuilder = new StringBuilder();
            cmdBuilder.Append(Command);
            cmdBuilder.Append(' ');
            cmdBuilder.Append(TransactionID.ToString(System.Globalization.CultureInfo.InvariantCulture));

            foreach (string val in CommandValues)
            {
                cmdBuilder.Append(' ');
                cmdBuilder.Append(val);
            }

            cmdBuilder.Append(' ');

            cmdBuilder.Append(System.Text.Encoding.UTF8.GetBytes(payLoad).Length);
            cmdBuilder.Append("\r\n");
            cmdBuilder.Append(payLoad);

            return System.Text.Encoding.UTF8.GetBytes(cmdBuilder.ToString());
        }
    }
}
