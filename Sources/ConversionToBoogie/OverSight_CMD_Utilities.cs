using System.Diagnostics;

namespace ConversionToBoogie
{
    /* 
     * Class dedicated for helping parse command line arguements.
     */
    public static class OverSight_CMD_Utilities
    {
        // TODO: extract into a VerificationFlags structure 
        public static void ParseCommandLineArgs(string[] args,
                                                out string solidityFile,
                                                out string nameOfContract)
        {
            solidityFile = args[0];
            nameOfContract = args[1];
            Debug.Assert(!nameOfContract.Contains("/"), $"Illegal contract name {nameOfContract}");
        }
    }
}
