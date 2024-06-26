# Oxygen
Asset server and multi-user level editor

# Features
* Storage and retrival of assets with versioning history
* Audit trail of events e.g. creating users, login history
* Rich user permissioning model
* Metric collection framework
* Command line tool to automate tasks
* Task scheduling using a plugin system e.g. baking assets

# Components
* Oxygen.exe - The oxygen server component
* O2.exe - The oxygen command line tool
* libOxygen - A C++ static library to communicate with the Oxygen server to embed within engines or games. This has a dependancy on LibCrypto (OpenSSL).

# Initial setup
For initial setup run 'Oxygen.exe setup' or 'dotnet Oxygen.dll setup'. You will then be prompted to create a root user. Once initial setup is complete, run 'Oxygen.exe' or 'dotnet Oxygen.dll' to start the server. To test the root user has been created correctly, run 'O2.exe login' or 'dotnet O2.dll login' and enter the username/password and check the login is successful.

A demo user group is created upon setup with some permissions assigned to try out Oxygen. Using the root user create a new user called demo and add them to the demo user group.
* o2 user create
* o2 group add demo demo

# O2 command line application
Some common example operations:
* o2 asset list - lists the assets which are stored on the server
* o2 asset upload myAsset.png - uploads the asset called myAsset.png to the asset server
* o2 asset download myAsset.png - downloads the asset called myAsset.png from the asset server
* o2 asset patch - downloads all the latest assets from the server

# Plugins
Plugins can be defined server side using a JSON file which is placed within a 'Plugins' directory.
```
{
    "Plugins": [
        {
            "Name": "Baking plugin",
            "Package": "SpriteSheetGenerator",
            "ManualStart": true,
            "Type": "Bake",
            "Filter": "*.png",
            "Time": "17:12:00",
            "Actions": [
                {
                    "Run": "run.bat"
                },
                {
                    "Run": "SpriteSheetGenerator.exe"
                }
             ],
             "Artefacts": [
                "Baked/Tileset.png",
                "Baked/Tileset.mat"
             ]
        }
    ]
}
```

This plugin has been defined to run two actions: 'run.bat' and 'SpriteSheetGenerator.exe'. It has been defined to start at '17:12:00', but it is allowed to be started manually. It has been set to type 'Bake' which will copy the assets into the working area for the execution of the plugin. The Artefacts section indicates which files should be output of the execution. These files would become available for download.

# libOxygen
This is designed to be integrated directly into games and engines. For example a connection can be created and a login message sent to the server.
```
Oxygen::ClientConnection* conn = new Oxygen::ClientConnection(hostname, Oxygen::DEFAULT_PORT);
conn->Logon(username, password);
conn->LogonHandler([this](int code, const std::string& text)
{
    if (code != 0)
    {
        std::cout << code << " " << text << std::endl;
    }
});
```
Once logged into the server. Assets can then be uploaded/downloaded/etc and the levels can be joined/created/etc. For levels once joined, the object stream and event streams can be subscribed to, to recieve updates in response to objects being added to the level and other users moving their cursors onto objects. More examples can be found in Samples directory.

## Using low level subscribers.
```
Oxygen::Message request("ASSET_SVR", "ASSET_LIST");
std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
conn->AddSubscriber(sub);
```
We can hook responses to the subscriber and then unhook when we are done.
```
sub->Signal([this, &assets, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& msg) {
    if (msg.ReadString() == "NACK")
    {
        std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
    }
    else
    {
        int numAssets = msg.ReadInt32();
        for (int i = 0; i < numAssets; i++)
        {
            assets.push_back(msg.ReadString());
        }
    }
    conn->RemoveSubscriber(sub2);
});
```

