<table width='100%'>
<tr>
<td>
<i>By Pang Wu (freezingsoft@hotmail.com), Ethem Evlice</i> <br />
<i>Thanks for the help from Scott Werndorfer</i> <br />
<i>Last Update: July/21/2010</i>
<br />
<br />
<img src='http://msnp-sharp.googlecode.com/svn/wiki/images/MSNPSharp_banner.png' />
</td>
</tr>
</table>
<table cellpadding='5' width='100%' cellspacing='0px'>
<tr>
<td>
<h1>Introduction</h1>

Here're some result of the uncompleted research on binary header that WLM 2009 used for its p2p protocol.<br>
<br>
<br>
<h1>Details (Only for switchboard data transfer)</h1>

Every field is big-endian.<br>
<br>
Transfer layer package<br>
<br>
<table><thead><th> <b>Length</b> </th><th> <b>Description</b> </th></thead><tbody>
<tr><td> 1 </td><td> Header length </td></tr>
<tr><td> 1 </td><td> Operation code, indicates the session state. </td></tr>
<tr><td> 2 </td><td> Payload data length </td></tr>
<tr><td> 4 </td><td> Sequence Number(Last sequence number add payload data length) </td></tr>
<tr><td> Header length - 8 </td><td> TLVs </td></tr>
<tr><td> Payload data length </td><td> Payload data (Data package) </td></tr>
<tr><td> 4 </td><td> Footer </td></tr></tbody></table>

The Data package consists following fields:<br>
<br>
<table><thead><th> <b>Length</b> </th><th> <b>Description</b> </th></thead><tbody>
<tr><td> 1 </td><td> Header length </td></tr>
<tr><td> 1 </td><td> TF combination (Consists of 7-bit Type and 1-bit Flag)</td></tr>
<tr><td> 2 </td><td> Package number </td></tr>
<tr><td> 4 </td><td> Session ID </td></tr>
<tr><td> Header length - 8 </td><td> TLVs </td></tr>
<tr><td> Payload data length - Header length </td><td> Payload </td></tr></tbody></table>

Consider the following examples:<br>
<br>
Example 1:<br>
<br>
Data from a displayimage transfer process (An acknowledgement message, the MSG text header is omitted).<br>
<br>
<br>
<table><thead><th> <b>Binary</b> </th><th> <b>ASCII</b> </th></thead><tbody>
<tr><td> 08 00 00 0c df 32 36  79 08 01 00 00 6c 99 fb c2 00 00 00 00 00 00 00 00 </td><td> fb ......26 y....l.. </td></tr></tbody></table>

For the Transfer Layer Package:<br>
<br>
<ul><li>The size of header is 0x08(8).<br>
</li><li>The operation code is 0x00(0).<br>
</li><li>Payload length is 0x000c(12).<br>
</li><li>8 - 8 = 0 so no TLVs in this sample package.<br>
</li><li>The sequence number is 0xdf323679(3744609913).<br>
</li><li>Footer 0x0000(0).</li></ul>


<blockquote>Then the following data is playload (data package):<br>
08 01 00 00 6c 99 fb c2 00 00 00 00</blockquote>

<ul><li>The size of header is 0x08(8).<br>
</li><li>The TF combination is 0x01(1).<br>
</li><li>Package number is 0x0000(0).<br>
</li><li>Session ID is 0x6c99fbc2.<br>
</li><li>8 - 8 = 0 so no TLVs in this sample package.<br>
</li><li>The payload is 0x0000(0), so this example package is a data preperation message.</li></ul>


Example 2:<br>
<br>
Here's a MSNSLP INVITE message that initialize the switchboard transfer negotiation (Including the MSG header):<br>
<br>
<br>
<pre><code>0000   4d 53 47 20 35 39 31 20 44 20 39 36 34 0d 0a 4d  MSG 591 D 964..M<br>
0010   49 4d 45 2d 56 65 72 73 69 6f 6e 3a 20 31 2e 30  IME-Version: 1.0<br>
0020   0d 0a 43 6f 6e 74 65 6e 74 2d 54 79 70 65 3a 20  ..Content-Type: <br>
0030   61 70 70 6c 69 63 61 74 69 6f 6e 2f 78 2d 6d 73  application/x-ms<br>
0040   6e 6d 73 67 72 70 32 70 0d 0a 50 32 50 2d 44 65  nmsgrp2p..P2P-De<br>
0050   73 74 3a 20 77 70 30 31 40 6c 69 76 65 2e 63 6e  st: wp01@live.cn<br>
0060   3b 7b 64 39 66 64 31 36 31 62 2d 64 34 64 38 2d  ;{d9fd161b-d4d8-<br>
0070   34 63 61 37 2d 38 61 36 38 2d 63 37 30 66 65 62  4ca7-8a68-c70feb<br>
0080   33 39 61 33 33 30 7d 0d 0a 50 32 50 2d 53 72 63  39a330}..P2P-Src<br>
0090   3a 20 66 72 65 65 7a 69 6e 67 73 6f 66 74 40 68  : freezingsoft@h<br>
00a0   6f 74 6d 61 69 6c 2e 63 6f 6d 3b 7b 37 65 64 66  otmail.com;{7edf<br>
00b0   39 64 32 34 2d 37 65 39 38 2d 34 37 32 30 2d 39  9d24-7e98-4720-9<br>
00c0   66 30 35 2d 37 34 63 33 61 62 61 61 30 37 37 30  f05-74c3abaa0770<br>
00d0   7d 0d 0a 0d 0a 18 03 02 e2 6a 23 43 ff 01 0c 00  }........j#C....<br>
00e0   02 00 00 00 0e 00 00 0f 01 00 00 00 00 08 01 00  ................<br>
00f0   00 00 00 00 00 49 4e 56 49 54 45 20 4d 53 4e 4d  .....INVITE MSNM<br>
0100   53 47 52 3a 77 70 30 31 40 6c 69 76 65 2e 63 6e  SGR:wp01@live.cn<br>
0110   3b 7b 64 39 66 64 31 36 31 62 2d 64 34 64 38 2d  ;{d9fd161b-d4d8-<br>
0120   34 63 61 37 2d 38 61 36 38 2d 63 37 30 66 65 62  4ca7-8a68-c70feb<br>
0130   33 39 61 33 33 30 7d 20 4d 53 4e 53 4c 50 2f 31  39a330} MSNSLP/1<br>
0140   2e 30 0d 0a 54 6f 3a 20 3c 6d 73 6e 6d 73 67 72  .0..To: &lt;msnmsgr<br>
0150   3a 77 70 30 31 40 6c 69 76 65 2e 63 6e 3b 7b 64  :wp01@live.cn;{d<br>
0160   39 66 64 31 36 31 62 2d 64 34 64 38 2d 34 63 61  9fd161b-d4d8-4ca<br>
0170   37 2d 38 61 36 38 2d 63 37 30 66 65 62 33 39 61  7-8a68-c70feb39a<br>
0180   33 33 30 7d 3e 0d 0a 46 72 6f 6d 3a 20 3c 6d 73  330}&gt;..From: &lt;ms<br>
0190   6e 6d 73 67 72 3a 66 72 65 65 7a 69 6e 67 73 6f  nmsgr:freezingso<br>
01a0   66 74 40 68 6f 74 6d 61 69 6c 2e 63 6f 6d 3b 7b  ft@hotmail.com;{<br>
01b0   37 65 64 66 39 64 32 34 2d 37 65 39 38 2d 34 37  7edf9d24-7e98-47<br>
01c0   32 30 2d 39 66 30 35 2d 37 34 63 33 61 62 61 61  20-9f05-74c3abaa<br>
01d0   30 37 37 30 7d 3e 0d 0a 56 69 61 3a 20 4d 53 4e  0770}&gt;..Via: MSN<br>
01e0   53 4c 50 2f 31 2e 30 2f 54 4c 50 20 3b 62 72 61  SLP/1.0/TLP ;bra<br>
01f0   6e 63 68 3d 7b 37 42 41 44 41 35 35 39 2d 39 31  nch={7BADA559-91<br>
0200   45 32 2d 34 41 46 45 2d 41 35 32 39 2d 39 43 46  E2-4AFE-A529-9CF<br>
0210   34 41 39 36 34 37 46 44 35 7d 0d 0a 43 53 65 71  4A9647FD5}..CSeq<br>
0220   3a 20 30 20 0d 0a 43 61 6c 6c 2d 49 44 3a 20 7b  : 0 ..Call-ID: {<br>
0230   42 30 42 34 31 41 30 41 2d 35 32 35 32 2d 34 42  B0B41A0A-5252-4B<br>
0240   43 44 2d 42 46 38 31 2d 30 39 36 37 45 39 44 31  CD-BF81-0967E9D1<br>
0250   38 36 34 34 7d 0d 0a 4d 61 78 2d 46 6f 72 77 61  8644}..Max-Forwa<br>
0260   72 64 73 3a 20 30 0d 0a 43 6f 6e 74 65 6e 74 2d  rds: 0..Content-<br>
0270   54 79 70 65 3a 20 61 70 70 6c 69 63 61 74 69 6f  Type: applicatio<br>
0280   6e 2f 78 2d 6d 73 6e 6d 73 67 72 2d 73 65 73 73  n/x-msnmsgr-sess<br>
0290   69 6f 6e 72 65 71 62 6f 64 79 0d 0a 43 6f 6e 74  ionreqbody..Cont<br>
02a0   65 6e 74 2d 4c 65 6e 67 74 68 3a 20 32 38 34 0d  ent-Length: 284.<br>
02b0   0a 0d 0a 45 55 46 2d 47 55 49 44 3a 20 7b 41 34  ...EUF-GUID: {A4<br>
02c0   32 36 38 45 45 43 2d 46 45 43 35 2d 34 39 45 35  268EEC-FEC5-49E5<br>
02d0   2d 39 35 43 33 2d 46 31 32 36 36 39 36 42 44 42  -95C3-F126696BDB<br>
02e0   46 36 7d 0d 0a 53 65 73 73 69 6f 6e 49 44 3a 20  F6}..SessionID: <br>
02f0   32 36 32 35 33 32 39 32 34 37 0d 0a 41 70 70 49  2625329247..AppI<br>
0300   44 3a 20 31 32 0d 0a 52 65 71 75 65 73 74 46 6c  D: 12..RequestFl<br>
0310   61 67 73 3a 20 31 38 0d 0a 43 6f 6e 74 65 78 74  ags: 18..Context<br>
0320   3a 20 50 47 31 7a 62 6d 39 69 61 69 42 44 63 6d  : PG1zbm9iaiBDcm<br>
0330   56 68 64 47 39 79 50 53 4a 33 63 44 41 78 51 47  VhdG9yPSJ3cDAxQG<br>
0340   78 70 64 6d 55 75 59 32 34 69 49 46 52 35 63 47  xpdmUuY24iIFR5cG<br>
0350   55 39 49 6a 4d 69 49 46 4e 49 51 54 46 45 50 53  U9IjMiIFNIQTFEPS<br>
0360   4a 5a 54 69 74 72 4e 48 70 4c 4b 30 6c 6d 52 30  JZTitrNHpLK0lmR0<br>
0370   64 30 52 31 68 79 54 48 49 78 64 56 4e 61 64 56  d0R1hyTHIxdVNadV<br>
0380   6c 5a 52 47 73 39 49 69 42 54 61 58 70 6c 50 53  lZRGs9IiBTaXplPS<br>
0390   49 78 4e 7a 41 35 4e 53 49 67 54 47 39 6a 59 58  IxNzA5NSIgTG9jYX<br>
03a0   52 70 62 32 34 39 49 6a 41 69 49 45 5a 79 61 57  Rpb249IjAiIEZyaW<br>
03b0   56 75 5a 47 78 35 50 53 4a 4e 55 55 46 42 51 55  VuZGx5PSJNUUFBQU<br>
03c0   45 39 50 53 49 76 50 67 41 3d 0d 0a 0d 0a 00 00  E9PSIvPgA=......<br>
03d0   00 00 00                                         ...<br>
</code></pre>


For the Transfer Layer Package:<br>
<br>
<ul><li>The size of header is 0x18(24).<br>
</li><li>The operation code is 0x03(3).<br>
</li><li>Payload length is 0x02e2(738).<br>
</li><li>24 - 8 = 16 there's 16 bytes TLVs in this sample package.<br>
</li><li>The sequence number is 0x6a2343ff (1780696063).<br>
</li><li>Footer 0x0000(0).</li></ul>

<ul><li>There're one TLV in this sample:<br>
<ol><li>T=0x01, L=0x0c, v={0x00 0x02 0x00 0x00 0x00 0x0e 0x00 0x00 0x0f 0x01 0x00 0x00}<br>
</li><li>The four bytes left is padding bytes (For more details, please see the below sections).</li></ol></li></ul>


<blockquote>Then the following data is playload (data package):</blockquote>

<ul><li>The size of header is 0x08(8).<br>
</li><li>The TF combination is 0x01(1).<br>
</li><li>Package number is 0x0000(0).<br>
</li><li>Session ID is 0x00000000(0).<br>
</li><li>8 - 8 = 0 so no TLVs in this sample package.<br>
</li><li>The payload contains a SLP test message.</li></ul>


About operation code:<br>
<br>
Operation code also called "flag" in the WLM. The code should be check using bitwise and.<br>
<table><thead><th> <b>Operation Code</b> </th><th> <b>Description</b> </th></thead><tbody>
<tr><td> 0x00(0) </td><td> None, nothing required. </td></tr>
<tr><td> 0x01(1) </td><td> SYN. Just the same like TCP SYN. </td></tr>
<tr><td> 0x02(2) </td><td> RAK, "Request for ACK". If a packet's operation code contains RAK, it must be acknowledged. </td></tr></tbody></table>

<table width='100%'>
<tr>
<td>
For example, a packet's operation code is 0x03, that is SYN | RAK.<br>
Then you need to response this packet with a packet containing an<br />ACK TLV (T=0x02, L=0x04, V=ACK Sequence number).<br>
<br>
A transfer initator should add RAK to the operation code every 9672 ms.<br>
</td>
</tr>
</table>

About TF combination:<br>
<table><thead><th> <b>Value of TF Combination</b> </th><th> <b>Description</b> </th></thead><tbody>
<tr><td> 0x01(1, T=0 F=1) </td><td> If the SessionID is 0x0000, the payload contains SIP text message.<br />If the SessionID is none zero, the payload is data preperation message. </td></tr>
<tr><td> 0x04(4, T=2 F=0) </td><td> The payload contains binary data for MSNObjet. </td></tr>
<tr><td> 0x05(5, T=2 F=1) </td><td> The payload contains the first package of binary data for MSNObject. </td></tr>
<tr><td> 0x06(6, T=3 F=0) </td><td> The payload contains binary data for file transfer. </td></tr>
<tr><td> 0x07(7, T=3 F=1) </td><td> The payload contains the first package of binary data for file transfer. </td></tr></tbody></table>

About TLVs:<br>
<table><thead><th> <b>Value of Type (T)</b> </th><th> <b>Value of Value (V)</b> </th><th> <b>Description</b> </th></thead><tbody>
<tr><td> 0x1(1) </td><td> 0x8(8) </td><td> Indicates that value is the size of untransfer data. </td></tr>
<tr><td> 0x1(1) </td><td> 0xc(12) </td><td> Client peer info TLV (Please see below). </td></tr>
<tr><td> 0x2(2) </td><td> 0x4(4) </td><td> ACK's sequence number. </td></tr>
<tr><td> 0x3(3) </td><td> 0x4(4) </td><td> NAK's sequence number. </td></tr></tbody></table>

<table width='100%'>
<tr>
<td>

The "Peer Info TLV": Peer Info TLV also called "Hello TLV" since it's always exchanged with SYN operation (OperationCode |= SYN). It usually sends with a SIP INVITE message by the initiator, then receiver should reply it by copying sender's Peer Info TLV to the reponding message. The Operation Code of packet containing Peer Info TLV should add SYN (SYN = 1) flag. Each transfer layer should only exchange Peer Info TLV once.<br>
<br>
Values in "Peer Info TLV" are little endian.<br>
<br>
</td>
</tr>
</table>

<table><thead><th> <b>Data Length</b> </th><th> <b>Desciption</b> </th></thead><tbody>
<tr><td> 2 </td><td> Protocol Version </td></tr>
<tr><td> 2 </td><td> Implementation ID </td></tr>
<tr><td> 2 </td><td> Version </td></tr>
<tr><td> 2 </td><td> Reserved (I think it's a random number) </td></tr>
<tr><td> 4 </td><td> Capabilities </td></tr></tbody></table>

Consider the following example (The TL field 0x01 0x0c of this TLV has been omitted):<br>
<br>
00 02 00 00 00 0e 76 2d 0f 01 00 00<br>
<br>
<ul><li>Bytes 0-1: Protocol version 0x0200(512)<br>
</li><li>Bytes 2-3: Implementation ID 0x0000(0)<br>
</li><li>Bytes 4-5: Version 0x0e00(3584)<br>
</li><li>Bytes 6-7: Unknown, 0x2d76, reserved bytes.<br>
</li><li>Bytes 8-11: Capabilities 0x0000010f(271).</li></ul>

<blockquote>A sample tranfer session (Custom emoticon):<br>
<pre><code> Receiver                                                        Sender<br>
    |---- Send "INVITE MSNMSGR" message with operation code 0x3 ---&gt;| [1]<br>
    |&lt;----------- Acknowledge to the invitation message ------------| [2]<br>
    |------------- Acknowledge to last message  -------------------&gt;|<br>
    |&lt;-------------- Send "MSNSLP/1.0 200 OK" message --------------|<br>
    |--------- Acknowledge the Initialize session message  --------&gt;|<br>
    |&lt;------------------- Data preparation message  ----------------| [3]<br>
    |&lt;-------------- Send the first data package -------------------|<br>
    |&lt;--------------- Send the Nst data package  -------------------|<br>
                              ... ...<br>
    |&lt;----------------- Send the last data package -----------------|<br>
    |&lt;----- Send "BYE MSNMSGR:[receiver_mail:guid]" message --------|<br>
    |-- Acknowledge to "BYE MSNMSGR:[receiver_mail:guid]" messag --&gt;|<br>
    |------ Send "BYE MSNMSGR:[sender_mail:guid]" message ---------&gt;|<br>
    |--- Acknowledge to "BYE MSNMSGR:[sender_mail:guid]" messag ---&gt;|<br>
</code></pre></blockquote>

<table width='100%'>
<tr>
<td>

<blockquote><code>[1]</code> The operation code 0x3 means SYN | RAK, however, this is not always the case. If you already send SYN in a switchboard connection, SYN is not needed. The RAK is added to a message's operation code after a certain period of time.</blockquote>

<blockquote><code>[2]</code> If the operation code of INVITE message not include RAK (operation_code | RAK == 0), this reply step should be omitted (Since the message is not required to ACK). If the operation code of the INVITE message include RAK, the operation code of response message (The ACK message) should also include RAK (operation_code |= RAK). However, if an ACK message's operation code includes RAK, you also need to ACK this ACK message, but DON'T add RAK to the operation code of the response message.</blockquote>

<blockquote>If the operation code of the INVITE message include SYN, the operation code of response message should also include SYN. But after that, no message's operation code in this switchboard connection should have SYN.</blockquote>

<blockquote><code>[3]</code> Only MSNObject transfer session send this message. File transfer does not need data preparation message.</blockquote>

</td>
</tr>
</table>



<h2>Key issues and FAQs (For switchboard data transfer)</h2>

<b>Q:</b>

<table width='100%'>
<tr>
<td>
Why can't my program receive an ""INVITE MSNMSGR" SLP message from the official client after the remote user established a switchboard conversation?<br>
</td>
</tr>
</table>
<b>A:</b>

<table width='100%'>
<tr>
<td>
You need to register the client capacities and endpoint info of your program to switchboard by sending 3 UUX commands (Send it after you send the ADL command(s)).<br>
</td>
</tr>
</table>

The pattern of these UUX commands is like this:<br>
<br>
<pre><code><br>
UUX [TransferID] [Payload length]\r\n<br>
&lt;EndpointData&gt;<br>
 &lt;Capabilities&gt;[Client capacities]:[New p2pv2 capacities]&lt;/Capabilities<br>
&lt;/EndpointData&gt;<br>
<br>
UUX [TransferID] [Payload length]\r\n<br>
&lt;Data&gt;<br>
 &lt;PSM&gt;[Personal message]&lt;/PSM&gt;<br>
 &lt;CurrentMedia&gt;&lt;/CurrentMedia&gt;<br>
 &lt;MachineGuid&gt;[XML encoded machine guid]&lt;/MachineGuid&gt;<br>
 &lt;SignatureSound&gt;&lt;/SignatureSound&gt;<br>
&lt;/Data&gt;<br>
<br>
UUX [TransferID] [Payload length]\r\n<br>
&lt;PrivateEndpointData&gt;<br>
 &lt;EpName&gt;[Your computer name]&lt;/EpName&gt;<br>
 &lt;Idle&gt;false&lt;/Idle&gt;<br>
 &lt;ClientType&gt;1&lt;/ClientType&gt;<br>
 &lt;State&gt;[Your online state]&lt;/State&gt;<br>
&lt;/PrivateEndpointData&gt;<br>
<br>
</code></pre>

An actual data example:<br>
<br>
<pre><code><br>
UUX 12 71\r\n<br>
&lt;EndpointData&gt;&lt;Capabilities&gt;2751496224:48&lt;/Capabilities&gt;&lt;/EndpointData&gt;<br>
<br>
UUX 13 124\r\n<br>
&lt;PrivateEndpointData&gt;<br>
 &lt;EpName&gt;WP&lt;/EpName&gt;&lt;Idle&gt;false&lt;/Idle&gt;<br>
 &lt;ClientType&gt;1&lt;/ClientType&gt;&lt;State&gt;NLN&lt;/State&gt;<br>
&lt;/PrivateEndpointData&gt;<br>
<br>
UUX 14 166\r\n<br>
&lt;Data&gt;&lt;PSM&gt;~&lt;/PSM&gt;&lt;MachineGuid&gt;&amp;#x7B;338c6533-882f-4980-aa34-62f50344526c&amp;#x7D;&lt;/MachineGuid&gt;&lt;/Data&gt;<br>
<br>
</code></pre>



<b>Q:</b>

When and how to send an acknowledgement message?<br>
<br>
<b>A:</b>
<table width='100%'>
<tr>
<td>

You need to check the operation code by using (operation_code & 2). The value 2 means "RAK (Require ACK)" here (Please refer to the "operation code" part of this artical). If the expression (operation_code & 2) returns true, you need to reply with an acknowledgement message. An acknowledgement message is a message with ACK TLV (T=0x02, L=0x04, V=Ack sequence number).<br>
<br>
If the message needs to be ACK is already an acknowlegment (i.e. Containing an ACK TLV). You need to set the operation code of your replying message into zero (Zero means nothing required to reply) since you cannot require remote client to ACK an ACK message.<br>
<br>
If the operation code equals RAK (OperationCode == 2) and the payload size of this message is zero (usually you receive this message because the remote client wants you to transmit an ACK, this is called ACK request message), the operation code of replying message also need to be zero since you are replying for a pure RAK message.<br>
<br>
Otherwise, just copy the operation code of incomeing message to your reply message and attach necessary TLVs. Usually you need to do this after receiving a message with operation code equals 3 (SYN | RAK). If you reply a message with SYN, you need to attach the "Peer info TLV" (T=0x01, L=0x0c).<br>
<br>
<br>
For example, you need to acknowledge a MSNSLP invite message as follow:<br>
</td>
</tr>
</table>
<pre><code><br>
0000   4d 53 47 20 66 72 65 65 7a 69 6e 67 73 6f 66 74  MSG freezingsoft<br>
0010   40 68 6f 74 6d 61 69 6c 2e 63 6f 6d 20 50 61 6e  @hotmail.com Pan<br>
0020   67 28 62 72 62 29 20 31 30 35 31 0d 0a 4d 49 4d  g(brb) 1051..MIM<br>
0030   45 2d 56 65 72 73 69 6f 6e 3a 20 31 2e 30 0d 0a  E-Version: 1.0..<br>
0040   43 6f 6e 74 65 6e 74 2d 54 79 70 65 3a 20 61 70  Content-Type: ap<br>
0050   70 6c 69 63 61 74 69 6f 6e 2f 78 2d 6d 73 6e 6d  plication/x-msnm<br>
0060   73 67 72 70 32 70 0d 0a 50 32 50 2d 44 65 73 74  sgrp2p..P2P-Dest<br>
0070   3a 20 74 65 73 74 6d 73 6e 70 73 68 61 72 70 40  : testmsnpsharp@<br>
0080   6c 69 76 65 2e 63 6e 3b 7b 37 65 64 66 39 64 32  live.cn;{7edf9d2<br>
0090   34 2d 37 65 39 38 2d 34 37 32 30 2d 39 66 30 35  4-7e98-4720-9f05<br>
00a0   2d 37 34 63 33 61 62 61 61 30 37 37 30 7d 0d 0a  -74c3abaa0770}..<br>
00b0   50 32 50 2d 53 72 63 3a 20 66 72 65 65 7a 69 6e  P2P-Src: freezin<br>
00c0   67 73 6f 66 74 40 68 6f 74 6d 61 69 6c 2e 63 6f  gsoft@hotmail.co<br>
00d0   6d 3b 7b 64 39 66 64 31 36 31 62 2d 64 34 64 38  m;{d9fd161b-d4d8<br>
00e0   2d 34 63 61 37 2d 38 61 36 38 2d 63 37 30 66 65  -4ca7-8a68-c70fe<br>
00f0   62 33 39 61 33 33 30 7d 0d 0a 0d 0a 18 03 03 30  b39a330}.......0<br>
0100   d3 b7 91 9f 01 0c 00 02 00 00 00 0e 00 00 0f 01  ................<br>
0110   00 00 00 00 08 01 00 00 00 00 00 00 49 4e 56 49  ............INVI<br>
0120   54 45 20 4d 53 4e 4d 53 47 52 3a 74 65 73 74 6d  TE MSNMSGR:testm<br>
0130   73 6e 70 73 68 61 72 70 40 6c 69 76 65 2e 63 6e  snpsharp@live.cn<br>
0140   3b 7b 37 65 64 66 39 64 32 34 2d 37 65 39 38 2d  ;{7edf9d24-7e98-<br>
0150   34 37 32 30 2d 39 66 30 35 2d 37 34 63 33 61 62  4720-9f05-74c3ab<br>
0160   61 61 30 37 37 30 7d 20 4d 53 4e 53 4c 50 2f 31  aa0770} MSNSLP/1<br>
<br>
....<br>
<br>
</code></pre>
<table width='100%'>
<tr>
<td>
The outgoing acknowledge message should be like this:<br>
(The package also has a TLV that T=0x01, L=0x0c (Peer info TLV) and the value of V field is not important, you can just copy the value from the INVITE message.)<br>
</td>
</tr>
</table>
<pre><code><br>
0000   4d 53 47 20 34 20 44 20 32 33 39 0d 0a 4d 49 4d  MSG 4 D 239..MIM<br>
0010   45 2d 56 65 72 73 69 6f 6e 3a 20 31 2e 30 0d 0a  E-Version: 1.0..<br>
0020   43 6f 6e 74 65 6e 74 2d 54 79 70 65 3a 20 61 70  Content-Type: ap<br>
0030   70 6c 69 63 61 74 69 6f 6e 2f 78 2d 6d 73 6e 6d  plication/x-msnm<br>
0040   73 67 72 70 32 70 0d 0a 50 32 50 2d 44 65 73 74  sgrp2p..P2P-Dest<br>
0050   3a 20 66 72 65 65 7a 69 6e 67 73 6f 66 74 40 68  : freezingsoft@h<br>
0060   6f 74 6d 61 69 6c 2e 63 6f 6d 3b 7b 64 39 66 64  otmail.com;{d9fd<br>
0070   31 36 31 62 2d 64 34 64 38 2d 34 63 61 37 2d 38  161b-d4d8-4ca7-8<br>
0080   61 36 38 2d 63 37 30 66 65 62 33 39 61 33 33 30  a68-c70feb39a330<br>
0090   7d 0d 0a 50 32 50 2d 53 72 63 3a 20 74 65 73 74  }..P2P-Src: test<br>
00a0   6d 73 6e 70 73 68 61 72 70 40 6c 69 76 65 2e 63  msnpsharp@live.c<br>
00b0   6e 3b 7b 37 65 64 66 39 64 32 34 2d 37 65 39 38  n;{7edf9d24-7e98<br>
00c0   2d 34 37 32 30 2d 39 66 30 35 2d 37 34 63 33 61  -4720-9f05-74c3a<br>
00d0   62 61 61 30 37 37 30 7d 0d 0a 0d 0a 1c 03 00 00  baa0770}........<br>
00e0   26 3e fe 3e 02 04 d3 b7 94 cf 01 0c 00 02 00 00  &amp;&gt;.&gt;............<br>
00f0   00 0e 61 6e 0f 01 00 00 00 00 00 00              ..an........<br>
<br>
</code></pre>


If the message you want to acknowledge is an initial session message, the operation code of outgoing message should be set to 0.<br>
<br>
For example, initial session message:<br>
<br>
<pre><code><br>
0000   4d 53 47 20 77 70 30 31 40 6c 69 76 65 2e 63 6e  MSG wp01@live.cn<br>
0010   20 e6 bb 82 20 32 31 30 0d 0a 4d 49 4d 45 2d 56   ... 210..MIME-V<br>
0020   65 72 73 69 6f 6e 3a 20 31 2e 30 0d 0a 43 6f 6e  ersion: 1.0..Con<br>
0030   74 65 6e 74 2d 54 79 70 65 3a 20 61 70 70 6c 69  tent-Type: appli<br>
0040   63 61 74 69 6f 6e 2f 78 2d 6d 73 6e 6d 73 67 72  cation/x-msnmsgr<br>
0050   70 32 70 0d 0a 50 32 50 2d 44 65 73 74 3a 20 66  p2p..P2P-Dest: f<br>
0060   72 65 65 7a 69 6e 67 73 6f 66 74 40 68 6f 74 6d  reezingsoft@hotm<br>
0070   61 69 6c 2e 63 6f 6d 3b 7b 37 65 64 66 39 64 32  ail.com;{7edf9d2<br>
0080   34 2d 37 65 39 38 2d 34 37 32 30 2d 39 66 30 35  4-7e98-4720-9f05<br>
0090   2d 37 34 63 33 61 62 61 61 30 37 37 30 7d 0d 0a  -74c3abaa0770}..<br>
00a0   50 32 50 2d 53 72 63 3a 20 77 70 30 31 40 6c 69  P2P-Src: wp01@li<br>
00b0   76 65 2e 63 6e 3b 7b 64 39 66 64 31 36 31 62 2d  ve.cn;{d9fd161b-<br>
00c0   64 34 64 38 2d 34 63 61 37 2d 38 61 36 38 2d 63  d4d8-4ca7-8a68-c<br>
00d0   37 30 66 65 62 33 39 61 33 33 30 7d 0d 0a 0d 0a  70feb39a330}....<br>
00e0   08 02 00 00 5b c8 00 f7 00 00 00 00              ....[.......<br>
<br>
</code></pre>

Outgoing message:<br>
<br>
<pre><code><br>
0000   4d 53 47 20 35 39 33 20 44 20 32 31 38 0d 0a 4d  MSG 593 D 218..M<br>
0010   49 4d 45 2d 56 65 72 73 69 6f 6e 3a 20 31 2e 30  IME-Version: 1.0<br>
0020   0d 0a 43 6f 6e 74 65 6e 74 2d 54 79 70 65 3a 20  ..Content-Type: <br>
0030   61 70 70 6c 69 63 61 74 69 6f 6e 2f 78 2d 6d 73  application/x-ms<br>
0040   6e 6d 73 67 72 70 32 70 0d 0a 50 32 50 2d 44 65  nmsgrp2p..P2P-De<br>
0050   73 74 3a 20 77 70 30 31 40 6c 69 76 65 2e 63 6e  st: wp01@live.cn<br>
0060   3b 7b 64 39 66 64 31 36 31 62 2d 64 34 64 38 2d  ;{d9fd161b-d4d8-<br>
0070   34 63 61 37 2d 38 61 36 38 2d 63 37 30 66 65 62  4ca7-8a68-c70feb<br>
0080   33 39 61 33 33 30 7d 0d 0a 50 32 50 2d 53 72 63  39a330}..P2P-Src<br>
0090   3a 20 66 72 65 65 7a 69 6e 67 73 6f 66 74 40 68  : freezingsoft@h<br>
00a0   6f 74 6d 61 69 6c 2e 63 6f 6d 3b 7b 37 65 64 66  otmail.com;{7edf<br>
00b0   39 64 32 34 2d 37 65 39 38 2d 34 37 32 30 2d 39  9d24-7e98-4720-9<br>
00c0   66 30 35 2d 37 34 63 33 61 62 61 61 30 37 37 30  f05-74c3abaa0770<br>
00d0   7d 0d 0a 0d 0a 10 00 00 00 6a 23 46 e1 02 04 5b  }........j#F...[<br>
00e0   c8 00 f7 00 00 00 00 00 00                       .........<br>
<br>
</code></pre>

<table width='100%'>
<tr>
<td>

At last, if the message you want to acknowledge is an acknowledgement of an initial session message, set the operation code to 0 and ignore all the TLVs in the header of transfer layer package.<br>
<br>
For example, to acknowledge the following message:<br>
(This example package is an acknowledge message. It contains one Peer info TLV in its header:<br>
01 0c 00 02 00 00 00 0e 61 6e 0f 01 00 00)<br>
</td>
</tr>
</table>

<pre><code><br>
0000   4d 53 47 20 34 20 44 20 32 33 39 0d 0a 4d 49 4d  MSG 4 D 239..MIM<br>
0010   45 2d 56 65 72 73 69 6f 6e 3a 20 31 2e 30 0d 0a  E-Version: 1.0..<br>
0020   43 6f 6e 74 65 6e 74 2d 54 79 70 65 3a 20 61 70  Content-Type: ap<br>
0030   70 6c 69 63 61 74 69 6f 6e 2f 78 2d 6d 73 6e 6d  plication/x-msnm<br>
0040   73 67 72 70 32 70 0d 0a 50 32 50 2d 44 65 73 74  sgrp2p..P2P-Dest<br>
0050   3a 20 66 72 65 65 7a 69 6e 67 73 6f 66 74 40 68  : freezingsoft@h<br>
0060   6f 74 6d 61 69 6c 2e 63 6f 6d 3b 7b 64 39 66 64  otmail.com;{d9fd<br>
0070   31 36 31 62 2d 64 34 64 38 2d 34 63 61 37 2d 38  161b-d4d8-4ca7-8<br>
0080   61 36 38 2d 63 37 30 66 65 62 33 39 61 33 33 30  a68-c70feb39a330<br>
0090   7d 0d 0a 50 32 50 2d 53 72 63 3a 20 74 65 73 74  }..P2P-Src: test<br>
00a0   6d 73 6e 70 73 68 61 72 70 40 6c 69 76 65 2e 63  msnpsharp@live.c<br>
00b0   6e 3b 7b 37 65 64 66 39 64 32 34 2d 37 65 39 38  n;{7edf9d24-7e98<br>
00c0   2d 34 37 32 30 2d 39 66 30 35 2d 37 34 63 33 61  -4720-9f05-74c3a<br>
00d0   62 61 61 30 37 37 30 7d 0d 0a 0d 0a 1c 03 00 00  baa0770}........<br>
00e0   26 3e fe 3e 02 04 d3 b7 94 cf 01 0c 00 02 00 00  &amp;&gt;.&gt;............<br>
00f0   00 0e 61 6e 0f 01 00 00 00 00 00 00              ..an........<br>
<br>
</code></pre>

The acknowledge should be like this:<br>
(The Peer info TLV that T=0x01, L=0x0c was dropped.)<br>
<br>
<pre><code><br>
0000   4d 53 47 20 66 72 65 65 7a 69 6e 67 73 6f 66 74  MSG freezingsoft<br>
0010   40 68 6f 74 6d 61 69 6c 2e 63 6f 6d 20 50 61 6e  @hotmail.com Pan<br>
0020   67 28 62 72 62 29 20 32 32 37 0d 0a 4d 49 4d 45  g(brb) 227..MIME<br>
0030   2d 56 65 72 73 69 6f 6e 3a 20 31 2e 30 0d 0a 43  -Version: 1.0..C<br>
0040   6f 6e 74 65 6e 74 2d 54 79 70 65 3a 20 61 70 70  ontent-Type: app<br>
0050   6c 69 63 61 74 69 6f 6e 2f 78 2d 6d 73 6e 6d 73  lication/x-msnms<br>
0060   67 72 70 32 70 0d 0a 50 32 50 2d 44 65 73 74 3a  grp2p..P2P-Dest:<br>
0070   20 74 65 73 74 6d 73 6e 70 73 68 61 72 70 40 6c   testmsnpsharp@l<br>
0080   69 76 65 2e 63 6e 3b 7b 37 65 64 66 39 64 32 34  ive.cn;{7edf9d24<br>
0090   2d 37 65 39 38 2d 34 37 32 30 2d 39 66 30 35 2d  -7e98-4720-9f05-<br>
00a0   37 34 63 33 61 62 61 61 30 37 37 30 7d 0d 0a 50  74c3abaa0770}..P<br>
00b0   32 50 2d 53 72 63 3a 20 66 72 65 65 7a 69 6e 67  2P-Src: freezing<br>
00c0   73 6f 66 74 40 68 6f 74 6d 61 69 6c 2e 63 6f 6d  soft@hotmail.com<br>
00d0   3b 7b 64 39 66 64 31 36 31 62 2d 64 34 64 38 2d  ;{d9fd161b-d4d8-<br>
00e0   34 63 61 37 2d 38 61 36 38 2d 63 37 30 66 65 62  4ca7-8a68-c70feb<br>
00f0   33 39 61 33 33 30 7d 0d 0a 0d 0a 10 00 00 00 d3  39a330}.........<br>
0100   b7 94 cf 02 04 26 3e fe 3e 00 00 00 00 00 00     .....&amp;&gt;.&gt;......<br>
<br>
</code></pre>

<b>Q:</b>

How to compute the Sequence Number and ACK Sequence Number for a Transfer Layer Package?<br>
<br>
<b>A:</b>

<table width='100%'>
<tr>
<td>
First of all, you need to set a base Sequence Number (just a random number) for the first Transfer Layer Package you want to send, say "out package 1". Then the "out package 2"'s sequence number should be the sequence number of "out package 1" plus payload data length of "out package 1".<br>
<br>
The calculation of ACK Sequence Number is similar. For example, A is the message you want to acknowledge to, B is the outgoing acknowledge message. So the ack sequence number should be (the sequence number of package A) plus (payload data length of package A). Then create a TLV item which T=0x02, L=0x04, V=(ack identifier). Add this TLV item to package B.<br>
</td>
</tr>
</table>

<b>Q:</b>

In old p2pv1, any message exceed a size of 1202 should be splitted. How to split a large message in p2pv2?<br>
<br>
<b>A:</b>
<table width='100%'>
<tr>
<td>
In p2pv2, the max size for the payload of a p2p Data Package is 1222. So the maximum number in the (payload data length) field for a Transfer Layer Package is 1222+20=1242(20 is the header length for a Data Package).<br>
<br>
To split a message, you need to set a none-zero Package Number for the data message packets. All the data messages splitted from that message should keep this Package Number. The TFCombination field for the first splitted message should be the same as original message, the following messages' TFCombination should be the value of the first minus one. <b>If the message is splitted into <i>N</i> packets, you also need to add a TLV field with T=0x01 L=0x08 V=(The remaining size of the splitted message after current packet) for the first <i>N-1</i> packets.</b>

For example, a message size of 1204 bytes with TFCombination of 0x01 (SLP message) should be splitted into 2 packets. So, firstly, give a random non-zero Package Number to all 3 packages. Then the first message's TFCombination in this sequence is 0x01. The TFCombination for the second and third package is 0x00. Then add a TLV field with T=0x01 L=0x08 V=0x00000002 for the first packet (You don't need to add such a TLV field for the second packet).<br>
</td>
</tr>
</table>
<b>Q:</b>

How to ack for a splitted message? Which sequence number I should ack to?<br>
<br>
<b>A:</b>
<table width='100%'>
<tr>
<td>
Just use the sequence number of the last packet of a splitted message. For instance, an INVITE message was splitted into two packets. The first packet's sequence number is 0x06990efd(110694141), the second packet's sequence number is 0x069913d7(110695383). Then you should ack this message by referencing the sequence number 0x069913d7 in you ack message.<br>
</td>
</tr>
</table>
<b>Q:</b>

Why don't you guys update all these to MSNPiki?<br>
<br>
<b>A:</b>
<table width='100%'>
<tr>
<td>
Good question. Actually this is not a final document for MSNC12, it is just a research draft and is subject to change. Maybe you look at it someday and come back tomorrow then find things described here are totally different. So I recommand you to come here and check whether this page has been changed constantly. And sharing your knowledge of MSNP protocol is also welcomed.<br>
<br>
At last, feel free to contact me if you:<br>
<ul><li>Have new research result of p2pv2 for MSNP18, or<br>
</li><li>Have questions about this new protocol.</li></ul>

My email address is: freezingsoft@hotmail.com. Good luck.<br>
<br>
<i>By Pang Wu</i>
</td>
</tr>
</table>

<h1>UDP data transfer</h1>

1. Frame Layer<br>
<br>
<table><thead><th> <b>Length</b> </th><th> <b>Description</b> </th></thead><tbody>
<tr><td> 4 </td><td> Local Identifier (get from last received package's remoteID or localID++) </td></tr>
<tr><td> 4 </td><td> Remote Identifier (get from last received package's localID or remoteID++) </td></tr>
<tr><td> 4 </td><td> SessionID? </td></tr>
<tr><td> 4 </td><td> Unknown </td></tr>
<tr><td> 4 </td><td> Unknown </td></tr></tbody></table>

<pre><code><br>
0                   1                   2                   3<br>
0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
| LID   | RID   | SID   |Unknown|Unknown|       Data ....<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
</code></pre>

Data is a Transport Layer packet.<br>
Other package is just the same as above.<br>
<br>
<h1>More info from Jabber community (TCP direct connection)</h1>

MSNP18 P2P protocol description.<br>
<br>
New P2P protocol is a stack of 3 layers: Frame Layer,<br>
Transport Layer and Data Layer.<br>
<br>
I. ENCODING RULES.<br>
<br>
1. Frame Layer.<br>
<br>
Frame Layer is responsible for data framing. In the case you<br>
establish direct TCP p2p connection the Frame looks like this:<br>
<br>
<pre><code><br>
0                   1                   2                   3<br>
0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
| Size  |           Data                                  ....<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
</code></pre>

<table width='100%'>
<tr>
<td>
The Size is a little-endian unsigned integer.<br>
The Data is a Transport Layer packet, Foo packet or Nonce packet.<br>
This depends on a state of a p2p connection.<br>
<br>
In the case you exchange p2p messages over a switchboard, you<br>
MUST incapsulate the Data into standard switcboard message and<br>
put 4 zeros to the end of that message. Example:<br>
<br>
</td>
</tr>
</table>
<pre><code>MIME-Version: 1.0\r\n<br>
Content-Type: application/x-msnmsgrp2p\r\n<br>
P2P-Dest: dest@hotmail.com;{41bd14f3-8904-4100-71be-89943607df9d}\r\n<br>
P2P-Src: src@live.com;{6028abad-5919-8be8-fd1c-5e1e84a86742}\r\n\r\n<br>
...the Data goes here...\0\0\0\0<br>
</code></pre>

2. Foo packet.<br>
<br>
Foo packet is a 4-byte data packet "foo\0": [0x66, 0x6f, 0x6f, 0x0].<br>
This packet is only used in direct TCP p2p connections.<br>
<br>
3. Nonce packet.<br>
<table width='100%'>
<tr>
<td>
Nonce packet is just an opaque 16-byte data packet. It is used to<br>
authorize a p2p connection. This packet is only used in direct<br>
TCP p2p connections. There is a one-to-one mapping between the<br>
Hashed-Nonce (which is the part of "application/x-msnmsgr-transreqbody"<br>
of an SLP message) and this Nonce. An algorithm of this mapping is<br>
unknown. An example of valid mappings:<br>
</td>
</tr>
</table>
<pre><code>{2B95F56D-9CA0-9A64-82CE-ADC1F3C55845} &lt;-&gt;<br>
[0x37,0x29,0x2d,0x12,0x86,0x5c,0x7b,0x4c,<br>
 0x81,0xf5,0xe,0x5,0x1,0x78,0x80,0xc2]<br>
<br>
<br>
{F960C412-A40F-E37D-DD1D-AE264E958F28} &lt;-&gt;<br>
[0x3d,0xa6,0x17,0xa5,0x2e,0xa2,0xdc,0x40,<br>
 0x85,0xa5,0x54,0xf9,0xe1,0x96,0xf4,0x6e]<br>
</code></pre>

4. Transport Layer.<br>
<table width='100%'>
<tr>
<td>
The Transport Layer packets are used to track a data sent over a p2p<br>
connection. The packet consists of the Header and the Payload.<br>
The Header consists of L, O, Len, Seq and TLVs fields.<br>
</td>
</tr>
</table>
<pre><code>0                   1                   2                   3<br>
0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
|L|O|Len|  Seq  |                TLVs                      ....<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
|                         Payload                          ....<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
</code></pre>

<ul><li>L - a length of the header including this field.<br>
</li><li>O - operation code.<br>
</li><li>Len - a length of the Payload. "0" means the Payload is empty.<br>
</li><li>Seq - a sequence number.<br>
</li><li>TLVs - a list of TLV-encoded values (see below). The list may be empty.<br>
</li><li>Payload - a Data Layer packet. The Payload may be empty.</li></ul>

All fields are in big-endian format.<br>
<br>
5. Data Layer.<br>
<table width='100%'>
<tr>
<td>
The Data Layer packets are used for data incapsulation. The packet<br>
consists of the Header and the Payload. The Header consists of<br>
L, O, Seq, SID and TLVs fields.<br>
</td>
</tr>
</table>

<pre><code>0                   1                   2                   3<br>
0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
|L|O|Seq|  SID  |                TLVs                      ....<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
|                         Payload                          ....<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
</code></pre>
<ul><li>L - a length of the header including this field.<br>
</li><li>O - an operation code. It consists of 7-bit Type and 1-bit flag F:<br>
<pre><code>    0 1 2 3 4 5 6 7 8<br>
    +-+-+-+-+-+-+-+-+<br>
    |    Type     |F|<br>
    +-+-+-+-+-+-+-+-+<br>
</code></pre>
</li><li>Seq - a sequence number.<br>
</li><li>SID - a session id.<br>
</li><li>TLVs - a list of TLV-encoded values (see below). The list may be empty.<br>
</li><li>Payload - an opaque data. The Payload may be empty. A size<br>
of the Payload must not exceed 1372 bytes.</li></ul>

All fields are in big-endian format.<br>
<br>
6. TLVs<br>
<br>
TLV list consists of TLV-encoded pairs (type, value). A whole<br>
TLV list is padded with zeros to fit 4-byte boundary.<br>
<pre><code>0                   1                   2                   3<br>
0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
|T|L|                       Value                          ....<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
|                            ....                               |<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
|T|L|                       Value         ....          | .... 0|<br>
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<br>
</code></pre>

T - type of the value.<br>
L - length of the value.<br>
Value - the value itself.<br>
<br>
All fields are in big-endian format.<br>
<br>
II. BUSSINESS LOGIC.<br>
<br>
1. Initialization.<br>
<table width='100%'>
<tr>
<td>
This state is only used in direct TCP p2p connections.<br>
Once a TCP connection is established, connecting peer should sent Foo<br>
packet and then Nonce packet. Receiving peer should check that Nonce<br>
and send its own Nonce packet back to the connecting peer.<br>
</td>
</tr>
</table>
<pre><code>Initiator             Responder<br>
    |-------  Foo  ------&gt;|<br>
    |------- Nonce ------&gt;|<br>
    |&lt;------ Nonce -------|<br>
</code></pre>

The p2p connection is now definitely established and the peers may<br>
exchange data.<br>
<br>
2. Sequence numbers in Transport Layer packets.<br>
<table width='100%'>
<tr>
<td>
The first sequence number picks up randomly between 1 and 2^32.<br>
The next sequence number should be increased on the size of the<br>
Payload of the previous Transport Layer packet. Thus, for example,<br>
if that Payload is empty, the sequence number is not increased.<br>
</td>
</tr>
</table>
3. TODO.<br>
<br>
<br>
<br>
<h1>Join us !</h1>
<table width='100%'>
<tr>
<td>
<b>If you are familiar with the MSN p2p protocol (No matter the old or new one) and have interest in our project, please contact me : freezingsoft@hotmail.com, you are the man we are seeking for!</b>
</td>
</tr>
</table>
<h2>References</h2>

<ul><li><a href='http://forums.fanatic.net.nz/index.php?showtopic=19372'>http://forums.fanatic.net.nz/index.php?showtopic=19372</a>
</li><li><a href='http://msnpiki.msnfanatic.com/index.php/MSNC:Binary_Headers'>http://msnpiki.msnfanatic.com/index.php/MSNC:Binary_Headers</a></li></ul>

</td>
</tr>
</table>