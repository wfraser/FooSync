FooSync
=======

FooSync is a filesystem directory synchronizer. It's specifically designed to
support a repository shared by multiple computers. This repository can be used
as a backup and/or to keep multiple computers synchronized.

It's written in C#/.NET, and designed as a console app, GUI app, and network
service.

The console app and network service are **designed to work in Linux with the
Mono runtime**, as well as Windows with the .NET runtime (version 4.0 or
higher). The GUI version only works under Windows, as WPF is not supported by
Mono.

"Windows support" means any version capable of running .NET 4.0, which
currently means **Windows XP, Vista, 7, and 8**, and the server versions based
off of those.

Status
------

FooSync is in the very early stages of development. The command line client
works for limited uses (specifically syncing local paths or mounted Windows
shares) but requires manually writing config .xml files, and isn't user
friendly yet.

The WPF GUI is being actively worked on.

The first WPFApp was mostly completed, and works for syncing local directories
and mounted Windows shares, just like the command-line client. The wizards for
editing the config files are unfinished, but with some light editing of .XML
config files, it does work, and I've been using it for my day-to-day needs while
working on the redesign. It lives in a branch called 'old-WPFApp'.

The new redesign in progress extends functionality to syncing with remote servers in a many-to-many ("M:M") fashion. This code doesn't work yet.

To support this, a network server (FooSync.Daemon) is in progress. It can run
as a Windows service, or as a simple process (it can be backgrounded and
detached to run as a daemon in Linux). It is also not complete; it currently
is read-only.

WPFApp version 1 will be deleted once WPFApp2 (and the network server and any
supporting changes in the engine library) is completed. 
