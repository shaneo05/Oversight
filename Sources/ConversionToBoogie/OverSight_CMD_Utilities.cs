using System.Diagnostics;

namespace SolToBoogie
{
    /// <summary>
    /// Class dedicated for helping parse command line arguements.
    /// </summary>
    public static class OverSight_CMD_Utilities
    {
        // TODO: extract into a VerificationFlags structure 
        public static void ParseCommandLineArgs(string[] args,
                                                out string solidityFile,
                                                out string entryPointContractName)
        {
            solidityFile = args[0];
            // Debug.Assert(!solidityFile.Contains("/"), $"Illegal solidity file name {solidityFile}"); //the file name can be foo/bar/baz.sol
            entryPointContractName = args[1];
            Debug.Assert(!entryPointContractName.Contains("/"), $"Illegal contract name {entryPointContractName}");
        }
    }
}
