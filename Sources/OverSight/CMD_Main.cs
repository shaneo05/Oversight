
namespace OverSightHandler
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Top level application to run OverSight to target proofs as well as scalable counterexamples
    /// </summary>
    class CMD_Main
    {
        public static int Main(string[] args)
        {
            //Entry point, if expected length is less than two, then prompt user with guidance such that the correct number of arguements can be entered 
            int expectedLength = 2;
            if (args.Length < expectedLength)
            {
                ShowCMDInterface();
                return 1;
            }

            string solFile, entryPointContractName;
            bool attemptProof = true;
            HashSet<Tuple<string, string>> ignoredMethods = new HashSet<Tuple<string, string>>();
            SolToBoogie.TranslatorFlags translatorFlags = new SolToBoogie.TranslatorFlags();

            
            //parse the index command line arguements to sol file being index 0 and entryPointContractName as index 1
            SolToBoogie.OverSight_CMD_Utilities.ParseCommandLineArgs(args, out solFile, out entryPointContractName);

            //Feed these to the OverSight
            var overSightExecutor = new OverSightController(
                                        Path.Combine(Directory.GetCurrentDirectory(), solFile), 
                                        entryPointContractName,
                                        ignoredMethods,
                                        attemptProof,
                                        translatorFlags);
                        return overSightExecutor.Execute();
        }

        static void ShowCMDInterface()
        {

            Console.WriteLine("-------------------------------------------------------------------------------------------");

            Console.WriteLine("\nLoading OverSight");
            Console.WriteLine("Version Alpha");
            Console.WriteLine("V.0.0.1\n");

            Console.WriteLine("-------------------------------------------------------------------------------------------");
            Console.WriteLine("\nWelcome to OverSight\n");
            Console.WriteLine("-------------------------------------------------------------------------------------------");
            Console.WriteLine("OverSight is a Validation and Verification tool dedicated to Smart Contracts written in Solidity.\n");

            Console.WriteLine("We recommend solidity contracts fed to this program contain relevant/neccessary assertions as inference capability is quite limited due to time constraints.\n");

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
    }
}
