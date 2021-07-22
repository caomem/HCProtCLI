#!/bin/sh

# This is a script to download PDB .ent files in ftp wwpdb repository.
# To get some files just pass their PDB names as parameter (e.g., ./getPDB.sh 2mjg 2mij ... )

# or, to get a list of files, you can pass a input file contain all the links of pdb files separated by new line. 
# For example, use ./getPDB.sh -f mols.txt

# where mols.txt contains:
# https://ftp.wwpdb.org/pub/pdb/data/structures/divided/pdb/a9/pdb1a93.ent.gz
# https://ftp.wwpdb.org/pub/pdb/data/structures/divided/pdb/b4/pdb1b4c.ent.gz
# ...
# https://ftp.wwpdb.org/pub/pdb/data/structures/divided/pdb/ba/pdb1ba5.ent.gz

# and this script will try generate a subfolder mols.txt_mols with the all .ent files.

if [[ "$1" == "-f" ]]; then
	filename="$2"
	if [ ! -d "${filename}_mols" ]; then
		 
		mkdir ${filename}_mols
		cd ${filename}_mols
			
		wget -i "../${filename}" -q --show-progress
		
		for file in *; do
			gzip -d $file
		done
		
		cd ..
	else
		echo "folder ${filename}_mols already exists."
		echo usage: $0 -f input_file
		echo or $0 mol_1_name mol_2_name ... mol_n_name
	fi

else
	for mol in "$@" 
	do
		lowermol="${mol,,}"
		if [ ! -f "pdb${lowermol}.ent" ]; then 
			wget https://ftp.wwpdb.org/pub/pdb/data/structures/divided/pdb/${lowermol:1:2}/pdb${lowermol}.ent.gz -q --show-progress
			if [[ "$?" == 0 ]]; then
				gzip -d pdb${lowermol}.ent.gz
			else
				echo pdb${lowermol} mol not found in WWPDB ftp.			
			fi
		else 
			echo "skipping ${lowermol}, file already exists."
		fi	
	done
fi