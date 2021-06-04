using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace HCProtCLI
{
    class Program
    {
        #region Components

        /// <summary>
        /// List of atoms extracted of pdb file
        /// </summary>
        static List<Atom> atoms = new List<Atom>();

        #endregion

        static void Main(string[] args)
        {
            
            double distance = 5;
            string atom = "", resName = "";
            int resId = 0, type = 0;
            bool isHcOrder = false, isToy = false, isBP = false, isDiscrete = false, isQuiet = false;
            try
            {
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] == "--order") isHcOrder = true;
                    else if (args[i] == "--toy") isToy = true;
                    else if (args[i] == "--bp") isBP = true;
                    else if (args[i] == "--discrete") isDiscrete = true;
                    else if (args[i] == "--quiet") isQuiet = true;
                    else if (i + 1 != args.Length)
                    {
                        if (args[i] == "-t") type = int.Parse(args[i + 1]);
                        else if (args[i] == "-d") distance = double.Parse(args[i + 1]);
                        else if (args[i] == "-a") atom = args[i + 1];
                        else if (args[i] == "-r") resName = args[i + 1];
                        else if (args[i] == "-R") resId = int.Parse(args[i + 1]);
                        else
                        {
                            usage();
                            return;
                        }
                        i++;
                    }
                    else
                    {
                        usage();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Param error: " + ex.ToString());
                usage();
                return;
            }

            if (args.Length >= 2)
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine("Error: File " + args[0] + " not exists");
                    return;
                }

                if (File.Exists(args[1]))
                {
                    if (isQuiet) return;
                    Console.Write("File " + args[1] + " already exists, you want to overwrite? (y,n): ");
                    if (Convert.ToChar(Console.Read()) != 'y') return;
                }
            }
            else
            {
                usage();
                return;
            }

            //Console.WriteLine(type.ToString() + " " + isHcOrder.ToString() + " " + distance.ToString() + " " + atom.ToString() + " " + resName.ToString() + " " + resId.ToString() + " " + isToy.ToString() + " " + isBP.ToString() + " " + isDiscrete.ToString());

            convert(args[0], args[1], type, isHcOrder: isHcOrder, distance: distance, filter: atom, resName: resName, resId: resId, isToy: isToy, isBP: isBP, isDiscrete: isDiscrete, isQuiet: isQuiet);
        }

        /// <summary>
        /// This is a very simple function to inform the correct utilization of HCProt CLI
        /// </summary>
        static void usage()
        {
            Console.WriteLine(" usage: dotnet run INPUT_PDB_FILE OUTPUT_FILE [OPTIONS]");
            Console.WriteLine("\n Some options:");
            Console.WriteLine("\t -t OUT_TYPE");
            Console.WriteLine("\t\t Specify the output type file. OUT_TYPE must be a integer, where");
            Console.WriteLine("\t\t\t 0: XML (default)");
            Console.WriteLine("\t\t\t 1: MATLAB .m");
            Console.WriteLine("\t\t\t 2: JSON ");
            Console.WriteLine("\t\t\t 3: old MolecularConformation.jl");
            Console.WriteLine("\t\t\t 4: XYZ");
            Console.WriteLine("\t\t\t 5: MD-jeep");
            Console.WriteLine("\t\t\t 6: Virtual order path");
            Console.WriteLine("\t -d DISTANCE");
            Console.WriteLine("\t\t Specify distance value. DISTANCE must be an integer value if --discrete option is set or a double if not. Default value is 5.");
            Console.WriteLine("\t -a ATOM");
            Console.WriteLine("\t\t Specify an atom filter value. ATOM must be an string, e.g., H");
            Console.WriteLine("\t -r RESIDUE");
            Console.WriteLine("\t\t Specify the residue name filter. RESIDUE must be an string, e.g., GLY");
            Console.WriteLine("\t -R RESIDUEID");
            Console.WriteLine("\t\t Specify the residue id filter. RESIDUEID must be an integer");
            Console.WriteLine("\t --order");
            Console.WriteLine("\t\t Use to order the molecule using the hc order");
            Console.WriteLine("\t --toy");
            Console.WriteLine("\t\t Use to accept repeated atoms in input file");
            Console.WriteLine("\t --bp");
            Console.WriteLine("\t\t Use to indicate that output must be prepared as a Branch&Prune algorith input");
            Console.WriteLine("\t --discrete");
            Console.WriteLine("\t\t Use to indicate that distance metric must be discrete");
            Console.WriteLine("\t --quiet");
            Console.WriteLine("\t\t Use to supress warnings and results output text and to automaticaly recuse overwrite files.");
        }

        /// <summary>
        /// Convert selected PDB file to Distances file
        /// </summary>
        static void convert(string pdbFile, string outputFileName, int outputType, double distance = 5, string filter = "", string resName = "", int resId = 0, bool isToy = false, bool isBP = false, bool isHcOrder = false, bool isDiscrete = false, bool isQuiet = false)
        {
            atoms.Clear();
            if (String.IsNullOrWhiteSpace(pdbFile))
            {
                Console.WriteLine("Error: Please select a valid PDB file");
                return;
            }
            string[] text = null;
            try
            {
                text = System.IO.File.ReadAllLines(pdbFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when read the file \n" + ex.ToString());
                return;
            }

            List<Exception> exceptions = new List<Exception>();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i].StartsWith("ATOM  ") || text[i].StartsWith("HETATM"))
                {
                    if (!string.IsNullOrWhiteSpace(filter) && !text[i].Substring(12, 4).Contains(filter)) continue;
                    if (!string.IsNullOrWhiteSpace(resName) && !text[i].Substring(17, 3).Contains(resName)) continue;
                    Atom a = new Atom();
                    try
                    {
                        a.Serial = int.Parse(text[i].Substring(6, 5));
                        if (!isToy) // Accept repeted atoms in toy problems
                            foreach (var aux in atoms) //don't repeat atoms (other models will also not be read)
                            {
                                if (a.Serial == aux.Serial) goto end;
                            }
                        a.Name = text[i].Substring(12, 4).Trim();
                        a.AltLoc = text[i].Length > 16 ? text[i][16] : ' ';
                        a.ResName = text[i].Substring(17, 3);
                        a.ChainID = text[i].Length > 21 ? text[i][21] : ' ';
                        a.ResSeq = int.Parse(text[i].Substring(22, 4));
                        a.ICode = text[i].Length > 26 ? text[i][26] : ' ';
                        a.X = double.Parse(text[i].Substring(30, 8), new CultureInfo("en-US"));
                        a.Y = double.Parse(text[i].Substring(38, 8), new CultureInfo("en-US"));
                        a.Z = double.Parse(text[i].Substring(46, 8), new CultureInfo("en-US"));
                        a.Occupancy = double.Parse(text[i].Substring(54, 6), new CultureInfo("en-US"));
                        a.TempFactor = double.Parse(text[i].Substring(60, 6), new CultureInfo("en-US"));
                        a.Element = text[i].Substring(76, 2).Trim();
                        a.Charge = text[i].Substring(78, 2).Trim();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        continue;
                    }
                    if (resId == 0 || resId == a.ResSeq) atoms.Add(a);
                }
            end: { }
            }
            {
                int i = 0;
                foreach (var ex in exceptions)
                {
                    Console.WriteLine("Error " + ((++i).ToString()) + " when list the atoms in file\n" + ex.ToString());
                }
            }

            if (isHcOrder)
            {
                if (atoms.Count == 0) return;
                List<Atom> chainReOrder = new List<Atom>(), hetAtoms = new List<Atom>(), currentResidue = new List<Atom>(), previousResidue = new List<Atom>();
                int currentRes = atoms.First().ResSeq;
                bool ended = false;
                foreach (var atom in atoms)
                {
                    if (atom.ResSeq == currentRes)
                    {
                        currentResidue.Add(atom);
                        if (atoms.Last() != atom)
                        {
                            continue;
                        }
                    }
                    try
                    {
                        AminoAcidName amino = currentResidue.First().GetAminoAcidName();
                        if (currentResidue.Find(a => a.Name == AminoAcidAtom.H2.GetPDBEquivalent()) != null)
                        {
                            Atom aux;
                            if ((aux = currentResidue.Find(a => a.Name == AminoAcidAtom.N.GetPDBEquivalent())) != null)
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem " + AminoAcidAtom.N.GetPDBEquivalent());
                            if ((amino == AminoAcidName.Proline && (aux = currentResidue.Find(a => a.Name == "HD2")) != null)
                                || ((aux = currentResidue.Find(a => a.Name == "H" || a.Name == "H1")) != null))
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem H");
                            if ((aux = currentResidue.Find(a => a.Name == AminoAcidAtom.H2.GetPDBEquivalent())) != null)
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem " + AminoAcidAtom.H2.GetPDBEquivalent());
                            if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem CA");
                            if ((aux = currentResidue.Find(a => a.Name == AminoAcidAtom.N.GetPDBEquivalent())) != null)
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem " + AminoAcidAtom.N.GetPDBEquivalent());
                            if ((amino == AminoAcidName.Glycine && (aux = currentResidue.Find(a => a.Name == "HA2")) != null)
                                || (aux = currentResidue.Find(a => a.Name == "HA")) != null)
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem HA");
                            if ((aux = currentResidue.Find(a => a.Name == "C")) != null)
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem C");
                            if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                chainReOrder.Add(aux);
                            else throw new Exception("Não tem CA");
                        }
                        else
                        {
                            if (chainReOrder.Count == 0) throw new Exception("Beginning of molecule is not good defined, please, verify the PDB instance");
                            if (currentResidue.Find(a => a.Name == "OXT") != null)
                            {
                                Atom aux;
                                if ((amino == AminoAcidName.Proline && (aux = currentResidue.Find(a => a.Name == "HD2")) != null)
                                    || (aux = currentResidue.Find(a => a.Name == "H")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem H");
                                if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem CA");
                                if ((aux = previousResidue.Find(a => a.Name == "O")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem O previo");
                                if ((aux = currentResidue.Find(a => a.Name == AminoAcidAtom.N.GetPDBEquivalent())) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem " + AminoAcidAtom.N.GetPDBEquivalent());
                                if ((amino == AminoAcidName.Proline && (aux = currentResidue.Find(a => a.Name == "HD2")) != null)
                                    || (aux = currentResidue.Find(a => a.Name == "H")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem H");
                                if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem CA");
                                if ((aux = currentResidue.Find(a => a.Name == AminoAcidAtom.N.GetPDBEquivalent())) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem " + AminoAcidAtom.N.GetPDBEquivalent());
                                if ((amino == AminoAcidName.Glycine && (aux = currentResidue.Find(a => a.Name == "HA2")) != null)
                                    || (aux = currentResidue.Find(a => a.Name == "HA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem HA");
                                if ((aux = currentResidue.Find(a => a.Name == "C")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem C");
                                if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem CA");
                                if ((aux = currentResidue.Find(a => a.Name == "OXT")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem OXT");
                                if ((aux = currentResidue.Find(a => a.Name == "C")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem C");
                                if ((aux = currentResidue.Find(a => a.Name == "O")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem O");
                                ended = true;
                            }
                            else
                            {
                                Atom aux;
                                if ((amino == AminoAcidName.Proline && (aux = currentResidue.Find(a => a.Name == "HD2")) != null)
                                    || (aux = currentResidue.Find(a => a.Name == "H")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem H");
                                if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem CA");
                                if ((aux = previousResidue.Find(a => a.Name == "O")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem O previo");
                                if ((aux = currentResidue.Find(a => a.Name == AminoAcidAtom.N.GetPDBEquivalent())) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem " + AminoAcidAtom.N.GetPDBEquivalent());
                                if ((amino == AminoAcidName.Proline && (aux = currentResidue.Find(a => a.Name == "HD2")) != null)
                                    || (aux = currentResidue.Find(a => a.Name == "H")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem H");
                                if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem CA");
                                if ((aux = currentResidue.Find(a => a.Name == AminoAcidAtom.N.GetPDBEquivalent())) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem " + AminoAcidAtom.N.GetPDBEquivalent());
                                if ((amino == AminoAcidName.Glycine && (aux = currentResidue.Find(a => a.Name == "HA2")) != null)
                                    || (aux = currentResidue.Find(a => a.Name == "HA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem HA");
                                if ((aux = currentResidue.Find(a => a.Name == "C")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem C");
                                if ((aux = currentResidue.Find(a => a.Name == "CA")) != null)
                                    chainReOrder.Add(aux);
                                else throw new Exception("Não tem CA");
                            }
                        }
                    }
                    catch (Exception er)
                    {
                        if (resId != 0)
                        {
                            Console.WriteLine("Error: Dont's possible to calc the HC Order for a non padronized protein.\n Please, dont use the HC Order with Res filter.");
                            return;
                        }
                        else if (ended)
                        {
                            if (!isQuiet) Console.WriteLine("Warning: The reorder is completed, but an error occurs.\n This molecule has het atoms?\n" + er.Message);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("HC Order calc Error\n"+er.Message);
                            return;
                        }
                    }

                    previousResidue = new List<Atom>(currentResidue);
                    currentResidue.Clear();
                    currentRes = atom.ResSeq;
                    currentResidue.Add(atom);
                }
                atoms = new List<Atom>(chainReOrder);
                for (int i = 3; i < atoms.Count; i++)
                {
                    if (Math.Pow(atoms.ElementAt(i - 3).Distance(atoms.ElementAt(i - 2)) + atoms.ElementAt(i - 2).Distance(atoms.ElementAt(i - 1)) - atoms.ElementAt(i - 3).Distance(atoms.ElementAt(i - 1)), 2) < 0.001
                        || atoms.ElementAt(i - 3).Distance(atoms.ElementAt(i - 1)) > atoms.ElementAt(i - 3).Distance(atoms.ElementAt(i - 2)) + atoms.ElementAt(i - 2).Distance(atoms.ElementAt(i - 1)))
                    {
                        Console.WriteLine("Error!\n Atoms " + (i - 3) + ", " + (i - 2) + " and " + (i - 1) + " are coplanar\n\t d(i-3,i-1) <= d(i-3,i-2) + d(i-2,i-1) => " +
                            atoms.ElementAt(i - 3).Distance(atoms.ElementAt(i - 1)) + " <= " + atoms.ElementAt(i - 3).Distance(atoms.ElementAt(i - 2)) + " + " + atoms.ElementAt(i - 2).Distance(atoms.ElementAt(i - 1)) +
                            "\n" + atoms.ElementAt(i - 3) + "\n" + atoms.ElementAt(i - 1));
                        return;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(outputFileName)) return;
            if (outputType == 0) //XML
            {
                if (isBP)
                {
                    if (!isQuiet) Console.WriteLine("Warning: BP is not implemented for XML");
                }
                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    XmlSerializer serializer = new XmlSerializer(atoms.GetType());
                    using (MemoryStream stream = new MemoryStream())
                    {
                        serializer.Serialize(stream, atoms);
                        stream.Position = 0;
                        xmlDocument.Load(stream);
                        xmlDocument.Save(outputFileName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (outputType == 1) // MATRIX
            {
                if (isBP)
                {
                    List<Atom> atomsSingle = new List<Atom>();
                    for (int i = 0; i < atoms.Count; i++)
                    {
                        if (atoms.IndexOf(atoms.ElementAt(i)) < i) continue;
                        atomsSingle.Add(atoms.ElementAt(i));
                    }
                    StringBuilder file = new StringBuilder();
                    file.Append("E = [");
                    bool first = true;
                    foreach (Atom ai in atoms)
                    {
                        if (first) first = false; else file.Append(";");
                        foreach (Atom aj in atoms)
                        {
                            if (isValidDistance(ai, aj, distance, isDiscrete))
                                //file.Append(" {" + atomsSingle.IndexOf(ai) + ", " + atomsSingle.IndexOf(aj) + " " + ai.Distance(aj).ToString(new CultureInfo("en-US")) + "}");
                                file.Append(" " + ai.Distance(aj).ToString(new CultureInfo("en-US")));
                            else file.Append(" 0");
                        }
                    }
                    file.Append("]");
                    File.WriteAllText(outputFileName, file.ToString());
                }
                else
                {
                    StringBuilder file = new StringBuilder();
                    file.Append("PDB = [");
                    bool first = true;
                    foreach (Atom a in atoms)
                    {
                        if (first) first = false; else file.Append(";");
                        file.Append(a.X.ToString(new CultureInfo("en-US")) + " " + a.Y.ToString(new CultureInfo("en-US")) + " " + a.Z.ToString(new CultureInfo("en-US")));
                    }
                    file.Append("]");
                    File.WriteAllText(outputFileName, file.ToString());
                }
            }
            else if (outputType == 2) // JSON
            {
                if (isBP)
                {
                    List<Atom> atomsSingle = new List<Atom>();
                    for (int i = 0; i < atoms.Count; i++)
                    {
                        if (atoms.IndexOf(atoms.ElementAt(i)) < i) continue;
                        atomsSingle.Add(atoms.ElementAt(i));
                    }
                    StringBuilder file = new StringBuilder();
                    file.Append("{\"BP\":{");
                    file.Append("V:[");
                    bool first = true;
                    foreach (Atom a in atoms)
                    {
                        if (first) first = false; else file.Append(",");
                        file.Append("{\"Vi\":\"" + "v" + atomsSingle.IndexOf(a) + "\"}");
                    }
                    file.Append("],E:[");
                    first = true;
                    int current = 0;
                    for (int i = 0; i < atomsSingle.Count; i++)
                    {
                        current = atoms.IndexOf(atomsSingle.ElementAt(i));
                        for (int j = 1; j < 4 && i - j >= 0; j++)
                        {
                            if (first) first = false; else file.Append(",");
                            file.Append("{\"v" + i + ",v" + atomsSingle.IndexOf(atoms.ElementAt(current - j)) +
                                "\":" + atomsSingle.ElementAt(i).Distance(atoms.ElementAt(current - j)).ToString(new CultureInfo("en-US")) + "}");
                        }
                    }
                    file.Append("]}}");
                    File.WriteAllText(outputFileName, file.ToString());
                }
                else
                {
                    StringBuilder file = new StringBuilder();
                    file.Append("{\"atoms\":[");
                    bool first = true;
                    foreach (Atom a in atoms)
                    {
                        if (first) first = false; else file.Append(",");
                        file.Append("{\"name\":\"" + a.Name + "\",\"position\":{\"x\":" + a.X.ToString(new CultureInfo("en-US")) + ",\"y\":" + a.Y.ToString(new CultureInfo("en-US")) + ",\"z\":" + a.Z.ToString(new CultureInfo("en-US")) + "}}");
                    }
                    file.Append("]}");
                    File.WriteAllText(outputFileName, file.ToString());
                }
            }
            else if (outputType == 3) // MOLECULAR CONFORMATION
            {
                if (isBP)
                {
                    StringBuilder file = new StringBuilder();
                    bool first = true;
                    foreach (Atom ai in atoms)
                    {
                        if (first) first = false; else file.Append("\r\n");
                        bool first2 = true;
                        foreach (Atom aj in atoms)
                        {
                            if (first2) first2 = false; else file.Append("\t");
                            file.Append(ai.Distance(aj).ToString(new CultureInfo("en-US")));
                        }
                    }
                    File.WriteAllText(outputFileName, file.ToString());
                }
                else
                {
                    StringBuilder file = new StringBuilder();
                    bool first = true;
                    foreach (Atom a in atoms)
                    {
                        if (first) first = false; else file.Append("\r\n");
                        file.Append(a.X.ToString(new CultureInfo("en-US")) + " " + a.Y.ToString(new CultureInfo("en-US")) + " " + a.Z.ToString(new CultureInfo("en-US")));
                    }
                    File.WriteAllText(outputFileName, file.ToString());
                }
            }
            else if (outputType == 4) // XYZ
            {
                if (isBP)
                {
                    Console.WriteLine("Error: BP is not implemented for xyz");
                }
                else
                {
                    List<Atom> validAtoms = new List<Atom>(); //Code for select single atoms
                    foreach (var a in atoms)
                    {
                        if (!validAtoms.Exists(temp => temp.Equals(a))) validAtoms.Add(a);
                    }
                    StringBuilder file = new StringBuilder();
                    file.Append(validAtoms.Count.ToString() + "\n\n"); //atoms
                    bool first = true;
                    foreach (Atom a in validAtoms) //atoms
                    {
                        if (first) first = false; else file.Append("\n");
                        file.Append(a.Name + "\t" + a.X.ToString(new CultureInfo("en-US")) + "\t" + a.Y.ToString(new CultureInfo("en-US")) + "\t" + a.Z.ToString(new CultureInfo("en-US")));
                    }
                    File.WriteAllText(outputFileName, file.ToString());
                }
            }
            else if (outputType == 5) // MD-Jeep
            {
                if (!isBP)
                {
                    if (!isQuiet) Console.WriteLine("Warning: Only BP configuration is implemented for MD-jeep");
                }
                StringBuilder file = new StringBuilder();
                bool first = true;
                Atom[] previusAtoms = new Atom[] { null, null, null };
                if (isHcOrder || isToy)
                {
                    List<Atom> atomsSingle = new List<Atom>();
                    //for (int i = 0; i < atoms.Count; i++)
                    //{
                    //   if (atoms.IndexOf(atoms.ElementAt(i)) < i) continue;
                    //    atomsSingle.Add(atoms.ElementAt(i));
                    //}
                    for (int i = 0; i < atoms.Count; i++)
                    {
                        if (atoms.IndexOf(atoms.ElementAt(i)) < i)
                        {
                            previusAtoms[0] = previusAtoms[1];
                            previusAtoms[1] = previusAtoms[2];
                            previusAtoms[2] = atoms.ElementAt(i);
                            continue;
                        }
                        atomsSingle.Add(atoms.ElementAt(i));

                        List<Atom> validAtoms = new List<Atom>();
                        foreach (Atom atom in previusAtoms)
                        {
                            if (atom != null)
                            {
                                validAtoms.Add(atom);
                            }
                        }
                        for (int j = 0; j < i - 3; j++)
                        {
                            if (atoms.ElementAt(i).Element == "H" && atoms.ElementAt(j).Element == "H" && validAtoms.IndexOf(atoms.ElementAt(j)) == -1 && isValidDistance(atoms.ElementAt(j), atoms.ElementAt(i), distance, isDiscrete))
                            {
                                validAtoms.Add(atoms.ElementAt(j));
                            }
                        }
                        foreach (Atom validAtom in validAtoms)
                        {
                            if (first) first = false; else file.Append("\r\n");
                            file.Append((atomsSingle.IndexOf(atoms.ElementAt(i)) + 1).ToString().PadLeft(5, ' ') + (atomsSingle.IndexOf(validAtom) + 1).ToString().PadLeft(5, ' ') + " " +
                                atoms.ElementAt(i).ResSeq.ToString().PadLeft(5, ' ') + validAtom.ResSeq.ToString().PadLeft(5, ' ') + " " +
                                atoms.ElementAt(i).Distance(validAtom).ToString(new CultureInfo("en-US")).PadLeft(21, ' ') +
                                atoms.ElementAt(i).Distance(validAtom).ToString(new CultureInfo("en-US")).PadLeft(21, ' ') + "  " +
                                atoms.ElementAt(i).Name.PadRight(4, ' ') + " " + validAtom.Name.PadRight(4, ' ') + "  " +
                                atoms.ElementAt(i).ResName.PadRight(4, ' ') + " " + validAtom.ResName.PadRight(4, ' '));
                        }

                        previusAtoms[0] = previusAtoms[1];
                        previusAtoms[1] = previusAtoms[2];
                        previusAtoms[2] = atoms.ElementAt(i);
                    }
                }
                else
                {
                    int i = 0;
                    foreach (Atom ai in atoms)
                    {
                        int j = 0;
                        foreach (Atom aj in atoms)
                        {
                            if (first) first = false; else file.Append("\r\n");
                            file.Append((i + 1).ToString().PadLeft(5, ' ') + (j + 1).ToString().PadLeft(5, ' ') + " " +
                                atoms.ElementAt(i).ResSeq.ToString().PadLeft(5, ' ') + atoms.ElementAt(j).ResSeq.ToString().PadLeft(5, ' ') + " " +
                                atoms.ElementAt(i).Distance(atoms.ElementAt(j)).ToString(new CultureInfo("en-US")).PadLeft(21, ' ') +
                                atoms.ElementAt(i).Distance(atoms.ElementAt(j)).ToString(new CultureInfo("en-US")).PadLeft(21, ' ') + "  " +
                                atoms.ElementAt(i).Name.PadRight(4, ' ') + " " + atoms.ElementAt(j).Name.PadRight(4, ' ') + "  " +
                                atoms.ElementAt(i).ResName.PadRight(4, ' ') + " " + atoms.ElementAt(j).ResName.PadRight(4, ' '));
                            j++;
                        }
                        i++;
                    }
                }

                File.WriteAllText(outputFileName, file.ToString());
            }
            else if (outputType == 6) // Virtual Order
            {
                if (!isBP)
                {
                    if (!isQuiet) Console.WriteLine("Warning: Only BP configuration is implemented for virtual order path");
                }
                List<Atom> atomsSingle = new List<Atom>();
                for (int i = 0; i < atoms.Count; i++)
                {
                    if (atoms.IndexOf(atoms.ElementAt(i)) < i) continue;
                    atomsSingle.Add(atoms.ElementAt(i));
                }
                StringBuilder file = new StringBuilder();
                bool first = true;
                foreach (Atom atom in atoms)
                {
                    if (first) first = false; else file.Append(" ");
                    file.Append((atomsSingle.IndexOf(atom) + 1).ToString());
                }
                File.WriteAllText(outputFileName, file.ToString());
            }
            else
            {
                Console.WriteLine("Error: Invalid output type.");
                return;
            }
            if (!isQuiet) Console.WriteLine("Finish.");
        }

        /// <summary>
        /// This functions returns if the distance between the two atoms is valid
        /// </summary>
        /// <param name="a">The first Atom</param>
        /// <param name="b">The secound Atom</param>
        /// <returns>True if the distance is valid or, otherwise, false</returns>
        static bool isValidDistance(Atom a, Atom b, double distance, bool isDiscrete)
        {
            return (isDiscrete) ? a.DistanceDiscretized(b, atoms) <= distance : a.Distance(b) <= distance;
        }
    }

    public class AminoAcid
    {
        #region Components

        private AminoAcidName name;
        /// <summary>
        /// The name of Amino Acid
        /// </summary>
        public AminoAcidName Name { get => name; set => name = value; }


        private readonly List<Atom> backbone;
        /// <summary>
        /// The main protein's chain 
        /// </summary>
        public List<Atom> Backbone { get => backbone; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor to amino acid
        /// </summary>
        /// <param name="name">The amino acid identification</param>
        public AminoAcid(AminoAcidName name)
        {
            Name = name;
            switch (name)
            {
                case AminoAcidName.Alanine:
                    backbone = new List<Atom>();
                    backbone.Add(new Atom("N", "N", "ALA"));
                    backbone.Add(new Atom("CA", "C", "ALA"));
                    backbone.Add(new Atom("C", "C", "ALA"));
                    backbone.Add(new Atom("O", "O", "ALA"));
                    backbone.Add(new Atom("CB", "C", "ALA"));
                    backbone.Add(new Atom("OXT", "O", "ALA"));
                    backbone.Add(new Atom("H", "H", "ALA"));
                    backbone.Add(new Atom("H2", "H", "ALA"));
                    backbone.Add(new Atom("HA", "H", "ALA"));
                    backbone.Add(new Atom("HB1", "H", "ALA"));
                    backbone.Add(new Atom("HB2", "H", "ALA"));
                    backbone.Add(new Atom("HB3", "H", "ALA"));
                    backbone.Add(new Atom("HXT", "H", "ALA"));
                    break;
                case AminoAcidName.Arginine:
                    break;
                case AminoAcidName.Asparagine:
                    break;
                case AminoAcidName.AsparticAcid:
                    break;
                case AminoAcidName.Cysteine:
                    break;
                case AminoAcidName.Glutamine:
                    break;
                case AminoAcidName.GlutamicAcid:
                    break;
                case AminoAcidName.Glycine:
                    break;
                case AminoAcidName.Histidine:
                    break;
                case AminoAcidName.Isoleucine:
                    break;
                case AminoAcidName.Leucine:
                    break;
                case AminoAcidName.Lysine:
                    break;
                case AminoAcidName.Methionine:
                    break;
                case AminoAcidName.Phenylalanine:
                    break;
                case AminoAcidName.Proline:
                    break;
                case AminoAcidName.Serine:
                    break;
                case AminoAcidName.Threonine:
                    break;
                case AminoAcidName.Tryptophan:
                    break;
                case AminoAcidName.Tyrosine:
                    break;
                case AminoAcidName.Valine:
                    break;
                case AminoAcidName.Selenocysteine:
                    break;
                case AminoAcidName.Pyrrolysine:
                    break;
                default:
                    throw new Exception("Error to create a amino acid: AminoAcidName not valid");
            }
        }

        #endregion

    }

    /// <summary>
    /// The 21 amino acids
    /// </summary>
    public enum AminoAcidName : long
    {
        Alanine = 52 << AminoAcidAtom.C | 52 << AminoAcidAtom.CA, Arginine, Asparagine, AsparticAcid, Cysteine, Glutamine, GlutamicAcid, Glycine, Histidine, Isoleucine, Leucine,
        Lysine, Methionine, Phenylalanine, Proline, Serine, Threonine, Tryptophan, Tyrosine, Valine, Selenocysteine, Pyrrolysine
    }

    /// <summary>
    /// PDB Atoms types 
    /// </summary>
    public enum AminoAcidAtom
    {
        CA = 0, N2, C = 6, H2, HA, O, CA2,
        N,
        CB,
        OXT,
        H,
        HB1,
        HB2,
        HB3,
        HXT
    }

    static class AminoAcidAtomMethods
    {
        /// <summary>
        /// Get the distances between two atoms
        /// </summary>
        /// <param name="amino">the atom to compare</param>
        /// <returns>return the vector distances between the atoms</returns>
        public static double GetDistance(this AminoAcidAtom atom, AminoAcidAtom amino)
        {

            switch (atom)
            {
                case AminoAcidAtom.N2:
                    goto N2;
                case AminoAcidAtom.CA:
                    goto Ca;
                case AminoAcidAtom.C:
                    goto C;
                case AminoAcidAtom.H2:
                    goto H2;
                case AminoAcidAtom.HA:
                    goto Ha;
                case AminoAcidAtom.O:
                    goto O;
                case AminoAcidAtom.CA2:
                    goto Ca2;
                default:
                    return -1;
            }
        N2: switch (amino)
            {
                case AminoAcidAtom.N2:
                    return 0;
                case AminoAcidAtom.CA2:
                    return 1.45;
                case AminoAcidAtom.CA:
                    return 2.414;
                case AminoAcidAtom.C:
                    return 1.33;
                case AminoAcidAtom.H2:
                    return 0.86;
                case AminoAcidAtom.O:
                    return 2.252;
                default:
                    break;
            }
        Ca: switch (amino)
            {
                case AminoAcidAtom.N2:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.CA:
                    return 0;
                case AminoAcidAtom.C:
                    return 1.52;
                case AminoAcidAtom.H2:
                    return 3.04;
                case AminoAcidAtom.O:
                    return 2.399;
                default:
                    break;
            }
        C: switch (amino)
            {
                case AminoAcidAtom.N2:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.CA:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.C:
                    return 0;
                case AminoAcidAtom.CA2:
                    return 2.431;
                case AminoAcidAtom.H2:
                    return 1.907;
                case AminoAcidAtom.O:
                    return 1.23;
                default:
                    break;
            }
        H2: switch (amino)
            {
                case AminoAcidAtom.N2:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.CA:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.C:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.CA2:
                    return 2.005;
                case AminoAcidAtom.H2:
                    return 0;
                default:
                    break;
            }
        Ha: switch (amino)
            {
                case AminoAcidAtom.N2:
                    break;
                case AminoAcidAtom.CA:
                    break;
                case AminoAcidAtom.C:
                    break;
                case AminoAcidAtom.H2:
                    break;
                case AminoAcidAtom.HA:
                    return 0;
                case AminoAcidAtom.O:
                    break;
                default:
                    break;
            }
        O: switch (amino)
            {
                case AminoAcidAtom.N2:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.CA:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.C:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.O:
                    return 0;
                default:
                    break;
            }
        Ca2: switch (amino)
            {
                case AminoAcidAtom.N2:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.C:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.H2:
                    return amino.GetDistance(atom);
                case AminoAcidAtom.O:
                    return 0;
                default:
                    break;
            }
            return -1;
        }

        /// <summary>
        /// Get the angle between tree atoms
        /// </summary>
        /// <param name="a">first atom</param>
        /// <param name="a2">middle atom</param>
        /// <param name="a3">last atom</param>
        /// <returns>return the angle in middle atom</returns>
        public static double GetAngle(this AminoAcidAtom a, AminoAcidAtom a2, AminoAcidAtom a3)
        {
            if (a2 == AminoAcidAtom.C)
            {
                if (a == AminoAcidAtom.CA)
                {
                    if (a3 == AminoAcidAtom.O) return 121.1;
                    else if (a3 == AminoAcidAtom.N2) return 115.6;
                    return 0;
                }
                else if (a == AminoAcidAtom.O)
                {
                    if (a3 == AminoAcidAtom.N2) return 123.2;
                    else if (a3 == AminoAcidAtom.CA) return 121.1;
                    return 0;
                }
                else if (a == AminoAcidAtom.N2)
                {
                    if (a3 == AminoAcidAtom.O) return 123.2;
                    else if (a3 == AminoAcidAtom.CA) return 115.6;
                    return 0;
                }
            }
            else if (a2 == AminoAcidAtom.N2)
            {
                if (a == AminoAcidAtom.H2)
                {
                    if (a3 == AminoAcidAtom.C) return 119.5;
                    else if (a3 == AminoAcidAtom.CA2) return 118.2;
                    return 0;
                }
                else if (a == AminoAcidAtom.C)
                {
                    if (a3 == AminoAcidAtom.H2) return 119.7;
                    else if (a3 == AminoAcidAtom.CA2) return 121.9;
                    return 0;
                }
                else if (a == AminoAcidAtom.CA2)
                {
                    if (a3 == AminoAcidAtom.H2) return 118.2;
                    else if (a3 == AminoAcidAtom.C) return 121.9;
                    return 0;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get de equivalent nomenclature standardized for wwPDB
        /// </summary>
        /// <returns>returns the name of atom in 3-letters standar of PDB file</returns>
        public static string GetPDBEquivalent(this AminoAcidAtom atom)
        {
            switch (atom)
            {
                case AminoAcidAtom.CA:
                    return "CA";
                case AminoAcidAtom.N2:
                    return "N2";
                case AminoAcidAtom.C:
                    return "C";
                case AminoAcidAtom.H2:
                    return "H2";
                case AminoAcidAtom.HA:
                    return "HA";
                case AminoAcidAtom.O:
                    return "O";
                case AminoAcidAtom.CA2:
                    return "CA2";
                case AminoAcidAtom.N:
                    return "N";
                case AminoAcidAtom.CB:
                    return "CB";
                case AminoAcidAtom.OXT:
                    return "OXT";
                case AminoAcidAtom.H:
                    return "H";
                case AminoAcidAtom.HB1:
                    return "HB1";
                case AminoAcidAtom.HB2:
                    return "HB2";
                case AminoAcidAtom.HB3:
                    return "HB3";
                case AminoAcidAtom.HXT:
                    return "HXT";
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Class Atom represents a atom in a polymer
    /// </summary>
    [Serializable]
    public class Atom
    {
        #region Components

        private int serial;
        /// <summary>
        /// Get or set atom serial number 
        /// </summary>
        public int Serial { get => serial; set => serial = value; }

        private string name;
        /// <summary>
        /// Get or set Atom name
        /// </summary>
        public string Name { get => name; set => name = value; }

        private char altLoc;
        /// <summary>
        /// Get or set Alternate location indicator
        /// </summary>
        public char AltLoc { get => altLoc; set => altLoc = value; }

        private string resName;
        /// <summary>
        /// Get or set Residue name
        /// </summary>
        public string ResName { get => resName; set => resName = value; }

        private char chainID;
        /// <summary>
        /// Get or set Chain identifier
        /// </summary>
        public char ChainID { get => chainID; set => chainID = value; }

        private int resSeq;
        /// <summary>
        /// Get or set Residue sequence number
        /// </summary>
        public int ResSeq { get => resSeq; set => resSeq = value; }

        private char iCode;
        /// <summary>
        /// Get or set Code for insertion of residues
        /// </summary>
        public char ICode { get => iCode; set => iCode = value; }

        private double x;
        /// <summary>
        /// Get or set Orthogonal coordinates for X in Angstroms
        /// </summary>
        public double X { get => x; set => x = value; }

        private double y;
        /// <summary>
        /// Get or set Orthogonal coordinates for Y in Angstroms
        /// </summary>
        public double Y { get => y; set => y = value; }

        private double z;
        /// <summary>
        /// Get or set Orthogonal coordinates for Z in Angstroms
        /// </summary>
        public double Z { get => z; set => z = value; }

        private double occupancy;
        /// <summary>
        /// Get or set Occupancy
        /// </summary>
        public double Occupancy { get => occupancy; set => occupancy = value; }

        private double tempFactor;
        /// <summary>
        /// Get or set Temperature factor
        /// </summary>
        public double TempFactor { get => tempFactor; set => tempFactor = value; }

        private string element;
        /// <summary>
        /// Get or set Element symbol, right-justified
        /// </summary>
        public string Element { get => element; set => element = value; }

        private string charge;
        /// <summary>
        /// Get or set Charge on the atom
        /// </summary>
        public string Charge { get => charge; set => charge = value; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor default for atom
        /// </summary>
        public Atom() { }
        /// <summary>
        /// Constructor for atom with indentification and coordenates
        /// </summary>
        /// <param name="serial">Atom serial number</param>
        /// <param name="x">Orthogonal coordinates for X in Angstroms</param>
        /// <param name="y">Orthogonal coordinates for Y in Angstroms</param>
        /// <param name="z">Orthogonal coordinates for Z in Angstroms</param>
        public Atom(int serial, double x, double y, double z)
        {
            Serial = serial;
            X = x;
            Y = y;
            Z = z;
        }
        /// <summary>
        /// Constructor for atom with indentification
        /// </summary>
        /// <param name="name">Name of atom</param>
        /// <param name="element">Element of atom</param>
        /// <param name="resName">Residue name of atomo</param>
        public Atom(string name, string element, string resName)
        {
            Name = name;
            Element = element;
            ResName = resName;
        }

        #endregion

        /// <summary>
        /// Convert the atom instance in string
        /// </summary>
        /// <returns>Retorns the serial, element simbol, coordnates X, Y and Z as string description of atom</returns>
        public override string ToString()
        {
            return "serial = " + Serial.ToString() + " name: " + Name + " chain: " + ChainID + " res: {" + ResName + ", " + ResSeq + "} x = " + X.ToString()
                + " y = " + Y.ToString() + " z = " + Z.ToString() + " " + Element;
        }

        /// <summary>
        /// Return the distance between this instance and the atom in parameter
        /// </summary>
        /// <param name="atom">Atom for calculate the distance</param>
        /// <returns>Distance in angstroms between this atoms</returns>
        public double Distance(Atom atom)
        {
            if (atom == null) return -1;
            return Math.Sqrt(Math.Pow(X - atom.X, 2) + Math.Pow(Y - atom.Y, 2) + Math.Pow(Z - atom.Z, 2));
        }

        /// <summary>
        /// Returns the distance between two atoms in a determined sequence
        /// </summary>
        /// <param name="atom">The atom you want to know the distance</param>
        /// <param name="sequence">The sequence of Atoms containing the two atoms</param>
        /// <returns>Return the distance in number of atoms between the two atoms</returns>
        public int DistanceDiscretized(Atom atom, List<Atom> sequence)
        {
            if (atom == null) throw new Exception("Null pointer parameter in atom");
            if (atom.Serial == this.Serial) return 0;
            Atom first = null;
            int count = 0;
            foreach (Atom a in sequence)
            {
                if (first != null) count++;
                if (a.Serial == atom.serial)
                {
                    if (first == atom)
                    {
                        count = 0;
                        continue;
                    }
                    if (first != null) return count; else first = atom;
                }
                if (a.Serial == this.Serial && first != this)
                {
                    if (first == this)
                    {
                        count = 0;
                        continue;
                    }
                    if (first != null) return count; else first = this;
                }
            }
            return int.MaxValue;
        }

        /// <summary>
        /// Return the AminoAcidName equivalent
        /// </summary>
        /// <returns>The equivalente AminoAcidName</returns>
        public AminoAcidName GetAminoAcidName()
        {
            if (ResName == null || string.IsNullOrWhiteSpace(ResName)) throw new Exception("Error in Atom.AminoAcidName(): ResName null");
            if (ResName == "ALA") return AminoAcidName.Alanine;
            else if (ResName == "ARG") return AminoAcidName.Arginine;
            else if (ResName == "HIS") return AminoAcidName.Histidine;
            else if (ResName == "LYS") return AminoAcidName.Lysine;
            else if (ResName == "ASP") return AminoAcidName.AsparticAcid;
            else if (ResName == "GLU") return AminoAcidName.GlutamicAcid;
            else if (ResName == "SER") return AminoAcidName.Serine;
            else if (ResName == "THR") return AminoAcidName.Threonine;
            else if (ResName == "ASN") return AminoAcidName.Asparagine;
            else if (ResName == "GLN") return AminoAcidName.Glutamine;
            else if (ResName == "CYS") return AminoAcidName.Cysteine;
            else if (ResName == "SEC") return AminoAcidName.Selenocysteine;
            else if (ResName == "GLY") return AminoAcidName.Glycine;
            else if (ResName == "PRO") return AminoAcidName.Proline;
            else if (ResName == "VAL") return AminoAcidName.Valine;
            else if (ResName == "ILE") return AminoAcidName.Isoleucine;
            else if (ResName == "LEU") return AminoAcidName.Leucine;
            else if (ResName == "MET") return AminoAcidName.Methionine;
            else if (ResName == "PHE") return AminoAcidName.Phenylalanine;
            else if (ResName == "TYR") return AminoAcidName.Tyrosine;
            else if (ResName == "TRP") return AminoAcidName.Tryptophan;
            else throw new Exception("Error in Atom.AminoAcidName(): ResName invalid");
        }

        /// <summary>
        /// Return the AminoAcidAtom equivalent
        /// </summary>
        /// <returns>The equivalente AminoAcidAtom</returns>
        public AminoAcidAtom GetAminoAcidAtom()
        {
            if (Name == null || string.IsNullOrWhiteSpace(Name)) throw new Exception("Error in Atom.AminoAcidAtom(): Name null");
            switch (GetAminoAcidName())
            {
                case AminoAcidName.Alanine:
                    if (Name == "N") return AminoAcidAtom.N;
                    else if (Name == "CA") return AminoAcidAtom.CA;
                    else if (Name == "C") return AminoAcidAtom.C;
                    else if (Name == "O") return AminoAcidAtom.O;
                    else if (Name == "CB") return AminoAcidAtom.CB;
                    else if (Name == "OXT") return AminoAcidAtom.OXT;
                    else if (Name == "H") return AminoAcidAtom.H;
                    else if (Name == "H2") return AminoAcidAtom.H2;
                    else if (Name == "HA") return AminoAcidAtom.HA;
                    else if (Name == "HB1") return AminoAcidAtom.HB1;
                    else if (Name == "HB2") return AminoAcidAtom.HB2;
                    else if (Name == "HB3") return AminoAcidAtom.HB3;
                    else if (Name == "HXT") return AminoAcidAtom.HXT;
                    else throw new Exception("Error in Atom.AminoAcidAtom(): Invalid Name (" + Name + ") for " + ResName + " amino");
                case AminoAcidName.Arginine:
                    break;
                case AminoAcidName.Asparagine:
                    break;
                case AminoAcidName.AsparticAcid:
                    break;
                case AminoAcidName.Cysteine:
                    break;
                case AminoAcidName.Glutamine:
                    break;
                case AminoAcidName.GlutamicAcid:
                    break;
                case AminoAcidName.Glycine:
                    break;
                case AminoAcidName.Histidine:
                    break;
                case AminoAcidName.Isoleucine:
                    break;
                case AminoAcidName.Leucine:
                    break;
                case AminoAcidName.Lysine:
                    break;
                case AminoAcidName.Methionine:
                    break;
                case AminoAcidName.Phenylalanine:
                    break;
                case AminoAcidName.Proline:
                    break;
                case AminoAcidName.Serine:
                    break;
                case AminoAcidName.Threonine:
                    break;
                case AminoAcidName.Tryptophan:
                    break;
                case AminoAcidName.Tyrosine:
                    break;
                case AminoAcidName.Valine:
                    break;
                case AminoAcidName.Selenocysteine:
                    break;
                case AminoAcidName.Pyrrolysine:
                    break;
                default:
                    throw new Exception("Error in Atom.AminoAcidAtom(): Invalid AminoAcidName");
            }

            return AminoAcidAtom.C;
        }

        public double TeoricalDistance(Atom atom)
        {
            switch (GetAminoAcidName())
            {
                case AminoAcidName.Alanine:
                    switch (GetAminoAcidAtom())
                    {
                        case AminoAcidAtom.CA:
                            switch (atom.GetAminoAcidAtom())
                            {
                                case AminoAcidAtom.CA:
                                    break;
                                case AminoAcidAtom.C:
                                    break;
                                case AminoAcidAtom.H2:
                                    break;
                                case AminoAcidAtom.HA:
                                    break;
                                case AminoAcidAtom.O:
                                    break;
                                case AminoAcidAtom.N:
                                    break;
                                case AminoAcidAtom.CB:
                                    break;
                                case AminoAcidAtom.OXT:
                                    break;
                                case AminoAcidAtom.H:
                                    break;
                                case AminoAcidAtom.HB1:
                                    break;
                                case AminoAcidAtom.HB2:
                                    break;
                                case AminoAcidAtom.HB3:
                                    break;
                                case AminoAcidAtom.HXT:
                                    break;
                                default:
                                    throw new Exception("Error in Atom.TeoricalDistance(): Invalid AminoAcidAtom of param");
                            }
                            break;
                        case AminoAcidAtom.C:
                            break;
                        case AminoAcidAtom.H2:
                            break;
                        case AminoAcidAtom.HA:
                            break;
                        case AminoAcidAtom.O:
                            break;
                        case AminoAcidAtom.N:
                            break;
                        case AminoAcidAtom.CB:
                            break;
                        case AminoAcidAtom.OXT:
                            break;
                        case AminoAcidAtom.H:
                            break;
                        case AminoAcidAtom.HB1:
                            break;
                        case AminoAcidAtom.HB2:
                            break;
                        case AminoAcidAtom.HB3:
                            break;
                        case AminoAcidAtom.HXT:
                            break;
                        default:
                            throw new Exception("Error in Atom.TeoricalDistance(): Invalid AminoAcidAtom");
                    }
                    break;
                case AminoAcidName.Arginine:
                    break;
                case AminoAcidName.Asparagine:
                    break;
                case AminoAcidName.AsparticAcid:
                    break;
                case AminoAcidName.Cysteine:
                    break;
                case AminoAcidName.Glutamine:
                    break;
                case AminoAcidName.GlutamicAcid:
                    break;
                case AminoAcidName.Glycine:
                    break;
                case AminoAcidName.Histidine:
                    break;
                case AminoAcidName.Isoleucine:
                    break;
                case AminoAcidName.Leucine:
                    break;
                case AminoAcidName.Lysine:
                    break;
                case AminoAcidName.Methionine:
                    break;
                case AminoAcidName.Phenylalanine:
                    break;
                case AminoAcidName.Proline:
                    break;
                case AminoAcidName.Serine:
                    break;
                case AminoAcidName.Threonine:
                    break;
                case AminoAcidName.Tryptophan:
                    break;
                case AminoAcidName.Tyrosine:
                    break;
                case AminoAcidName.Valine:
                    break;
                case AminoAcidName.Selenocysteine:
                    break;
                case AminoAcidName.Pyrrolysine:
                    break;
                default:
                    throw new Exception("Error in Atom.TeoricalDistance(): Invalid AminoAcidName");
            }
            return 0;
        }
    }
}
