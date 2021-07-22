#!/bin/sh

# This is a script to process a sequence of PDB files (.ent) in a folder (input_folder) with HCProtCLI and write results in other folder (output_folder).

# Make sure that the HCProtCLI and all config files are in the same folder that this script. 
# This files can be obtained in /HCProtCLI/realese folder of https://github.com/caomem/HCProtCLI repository.

if [ -d "${1}" ]; then
	if [ -d "${2}" ]; then
		for file in $1/*.ent; do
			fileName=${file#$1/}			
			echo " ++" Processing ${fileName%.ent}.nmr
			./HCProtCLI $file $2/${fileName%.ent}.nmr -t 5 --order --bp --quiet
			if [ -f $2/${fileName%.ent}.nmr ]; then
				echo " ==" Processing ${fileName%.ent}.xyz
				./HCProtCLI $file $2/${fileName%.ent}.xyz -t 4 --order --quiet
			else
				echo " **" Error with ${fileName%.ent}.nmr. Skipping.	
			fi
		done
	else
		echo erro: output folder $2 dont exist.
		echo usage: $0 input_folder output_folder 	
	fi
else
	echo error: input folder $1 dont exist.
	echo usage: $0 input_folder output_folder
fi