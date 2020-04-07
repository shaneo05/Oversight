using System;

using System.Text.RegularExpressions;
using Boogie_Syntax_Tree;

namespace ConversionToBoogie
{
    /// <summary>
    /// A set of flags to intended to control translation (made redundant) and mapArrayHelper
    /// </summary>
    /// 

    //Contains Mapping Helper functions and Static Flags. 
    //The use of these flags are now redundant as are not implementable or usable by the user to control flow
    public class Flags_HelperClass
    {

        private static string mapRegexStatement = @"mapping\((\w+)\s*\w*\s*=>\s*(.+)\)$";

        private static Regex mappingRegexImplementation = new Regex(mapRegexStatement);

        private static readonly Regex arrayRegex = new Regex(@"(.+)\[\w*\] (storage ref|storage pointer|memory)$");
       
        public static string generateMemoryMapName(BoogieType keyType, BoogieType valType)
        {
            return "M_" + keyType.ToString() + "_" + valType.ToString();
        }

        //return a BoogieExpression 
        public static BoogieExpr GetMemoryMapSelectExpr(BoogieType mapKeyType, BoogieType mapValType, BoogieExpr baseExpr, BoogieExpr indexExpr)
        {
            string generateMapName = generateMemoryMapName(mapKeyType, mapValType);
            BoogieIdentifierExpr mapIdentifier = new BoogieIdentifierExpr(generateMapName);
            BoogieMapSelect mapSelectExpr = new BoogieMapSelect(mapIdentifier, baseExpr);
            mapSelectExpr = new BoogieMapSelect(mapSelectExpr, indexExpr);

            return mapSelectExpr;
        }

        //Return Boogie Expression through type invocation, quantity greater binary.
        public static BoogieType getBoogieExpression(string type)
        {
            // All associated types that can be expressed in Boogie as Reference types
            if (IsArrayTypeString(type) || checkMappingTypeEquality(type) || type.Equals("address") || type.Equals("address payable"))
            {
                return BoogieType.Ref;
            }
  
            // All associated types that can be expressed in Boogie as boolean types
            else if (type.Equals("bool"))
            {
                return BoogieType.Bool;
            }
            else if (type.StartsWith("uint") && !type.Contains("[") || (type.StartsWith("int") && !type.Contains("[")) || (type.StartsWith("byte") && !type.Contains("[")))
            {
                return BoogieType.Int;
            }
  
            // All associated types that can be expressed as a reference type, second conditional
            else if (type.StartsWith("contract ")|| type.StartsWith("struct "))
            {
                return BoogieType.Ref;
            }
     
            // All associated types that can be xpressed as an integer second conditional
            else if (type.Equals("string") || type.StartsWith("string ") || (type.StartsWith("literal_string ")))
            {
                return BoogieType.Int; 
            }
            else
            {
                throw new SystemException($"Cannot infer from type string: {type}");
            }
        }

        //Generates key type from string given type
        public static BoogieType GenerateKeyTypeFromString(string givenType)
        {
            if (mappingRegexImplementation.IsMatch(givenType))
            {
                Match match = mappingRegexImplementation.Match(givenType);
                return getBoogieExpression(match.Groups[1].Value);
            }
            else if (arrayRegex.IsMatch(givenType))
            {
                Match match = arrayRegex.Match(givenType);
                return BoogieType.Int;
            }
            else if (givenType == "bytes calldata")
            {
                return BoogieType.Ref;
            }
            else
            {
                throw new SystemException($"Unknown type string during InferKeyTypeFromTypeString: {givenType}");
            }
        }

        //generates value type from type string 
        public static BoogieType GenerateValueTypeFromString(string typeString)
        {
            if (mappingRegexImplementation.IsMatch(typeString))
            {
                Match match = mappingRegexImplementation.Match(typeString);
                return getBoogieExpression(match.Groups[2].Value);
            }
            else if (arrayRegex.IsMatch(typeString))
            {
                Match match = arrayRegex.Match(typeString);
                return getBoogieExpression(match.Groups[1].Value);
            }
            else if (typeString == "bytes calldata")
            {
                return BoogieType.Ref;
            }
            else
            {
                throw new SystemException($"Unknown type string during InferValueTypeFromTypeString: {typeString}");
            }
        }
        //function to check equality over given type string 
        public static bool checkMappingTypeEquality(string type)
        {
            return mappingRegexImplementation.IsMatch(type);
        }

        public static bool IsArrayTypeString(string typeString)
        {
            return arrayRegex.IsMatch(typeString);
        }


        public static bool NoSourceLineInfoFlag = false;
        public static bool NoDataValuesInfoFlag = false;
        public static bool NoAxiomsFlag = false;
        public static bool UseModularArithmetic = false;
        public static bool NoUnsignedAssumesFlag = false;
        public static bool NoHarness = false;
        public static bool GenerateInlineAttributes = true;
        public static bool InstrumentGas = false;
        public static int InlineDepthForBoogie = 4;
        public static bool PerformContractInferce = false;
        public static bool DoModSetAnalysis = false;
        public static bool RemoveScopeInVarName = false;


        /// <summary>
        /// Model revert logic when an exception happens.
        /// </summary>
        public bool ModelReverts { get; set; }

    }

}
