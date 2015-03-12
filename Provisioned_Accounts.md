# About Provisioned accounts #

Provisioned accounts are special type of accounts which was made by Microsoft before August 2008 specially for Bots.
A normal Windows Live ID has a limitation of 1000 users in its contact list. When an account is provisioned, it does not have that limitation.
This type of accounts doesn't support block/allow lists, because everybody is able to communicate with such WLID by default.

For now Microsoft stopped providing provisioned accounts, but most probably they will continue this in the nearly future.

# Implementation #

You can login to any account in MSNPSharp without any additional settings.

However, if your bot has a lot of buddies in contact list, it might be better if you will avoid loading of contact list and working with it, because bot doesn't need his contact list, he only need to reply to incoming messages.

```

messenger = new Messenger();
messenger.Credentials = new Credentials("bot@domain.com", "password", MsnProtocol.MSNP18);
messenger.Nameserver.BotMode = true;
messenger.Nameserver.AutoSynchronize = false;

messenger.Nameserver.SignedIn += (Nameserver_SignedIn);
```

That will make MSNPSharp don't use Address Book anymore, and by default `AutoSynchronize` is turned on.
That will also restrict access to `StorageService` and `ContactService`, as it is not needed if you don't work with Address Book.

You will also need to put the following code on `SignedIn` event:
```

void Nameserver_SignedIn(object sender, EventArgs e)
{
    // Set initial status
    messenger.Owner.Status = PresenceStatus.Online;

    // Set display name and personal message
    messenger.Nameserver.SetScreenName("MyBot");
    messenger.Nameserver.SetPersonalMessage(new PersonalMessage("I like MSNPSharp", MediaType.None, null, NSMessageHandler.MachineGuid));

    // Set display picture
    DisplayImage displayImage = new DisplayImage();
    displayImage.Image = Image.FromFile("avatar.png");
    messenger.StorageService.UpdateProfile(displayImage.Image, "MyPhoto");
}
```