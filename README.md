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

## Usage Tips

There are some useful shell scripts in utils folder. 
For example, if you want to process the [2y4q](https://www.rcsb.org/structure/2Y4Q) and [2rv5](https://www.rcsb.org/structure/2RV5) PDB mols with HCProtCLI in Linux, you can fallow this steps:

```bash
cp utils/* HCProtCLI/release 
cd HCProtCLI/release
bash ./getPDB.sh 2y4q 2rv5
mkdir mols
mv *.ent mols
mkdir processedMols
bash ./HCProtProcess.sh mols processedMols
```
and now you has the 2y4q and 2rv5 PDB files ordained in processedMols files.
The HCProtProcess.sh script makes the preprocessing files used by default in [MolecularConformation.jl](https://github.com/evcastelani/MolecularConformation.jl) julia package. This is made by using the `-t 5 --order --bp` HCProtCLI parameters. 
