

namespace OverSightHandler
{

    using SolidityAST;
    using BoogieAST;
    using SolToBoogie;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;


    internal class OverSightExecutor
    {
        //Class Variables

        //Loc of Solidity File Path
        private string SolidityFilePath;
        //Loc of Solidity File Directory
        private string SolidityFileDir;

        //Loc of Solidity Contract Name
        private string ContractName;

        //Loc of BoogieExecutablePath
        private string BoogieExecutablePath;
        //Loc of SolidityCompilerPath
        private string SolidityCompilerPath;

        //Bool attempt proof value
        private bool AttemptProof;

        //BPL file containing boogie output 
        private readonly string outFileName = "BoogieConversion.bpl";

        private HashSet<Tuple<string, string>> ignoreMethods;
        private TranslatorFlags translatorFlags;

        public OverSightExecutor(string solidityFilePath, string contractName, HashSet<Tuple<string, string>> ignoreMethods,bool tryProofFlag, TranslatorFlags _translatorFlags = null)
        {
            this.SolidityFilePath = solidityFilePath;
            this.ContractName = contractName;
            this.SolidityFileDir = Path.GetDirectoryName(solidityFilePath);

            Console.WriteLine($"Application arguments accepted.\n");
            Console.WriteLine($"Running OverSight on Solidity Contract : {contractName}\n");

            //Due to no automation, the download path of the solidity and boogie compilers/exe must be manually stated
            //This is subject to change within the next week as dynamic flexibility is required across multiple machines
            this.BoogieExecutablePath = "C:\\Users\\shane\\Desktop\\TempOversight\\bin\\Debug\\boogie.exe";
            this.SolidityCompilerPath = "C:\\Users\\shane\\Desktop\\TempOversight\\bin\\Debug\\solc.exe";
            this.ignoreMethods = new HashSet<Tuple<string, string>>(ignoreMethods);
            this.AttemptProof = tryProofFlag;

            this.translatorFlags = _translatorFlags;
        }

        public int Execute()
        {
            // call SolToBoogie on specFilePath
            //This should be called on execution failure
            if (!RunSolidityToBoogieConversion())
            {
                return 1;
            }

            if (AttemptProof)   //if proofing is true attempt to find a proof via FindProof();
            {
                if (FindProof())
                {
                    return 0;
                }
            }

            return 0;
        }

        private bool FindProof()
        {
            var boogieArgs = new List<string>
            {
                //-doModSetAnalysis -inline:spec (was assert) -noinfer -contractInfer -proc:BoogieEntry_* out.bpl
                //
                $"-doModSetAnalysis",
                $"-inline:spec", //was assert to before to fail when reaching recursive functions
                $"-noinfer",
                $"-inlineDepth:{translatorFlags.InlineDepthForBoogie}", //contractInfer can perform inlining as well
                // main method
                $"-proc:BoogieEntry_*",
                // Boogie file
                outFileName
            };
            
            var boogieArgString = string.Join(" ", boogieArgs);

            Console.WriteLine($"\nSolidity to Boogie Conversion has successfully completed.");
            Console.WriteLine($"Refer to {outFileName} for boogie src code\n");
            Console.WriteLine($"Attempting to find proof.....");

            //Console.WriteLine($"... running {BoogiePath} {boogieArgString}");
            var boogieOut = RunBinary(BoogieExecutablePath, boogieArgString);
            var boogieOutFile = "boogie.txt";
            using (var bFile = new StreamWriter(boogieOutFile))
            {
                bFile.Write(boogieOut);
            }
            // Console.WriteLine($"\tFinished Boogie, output in {boogieOutFile}....\n");

            // compare Corral output against expected output
            if (CompareBoogieOutput(boogieOut))
            {
                Console.WriteLine($"Validation/Verification has proved [successful].");
                Console.WriteLine($"\n -- Proof located in Sol Contract.");
                Console.WriteLine($"Verification Success, refer to {boogieOutFile} for further details");
                    
                Console.WriteLine($"\n{boogieOut}");
                return true;
            }
            else
            {
                Console.WriteLine($"Validation/Verification has proved [unsuccessful].");
                Console.WriteLine($"\t*** OverSight was unable to find a proof (see {boogieOutFile})");
                return false;
            }
                 
        }

        /**
         * Runs solidity conversion function
         */
        private bool RunSolidityToBoogieConversion()
        {
            Console.WriteLine("Starting Solidity Compiler.");

            // compile the program

            Console.WriteLine($"Running Compiler on {ContractName}.");

            SolidityCompiler compiler = new SolidityCompiler();
            CompilerOutput compilerOutput = compiler.Compile(SolidityCompilerPath, SolidityFilePath);

            if (compilerOutput.ContainsError())
            {
                compilerOutput.PrintErrorsToConsole();
                throw new SystemException("Compilation Error");
            }

            // build the Solidity AST from solc output
            AST solidityAST = new AST(compilerOutput, Path.GetDirectoryName(SolidityFilePath));

            // translate Solidity to Boogie
            try
            {
                // if application reaches this stage, compilation of the program was successful
                // The application now attemps to convert the solidity code to Boogie through the use of collection and syntax trees.

                ConversionToBoogieTranslator translator = new ConversionToBoogieTranslator();
                Console.WriteLine($"\nAttempting Conversion to Boogie.");
                BoogieAST boogieAST = translator.Translate(solidityAST, ignoreMethods, translatorFlags);

                // dump the Boogie program to a file
                var outFilePath = Path.Combine(SolidityFileDir, outFileName);
                using (var outWriter = new StreamWriter(outFileName))
                {
                    outWriter.WriteLine(boogieAST.GetRoot());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"OverSight translation error: {e.Message}");
                return false;
            }
            return true;
        }

        private string RunBinary(string cmdName, string arguments)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = cmdName;
            p.StartInfo.Arguments = $"{arguments}";
            p.Start();

            string outputBinary = p.StandardOutput.ReadToEnd();
            string errorMsg = p.StandardError.ReadToEnd();
            if (!String.IsNullOrEmpty(errorMsg))
            {
                Console.WriteLine($"Error: {errorMsg}");
            }
            p.StandardOutput.Close();
            p.StandardError.Close();

            // TODO: should set up a timeout here
            // but it seems there is a problem if we execute corral using mono

            return outputBinary;
        }
        
        //Get other stuff fixed before this function, designate priority of this last
        private bool CompareCorralOutput(string expected, string actual)
        {
            if (actual == null)
            {
                return false;
            }
            string[] actualList = actual.Split("Boogie verification time");
            if (actualList.Length == 2)
            {
                if (actualList[0].Contains(expected))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CompareBoogieOutput(string actual)
        {
            if (actual == null)
            {
                return false;
            }
            // Boogie program verifier finished with x verified, 0 errors
            if (actual.Contains("Boogie program verifier finished with ") &&
                actual.Contains(" verified, 0 errors"))
            {
                Console.WriteLine("\nBoogie program has completed verification / validation.");
                return true;
            }
            return false;
        }

    }
}