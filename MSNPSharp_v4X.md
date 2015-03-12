# Version History #

| **Version** | **SVN Tag / Branch** | **Date** | **Information** |
|:------------|:---------------------|:---------|:----------------|
| **4.X+** | **[MSNPSHARP\_45\_STABLE](http://msnp-sharp.googlecode.com/svn/branches/MSNPSHARP_45_STABLE)** | **OUTDATED** | <font color='red'>Latest 4.X+ stable branch </font>|
| 4.5.2 | [MSNPSHARP\_452\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_452_RELEASE) | 21 December 2011 | MSNP-Sharp v4.5.2.2673 for MSNP21 |
| 4.5.1 | [MSNPSHARP\_451\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_451_RELEASE) | 14 November 2011 | MSNP-Sharp v4.5.1.2582 for MSNP21 |
| 4.5.0 | [MSNPSHARP\_450\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_450_RELEASE) | 10 November 2011 | MSNP-Sharp v4.5.0.2565 for MSNP21 |
| 4.0.5 | [MSNPSHARP\_405\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_405_RELEASE) | 14 November 2011 | MSNP-Sharp v4.0.5.2580 for MSNP21 |
| 4.0.4 | [MSNPSHARP\_404\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_404_RELEASE) | 10 November 2011 | MSNP-Sharp v4.0.4.2562 for MSNP21 |
| 4.0.3 | [MSNPSHARP\_403\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_403_RELEASE) | 13 September 2011 | MSNP-Sharp v4.0.3.2539 for MSNP21 |
| 4.0.2 | [MSNPSHARP\_402\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_402_RELEASE) | 14 August 2011 | MSNP-Sharp v4.0.2.2512 for MSNP21 |
| 4.0.1 | [MSNPSHARP\_401\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_401_RELEASE) | 16 May 2011 | MSNP-Sharp v4.0.1.2414 for MSNP21 |
| 4.0.0 | [MSNPSHARP\_400\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_400_RELEASE) | 03 May 2011 | MSNP-Sharp v4.0.0.2393 for MSNP21 |


  * <font color='red'>The 4.X+ stable branch is continually being improved with new bug fixes since the last release (but not new features).</font>*****<font color='red'>THIS VERSION HASN'T SUPPORTED SINCE 21 December 2011.</font>*** If you want get the 4.X+ stable version, check out the svn http://msnp-sharp.googlecode.com/svn/branches/MSNPSHARP_45_STABLE
  * You can download the full source code of this project by**<a href='http://tortoisesvn.tigris.org/' title='TortoiseSVN'><font color='#000099'>a svn client</font></a> and compile by a <a href='http://msdn.microsoft.com/en-us/vstudio/default.aspx' title='Visual Studio 2005'><font color='#000099'>development software</font></a>. We don't release snapshots.


# Upgrade Notice #

<font color='navy'><b>
<ul><li>You can upgrade to 5.0.X from 4.5.X without problems.<br>
</li><li>If you stay in 4.X, your TCP connection can be disconnected to use forcing polling feature by M$ in the future (see <a href='https://code.google.com/p/msnp-sharp/issues/detail?id=307'>Issue 307</a>).<br>
</li><li>In v5.0, a feature called "HTTP Polling" was implemented. If you want not to use the http polling just set <code>Settings.DisableHttpPolling=true</code>, so legacy tcp connection is used for name server connections and v5.0 acts as v4.5. The difference between 4.X and 5.X is "HTTP Polling".<br>
</b></font></li></ul>

# Documents #

> Please download the document files from:

  * http://msnp-sharp.googlecode.com/svn/docs/v40/



---


# Known Issues #

  * **P2P framework doesn't work for Provisioned\_Accounts:** There is no more P2P support for provisioned accounts in MSNP21. That means; Broadcasting avatar, custom emoticons, file transfers and activity invitations won't work if you are using provisioned accounts for your bots. This is M$'s bug, not ours. If you want to use P2P framework for provisioned accounts, you must downgrade to MSNP18 (MSNPSharp v3.2.0).


---


# Change Log #


## <font color='#0099CC'>Version 4.5.2.2673 (21 December 2011 -  Last Release for 4.X+)</font> ##

### What's new ###

  * Ping/Pong: You don't need to setup a timer for PING, it is managed internally.
  * Show friends in common when a pending contact added.
  * Invalidate cache when mcl file deleted.
  * Update WSDL and XSD files.
  * Tidy the coding style.

### Bugs Fixed ###

  * Add support for DeleteContact soap call. BreakConnection doesn't work for Yahoo/Phone contacts.
  * Don't delete hidden contact from contact list if he requested friendship again.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.5.2' title='milestone:Release4.5.2'>The full list of fixed issues in 4.5.2</a>


---


## <font color='#0099CC'>Version 4.5.1.2582 (14 November 2011)</font> ##

### What's new ###

  * Add HTTP proxy support for nameserver connection.

### Bugs Fixed ###

  * [Issue 300](https://code.google.com/p/msnp-sharp/issues/detail?id=300): Use Location header instead of preferred host.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.5.1' title='milestone:Release4.5.1'>The full list of fixed issues in 4.5.1</a>


---


## <font color='#0099CC'>Version 4.5.0.2565 (10 November 2011)</font> ##

### What's new ###

  * Many obsoleted things was deleted for MSNP21. You can upgrade to 4.5.X from 4.0.X without problems.
  * Remove Block role. It is not valid for MSNP21 and replaced by Hide role.
  * Add FriendshipRequested event instead of ReverseAdded/ReverseRemoved. Add FriendshipStatusChanged event and FriendshipStatus property to contact class.
  * Add support for Office Communicator.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.5.0' title='milestone:Release4.5.0'>The full list of fixed issues in 4.5.0</a>


---



## <font color='#CC0000'>Version 4.0.5.2580 (14 November 2011 - Last Release for 4.0.X+)</font> ##

### Bugs Fixed ###

  * [Issue 300](https://code.google.com/p/msnp-sharp/issues/detail?id=300): Use Location header instead of preferred host.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.0.5' title='milestone:Release4.0.5'>The full list of fixed issues in 4.0.5</a>


---


## <font color='#CC0000'>Version 4.0.4.2562 (10 November 2011)</font> ##

### Bugs Fixed ###

  * [Issue 293](https://code.google.com/p/msnp-sharp/issues/detail?id=293): After PersonalMessage.SetListeningAlbum, can't set PersonalMessage again.
  * [Issue 296](https://code.google.com/p/msnp-sharp/issues/detail?id=296): Need a work around under Mono.
  * [Issue 297](https://code.google.com/p/msnp-sharp/issues/detail?id=297): ContactService unhandled error.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.0.4' title='milestone:Release4.0.4'>The full list of fixed issues in 4.0.4</a>


---


## <font color='#CC0000'>Version 4.0.3.2539 (13 September 2011)</font> ##

### What's new ###

  * OIM web service removed completely. Contact.SendMessage() will send an offline message if user is offline. To receive all messages (including OIM), listen Nameserver.TextMessageReceived event.
  * Add RoutingInfo property to MessageArrivedEventArgs to know who is sender/receiver if signed in multiple places.
  * Add support for Live Atom API to change personal message. Changing PSM via storage service was denied. See [issue 291](https://code.google.com/p/msnp-sharp/issues/detail?id=291).

### Bugs Fixed ###

  * [Issue 292](https://code.google.com/p/msnp-sharp/issues/detail?id=292): Example considers the UnauthorizedException that no longer used


<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.0.3' title='milestone:Release4.0.3'>The full list of fixed issues in 4.0.3</a>


---


## <font color='#CC0000'>Version 4.0.2.2512 (14 August 2011)</font> ##

### Bugs Fixed ###

  * [Issue 287](https://code.google.com/p/msnp-sharp/issues/detail?id=287): MSNPSharp will crash the client application when adding a contact that is not exists.
  * Prevent memory leak after add contact.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.0.2' title='milestone:Release4.0.2'>The full list of fixed issues in 4.0.2</a>


---


## <font color='#CC0000'>Version 4.0.1.2414 (16 May 2011)</font> ##

### Bugs Fixed ###

  * [Issue 273](https://code.google.com/p/msnp-sharp/issues/detail?id=273): PersonalMessage.Message cannot show in WLM.
  * [Issue 275](https://code.google.com/p/msnp-sharp/issues/detail?id=275): Changing personal message with provisional account produce a disconnection

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.0.1' title='milestone:Release4.0.1'>The full list of fixed issues in 4.0.1</a>



---


## <font color='#CC0000'>Version 4.0.0.2393 (03 May 2011)</font> ##

### What's new ###

  * Add support for MSNP21 http://code.google.com/p/msnp-sharp/wiki/KB_MSNP21
  * Revamped P2P framework to support <a href='http://code.google.com/p/msnp-sharp/wiki/P2PApplication' title='P2PApplication'>P2PApplication</a>. So, everybody can write their own P2P applications like WebCam, Games, etc.
  * Add support for Direct Connect Data Transfer for P2Pv2 clients.
  * Add support for Wink (receive only)
  * Add support for Facebook chat.
  * **Switchboard conversation is deprecated. See [issue 263](https://code.google.com/p/msnp-sharp/issues/detail?id=263)** You can call methods directly like following:

```
Contact contact = ...;                  // Get your receiver
contact.SendMessage(TextMessage);       // Send a text message to remote contact.
contact.SendNudge();                    // Send a nudge.
contact.SendEmoticonDefinitions(...);   // Send custom emoticons
```

Group chat can be managed by calling these methods ands events:
```
// Get nameserver handler.
NSMessageHandler ns = Messenger.Nameserver;

// Methods
ns.CreateMultiparty(List<Contact> inviteQueue, ns_MultipartyCreatedLocally)
Contact group = ns.GetMultiparty(string imAddress)
ns.InviteContactToMultiparty(Contact contact, Contact group)
ns.LeaveMultiparty(Contact group)

// Events
ns.MultipartyCreatedRemotely += ns_MultipartyCreatedRemotely;
ns.JoinedGroupChat += ns_OnJoinedGroupChat;
ns.LeftGroupChat += ns_OnLeftGroupChat;

void ns_OnJoinedGroupChat(object s, GroupChatParticipationEventArgs e)
{
 // e.Contact joined group chat e.Via(TempGroup or Circle)
}

void ns_ns_OnLeftGroupChat(object s, GroupChatParticipationEventArgs e)
{
 // e.Contact left group chat e.Via(TempGroup or Circle)
}

void ns_MultipartyCreatedRemotely(object s, MultipartyCreatedEventArgs e)
{
  // Someone invited to multiparty and we are in multiparty.
}

void ns_MultipartyCreatedLocally(object s, MultipartyCreatedEventArgs e)
{
  // We created multiparty locally and e.Group is TempGroup.
}

```


### Upgrade Notes ###

  * Old name: `ClientCapacities`, New name: `ClientCapabilities`
  * Old name: `MSNLists`, New name: `RoleLists`
  * Old name: `ClientType`, New name: `IMAddressInfoType`

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release4.0.0' title='milestone:Release4.0.0'>The full list of fixed issues in 4.0.0</a>