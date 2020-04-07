

namespace ConversionToBoogie
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Boogie_Syntax_Tree;
    using Sol_Syntax_Tree;

    /**
     * ASTHandler class to act as filtration for solidity AST properties
     */
    public class AST_Handler
    {
        public static int MAX_GAS_LIMIT = 7000000;
        public static int MIN_GAS_LIMIT = 3000000;

        //return 
        public BoogieProgram getProgram { get; private set; }

        public Dictionary<int, ASTNode> IdToNodeMap { get; set; }

        public string SourceDirectory { get; set; }

        // source file path of ASTNode
        public Dictionary<ASTNode, string> ASTNodeToSourcePathMap { get; private set; }

        // source file line number of ASTNode
        public Dictionary<ASTNode, int> ASTLineNumberMap { get; private set; }

        // all contracts defined in the program
        public HashSet<ContractDefinition> ContractDefinitionsMap { get; private set; }

        // map from each contract to its sub types (including itself)
        public Dictionary<ContractDefinition, HashSet<ContractDefinition>> SubTypeMapping { get; private set; }

        // all state variables explicitly defined in each contract
        public Dictionary<ContractDefinition, HashSet<VariableDeclaration>> StateVarMaps { get; private set; }
        public Dictionary<VariableDeclaration, ContractDefinition> StateVarToContractMap { get; private set; }

        // all mappings explicitly defined in each contract
        public Dictionary<ContractDefinition, HashSet<VariableDeclaration>> MappingFrame { get; private set; }
        // all arrays explicityly defined in each contract
        public Dictionary<ContractDefinition, HashSet<VariableDeclaration>> constructArrayMaps { get; private set; }

        // explicit constructor defined in each contract
        // solidity only allows at most one constructor for each contract
        public Dictionary<ContractDefinition, FunctionDefinition> ConstructorMapping { get; private set; }

        // explicit fallback defined in each contract
        // solidity only allows at most one fallback for each contract
        public Dictionary<ContractDefinition, FunctionDefinition> FallbackMap { get; private set; }

        // all events explicitly defined in each contract
        public Dictionary<ContractDefinition, HashSet<EventDefinition>> EventMapping { get; private set; }
        public Dictionary<EventDefinition, ContractDefinition> EventToContractMap { get; private set; }

        // all functions explicitly defined in each contract
        // FIXME: getters for public state variables
        public Dictionary<ContractDefinition, HashSet<FunctionDefinition>> FunctionMapping { get; private set; }
        public Dictionary<ContractDefinition, HashSet<string>> FunctionSignatureMapping { get; private set; }
        public Dictionary<FunctionDefinition, ContractDefinition> FunctionToContractMap { get; private set; }

        // FunctionSignature -> (DynamicType -> FunctionDefinition)
        public Dictionary<string, Dictionary<ContractDefinition, FunctionDefinition>> SignatureFunctionMap { get; private set; }
        // all functions (including private ones) visible in each contract
        public Dictionary<ContractDefinition, HashSet<FunctionDefinition>> FunctionsMap { get; private set; }

        // StateVarName -> (Dynamictype -> StateVariableDeclaration)
        public Dictionary<string, Dictionary<ContractDefinition, VariableDeclaration>> stateVarLookUpTable { get; private set; }
        // all state variables (including private ones) visible in each contract
        public Dictionary<ContractDefinition, HashSet<VariableDeclaration>> VisibleStateMapping { get; private set; }


        public Dictionary<string, BoogieImplementation> ModifierToBoogiePreImpl { get; private set;}

        public Dictionary<string, BoogieImplementation> ModifierToBoogiePostImpl { get; private set;}

        // Options flags, currently not in use
        public Flags_HelperClass TranslateFlags { get; private set; }

        public Dictionary<ContractDefinition, Dictionary<UserDefinedTypeName, TypeName>> constructDefinitionsMap => usingMap;

        // num of fresh identifiers, should be incremented when making new fresh id
        private int indentifierCount = 0;

        // methods whose translation has to be skipped, 
        private readonly HashSet<Tuple<string, string>> IgnoreMethods;

        private readonly bool genInlineAttrInBpl;

        // data structures for using
        // maps Contract C --> (source, dest), where source is a library type
        private Dictionary<ContractDefinition, Dictionary<UserDefinedTypeName, TypeName>> usingMap;

        //ignored methods and translate flags not used, intended for implementation however timeconstraints prohibited this.
        //Parameter based constructor.
        public AST_Handler(HashSet<Tuple<string, string>> ignoreMethods, bool _genInlineAttrInBpl, Flags_HelperClass translateFlags = null)
        {

            //returns boogie program instance containing the building blocks for boogie statements.
            //Initially this property is populated with default Boogie Statements as per expectation of the Boogie veriifer.

            //The process handler then aids in conversion to boogie by providing Boogie alternatives to the soldiity constructs contained within each mapping.
            getProgram = new BoogieProgram();
            //generates the mappings for the AST_hander, used to filter the generic AST properties into categorizable subsections
            instantiateMappings();

            IgnoreMethods = ignoreMethods;
            genInlineAttrInBpl = _genInlineAttrInBpl; // set by default to true;
            TranslateFlags = translateFlags;//Not used
        }
        /**
         * function check whether an id contains an ast node at the given index
         */
        public bool HasASTNodeId(int id)
        {
            return IdToNodeMap.ContainsKey(id);
        }

        // function to retrieve an ASTNode instance given an id
        public ASTNode retrieveASTNodethroughID(int id)
        {
            if (IdToNodeMap.ContainsKey(id))
            {
                return IdToNodeMap[id];
            }
            else
            {
                return null;
            }
        }
        // function to add source info to ast node
        public void AddSourceInfoForASTNode(ASTNode node, string absolutePath, int lineNumber)
        {
            Debug.Assert(!ASTNodeToSourcePathMap.ContainsKey(node));
            Debug.Assert(!ASTLineNumberMap.ContainsKey(node));
            ASTNodeToSourcePathMap[node] = absolutePath;
            ASTLineNumberMap[node] = lineNumber;
        }
        /**
         * Return absolute path of ast node, given the node instance
         */
        public string GetAbsoluteSourcePathOfASTNode(ASTNode node)
        {
            return ASTNodeToSourcePathMap[node];
        }
        /**
         * Return line number of ast node, given node
         */
        public int GetLineNumberOfASTNode(ASTNode node)
        {
            return ASTLineNumberMap[node];
        }
        /**
         * retrieve function defintions using contract as an identifier to traverse the FunctionMapping.
         */
        public HashSet<FunctionDefinition> returnFunctionDefs(ContractDefinition contract)
        {
            if (FunctionMapping.ContainsKey(contract))
            {
                return FunctionMapping[contract];
            }
            return new HashSet<FunctionDefinition>();
        }
        //Return Event Defintions using Contract as an indentifier to traverse the Event mapping.
        public HashSet<EventDefinition> retrieveEventDefinitionsUsingContract(ContractDefinition contract)
        {
            if (EventMapping.ContainsKey(contract))
            {
                return EventMapping[contract];
            }
            else
            {
                return new HashSet<EventDefinition>();
            }
        }

        //Insert contract into contract defintiions collection.
        public void InsertContractInMapping(ContractDefinition contract)
        {
            ContractDefinitionsMap.Add(contract);
        }

        /**
         * Return the contract name from a collection of contracts (using identifier) 
         */
        public ContractDefinition retrieveContractName(string contractName)
        {
            foreach (ContractDefinition contract in ContractDefinitionsMap)
            {
                if (contract.Name.Equals(contractName))
                {
                    return contract;
                }
            }
            return null;
        }

        //Insert SubTypeMapping to contract parameter.
        public void InsertTypeInContract(ContractDefinition contract, ContractDefinition type)
        {
            if (SubTypeMapping.ContainsKey(contract) == false)
            {
                SubTypeMapping[contract] = new HashSet<ContractDefinition>();
            }
            SubTypeMapping[contract].Add(type);
        }

        //Function to return sub types for a specific contract through indexing it in the collections map
        public HashSet<ContractDefinition> returnSubTypesIndex(ContractDefinition contract)
        {
            return SubTypeMapping[contract];
        }

        public void AddStateVarToContract(ContractDefinition contract, VariableDeclaration Declaration)
        {
            //check whether collection does not already contain the new addition 
            //if not then create a new hashset with the proposed contract and place it into the collection
            if (StateVarMaps.ContainsKey(contract) == false)
            {
                StateVarMaps[contract] = new HashSet<VariableDeclaration>();
            }

            //add the node to the indexed contract within the collection
            StateVarMaps[contract].Add(Declaration);
            //assign statevar collections map with the index of varDec1 the value of the given contract
            StateVarToContractMap[Declaration] = contract;
        }

        //Insert Mapping declaration into contract using 
        public void InsertMappingInContract(ContractDefinition contract, VariableDeclaration mappingDeclaration)
        {
            if (MappingFrame.ContainsKey(contract) == false)
            {
                MappingFrame[contract] = new HashSet<VariableDeclaration>();
            }
            MappingFrame[contract].Add(mappingDeclaration);
        }

        //Insert Array into contract using ArrayMapping.
        public void insertArrayInContract(ContractDefinition contract, VariableDeclaration array)
        {
            if (constructArrayMaps.ContainsKey(contract) == false)
            {
                constructArrayMaps[contract] = new HashSet<VariableDeclaration>();
            }
            constructArrayMaps[contract].Add(array);
        }

        //Retrieve state variables through contract 
        public HashSet<VariableDeclaration> retrieveStateVariables(ContractDefinition contract)
        {
            bool containsKey = StateVarMaps.ContainsKey(contract);

            if (containsKey){
                return StateVarMaps[contract];
            }
            else
            {
                new HashSet<VariableDeclaration>();
            }
            return null;
        }

        //Insert constructor into contract using constructor mapping
        public void InsertConstructorInContract(ContractDefinition contract, FunctionDefinition ctor)
        {
            ConstructorMapping[contract] = ctor;
        }

        //Function to insert fallback definition to contract using fallback mapping
        public void InsertFallBackInContract(ContractDefinition contract, FunctionDefinition fallback)
        {
            FallbackMap[contract] = fallback;
        }

        //Function to check if constructor exists.
        public bool checkConstructorExists(ContractDefinition contract)
        {
            bool contains = ConstructorMapping.ContainsKey(contract);

            return contains;
        }

        //Function to retrieve constructor from ConstructorMap
        public FunctionDefinition retrieveConstructor(ContractDefinition contract)
        {
            if (ConstructorMapping.ContainsKey(contract))
            {
                return ConstructorMapping[contract];
            }
            return null;
        }

        //Function to insert an event into the argument contract.
        public void InsertEventInContract(ContractDefinition contract, EventDefinition eventDef)
        {
            //if the event map does not contain the contract, then add it
            //if the eventMap does not contain the event definition, then add it.
            if (EventMapping.ContainsKey(contract) == false)
            {
                EventMapping[contract] = new HashSet<EventDefinition>();
            }

            if (EventMapping[contract].Contains(eventDef) == false)
            {
                EventMapping[contract].Add(eventDef);
            }
        }

        //function to check whether an event name is contain in a contract.
        public bool containsEventName(ContractDefinition contract, string eventName)
        {
            if (!EventMapping.ContainsKey(contract))
            {
                return false;
            }
            foreach (EventDefinition eventDef in EventMapping[contract])
            {
                if (eventName.Equals(eventDef.Name))
                {
                    return true;
                }
            }
            return false;
        }

        //Function to insert a function into the specified contract.
        public void InsertFunctionInContract(ContractDefinition contract, FunctionDefinition funcDef)
        {
            if (FunctionMapping.ContainsKey(contract) == false)
            {
                Debug.Assert(!FunctionSignatureMapping.ContainsKey(contract));
                FunctionMapping[contract] = new HashSet<FunctionDefinition>();
                FunctionSignatureMapping[contract] = new HashSet<string>();
            }

            Debug.Assert(!FunctionMapping[contract].Contains(funcDef), $"Repeated function warning: {funcDef.Name}");
            FunctionMapping[contract].Add(funcDef);

            string signature = Conversion_Utility_Tool.ComputeFunctionSignature(funcDef);
            FunctionSignatureMapping[contract].Add(signature);

            Debug.Assert(!FunctionToContractMap.ContainsKey(funcDef), $" Repeated function warning: {funcDef.Name}");
            FunctionToContractMap[funcDef] = contract;
        }

        public ContractDefinition GetContractByFunction(FunctionDefinition funcDef)
        {
            Debug.Assert(FunctionToContractMap.ContainsKey(funcDef));
            return FunctionToContractMap[funcDef];
        }


        public void AddFunctionToDynamicType(string funcSig, ContractDefinition dynamicType, FunctionDefinition funcDef)
        {
            if (!SignatureFunctionMap.ContainsKey(funcSig))
            {
                SignatureFunctionMap[funcSig] = new Dictionary<ContractDefinition, FunctionDefinition>();
            }

            // may potentially override the previous value due to inheritance
            SignatureFunctionMap[funcSig][dynamicType] = funcDef;
        }

        public bool doesContainFunctionSignature(string funcSig)
        {
            return SignatureFunctionMap.ContainsKey(funcSig);
        }

        //Function to return definitions.
        public Dictionary<ContractDefinition, FunctionDefinition> returnFunctionDefintiions(string functionSignature)
        {
            return SignatureFunctionMap[functionSignature];
        }

        public void AddVisibleFunctionToContract(FunctionDefinition funcDef, ContractDefinition contract)
        {
            if (FunctionsMap.ContainsKey(contract) == false)
            {
                FunctionsMap[contract] = new HashSet<FunctionDefinition>();
            }
            else
            {
                //do nothing
            }
            FunctionsMap[contract].Add(funcDef);
        }


        //Function to return a hashset of functiondefinitions that are retrieved using a contract instance.
        public HashSet<FunctionDefinition> RetrieveVisibleFunctions(ContractDefinition contract)
        {
            if (FunctionsMap.ContainsKey(contract)){
                return FunctionsMap[contract];
            }
            else
            {
                new HashSet<FunctionDefinition>();
            }
            //should not reutnr null, but there lies a possiblity
            return null;
        }



        //Function to check whether conditional contains statevar
        public bool ContainsStateVar(string varName)
        {
            return stateVarLookUpTable.ContainsKey(varName);
        }

        //returns state variable using lookup map with an index of dynamic type
        public VariableDeclaration retrieveStateVarDynamicType(string variableName, ContractDefinition dynamicType)
        {
            return stateVarLookUpTable[variableName][dynamicType];
        }

        //generate fresh identifier using BoogieType
        public BoogieTypedIdent createFreshIdentifier(BoogieType type)
        {
            //return new BoogieTypedIdent.
            string name = "__var_" + indentifierCount;
            return new BoogieTypedIdent(name, type);
        }

        private void instantiateMappings()
        {
            ContractDefinitionsMap = new HashSet<ContractDefinition>();
            ASTNodeToSourcePathMap = new Dictionary<ASTNode, string>();
            ASTLineNumberMap = new Dictionary<ASTNode, int>();
            SubTypeMapping = new Dictionary<ContractDefinition, HashSet<ContractDefinition>>();
            StateVarMaps = new Dictionary<ContractDefinition, HashSet<VariableDeclaration>>();
            MappingFrame = new Dictionary<ContractDefinition, HashSet<VariableDeclaration>>();
            constructArrayMaps = new Dictionary<ContractDefinition, HashSet<VariableDeclaration>>();
            StateVarToContractMap = new Dictionary<VariableDeclaration, ContractDefinition>();
            ConstructorMapping = new Dictionary<ContractDefinition, FunctionDefinition>();
            FallbackMap = new Dictionary<ContractDefinition, FunctionDefinition>();
            EventMapping = new Dictionary<ContractDefinition, HashSet<EventDefinition>>();
            EventToContractMap = new Dictionary<EventDefinition, ContractDefinition>();
            FunctionMapping = new Dictionary<ContractDefinition, HashSet<FunctionDefinition>>();
            FunctionSignatureMapping = new Dictionary<ContractDefinition, HashSet<string>>();
            FunctionToContractMap = new Dictionary<FunctionDefinition, ContractDefinition>();
            SignatureFunctionMap = new Dictionary<string, Dictionary<ContractDefinition, FunctionDefinition>>();
            FunctionsMap = new Dictionary<ContractDefinition, HashSet<FunctionDefinition>>();
            stateVarLookUpTable = new Dictionary<string, Dictionary<ContractDefinition, VariableDeclaration>>();
            VisibleStateMapping = new Dictionary<ContractDefinition, HashSet<VariableDeclaration>>();
            usingMap = new Dictionary<ContractDefinition, Dictionary<UserDefinedTypeName, TypeName>>();
        }

    }
}
