# Welcome to the HCProt project

 This project has the objective to facilitate the read of files extracted of Protein Data Bank.

## .NET Core
Maybe you will have to install the .Net Core in your system.
### Linux
In arch, you only need to install `dotnet-runtime` package. Do it with pacman as follow 
```bash
sudo pacman -S dotnet-runtime
```

For other distros see https://docs.microsoft.com/en-us/dotnet/core/install/linux 

### Windows
See this tutorial: https://docs.microsoft.com/pt-br/dotnet/core/install/windows?tabs=net50

## Basic usage
You can find the `HCProtCLI` executable file in HCProtCLI/release/ folder.
Thus, in linux, just type
```bash
cd HCProtCLI/release
./HCProtCLI
```
to view a simple usage help.

In windows, you can run the application by using the `HCProtCLI.dll` file, also finding in HCProtCLI/release folder.

Just open the powershell in the HCProtCLI/release folder and type
```bash
dotnet HCProtCLI.dll
```

to view the usage help.
