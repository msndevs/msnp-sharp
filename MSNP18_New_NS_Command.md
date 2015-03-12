<table>
<tr>
<td>
<img src='http://msnp-sharp.googlecode.com/svn/wiki/images/MSNPSharp_banner.png' />
</td>
</tr>
<table cellpadding='30px' cellspacing='0px'>
<tr>
<td>
<h1>Introduction</h1>

This is an document for NS command comes with MSNP18.<br>
Thanks for our protocol researcher AUX who reverse engineered the USR SHA command.<br>
<br>
<br>
<h1>Details</h1>

1. New USR command with SHA A parameters. This command is sent by client to get authority in new MSN group (circle) operation.<br>
<br>
Example:<br>
<br>
<pre><code>USR 30 SHA A PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTE2Ij8+CjxTaWduZWRUaW..[too long, omitted]..mVkVGlja2V0Pg==<br>
<br>
</code></pre>

The format of this command is USR <i>TransID</i> SHA A <i>Nonce</i>

<b>TransID:</b> The transaction id of this NS message.<br>
<br>
<b>Nonce:</b> A base64 string encoded from the field "CircleTicket" in the xml document returned by ABFindContactsPaged. The returned xml document of ABFindContactsPaged is something like this:<br>
<br>
<pre><code><br>
- &lt;ABFindContactsPagedResponse xmlns="http://www.msn.com/webservices/AddressBook"&gt;<br>
- &lt;ABFindContactsPagedResult&gt;<br>
+ &lt;Contacts&gt;<br>
- &lt;CircleResult&gt;<br>
+ &lt;Circles&gt;<br>
- &lt;CircleInverseInfo&gt;<br>
- &lt;Content&gt;<br>
- &lt;Handle&gt;<br>
  &lt;Id&gt;00000000-0000-0000-0009-96c9144c6bc6&lt;/Id&gt; <br>
  &lt;/Handle&gt;<br>
+ &lt;Info&gt;<br>
  &lt;/Content&gt;<br>
+ &lt;PersonalInfo&gt;<br>
  &lt;Deleted&gt;false&lt;/Deleted&gt; <br>
  &lt;/CircleInverseInfo&gt;<br>
  &lt;/Circles&gt;<br>
  &lt;CircleTicket&gt;&lt;?xml version="1.0" encoding="utf-16"?&gt; &lt;SignedTicket xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" ver="1" keyVer="1"&gt; &lt;Data&gt;PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTE2Ij8+DQo8VGlja2V0IHhtbG5zOnhzaT0iaHR0cDovL3d3dy53My5vcmcvMjAwMS9YTUxTY2hlbWEtaW5zdGFuY2UiIHhtbG5zOnhzZD0iaHR0cDovL3d3dy53My5vcmcvMjAwMS9YTUxTY2hlbWEiPg0KICA8Q2lyY2xlIElkPSIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwOS0wYjg0NmExNWUwMjEiIEhvc3RlZERvbWFpbj0ibGl2ZS5jb20iIC8+DQogIDxDaXJjbGUgSWQ9IjAwMDAwMDAwLTAwMDAtMDAwMC0wMDA5LTExMzMzMzBlNWVmYSIgSG9zdGVkRG9tYWluPSJsaXZlLmNvbSIgLz4NCiAgPENpcmNsZSBJZD0iMDAwMDAwMDAtMDAwMC0wMDAwLTAwMDktNDJiODI5MTQxNTZjIiBIb3N0ZWREb21haW49ImxpdmUuY29tIiAvPg0KICA8Q2lyY2xlIElkPSIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwOS1kYmYzNzI5MzI1ZmMiIEhvc3RlZERvbWFpbj0ibGl2ZS5jb20iIC8+DQogIDxUUz4yMDA5LTA5LTE1VDEzOjIzOjIxLjAzNDY0NjdaPC9UUz4NCiAgPENJRD4yNjI4NzEzNzA2Mjg3OTcyMDk1PC9DSUQ+DQo8L1RpY2tldD4=&lt;/Data&gt; &lt;Sig&gt;qa14qnW48CTxD7DphXZpj3eqFxyH/aURvJ2Ge6gbx8AJ9d0ByLeCBzWMaWCpmwsrbhVeLNDLEW8aVCCdZ4cFKoaMC/WHO8qmyl3Zis6yShljwGhVe0+kq98TL2KGSFSl4SFzu5p+reJCPtqSxWXp/14q581J7L2sBuEXyA1uxi0=&lt;/Sig&gt; &lt;/SignedTicket&gt;&lt;/CircleTicket&gt; <br>
  &lt;/CircleResult&gt;<br>
+ &lt;Ab&gt;<br>
  &lt;/ABFindContactsPagedResult&gt;<br>
  &lt;/ABFindContactsPagedResponse&gt;<br>
<br>
</code></pre>

Get the entire content of CircleTicket field, convert all the escaped character (For example: &lt; should be converted to <), then apply base64 on it, you will get the correct nonce.<br>
<br>
You have to send this command once you successfully login, or you will get a NS server error if you want to send any message related to circle operation (Such as creating a circle, accepting join circle invitation, sending ADL and RML command to register circle users and so on).<br>
</td>
</tr>
</table>
</table>