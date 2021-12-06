# Z3 For RPi 4

## Building Z3 for dotnet and arm64

### On your desktop

1. Download and unzip the [SysBot.NET source](https://github.com/kwsch/SysBot.NET/archive/master.zip)
1. Download and unzip the latest version of [Z3](https://github.com/Z3Prover/z3/archive/master.zip)
1. In the SysBot.NET-master folder, open SysBot.Net.sln with Visual Studio.
1. Open a command prompt window. On Windows this is cmd and on Mac this is terminal, unless you are using another CLI.
1. cd to wherever you've downloaded and unzipped Z3. On my device, I entered `cd C:\Users\Berichan\Downloads\z3-master` but this will be different for you unless you are also berichan.
1. Run `python scripts\mk_make.py --dotnet` or `*path/to/python.exe* scripts\mk_make.py --dotnet` depending on how you've set up python. Wait for it to generate the makefile.
1. The output window will end with something like this.

    To build Z3, open a Visual Studio Command Prompt, then type `cd C:\Users\Berichan\Downloads\z3-master\build && nmake`

1. Go back to Visual Studio, open the Developer CLI by going to Tools > Command Line > Developer Command Prompt and enter the command the Z3 output window asked you to enter. In my case, I entered `cd C:\Users\Berichan\Downloads\z3-master\build && nmake`. Do the next step while you are waiting for Z3 to compile on your desktop.
  
### On your Raspberry Pi

1. [Download and install git](https://linuxize.com/post/how-to-install-git-on-raspberry-pi/) and [Python](https://projects.raspberrypi.org/en/projects/generic-python-install-python3#linux) if you haven't already done so. My Raspbian already had these installed but I don't know if it will change one day.
1. Clone the Z3 source to your pi `git clone https://github.com/Z3Prover/z3.git` then cd into the newly made folder where the z3 source resides.
1. [Follow the steps on the Z3 page](https://github.com/Z3Prover/z3#building-z3-using-make-and-gccclang) to compile and install Z3 using gcc (gcc comes with Raspbian but is easily installed with apt if not). Make sure you also run `sudo make install` at the end! It's only 4 commands so I haven't bothered going into detail or copying the guide here.

Building Z3 on your pi will take a while so do the next few steps while this is happening.

### On your desktop again

1. Once Z3 has finished building the dotnet version on your desktop, close the Visual Studio Command Prompt.
1. Right-click the SysBot.Pokemon.Z3 project and Select "Manage NuGet Packages"
1. In the top-right corner of the NuGet Package Manager, select the gear to open NuGet options, click the green "+" button to add a new package source, click the ... button and select the folder where z3 was built, for me this was `C:\Users\Berichan\Downloads\z3-master\build` then click Update in the options window, followed by OK.
1. In the NuGet window again, in the top-right dropdown where it says "Package source" select the new Package source we just created (should just be called `Package source` in the dropdown if this in your only one)
1. Hit Browse in the top left. You should see "Microsoft.Z3", click it, then in the right window hit "Install" and press OK on any warnings.
1. Right-click the SysBot.Pokemon.Z3 project and Select "build". It should build the SysBot.Pokemon.Z3 library without any issues.
