﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SolToBoogie
{
    /// <summary>
    /// Class dedicated for helping parse command line arguements.
    /// </summary>
    public static class ParseUtils
    {
        // TODO: extract into a VerificationFlags structure 
        public static void ParseCommandLineArgs(string[] args, out string solidityFile, out string entryPointContractName, out bool tryProofFlag, out HashSet<Tuple<string, string>> ignoredMethods, ref TranslatorFlags translatorFlags)
        {
            solidityFile = args[0];
            // Debug.Assert(!solidityFile.Contains("/"), $"Illegal solidity file name {solidityFile}"); //the file name can be foo/bar/baz.sol
            entryPointContractName = args[1];
            Debug.Assert(!entryPointContractName.Contains("/"), $"Illegal contract name {entryPointContractName}");

            tryProofFlag = !(args.Any(x => x.Equals("/noPrf")) || args.Any(x => x.Equals("/noChk"))); //args.Any(x => x.Equals("/tryProof"));

            var txBounds = args.Where(x => x.StartsWith("/txBound:"));
            if (txBounds.Count() > 0)
            {
                Debug.Assert(txBounds.Count() == 1, $"At most 1 /txBound:k expected, found {txBounds.Count()}");
            }

            ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Information);

            ignoredMethods = new HashSet<Tuple<string, string>>();
            foreach (var arg in args.Where(x => x.StartsWith("/ignoreMethod:")))
            {
                Debug.Assert(arg.Contains("@"), $"Error: incorrect use of /ignoreMethod in {arg}");
                Debug.Assert(arg.LastIndexOf("@") == arg.IndexOf("@"), $"Error: incorrect use of /ignoreMethod in {arg}");
                var str = arg.Substring("/ignoreMethod:".Length);
                var method = str.Substring(0, str.IndexOf("@"));
                var contract = str.Substring(str.IndexOf("@") + 1);
                ignoredMethods.Add(Tuple.Create(method, contract));
            }
            if (args.Any(x => x.StartsWith("/ignoreMethod:")))
            {
                Console.WriteLine($"Ignored method/contract pairs ==> \n\t {string.Join(",", ignoredMethods.Select(x => x.Item1 + "@" + x.Item2))}");
            }
            translatorFlags.GenerateInlineAttributes = true;
            if (args.Any(x => x.Equals("/noInlineAttrs")))
            {
                translatorFlags.GenerateInlineAttributes = false;
                if (tryProofFlag)
                    throw new Exception("/noInlineAttrs cannot be used when /tryProof is used");
            }
            if (args.Any(x => x.Equals("/break")))
            {
                Debugger.Launch();
            }
            if (args.Any(x => x.Equals("/omitSourceLineInfo")))
            {
                translatorFlags.NoSourceLineInfoFlag = true;
            }
            if (args.Any(x => x.Equals("/omitDataValuesInTrace")))
            {
                translatorFlags.NoDataValuesInfoFlag = true;
            }
            if (args.Any(x => x.Equals("/useModularArithmetic")))
            {
                translatorFlags.UseModularArithmetic = true;
            }
            if (args.Any(x => x.Equals("/omitUnsignedSemantics")))
            {
                translatorFlags.NoUnsignedAssumesFlag = true;
            }
            if (args.Any(x => x.Equals("/omitAxioms")))
            {
                translatorFlags.NoAxiomsFlag = true;
            }
            if (args.Any(x => x.Equals("/omitHarness")))
            {
                translatorFlags.NoHarness = true;
            }

            if (args.Any(x => x.Equals("/modelReverts")))
            {
                translatorFlags.ModelReverts = true;
            }

            if (args.Any(x => x.Equals("/instrumentGas")))
            {
                translatorFlags.InstrumentGas = true;
            }

            var stubModels = args.Where(x => x.StartsWith("/stubModel:"));
            if (stubModels.Count() > 0)
            {
                Debug.Assert(stubModels.Count() == 1, "Multiple instances of /stubModel:");
                var model = stubModels.First().Substring("/stubModel:".Length);
                Debug.Assert(model.Equals("skip") || model.Equals("havoc") || model.Equals("callback"),
                    $"The argument to /stubModel: can be either {{skip, havoc, callback}}, found {model}");
                translatorFlags.ModelOfStubs = model;
            }
            if (args.Any(x => x.StartsWith("/inlineDepth:")))
            {
                var depth = args.Where(x => x.StartsWith("/inlineDepth:")).First();
                translatorFlags.InlineDepthForBoogie = int.Parse(depth.Substring("/inlineDepth:".Length));
            }
            if (args.Any(x => x.Equals("/doModSet")))
            {
                translatorFlags.DoModSetAnalysis = true;
            }
            if (args.Any(x => x.Equals("/removeScopeInVarName")))
            {
                // do not document this option, only needed for equivalence checking in Symdiff
                translatorFlags.RemoveScopeInVarName = true;
            }

            translatorFlags.PerformContractInferce = args.Any(x => x.StartsWith("/contractInfer"));

            // don't perform verification for some of these omitFlags
            if (tryProofFlag)
            {
                Debug.Assert(!translatorFlags.NoHarness &&
                    !translatorFlags.NoAxiomsFlag &&
                    !translatorFlags.NoUnsignedAssumesFlag &&
                    !translatorFlags.NoDataValuesInfoFlag &&
                    !translatorFlags.NoSourceLineInfoFlag &&
                    !translatorFlags.RemoveScopeInVarName,
                    "Cannot perform verification when any of " +
                    "/omitSourceLineInfo, " +
                    "/omitDataValuesInTrace, " +
                    "/omitAxioms, " +
                    "/omitHarness, " +
                    "/omitUnsignedSemantics, " +
                    "/removeScopeInVarName are specified");
            }
        }

    }
}
