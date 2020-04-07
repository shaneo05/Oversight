
namespace OverSightHandler
{
    //Uses following System imports
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Main Entry to OverSight to target proofs in sol contracts and analyse areas of potential future failure for user feedback
    /// </summary>
    class CMD_Main
    {
        public static int Main(string[] args)
        {
            OverSightController overSightObjec = new OverSightController();

            //Entry point, if expected length is less than two, then prompt user with guidance such that the correct number of arguements can be entered 

            //Simplistic form of error checking in terms of number of arguments.
            int expectedLength = 2; 
            if (args.Length < expectedLength)
            {
                overSightObjec.ShowCMDInterface();
                return 1;//represents incorrect execution.
            }
            //if length is greatr than expected then prompt interface
            if (args.Length > expectedLength)
            {
                overSightObjec.ShowCMDInterface();
                return 1;
            }

            //Name of the sol contract.sol and its contract class name will be taken from args[] after being fed to the parser.
            string solFile;
            string classContractName;
                  
            //Status value to attempt proof on the sol contract
            bool attemptProof = true;

            HashSet<Tuple<string, string>> ignoredMethods = new HashSet<Tuple<string, string>>();
            ConversionToBoogie.Flags_HelperClass translatorFlags = new ConversionToBoogie.Flags_HelperClass();

            //parse the index command line arguements to sol file being index 0 and entryPointContractName as index 1
            overSightObjec.ParseCommandLineArgs(args, out solFile, out classContractName);

            //Feed these to the OverSight constructor as parameters to begin proof conversion.

            overSightObjec.setSolidityFilePath(Path.Combine(Directory.GetCurrentDirectory(), solFile));
            overSightObjec.setContractName(classContractName);
            overSightObjec.setIgnoredMethods(ignoredMethods);//currently no benefit.
            overSightObjec.setProofFlag(attemptProof);//proof is performed by default 
            overSightObjec.setTranslatorFlag(translatorFlags);//currently no benefit.
            
            return overSightObjec.startOverSight(); //Begins execution of program in finding proof.
            //The above call to execute should result in a 0 for successful completion.
        }
    }
}
