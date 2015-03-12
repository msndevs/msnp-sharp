# Version History #

| **Version** | **SVN Tag / Branch** | **Date** | **Information** |
|:------------|:---------------------|:---------|:----------------|
| 2.5.X+ | [MSNPSHARP\_25\_STABLE](http://msnp-sharp.googlecode.com/svn/branches/MSNPSHARP_25_STABLE) | OUTDATED | <font color='red'>Latest 2.5.X+ stable branch (NO MORE SUPPORTED)</font>|
| 2.5.8 | [MSNPSHARP\_258\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_258_RELEASE) | 30 Oct 2009 | MSNP-Sharp v2.5.8.1372 for MSNP15 |
| 2.5.7 | [MSNPSHARP\_257\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_257_RELEASE) | 7 Aug 2009 | MSNP-Sharp v2.5.7.1112 for MSNP15 |
| 2.5.6 | [MSNPSHARP\_256\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_256_RELEASE) | 6 Jul 2009 | MSNP-Sharp v2.5.6.1076 for MSNP15/MSNP16 |
| 2.5.5 | [MSNPSHARP\_255\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_255_RELEASE) | 3 Apr 2009 | MSNP-Sharp v2.5.5.957 for MSNP15/MSNP16 |
| 2.5.4 | [MSNPSHARP\_254\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_254_RELEASE) | 13 Feb 2009 | MSNP-Sharp v2.5.4 for MSNP15/MSNP16 |
| 2.5.3 | [MSNPSHARP\_253\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_253_RELEASE) | 17 Jan 2009 | MSNP-Sharp v2.5.3 for MSNP15/MSNP16 |
| 2.5.2 | [MSNPSHARP\_252\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_252_RELEASE) | 07 Jan 2009 |MSNP-Sharp v2.5.2 for MSNP15/MSNP16 |
| 2.5.1 | MSNPSHARP\_251\_RELEASE | 16 Nov 2008 | MSNP-Sharp v2.5.1 for MSNP15/MSNP16 |
| 2.5.B | [MSNPSHARP\_250\_RELEASE](http://msnp-sharp.googlecode.com/svn/tags/MSNPSHARP_250_RELEASE) | 05 Nov 2008 | MSNP-Sharp v2.5.Beta (with known bugs!) |

  * <font color='red'>The 2.5.X+ stable branch is continually being improved with new bug fixes since the last release (but not new features).</font>*****<font color='red'>THIS VERSION HASN'T SUPPORTED SINCE 30 Oct 2009.</font>*** If you want get the 2.5.X+ stable version, check out the svn http://msnp-sharp.googlecode.com/svn/branches/MSNPSHARP_25_STABLE
  * You can download the full source code of this project by**<a href='http://tortoisesvn.tigris.org/' title='TortoiseSVN'><font color='#000099'>a svn client</font></a> and compile by a <a href='http://msdn.microsoft.com/en-us/vstudio/default.aspx' title='Visual Studio 2005'><font color='#000099'>development software</font></a>. We don't release snapshots.



---


# Documents #

> Please download the document files from:

  * http://msnp-sharp.googlecode.com/svn/docs/v20/
  * http://msnp-sharp.googlecode.com/svn/docs/v30/


---


# Change Log #

## <font color='#006666'>Version 2.5.8 (30 Oct 2009 - Last Release for 2.5.X+)</font> ##

### What's new ###

  * Remove contact card support

### Bugs fixed ###

  * Fixed mono issues (Socket and date time)

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.8' title='milestone:Release2.5.8'>The full list of fixed issues in 2.5.8</a> ([r1372](https://code.google.com/p/msnp-sharp/source/detail?r=1372))**---**


## <font color='#006666'>Version 2.5.7 (7 Aug 2009)</font> ##

### What's new ###

  * Remove MSNP16 support from this version.
  * Add activity demo to example client ([Issue 57](https://code.google.com/p/msnp-sharp/issues/detail?id=57)).

### Bugs fixed ###

  * [Issue 124](https://code.google.com/p/msnp-sharp/issues/detail?id=124),85,32: Correct the displayimage and emoticon transfer.
  * [Issue 7](https://code.google.com/p/msnp-sharp/issues/detail?id=7): Correct and separate the MSNObject description encoding.
  * [Issue 105](https://code.google.com/p/msnp-sharp/issues/detail?id=105): Not to send data prepare message in file transfer procedure.
  * [Issue 125](https://code.google.com/p/msnp-sharp/issues/detail?id=125): Add application partner support in addressbook.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.7' title='milestone:Release2.5.7'>The full list of fixed issues in 2.5.7</a> ([r1112](https://code.google.com/p/msnp-sharp/source/detail?r=1112))**---**

## <font color='#006666'>Version 2.5.6 (6 Jul 2009)</font> ##

### What's new ###

  * **Stable version use MSNP15 by default, MSNP16 is just a beta protocol**
  * Add cryptography support for addressbook files (disabled by default)
  * Add compression support for web services to save bandwidth
  * Contact card related class are no more supported by M$ ([Issue 104](https://code.google.com/p/msnp-sharp/issues/detail?id=104))
  * Add BotMode property to nameserver for Provisioned\_Accounts
  * Memory improvement for address book and deltas files ([r1022](https://code.google.com/p/msnp-sharp/source/detail?r=1022))

### Bugs fixed ###

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.6' title='milestone:Release2.5.6'>The full list of fixed issues in 2.5.6</a> ([r1076](https://code.google.com/p/msnp-sharp/source/detail?r=1076))**---**


## <font color='#006666'>Version 2.5.5 (3 Apr 2009)</font> ##

### What's new ###

  * Enhancements for addressbook files (.mcl)

### Bugs fixed ###

  * [Issue 81](https://code.google.com/p/msnp-sharp/issues/detail?id=81): Uncaught Exception in AddContactToList method
  * [Issue 86](https://code.google.com/p/msnp-sharp/issues/detail?id=86): Fail to accept friend requests when database is old
  * [Issue 88](https://code.google.com/p/msnp-sharp/issues/detail?id=88): Conversation.Switchboard.IsSessionEstablished is always false (DotMSNClient)
  * [Issue 91](https://code.google.com/p/msnp-sharp/issues/detail?id=91): The friendly name is empty if OIM comes from MSNP18
  * [Issue 93](https://code.google.com/p/msnp-sharp/issues/detail?id=93): IOException when adding or removing contacts (UnauthorizedAccessException)
  * [Issue 95](https://code.google.com/p/msnp-sharp/issues/detail?id=95) and [Issue 98](https://code.google.com/p/msnp-sharp/issues/detail?id=98): NullPointer exception when trying to get ContactCard
  * [Issue 97](https://code.google.com/p/msnp-sharp/issues/detail?id=97): Handling FQY command for country based emails

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.5' title='milestone:Release2.5.5'>The full list of fixed issues in 2.5.5</a> ([r957](https://code.google.com/p/msnp-sharp/source/detail?r=957))**---**


## <font color='#006666'>Version 2.5.4 (13 Feb 2009)</font> ##

### What's new ###

  * Write trace output to a richtextbox

### Bugs fixed ###

  * [Issue 69](https://code.google.com/p/msnp-sharp/issues/detail?id=69): Nameserver.AuthenticationError doesn't work
  * [Issue 70](https://code.google.com/p/msnp-sharp/issues/detail?id=70): Receive "BadCVRParameters" after login
  * [Issue 72](https://code.google.com/p/msnp-sharp/issues/detail?id=72): Can't send SMS to PhoneMember ClientType
  * [Issue 74](https://code.google.com/p/msnp-sharp/issues/detail?id=74): Disconnection problem when several places have the same name
  * [Issue 76](https://code.google.com/p/msnp-sharp/issues/detail?id=76) and [Issue 77](https://code.google.com/p/msnp-sharp/issues/detail?id=77): Conversation with people not in contact list
  * [Issue 79](https://code.google.com/p/msnp-sharp/issues/detail?id=79): Log off when an other client logs with the same account (MPOP.AutoLogoff)
  * [Issue 82](https://code.google.com/p/msnp-sharp/issues/detail?id=82) and [Issue 84](https://code.google.com/p/msnp-sharp/issues/detail?id=84): Adding Yahoo contacts (MSN Server Error 204)
  * [Issue 85](https://code.google.com/p/msnp-sharp/issues/detail?id=85): Broadcast avatar; Autosync is set to OFF
  * Change screen name if Owner.PassportVerified
  * Fix for MCLFileManager: If msnpsharp.dll is in GAC, One Deltas.Save() saves all cached files

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.4' title='milestone:Release2.5.4'>The full list of fixed issues in 2.5.4</a>**---**


## <font color='#006666'>Version 2.5.3 (17 Jan 2009)</font> ##

### What's new ###

  * Add OnPlacesChanged event to Owner when end point changed.
  * Add support for Provisioned\_Accounts (bots)
  * Add debug window

### Bugs fixed ###

  * [Issue 60](https://code.google.com/p/msnp-sharp/issues/detail?id=60): Renotify Pending
  * [Issue 61](https://code.google.com/p/msnp-sharp/issues/detail?id=61): Passport Authorization Failed
  * Save delta file when owner picture changed
  * Fix for @msn.com accounts, M$ changed the login url again.
  * Fix for Circle contacts (new groups) and Circle Dynamic Items (group news). Old groups renamed as categories.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.3' title='milestone:Release2.5.3'>The full list of fixed issues in 2.5.3</a>**---**


## <font color='#006666'>Version 2.5.2 (07 Jan 2009)</font> ##

### What's new ###

  * Add activity support & keep-alive message support
  * Improve inner membership list
  * Add DisplayImageChanged event to Contact
  * Add NickName, CID and AutoSubscribeToUpdates properties to Contact
  * Add EpName and Places properties; SignoutFrom(string place) and SignoutFromEverywhere() methods to Owner

### Bugs fixed ###

  * [Issue 47](https://code.google.com/p/msnp-sharp/issues/detail?id=47): Reflection error in Mono
  * [Issue 48](https://code.google.com/p/msnp-sharp/issues/detail?id=48): Add DomainMember for Web Chat.
  * [Issue 49](https://code.google.com/p/msnp-sharp/issues/detail?id=49): Error on XMLContactList.AddMemberhip
  * [Issue 50](https://code.google.com/p/msnp-sharp/issues/detail?id=50): Potential bug in MSNSLPMessage.FromMail

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.2' title='milestone:Release2.5.2'>The full list of fixed issues in 2.5.2</a>**---**


## <font color='#006666'>Version 2.5.1 (16 Nov 2008)</font> ##

### Bugs fixed ###

  * Fix membership lists (AL,BL,RL,PL) for the second connect to MSN service.
  * Fix the bug that MSNObjectCatalog will duplicate add the same emoticon.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release2.5.1' title='milestone:Release2.5.1'>The full list of fixed issues in 2.5.1</a>**---**


## <font color='#006666'>Version 2.5.0 BETA (05 Nov 2008)</font> ##

### What's new ###

  * Add support for MSNP16
  * Add support for updating roaming display image.
  * Add support for sending animate custom emoticons.
  * Add OfficeLiveWebNotification into ServiceFilterType.
  * Sign the assembly.
  * Redesign the inner membership list.
  * **Redesign events (Remove delegates and replace its usage with generic `EventHandler<T>`).**
  * Add examples to example client for sending and receiving custom emoticons.