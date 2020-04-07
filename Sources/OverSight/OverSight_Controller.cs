

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
        private string boogieRepresentationOfSolContract = "BoogieConversion.bpl";

        private HashSet<Tuple<string, string>> ignoreMethods;//not currently being used.
        private Flags_HelperClass translatorFlags;//not currently being used.

        private string cmdBoogieOutput;
        /**
         * Function is called via CMD_Main with attempt proof being a standard true value without user invocation
         */
        public int startOverSight()
        {
            int executionState = 0;

            Console.WriteLine($"Application arguments accepted.\n");
            Console.WriteLine($"Running OverSight on Solidity Contract : {contractName}\n");

            //Due to no automation, the download path of the solidity and boogie compilers/exe must be manually stated
            //This is for the time being and can be subject to change at a later date if their is time later on
            string basePath = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

            boogieExecutablePath = basePath + "\\boogie.exe";     //locate boogieExe path for 
            solidityCompilerPath = basePath + "\\solc.exe";

            // call SolToBoogie on specFilePath
            //This should be called on execution failure
            if (runSolidityToBoogieConversion() == false)
            {
                //Conversion was attempted but failed
                //Value of 1 represents failure
                executionState = 1;
            }

            //Uneccessary
            if (attemptProof == true)  //if proofing is true attempt to find a proof via FindProof();
            {
                bool success = searchForProofs();

                if (success == true)
                {
                    executionState = 0;//represents successful execution
                }
            }

            return executionState;
        }

        /**
         * If OverSight can perform conversion succesfully then begin proof searching.
         */
        private bool searchForProofs()
        {
         
            //Boogie.exe arguments as a list string 
            
            //The follow command will be executed to attempt verification of the contract 
            //-doModSetAnalysis -inline:spec (was assert) -noinfer -contractInfer -proc:BoogieEntry_* out.bpl
            //
            List<string> verificationArguments = new List<string>();
            verificationArguments.Add($"-doModSetAnalysis");
            verificationArguments.Add($"-inline:spec"); //was assert to before to fail when reaching recursive functions
            verificationArguments.Add($"-noinfer"); //no inference, However can it be implemented later if there is time
            verificationArguments.Add($"-inlineDepth:{Flags_HelperClass.InlineDepthForBoogie}");//contractInfer can perform inlining as well
            verificationArguments.Add($"-proc:BoogieEntry_*");//entry point for Boogie.exe 
            verificationArguments.Add(boogieRepresentationOfSolContract);

            var verificationArguementsAsString = string.Join(" ", verificationArguments);
            //BoogieArgString will represent -doModSetAnalysis -inline:spec (was assert) -noinfer -contractInfer -proc:BoogieEntry_* out.bpl

            Console.WriteLine($"\nSolidity to Boogie Conversion has successfully completed.");
            Console.WriteLine($"Refer to {boogieRepresentationOfSolContract} for boogie src code\n");
            Console.WriteLine($"Running Boogie.exe to generate verification conditions.....");

            var boogieOut = RunBoogieAnalysisExe(boogieExecutablePath, verificationArguementsAsString);
            var boogieOutFile = "verificationOutcome.txt";

            using (var verificationFile = new StreamWriter(boogieOutFile))
            {
                verificationFile.Write(boogieOut);
            }

            // hardcoded checking to determine generic success or not, not efficient but works 
            if (decipherOutput(boogieOut))
            {
                Console.WriteLine($"Validation/Verification has proved [successful].");
                Console.WriteLine($"\n -- Proof located in Sol Contract.");
                Console.WriteLine($"      Refer to {boogieOutFile} for further details");
                    
                Console.WriteLine($"\n{cmdBoogieOutput}");
                return true;
            }
            else
            {
                Console.WriteLine($"Validation/Verification has proved [unsuccessful].");
                Console.WriteLine($"\t ---OverSight was unable to find a proof (see {boogieOutFile})");
                Console.WriteLine("\nAdvised to revisit contract before publication onto distributed ledger");
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

                ConversionToBoogie_Main translator = new ConversionToBoogie_Main();
                Console.WriteLine($"\nAttempting Conversion to Boogie.");
                BoogieAST boogieAST = translator.Translate(solidityAST, ignoreMethods, translatorFlags);

                // dump the Boogie program to a file
                var outFilePath = Path.Combine(solidityFileDir, boogieRepresentationOfSolContract);
                using (var outWriter = new StreamWriter(boogieRepresentationOfSolContract))
                {
                    outWriter.WriteLine(boogieAST.GetRoot());//get all AST components
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
            string boogieOutput = "";
            string errorMessage = "";

            Process process = new Process();
            //process for verification

            try
            {
                //run background process to perform verification in background windown, dont disturb main thread whilst this process is underway.

                // Output is stored in standard output stream                
                process.StartInfo.UseShellExecute = false;

                process.StartInfo.RedirectStandardInput = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.StartInfo.RedirectStandardError = true;

                process.StartInfo.CreateNoWindow = true;//run the process in the background
                process.StartInfo.FileName = cmdName; 
                process.StartInfo.Arguments = $"{arguments}";
                process.Start(); //start the process on the bpl file

                boogieOutput += "Verification Outcome from Boogie utilising Z3 SMT\n\n";

                boogieOutput += "Executor : " + process.StartInfo.FileName +"\n\n";

                boogieOutput += "Arguments :" + process.StartInfo.Arguments+"\n\n";

                cmdBoogieOutput = process.StandardOutput.ReadToEnd();

                boogieOutput +=  cmdBoogieOutput;

                errorMessage = process.StandardError.ReadToEnd();
               
            }
            //catch any appropriate exceptiosn and log them
            catch (Exception e)
            {
                Console.WriteLine(e.Message);//print message
                Console.WriteLine(e.StackTrace); //print stacktrace
            }
            //execute regardless of failure or not
            finally
            {
                //close appropriate output and error streams.
                process.StandardOutput.Close();
                process.StandardError.Close();

            }
            //return string output binary to invoker of this function.
            return boogieOutput;
        }


        private bool decipherOutput(string actual)
        {
            if (actual == null)
            {
                return false;
            }
            Console.WriteLine("\nBoogie program has completed verification / validation.");

            // Boogie program verifier finished with x number of variables verified, x errors
            if (actual.Contains("Boogie program verifier finished with ") &&
                actual.Contains(" verified, 0 errors")) {

                return true;
            }
            return false;
        }

        /**
         * CMD Interface that is invoked upon, where less than two arguments are present 
         * Or when more than two arguments are present.
         * We are only looking for the solidity contract file location and its invocable 
         * 
         */
        public void ShowCMDInterface()
        {

            Console.WriteLine("-------------------------------------------------------------------------------------------");

            Console.WriteLine("\nLoading OverSight");
            Console.WriteLine("Version Alpha");
            Console.WriteLine("V.0.0.1\n");

            Console.WriteLine("-------------------------------------------------------------------------------------------");
            Console.WriteLine("\nWelcome to OverSight\n");
            Console.WriteLine("-------------------------------------------------------------------------------------------");
            Console.WriteLine("OverSight is a Validation and Verification tool dedicated to Smart Contracts written in Solidity.\n");

            Console.WriteLine("We recommend solidity contracts fed to this program contain relevant/neccessary predicate assertions as inference capability is yet to be implemented.\n");

            Console.WriteLine("To use the application state the following as command arguments, 'OverSight', the Solidity Contract file that you wish to validate/verifiy followed by the contracts class name.\n");
            Console.WriteLine("For further clarification the format can be shown as follows: \t\t OverSight '[nameOfContract]'.sol '[className].'\n");

            Console.WriteLine("The application will then attempt to find a proof within the sol contract.\n");

            Console.WriteLine("If no proof is found, despite assertions being present, it dictates that the assertion(s) may fail in a future context.\n");
            Console.WriteLine("This is an indication to the developer of said contract that prior to publification onto the ETH blockchain, the contract should be investigated further.\n");

            Console.WriteLine("For further information on the application refer to 'https://github.com/shaneo05/Oversight'");
            Console.WriteLine("Report any bugs through a issue invocation on the above github link\n");
            Console.WriteLine("Thank you for using the software!\n");


            Console.WriteLine("-------------------------------------------------------------------------------------------");
            Console.WriteLine("-------------------------------------------------------------------------------------------");

        }

        /**
         * Function dedicated for command line parsing 
         */
        public void ParseCommandLineArgs(string[] args,
                                                out string solidityFile,
                                                out string nameOfContract)
        {
            solidityFile = args[0];
            nameOfContract = args[1];
            Debug.Assert(!nameOfContract.Contains("/"), $"Illegal contract name {nameOfContract}");
        }

        /**
         * Set Solidity File Path
         */
        public void setSolidityFilePath(string givenSolidityPath)
        {
            this.solidityFilePath = givenSolidityPath;
            solidityFileDir = Path.GetDirectoryName(solidityFilePath);
        }

        /**
         * Set Contract Name
         */
        public void setContractName(string givenContractName)
        {
            this.contractName = givenContractName;
        }
        /*
         * Set list of ignored methods if any
         */
        public void setIgnoredMethods(HashSet<Tuple<string, string>> ignoreMethods)
        {
            this.ignoreMethods = ignoreMethods;
        }

        /**
         * Set proof flag, Binary value
         */
        public void setProofFlag(bool tryProof)
        {
            this.attemptProof = tryProof;
        }
        /**
         * Set Translator hashset
         * ,Debatable as to its use further on.
         */
        public void setTranslatorFlag(Flags_HelperClass translatorFlags)
        {
            this.translatorFlags = translatorFlags;
        }
    }
}