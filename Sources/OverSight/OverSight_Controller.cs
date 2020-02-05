

namespace OverSightHandler
{
    using System;
    using System.IO;

    using System.Collections.Generic;
    using System.Diagnostics;

    using Sol_Syntax_Tree;
    using Boogie_Syntax_Tree;

    using ConversionToBoogie;

    class OverSightController
    {
        //Class Variables

        //Loc of Solidity File Path
        private string solidityFilePath;
        //Loc of Solidity File Directory
        private string solidityFileDir;

        //Loc of Solidity Contract Name
        private string contractName;

        //Location of Boogie Executable on runnable machine
        private string boogieExecutablePath;
        //Location of Solidity Compiler on runnable machine
        private string solidityCompilerPath;

        //Bool attempt proof value
        private bool attemptProof;

        //BPL file containing boogie output 
        private readonly string outFileName = "BoogieConversion.bpl";

        private HashSet<Tuple<string, string>> ignoreMethods;
        private TranslatorFlags translatorFlags;

        public OverSightController(string solidityFilePath, string contractName, HashSet<Tuple<string, string>> ignoreMethods,bool tryProofFlag, TranslatorFlags _translatorFlags = null)
        {
            this.solidityFilePath = solidityFilePath;
            this.contractName = contractName;
            solidityFileDir = Path.GetDirectoryName(solidityFilePath);

            Console.WriteLine($"Application arguments accepted.\n");
            Console.WriteLine($"Running OverSight on Solidity Contract : {contractName}\n");

            //Due to no automation, the download path of the solidity and boogie compilers/exe must be manually stated
            // This is for the time being and can be subject to change at a later date
            //This is subject to change within the next week as dynamic flexibility is required across multiple machines
            boogieExecutablePath = "C:\\Users\\shane\\Desktop\\TempOversight\\bin\\Debug\\boogie.exe";
            solidityCompilerPath = "C:\\Users\\shane\\Desktop\\TempOversight\\bin\\Debug\\solc.exe";
            this.ignoreMethods = new HashSet<Tuple<string, string>>(ignoreMethods);
            attemptProof = tryProofFlag;

            translatorFlags = _translatorFlags;
        }
        /**
         * Function is called via CMD_Main with attempt proof being a standard true value without user invocation
         */
        public int startOverSight()
        {
            // call SolToBoogie on specFilePath
            //This should be called on execution failure
            if (runSolidityToBoogieConversion() == false)
            {
                //Conversion was attempted but failed
                //Value of 1 represents failure
                return 1;
            }

            if (attemptProof == true)   //if proofing is true attempt to find a proof via FindProof();
            {
                bool success = searchForProofs();

                if (success == true)
                {
                    return 0;
                }
            }

            return 0;
        }

        private bool searchForProofs()
        {
            //Boogie arguments as a list string 
            var verificationArguments = new List<string>
            {
                //The follow command will be executed to attempt verification of the contract 
                //-doModSetAnalysis -inline:spec (was assert) -noinfer -contractInfer -proc:BoogieEntry_* out.bpl
                //
                $"-doModSetAnalysis",
                $"-inline:spec", //was assert to before to fail when reaching recursive functions
                $"-noinfer", //no inference,        However can it be implemented later
                $"-inlineDepth:{translatorFlags.InlineDepthForBoogie}", //contractInfer can perform inlining as well
                // main method
                $"-proc:BoogieEntry_*",
                // The boogie file to perform verification on , in this case will be ConversionToBoogie.bpl
                outFileName
            };
            
            var verificationArguementsAsString = string.Join(" ", verificationArguments);
            //BoogieArgString will represent -doModSetAnalysis -inline:spec (was assert) -noinfer -contractInfer -proc:BoogieEntry_* out.bpl

            Console.WriteLine($"\nSolidity to Boogie Conversion has successfully completed.");
            Console.WriteLine($"Refer to {outFileName} for boogie src code\n");
            Console.WriteLine($"Attempting to find proof.....");

            var boogieOut = RunBoogieAnalysisExe(boogieExecutablePath, verificationArguementsAsString);
            var boogieOutFile = "verificationOutcome.txt";

            using (var verificationFile = new StreamWriter(boogieOutFile))
            {
                verificationFile.Write(boogieOut);
            }
            // Console.WriteLine($"\tFinished Boogie, output in {boogieOutFile}....\n");

            // compare Corral output against expected output
            if (ExpectedOutput(boogieOut))
            {
                Console.WriteLine($"Validation/Verification has proved [successful].");
                Console.WriteLine($"\n -- Proof located in Sol Contract.");
                Console.WriteLine($"      Refer to {boogieOutFile} for further details");
                    
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
        private bool runSolidityToBoogieConversion()
        {
            Console.WriteLine("Starting Solidity Compiler.");

            // compile the program
            Console.WriteLine($"Running Compiler on {contractName}.");

            SolidityCompiler compiler = new SolidityCompiler();
            CompilerOutput compilerOutput = compiler.Compile(solidityCompilerPath, solidityFilePath);

            if (compilerOutput.ContainsError())
            {
                compilerOutput.PrintErrorsToConsole();
                throw new SystemException("Compilation Error");
            }

            // build the Solidity AST from solc output
            AST solidityAST = new AST(compilerOutput, Path.GetDirectoryName(solidityFilePath));

            // translate Solidity to Boogie
            try
            {
                // if application reaches this stage, compilation of the program was successful
                // The application now attemps to convert the solidity code to Boogie through the use of collection and syntax trees.

                ConversionToBoogieTranslator translator = new ConversionToBoogieTranslator();
                Console.WriteLine($"\nAttempting Conversion to Boogie.");
                BoogieAST boogieAST = translator.Translate(solidityAST, ignoreMethods, translatorFlags);

                // dump the Boogie program to a file
                var outFilePath = Path.Combine(solidityFileDir, outFileName);
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

        private string RunBoogieAnalysisExe(string cmdName, string arguments)
        {
            //Should not remain empty
            string outputBinary = "";
            string errorMessage = "";

            Process process = new Process();

            try
            {
                // Creates binary                 
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = cmdName;
                process.StartInfo.Arguments = $"{arguments}";
                process.Start();

                outputBinary = process.StandardOutput.ReadToEnd();
                errorMessage = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Console.WriteLine($"Error: {errorMessage}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                process.StandardOutput.Close();
                process.StandardError.Close();

            }
            return outputBinary;
        }


        private bool ExpectedOutput(string actual)
        {
            if (actual == null)
            {
                return false;
            }

            // Boogie program verifier finished with x number of variables verified, x errors (if unsuccessful)
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