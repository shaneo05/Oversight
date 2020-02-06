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
                                                out string entryPointContractName)
        {
            solidityFile = args[0];
            entryPointContractName = args[1];
            Debug.Assert(!entryPointContractName.Contains("/"), $"Illegal contract name {entryPointContractName}");
        }
    }
}
