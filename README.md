.Net standard binding for Frida (https://www.frida.re) , with RPC calls support

Tested on:
 * Windows 10 (X64)
 * OSX

# Use it

See [TestProgram/Program.cs](TestProgram/Program.cs) for an example


# Build it

Clone the Frida.NetStandard directory
```
$ git clone https://github.com/lenacloud/frida-netstandard
```

## ... on Windows
Then, enter the `frida-netstandard` folder, and download dependencies running the script below (downloads Frida release binaries & extracts them) 

```
$ git clone 
$ powershell
PS> Set-ExecutionPolicy -Scope Process -ExecutionPolicy Unrestricted
PS> .\setup.ps1
```

Then: 
1) open "Frida.NetStandard.sln"
2) build Fria.Exports.vcxproj
3) Run TestProgram.ccsproj : It should pop a notepad, and inject it.


# Distribute it

You __must__ distribute and load the correct Frida.Exports library yourself, depending on the platform you are running on (see [TestProgram/Program.cs](TestProgram/Program.cs)).

Frida being a quite big library, this behaviour is indended to force you to chose which platform/architecture you are willing to distribute.