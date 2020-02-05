
namespace OverSightHandler
{
    //Uses following System imports
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Top level application to run OverSight to target proofs in sol contracts and analyse areas of potential future failure for user feedback
    /// </summary>
    class CMD_Main
    {
        public static int Main(string[] args)
        {
            //Entry point, if expected length is less than two, then prompt user with guidance such that the correct number of arguements can be entered 

            /////////////////////////Simplistic form of error checking in terms of number of arguments.
            int expectedLength = 2;
            if (args.Length < expectedLength)
            {
                ShowCMDInterface();
                return 1;
            }

            if (args.Length > expectedLength)
            {
                ShowCMDInterface();
                return 1;
            }

            //Name of the sol contract.sol and its contract class name will be taken from args[] after being fed to the parser.
            string solFile;
            string classContractName;

            //Status value to attempt proof on the sol contract
            bool attemptProof = true;

            
            HashSet<Tuple<string, string>> ignoredMethods = new HashSet<Tuple<string, string>>();
            SolToBoogie.TranslatorFlags translatorFlags = new SolToBoogie.TranslatorFlags();

            
            //parse the index command line arguements to sol file being index 0 and entryPointContractName as index 1
            SolToBoogie.OverSight_CMD_Utilities.ParseCommandLineArgs(args, out solFile, out classContractName);

            //Feed these to the OverSight constructor as parameters to begin proof conversion.
            var overSightExecutor = new OverSightController(
                                        Path.Combine(Directory.GetCurrentDirectory(), solFile), 
                                        classContractName,
                                        ignoredMethods,
                                        attemptProof,
                                        translatorFlags);
            
            return overSightExecutor.Execute(); //Begins execution of program in finding proof.
            //The above call to execute should result in a 0 for successful completion.
        }

        /**
         * CMD Interface that is invoked upon, where less than two arguments are present 
         * Or when more than two arguments are present.
         * We are only looking for the solidity contract file location and its invocable 
         * 
         */
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
