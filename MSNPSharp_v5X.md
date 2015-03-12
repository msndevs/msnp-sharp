# Version History #

| **Version** | **GIT Tag / Branch** | **Date** | **Information** |
|:------------|:---------------------|:---------|:----------------|
| **5.X+** | **[MSNPSHARP\_50\_STABLE](http://msnp-sharp.googlecode.com/git-history/MSNPSHARP_50_STABLE/)** | **Weekly** | <font color='#009900'>Latest 5.X+ stable branch for MSNP21</font>|
| 5.0.4 | [MSNPSHARP\_504\_RELEASE](http://msnp-sharp.googlecode.com/git-history/MSNPSHARP_504_RELEASE/) | 07 Mar 2012 | MSNP-Sharp v5.0.4.12034 for MSNP21 |
| 5.0.3 | [MSNPSHARP\_503\_RELEASE](http://msnp-sharp.googlecode.com/git-history/MSNPSHARP_503_RELEASE/) | 05 Mar 2012 | MSNP-Sharp v5.0.3.12032 for MSNP21 |
| 5.0.2 | [MSNPSHARP\_502\_RELEASE](http://msnp-sharp.googlecode.com/git-history/MSNPSHARP_502_RELEASE/) | 01 Mar 2012 | MSNP-Sharp v5.0.2.12031 for MSNP21 |
| 5.0.1 | [MSNPSHARP\_501\_RELEASE](http://msnp-sharp.googlecode.com/git-history/MSNPSHARP_501_RELEASE/) | 20 Feb 2012 | MSNP-Sharp v5.0.1.12021 for MSNP21 |
| 5.0.0 | [MSNPSHARP\_500\_RELEASE](http://msnp-sharp.googlecode.com/git-history/MSNPSHARP_500_RELEASE/) | 03 Jan 2012 | MSNP-Sharp v5.0.0.2717 for MSNP21 |


  * **The 5.X+ stable branch is continually being improved with new bug fixes since the last release (but not new features).**
  * If you want get the 5.X+ stable version, <font color='#009900'><code>git clone http://code.google.com/p/msnp-sharp/ -b MSNPSHARP_50_STABLE</code></font>*** You can download the full source code of this project by**<a href='http://code.google.com/p/tortoisegit/' title='TortoiseGIT'><font color='#000099'>a git client</font></a> and compile by a <a href='http://msdn.microsoft.com/en-us/vstudio/default.aspx' title='Visual Studio 2010'><font color='#000099'>development software</font></a>. We don't release snapshots.


# Documents #

> Please download the document files from:

  * http://docs.msnp-sharp.googlecode.com/git/v40/


---


# Known Issues #

  * **P2P framework doesn't work for Provisioned\_Accounts:** There is no more P2P support for provisioned accounts in MSNP21. That means; Broadcasting avatar, custom emoticons, file transfers and activity invitations won't work if you are using provisioned accounts for your bots. This is M$'s bug, not ours. If you want to use P2P framework for provisioned accounts, you must downgrade to MSNP18 (MSNPSharp v3.2.X).


---


# Change Log #

## <font color='#00CC00'>Version 5.0.4.12034 (07 March 2012)</font> ##

### What's New ###

  * IDisposable implementation for SocketMessageProcessor

### Bugs Fixed ###

  * Close tcp socket when Disconnected.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release5.0.4' title='milestone:Release5.0.4'>The full list of fixed issues in 5.0.4</a>


---



## <font color='#00CC00'>Version 5.0.3.12032 (05 March 2012)</font> ##

### What's New ###

  * Add automatic decompression to HTTP polling gateway. This saves bandwidth (~ 50-80 %).
  * Don't use lifespan before signed in. Now, login time is ~30% is faster.
  * Handle all soft errors and set timeouts for HTTP polling gateway who have slow internet connections. Now PNG works.
  * Tidy up for IMessageHandler and IMessageProcessor interfaces.

### Bugs Fixed ###

  * [Issue 318](https://code.google.com/p/msnp-sharp/issues/detail?id=318): Kick out from server.
  * [Issue 319](https://code.google.com/p/msnp-sharp/issues/detail?id=319): Messenger object is not disconnected when there is no internet connection.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release5.0.3' title='milestone:Release5.0.3'>The full list of fixed issues in 5.0.3</a>


---



## <font color='#00CC00'>Version 5.0.2.12031 (01 March 2012)</font> ##

### What's New ###

  * SingleSignOn: Index tickets by SHA for security reasons.
  * SingleSignOnManager: Delete tickets if auth failed or user changed own password.
  * MSNTicket: Hit delete tick to prevent deleting most used tickets from cache for performance reasons.
  * ConnectivitySettings: Setup web proxy and local bind address when http requests were created.

### Bugs Fixed ###

  * [Issue 316](https://code.google.com/p/msnp-sharp/issues/detail?id=316): ArgumentNullException in CreateFaceBookContactFromShellContact().
  * [Issue 317](https://code.google.com/p/msnp-sharp/issues/detail?id=317): NullReferenceException in CreateCircle().

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release5.0.2' title='milestone:Release5.0.2'>The full list of fixed issues in 5.0.2</a>


---



## <font color='#00CC00'>Version 5.0.1.12021 (20 February 2012)</font> ##

### What's New ###

  * Repository moved to GIT and the project upgraded to VS2010.
  * Load/Save personal settings in XML file like account and presence status.
  * Many many code optimizations and tidy up codes.

### Bugs Fixed ###

  * [Issue 313](https://code.google.com/p/msnp-sharp/issues/detail?id=313): Disconnect() problem. Now OUT command is sent via http polling gateway. You must wait 3-5 seconds for full disconnection.
  * [Issue 314](https://code.google.com/p/msnp-sharp/issues/detail?id=314): Unhandled FATAL exception during authentication when no Credentials provided.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release5.0.1' title='milestone:Release5.0.1'>The full list of fixed issues in 5.0.1</a>


---



## <font color='#00CC00'>Version 5.0.0.2717 (03 January 2012)</font> ##

### What's New ###

  * Implement HTTP polling transport, basically it encapsulates the MSNP commands into an HTTP request, and receive responses by polling a specific url (gateway.messenger.hotmail.com:80). Port 1863 is blocked by some ISP. If you want not to use http polling feature just set `Settings.DisableHttpPolling=true`, so tcp connection is used for name server connections.

### Upgrade Notes ###

  * 5.X is backward compatible with 4.X.
  * `NameserverProcessor` property removed from Messenger class and added `ConnectionEstablished` and `ConnectionClosed` events. Added `MessageProcessorChanged` event to nameserver to listen connected, disconnected events. It will remain null until connected, so you can use `NameserverProcessor` only when `MessageProcessorChanged` was fired.

<a href='http://code.google.com/p/msnp-sharp/issues/list?can=1&q=milestone:Release5.0.0' title='milestone:Release5.0.0'>The full list of fixed issues in 5.0.0</a>