

namespace OverSightHandler
{

    using Microsoft.Extensions.Logging;
    using SolidityAST;
    using BoogieAST;
    using SolToBoogie;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Linq;

    internal class OverSightExecutor
    {
        private string SolidityFilePath;
        private string SolidityFileDir;
        private string ContractName;
        private string CorralPath;
        private string BoogiePath;
        private string SolcPath;
        private bool TryProof;
        // private bool GenInlineAttrs;
        private ILogger Logger;
        private readonly string outFileName = "BoogieConversion.bpl";
        private readonly string corralTraceFileName = "corral_out_trace.txt";

        private HashSet<Tuple<string, string>> ignoreMethods;
        private TranslatorFlags translatorFlags;
        private bool printTransactionSequence = false; 

        public OverSightExecutor(string solidityFilePath, string contractName, HashSet<Tuple<string, string>> ignoreMethods,bool tryProofFlag, ILogger logger, bool _printTransactionSequence, TranslatorFlags _translatorFlags = null)
        {
            this.SolidityFilePath = solidityFilePath;
            this.ContractName = contractName;
            this.SolidityFileDir = Path.GetDirectoryName(solidityFilePath);

            Console.WriteLine($"Application arguments accepted.\n");
            Console.WriteLine($"Running OverSight on Solidity Contract : {contractName}\n");

            //Console.WriteLine($"SpecFilesDir = {SolidityFileDir}");

            //Due to no automation, the download path of the solidity and boogie compilers/exe must be manually stated
            //This is subject to change within the next week as dynamic flexibility is required across multiple machines
            this.BoogiePath = "C:\\Users\\shane\\Desktop\\TempOversight\\bin\\Debug\\boogie.exe";
            this.SolcPath = "C:\\Users\\shane\\Desktop\\TempOversight\\bin\\Debug\\solc.exe";
            this.ignoreMethods = new HashSet<Tuple<string, string>>(ignoreMethods);
            this.Logger = logger;
            this.TryProof = tryProofFlag;
            this.printTransactionSequence = _printTransactionSequence;
            //this.GenInlineAttrs = genInlineAttrs;
            this.translatorFlags = _translatorFlags;
        }

        public int Execute()
        {
            // call SolToBoogie on specFilePath
            if (!ExecuteSolToBoogie())
            {
                return 1;
            }

            /*
            

            // try to prove first
            if (TryProof && FindProof())
            {
                return 0;
            }
            
            */

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
                translatorFlags.PerformContractInferce? $"-contractInfer" : "",
                $"-inlineDepth:{translatorFlags.InlineDepthForBoogie}", //contractInfer can perform inlining as well
                // main method
                $"-proc:BoogieEntry_*",
                // Boogie file
                outFileName
            };

            
            /*
            var boogieArgString = string.Join(" ", boogieArgs);

            Console.WriteLine($"\nSolidity to Boogie Conversion has successfully completed.");
            Console.WriteLine($"Refer to {outFileName} for boogie src code\n");
            Console.WriteLine($"Attempting to find proof.....");
           

            //Console.WriteLine($"... running {BoogiePath} {boogieArgString}");
            var boogieOut = RunBinary(BoogiePath, boogieArgString);
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
                Console.WriteLine($"\n   *** Proof found! Formal Verification successful! (refer to {boogieOutFile})");
                Console.WriteLine($"\n{boogieOut}");
                return true;
            }
            else
            {
                Console.WriteLine($"Validation/Verification has proved [unsuccessful].");
                Console.WriteLine($"\t*** OverSight was unable to find a proof (see {boogieOutFile})");
                return false;
            }
            */
      
            return false;
        }

     
        private bool ExecuteSolToBoogie()
        {
            Console.WriteLine("Starting Solidity Compiler.");

            // compile the program

            Console.WriteLine($"Running Compiler on {ContractName}.");

            SolidityCompiler compiler = new SolidityCompiler();
            CompilerOutput compilerOutput = compiler.Compile(SolcPath, SolidityFilePath);

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

                BoogieTranslator translator = new BoogieTranslator();
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