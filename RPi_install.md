# RPi Installation

You will need to install the following development tools and runtimes:

1. You should have the lastest version of [Visual Studio](https://visualstudio.microsoft.com/downloads/) and the .NET Core SDK installed on your desktop where you will be compiling SysBot.NET.
2. You will also need the `ARM64` runtime. See below for instructions on installing the SDK on RPi
3. You will need [Python](https://www.python.org/downloads/) installed on your desktop (with unchanged/correct environment variables)

## Install Dotnet 5 SDK on RPi

1. Run `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel Current`

## Building SysBot.Pokemon.ConsoleApp

### On Windows Desktop

1. Open a command prompt window (WIN + r, type cmd.exe, press enter)
1. Run `cd <path-to-sporkbot-project>`
1. Run `.\Build\build.bat`
1. Using a file transfer application such as WinSCP, copy the newly created `<path-to-sporkbot-project>\publish` directory to your Raspberry Pi.

### On RPi

1. From the command line, run `cd <path-to-publish-directory>` 
1. Run `chmod +x SysBot.Pokemon.ConsoleApp` to make the program executable
1. You will need to run the application once to generate the apps config.json file. Run `./SysBot.Pokemon.ConsoleApp` in the publish directory and after you see `Starting up...` printed in the terminal, hit `CTRL+c` to stop the program.
1. You can now open and edit the `config.json` file in a text editor and update it with your personal customizations.
1. Once done editing the configuration file, you can now run the application again with the same command (`./SysBot.Pokemon.ConsoleApp`).
1. You should now start seeing output from the application in the terminal indicating that it is starting to do **things**!
