<img src='http://msnp-sharp.googlecode.com/svn/wiki/images/MSNPSharp_banner.png' />

# Introduction #

This is a quick draft for my research in MSNP21. If you have any question on it, please email me: freezingsoft@hotmail.com

# Details #

## Log in ##

Here are the login commands sent by WLM 2011:

```
> VER 1 MSNP21 MSNP20 MSNP19 MSNP18 MSNP17 CVR0
< VER 1 MSNP21
> CVR 2 0x0409 winnt 6.1.0 i386 MSNMSGR 15.4.3502.0922 MSNMSGR mypassport@hotmail.com VmVyc2lvbjogMQ0KWGZyQ291bnQ6IDENCg==
< XFR 3 NS 207.46.124.125:1863 U D VmVyc2lvbjogMQ0KWGZyQ291bnQ6IDINCg==
```

We saw something new in CVR command. If you apply base64 decode for the last part of CVR command, you can see it's an XFR hop count string:

```
Encoded String: VmVyc2lvbjogMQ0KWGZyQ291bnQ6IDENCg==
Decoded String: Version: 1\r\nXfrCount: 1
Decoded String Length: 25
```

I think "XfrCount" is the hop counter when you get redirect to a new NS Server. You need to start with one, and plus one each time you get redirected by receiving an XFR command.

## User Status Notification ##

In MSNP21, NFY command is used for notifying a remote user's status (and maybe other properties) change:

```
NFY PUT 863
Routing: 1.0
To: 1:mypassport@hotmail.com
From: 1:en-cn@hotmail.com

Reliability: 1.0

Notification: 1.0
NotifNum: 0
Uri: /user
NotifType: Partial
Content-Type: application/user+xml
Content-Length: 645

<user><s n="IM">
<Status>NLN</Status>
</s><s n="PE">
<UserTileLocation>&#x3C;msnobj Creator=&#x22;en-cn@hotmail.com&#x22; 
Size=&#x22;25620&#x22; Type=&#x22;3&#x22; Location=&#x22;EnglishAssistantRobot.jpg&#x22; 
Friendly=&#x22;AAA=&#x22; SHA1D=&#x22;ORqJ9HXpyhzHkSi6kjU0pYqlNGE=&#x22;
 SHA1C=&#x22;qkZ9VvsPUnD+8VDFlgCAtw9VmNg=&#x22;/&#x3E;</UserTileLocation>
<FriendlyName>(#)ICS - Live</FriendlyName>
<PSM></PSM></s>
<sep n="IM" epid="{00000000-0000-0000-0000-000000000000}">
<Capabilities>1074249760:2281833472</Capabilities>
</sep>
<sep n="PE" epid="{00000000-0000-0000-0000-000000000000}">
<Capabilities>0:1073856512</Capabilities>
</sep></user>
```

The "From" item in MIME header indicates the remote user that sends you this update information.

## Facebook integration ##

When you are processing log in to MSN NS Server, you will get a "special" NFY PUT command as follows:

```
NFY PUT 295
Routing: 1.0
To: 1:mypassport@hotmail.com;epid={03c49942-1aea-4cda-8d9a-2c0a403b6782}
From: 14:fb

Reliability: 1.0

Notification: 1.0
NotifNum: 1
Uri: /network
NotifType: Full
Content-Type: application/network+xml
Content-Length: 45

<network><status>SigningIn</status></network>
```

As you can see the "From" MIME header has a value "14:fb". "fb" stands for "Facebook", 14 means remote network, you can see this number again when chatting with someone in facebook.

The XML body of this message means the MSN server is connecting you to facebook server through the Facebook gateway.

### Chat (Windows Live, Facebook, Circle, Multiparty) ###

In MSNP21, all the chatting messages are sent with SDG command, switchboard are simply deprecated, we don't need to deal with those complex SB negotiation anymore.

Here's an example for chatting to a facebook contact:

```
SDG 58 347
Routing: 1.0
To: 13:1065663773;via=14:fb
From: 1:mypassport@hotmail.com;epid={03c49942-1aea-4cda-8d9a-2c0a403b6782}

Reliability: 1.0

Messaging: 2.0
Message-Type: Text
Content-Length: 21
Content-Type: text/plain; charset=UTF-8
X-MMS-IM-Format: FN=%E5%BE%AE%E8%BD%AF%E9%9B%85%E9%BB%91; EF=; CO=80; CS=86; PF=22

facebook is gonna to kill you.
```

In "To" header, you can see the identifier for your facebook contact "13:1065663773". "13" is the type of "Connect" contacts, just like "1" means windows live contact, "9" means circle group, "10" means multiparty chat (temp group), "32" stands for Yahoo Messenger contacts.

The number followed by "13:" is your contact's facebook id (ConnectID).

String "via=14:fb" is Facebook's chatting gateway, which is already mentioned above.

## MSNP2P via SBBridge ##

There're no more Switchboard concept in MSNP21, all P2P SLP and data messages will be sent through NS server, they will be wrapped by a SDG message. Further research needs to be done on this topic.

## Multiparty (Group Chat) ##

There're no more Switchboard concept in MSNP21. It is replacement for Switchboard "CAL" command.

Create multiparty:
```
PUT 35 260
Routing: 1.0
From: 1:testmsnpsharp@live.cn;epid={ad9d9247-9181-4c57-8388-248304e153d3}
To: 10:00000000-0000-0000-0000-000000000000@live.com

Reliability: 1.0

Publication: 1.0
Content-Length: 0
Content-Type: application/multiparty+xml
Uri: /circle
```

As we can see from the "To" MIME header, the target user's account is 00000000-0000-0000-0000-000000000000@live.com, the part before "@" character is an empty GUID, which means we want to create a temporary group. After the group was created, the actual group account will returned in the "From" header (Just look at following examples).

Incoming message when multiparty created:
```
PUT 35 OK 165
Routing: 1.0
To: 1:testmsnpsharp@live.cn;epid={ad9d9247-9181-4c57-8388-248304e153d3}
From: 10:588e793a-003d-0003-4136-314b00000000@live.com

Reliability: 1.0

```

Join multiparty:
```
PUT 36 348
Routing: 1.0
From: 1:testmsnpsharp@live.cn;epid={ad9d9247-9181-4c57-8388-248304e153d3}
To: 10:588e793a-003d-0003-4136-314b00000000@live.com

Reliability: 1.0

Publication: 1.0
Content-Length: 90
Content-Type: application/circles+xml
Uri: /circle

<circle><roster><id>IM</id><user><id>1:testmsnpsharp@live.cn</id></user></roster></circle>
```


Invite an user to multiparty:
```
PUT 37 363
Routing: 1.0
From: 1:testmsnpsharp@live.cn;epid={ad9d9247-9181-4c57-8388-248304e153d3}
To: 10:588e793a-003d-0003-4136-314b00000000@live.com;path=IM

Reliability: 1.0

Publication: 1.0
Content-Length: 94
Content-Type: application/multiparty+xml
Uri: /circle

<circle><roster><id>IM</id><user><id>1:testmsnpsharp@hotmail.com</id></user></roster></circle>
```

An user joined multiparty:
```
NFY PUT 393
Routing: 1.0
To: 1:testmsnpsharp@live.cn;epid={ad9d9247-9181-4c57-8388-248304e153d3}
From: 10:588e793a-003d-0003-4136-314b00000000@live.com

Reliability: 1.0
Stream: 0

Notification: 1.0
NotifNum: 0
Uri: /circle
NotifType: Partial
Content-Type: application/circles+xml
Content-Length: 90

<circle><roster><id>IM</id><user><id>1:testmsnpsharp@live.cn</id></user></roster></circle>
```

Leave multiparty:
```
DEL 44 298
Routing: 1.0
From: 1:testmsnpsharp@live.cn;epid={ad9d9247-9181-4c57-8388-248304e153d3}
To: 10:588e793a-003d-0003-4136-314b00000000@live.com

Reliability: 1.0

Publication: 1.0
Content-Length: 0
Content-Type: application/circles+xml
Uri: /circle/roster(IM)/user(1:testmsnpsharp@live.cn)
```

An user left multiparty:
```
NFY DEL 298
Routing: 1.0
To: 1:testmsnpsharp@live.cn
From: 10:6935caae-02a5-0001-4004-2c1c00000000@live.com

Reliability: 1.0
Stream: 0

Notification: 1.0
NotifNum: 2
Uri: /circle/roster(IM)/user(1:left1234@hotmail.com)
NotifType: Partial
Content-Type: application/circles+xml
Content-Length: 0
```

## Close IM Window Notification ##

If a user has logged in multiple places (end points) and he close the chatting window in one end point, the "CloseIMWindow" message will broadcast to all login end points. These messages has a value of "Signal/CloseIMWindow" in their "Message-Type" Messaging headers. Here is an example of the CloseIMWindow message:

```
SDG 0 332\r\n
Routing: 1.0\r\n
To: 1:testmsnpsharp@live.cn\r\n
From: 1:testmsnpsharp@live.cn;epid={03c49942-1aea-4cda-8d9a-2c0a403b6782}\r\n
Options: 0\r\n
Service-Channel: PD\r\n
\r\n
Reliability: 1.0\r\n
\r\n
Messaging: 2.0\r\n
Message-Type: Signal/CloseIMWindow\r\n
Content-Type: text/plain; charset=UTF-8\r\n
Content-Length: 44\r\n
\r\n
1:testmsnpsharp@live.cn/1:testmsnpsharp@hotmail.com
```

In the last line of the message, we can see two account separated by "/". These accounts are the two parties in the chatting conversation:
` [local user]/[remote user] `.

If one of the end point closes a group chat/circle chat window, the message looks like following:

```
SDG 0 349
Routing: 1.0\r\n
To: 1:testmsnpsharp@hotmail.com\r\n
From: 1:testmsnpsharp@hotmail.com;epid={03c49942-1aea-4cda-8d9a-2c0a403b6782}\r\n
Options: 0\r\n
Service-Channel: PD\r\n
\r\n
Reliability: 1.0\r\n
\r\n
Messaging: 2.0\r\n
Message-Type: Signal/CloseIMWindow\r\n
Content-Type: text/plain; charset=UTF-8\r\n
Content-Length: 55\r\n
\r\n
9:00000000-0000-0000-0009-73944a8f7947@live.com;path=IM
```

As we can see, the only different thing is the last line which consists the group(circle) account and the path string.


## MSNObject and Binary Data Protocol (P2P) ##

MSNP21 keeps using the new p2pv2 protocol introduced with MSNP18, which we have a detailed documentation [here](http://code.google.com/p/msnp-sharp/wiki/KB_MSNC12_BinaryHeader).