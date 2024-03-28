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

# O2 command line application
Some common example operations:
* o2 asset list - lists the assets which are stored on the server
* o2 asset upload myAsset.png - uploads the asset called myAsset.png to the asset server
* o2 asset download myAsset.png - downloads the asset called myAsset.png from the asset server
* o2 asset patch - downloads all the latest assets from the server
