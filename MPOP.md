<table>
<tr>
<td>
<img src='http://msnp-sharp.googlecode.com/svn/wiki/images/MSNPSharp_banner.png' />
</td>
</tr>
<table cellpadding='30px' cellspacing='0px'>
<tr>
<td>
<h1>Multiple Points Of Presence (MPOP)</h1>

Sign in or out of Messenger from multiple places or devices.<br>
<br>
You can sign in to Messenger on multiple devices or computers at the same time. For example, if you're already signed in to Messenger on a computer, you can sign in on a mobile device or on another computer without having to sign out on the first computer. You can also sign in to Messenger on your computer and on the web at the same time.<br>
<br>
<br>
<h2>FAQ</h2>

<h3>How can I change the name of a device?</h3>
<pre><code>Owner.EpName = "NewName";<br>
</code></pre>

<h3>How can I turn off or on the multiple sign-in feature?</h3>
<pre><code>Owner.MPOPMode = MPOP.KeepOnline;<br>
Owner.MPOPMode = MPOP.AutoLogoff;<br>
</code></pre>

<h3>How can I sign out of Messenger from any device?</h3>
<pre><code>Owner.SignoutFrom(Guid machineGuid);<br>
Owner.SignoutFromEverywhere();<br>
</code></pre>

<h3>How can I notify whenever a place signed off or on?</h3>
Register an event for Owner.PlacesChanged and use Owner.Places property.<br>
<pre><code>Owner.PlacesChanged += Owner_PlacesChanged;<br>
<br>
Owner_PlacesChanged(object sender, EventArgs e)<br>
{<br>
    Dictionary&lt;Guid, string&gt; places = Owner.Places;<br>
}<br>
</code></pre>


<h2>Notes</h2>

<ul><li>If you receive a message while you're signed in on multiple devices, the message will appear on each device that you're signed in on. Also, if you perform an action on one device, such as open or close a conversation window, the action will occur on all devices.<br>
</li><li>If you sign in to a version of Messenger that doesn't support the multiple sign-in feature (older than MSNP16), you will automatically be signed out from the other devices.<br>
</td>
</tr>
</table>
</table>