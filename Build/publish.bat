@ECHO OFF

cd SysBot.Pokemon.ConsoleApp\

dotnet clean
dotnet restore
dotnet publish --configuration release --framework net5.0 --runtime linux-arm64 --output ..\publish

cd ..