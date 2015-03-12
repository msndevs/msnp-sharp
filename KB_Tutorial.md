# Introduction #

This is a **very basic** tutorial regarding how to utilize MSNPSharp to complete some basic tasks such as signing in/out, getting contact list and sending text messages. For more detail samples and advance functions, please checkout the code from our code repository or download a copy from our [Download Page](http://code.google.com/p/msnp-sharp/wiki/Downloads?tm=2).

This tutorial will be completed slowly when I have time.

# Creating Messenger Object #

The first step of using MSNPSharp is to create a Messenger object, all your later operations will be applied on this Messenger instance.
The code can't be simpler:

```
// Define it as class member variable
private Messenger messenger = new Messenger();
```


# Sign In/Off #

Before you do the sign in, there is actually one more step needs to be finished: Issue the correct setting to MSNPSharp, for example, setting the type of debug info to output, set the proxy host port and address(if any), tell the library whether to save the contact file to your local disk (If you use it on asp.net web form, you don't have the write permission) and the format and encryption level of the contact files...

But to make things simpler, I decide to talk about those fuzzy stuff in later sections and dive into sign in first.

To be notified when you successfully signed in/off, you need to listen to the `SignedIn` event.

```
// Get notified when successfully signed in.
messenger.Nameserver.SignedIn += new EventHandler<EventArgs>(Nameserver_SignedIn);

// Get notified when the user signed off.
messenger.Nameserver.SignedOff += new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff);
```

The `Nameserver_SignedIn` and `Nameserver_SignedOff` function looks like following:

```

private void Nameserver_SignedIn(object sender, EventArgs e)
{
    Trace.WriteLine("Successfully signed in!");
}

private void Nameserver_SignedOff(object sender, SignedOffEventArgs e)
{
    Trace.WriteLine("Successfully signed off!");
    // Do your clean up here.
}

```

One thing needs to keep in mind is these two events **will not** be called in the UI thread. In other words, if you want to modify the UI controls in this two methods, please invoke to the UI thread first:


```
private void Nameserver_SignedIn(object sender, EventArgs e)
{
    // Assume the method is already in a Form's class.
    if (InvokeRequired)
    {
        BeginInvoke(new EventHandler(Nameserver_SignedIn), sender, e);
        return;
    }

    // Now you can operate on the controls.
    TextBox1.Text = "Successfully signed in!";
}
```

The SignedOff event is similar, the only difference is you can know the sign off reason from the `SignedOffEventArgs.SignedOffReason`. If the `SignedOffReason` is `None`, usually this sign off is caused by the local user. If it is `OtherClient`, that means another user signed in with the same account but a MSN client that doesn't support MPOP(usually a client that only supports MSNP15 and lower. For what is MPOP, please refer [here](http://code.google.com/p/msnp-sharp/wiki/MPOP)) on another computer so you got kick out. If the reason is `ServerDown`, I think everyone knows what that mean.

After placing these events to the correct place, you can connect/disconnect to/from the server now:

```
private void loginButton_Click(object sender, System.EventArgs e)
    if (messenger.Connected)
    {
        // The MSNPSharp is signed in or in the procedure of signing in.
        // which means the user click the button again while signing in, or, after signed in.
        messenger.Disconnect();
    }
    
    messenger.Credentials = new Credentials("Your Account", "Your Password");
    messenger.Connect();
}


private void logoutButton_Click(object sender, System.EventArgs e)
    if (messenger.Connected)
    {
        messenger.Disconnect();
    }else{
        // Dude...
    }
}

```

One import thing here is the `messenger.Connected` property. This property does not indicate if you have signed in to your account but tells you whether MSNPSharp have connected with the MSN server (To sign in you need to connect to the server first, then run the user account authentication. You are signed in after you pass the authentication. But when you signed out, you must be disconnected from server).


## Monitoring the sign in failure ##

Not every sign in can be successful, you need to notify the user when sign in was failed. Generally speaking, there are three types of failures: Authentication error, which means wrong username password. Server errors, mostly due to MSN server maintenance, and general exception: other error like connect problem (note that sometimes after the authentication error was raised, a general exception will be issue as well because authentication was failed by bad internet connection).

To capture these three types of errors, you can listen to three events based on what you need.

```
    messenger.Nameserver.ExceptionOccurred += new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred);
    messenger.Nameserver.AuthenticationError += new EventHandler<ExceptionEventArgs>(Nameserver_AuthenticationError);
    messenger.Nameserver.ServerErrorReceived += new EventHandler<MSNErrorEventArgs>(Nameserver_ServerErrorReceived);
```


And the methods:

```

private void Nameserver_AuthenticationError(object sender, ExceptionEventArgs e)
{
    if (InvokeRequired)
    {
        BeginInvoke(new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred), new object[] { sender, e });
    }
    else
    {
        MessageBox.Show("Authentication failed, check your account or password.\r\n Error detail:\r\n " 
            + e.Exception.InnerException.Message + "\r\n"
            + " StackTrace:\r\n " + e.Exception.InnerException.StackTrace
            , "Authentication Error");
        Trace.WriteLine("Authentication failed");
    }
}


private void Nameserver_ExceptionOccurred(object sender, ExceptionEventArgs e)
{
    if (InvokeRequired)
    {
        BeginInvoke(new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred), new object[] { sender, e });
    }
    else
    {
        MessageBox.Show(e.Exception.ToString(), "Nameserver exception");
    }
}


private void Nameserver_ServerErrorReceived(object sender, MSNErrorEventArgs e)
{
    if (InvokeRequired)
    {
        BeginInvoke(new EventHandler<MSNErrorEventArgs>(Nameserver_ServerErrorReceived), new object[] { sender, e });
    }
    else
    {
        // when the MSN server sends an error code we want to be notified.
        MessageBox.Show("Error code: " + e.MSNError.ToString() + " (" + ((int)e.MSNError) + ")" +
            "\r\n\r\nDescription: " + e.Description, "Server error received");
        Trace.WriteLine("Server error received");
    }
}

```

Please do note that all these callback methods are not called in the UI thread.

# Getting Contact List #

After the `SignedIn` event was triggered, the contact list is not yet ready. MSNPSharp needs to request it via MSN server. After MSNPSharp retrieved the whole contact list, it will trigger `Messenger.ContactService.SynchronizationCompleted` event.

```
messenger.ContactService.SynchronizationCompleted += 
    new EventHandler<EventArgs>(ContactService_SynchronizationCompleted);
```

In `ContactService_SynchronizationCompleted` method, you can get all your contacts (`Windows Live/Facebook/Groups/LinkedIn/Email Contacts`) by iterating through `messenger.ContactList.All`. You can also get different types of contacts respectively by iterating `messenger.ContactList.WindowsLive/messenger.ContactList.Facebook` .. etc.

```
void ContactService_SynchronizationCompleted(object sender, EventArgs e)
{
    if (InvokeRequired)  // For UI operation only, remove if you don't need it.
    {
        BeginInvoke(new EventHandler<EventArgs>(ContactService_SynchronizationCompleted), sender, e);
        return;
    }

    for(Contact contact in messenger.ContactList.All)
    {
        // Do something here.
    }
}
```

When you got all these contacts, their online status were not valid at the moment.

# Getting Contact Status #

As mentioned in above section, contacts get right after sign in doesn't have valid online status. MSN must wait for the server to update contact status. MSNPsharp will trigger events when it received a status update, these events are `Messenger.Nameserver.ContactOnline` and `Messenger.Nameserver.ContactOffline`.

These events are straight forward:

```
messenger.Nameserver.ContactOnline += 
    new EventHandler<ContactStatusChangedEventArgs>(Nameserver_ContactOnline);
messenger.Nameserver.ContactOffline += 
    new EventHandler<ContactStatusChangedEventArgs>(Nameserver_ContactOffline);
```

Callback methods:

```
private void Nameserver_ContactOnline(object sender, ContactStatusChangedEventArgs e)
{
    Trace.WriteLine("Contact " + e.Contact.Account + " updated to " + e.Contact.Status);
}
```