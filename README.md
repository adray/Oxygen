# Oxygen
Asset server and multi-user level editor

# Features
* Storage and retrival of assets with versioning history
* Audit trail of events e.g. creating users, login history
* Rich user permissioning model
* Command line tool to automate tasks

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

# libOxygen
This is designed to be integrated directly into games and engines. For example a connection can be created and a login message sent to the server.
```
  Oxygen::ClientConnection* conn = new Oxygen::ClientConnection(hostname, Oxygen::DEFAULT_PORT);
  Oxygen::Message request("LOGIN_SVR", "LOGIN");
  request.WriteString(username);
  conn->HashPassword(password, request);
  request.Prepare();
  std::shared_ptr<Oxygen::Subscriber> logSub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
  conn->AddSubscriber(logSub);
```
We can hook responses to the subscriber and then unhook when we are done.
```
logSub ->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(logSub)](Oxygen::Message& msg) {
    conn->RemoveSubscriber(sub2);
    if (msg.ReadString() == "NACK")
    {
        std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
    }
    });
```
Once logged into the server. Assets can then be uploaded/downloaded/etc and the levels can be joined/created/etc. For levels once joined, the object stream and event streams can be subscribed to, to recieve updates in response to objects being added to the level and other users moving their cursors onto objects. More examples can be found in Samples directory.
