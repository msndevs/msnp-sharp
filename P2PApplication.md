# Introduction #

We support P2P applications in v4

  * Apps/ObjectTransfer (Display picture, Emoticon, Scenes etc.)
  * Apps/FileTransfer over switchboard and direct connection

# FooGame Application #

First, design a class extends P2PApplication. Pass your App-GUID and App-ID to P2PApplicationAttribute.

```

[P2PApplication(APP_ID, "APP-GUID")]
public class FooGameApplication : P2PApplication
{
}

```

Register your class.

```

P2PApplication.RegisterApplication(Assembly assembly); // All applications in your dll
P2PApplication.RegisterApplication(typeof(FooGameApplication)); // One type

```

If you have a Provisioned\_Account (bot), you will probably unregister FileTransfer application.

```

P2PApplication.UnregisterApplication(typeof(FileTransfer));

```

Override properties and methods.

```

public override string InvitationContext
{
    get
    {
        string activityUrl = AppID + ";1;FooGame";
        byte[] contextData = System.Text.UnicodeEncoding.Unicode.GetBytes(activityUrl);
        return Convert.ToBase64String(contextData, 0, contextData.Length);
    }
}

public override bool AutoAccept
{
    get
    {
        return true;
    }
}


public override bool ValidateInvitation(SLPMessage invitation)
{
    return base.ValidateInvitation(invitation);
}


public override void ProcessData(P2PBridge bridge, byte[] data, bool reset)
{
    // Handle data
    string incomingXml = Encoding.UTF8.GetString(data);

    // And send new data
    P2PDataMessage p2pData = new P2PDataMessage(P2PVersion);
    p2pData.InnerBody = newData; // new data
    SendMessage(p2pData);
}

```

Constructors

```

// We have received an invitation
public FooGameApplication(P2PSession session)
    : base(session)
{
    SLPMessage slp = session.Invite;
}

// We are initializer 
public FooGameApplication(P2PVersion ver, Contact remote, Guid remoteEP)
    : base(ver, contact, remoteEP)
{
}

```


Kick your FooGameApplication

```

FooGameApplication fooGameApp = new FooGameApplication(remote.P2PVersionSupported, remote, remote.MachineGuid);

P2PSession p2pSession = nameserver.P2PHandler.AddTransfer(fooGameApp);

```