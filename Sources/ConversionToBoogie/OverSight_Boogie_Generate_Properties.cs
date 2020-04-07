

namespace ConversionToBoogie
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Boogie_Syntax_Tree;
    using Sol_Syntax_Tree;

    /**
     * Public Generation class for Boogie Properties 
     * Invoked via ConversionToBoogieMain,cs
     */
    public class OverSight_Boogie_Generate_Properties
    {
        // AST_Handler context to leverage "getProgram" and updated with
        // Boogie Types, Constants, Functions, Global Variables and Global Implementations 
        private AST_Handler context;

        public OverSight_Boogie_Generate_Properties(AST_Handler context){ this.context = context; }

        //Function invoked via ConversionToBoogie_Main.cs to generate default boogie properties expected by verifier.
        public void GenerateBoogieProperties()
        {
            buildTypes(); //Generate types and add to classContext.BoogieProgram
            buildConstants(); //Generate Constants and add to classContext.BoogieProgram
            buildFunctions(); //Generate Functions and add to classContext.BoogieProgram
            buildGlobalVariables(); //Generate GlobalVariables and add to classContext.BoogieProgram
            buildGlobalImplementations(); //Generate Global implementations and add to classContext.BoogieProgram
        }
        /**
         * Build types and add them to Boogie Program attached to the Handler reference.
         */
        private void buildTypes()
        {
            context.getProgram.AddBoogieDeclaration(new BoogieTypeCtorDecl("Ref"));
            context.getProgram.AddBoogieDeclaration(new BoogieTypeCtorDecl("ContractName"));
        }

        //Build functions and add them as declarations to the Boogie program located within AST_Handler
        private void buildFunctions()
        { 
            context.getProgram.AddBoogieDeclaration(BuildConstantToReferenceFunction());//conversion of primitve type int to refrence type
            context.getProgram.AddBoogieDeclaration(BuildModularFunction());//modulo operation for unsigned integers
            context.getProgram.AddBoogieDeclaration(buildOverSightSumFunction());//Used for creation of summaping in specification as Boogie Function
        }

        /**
        * Function to handle global implementation generation
        */
        private void buildGlobalImplementations()
        {
            BuildGlobalProcedureFresh();
            GenerateGlobalProcedureAllocMany();
            BuildBoogieRecordType("int", BoogieType.Int);
            BuildBoogieRecordType("ref", BoogieType.Ref);
            BuildBoogieRecordType("bool", BoogieType.Bool);
            buildStructConstructor();
        }

        //Build ConstantToReferenceFunction
        private BoogieFunction BuildConstantToReferenceFunction()
        {
            //function for Int to Ref
            var inVar = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int));
            var outVar = new BoogieFormalParam(new BoogieTypedIdent("ret", BoogieType.Ref));
            return new BoogieFunction(
                "ConstantToRef",
                new List<BoogieVariable>() { inVar },
                new List<BoogieVariable>() { outVar },
                null);
        }
        //Function to construct modular function call witin bpl file when leveraging the boogie verifier.
        private BoogieFunction BuildModularFunction()
        {
            //function for arithmetic "modulo" operation for unsigned integers
            string functionName = "modBpl";

            var inputVariable_1 = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int));
            var inputVariable_2 = new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Int));

            var outVar = new BoogieFormalParam(new BoogieTypedIdent("ret", BoogieType.Int));

            return new BoogieFunction(
                functionName,
                new List<BoogieVariable>() { inputVariable_1, inputVariable_2 },
                new List<BoogieVariable>() { outVar },
                new List<BoogieAttribute> { new BoogieAttribute("bvbuiltin", "\"" + "mod" + "\"") });
        }

        //Function to construct OverSight Sum function builder as part of the BoogieProgram.
        private BoogieFunction buildOverSightSumFunction()
        {
            //function for [Ref]int to int
            var inputVariables = new BoogieFormalParam(new BoogieTypedIdent("x", new BoogieMapType(BoogieType.Ref, BoogieType.Int)));
            var outputVriables = new BoogieFormalParam(new BoogieTypedIdent("ret", BoogieType.Int));
            return new BoogieFunction(
                "_SumMapping_OverSight",
                new List<BoogieVariable>() { inputVariables },
                new List<BoogieVariable>() { outputVriables },
                null);
        }

        //Function to introduce constant properties to Boogie such as REF and Constant
        private void buildConstants()
        {
            BoogieConstant nullConstant = new BoogieConstant(new BoogieTypedIdent("null", BoogieType.Ref), true);
            context.getProgram.AddBoogieDeclaration(nullConstant);

            // constants for contract names
            BoogieCtorType tnameType = new BoogieCtorType("ContractName");
            foreach (ContractDefinition contract in context.ContractDefinitionsMap)
            {
                BoogieTypedIdent typedIdent = new BoogieTypedIdent(contract.Name, tnameType);
                BoogieConstant contractNameConstant = new BoogieConstant(typedIdent, true);
                context.getProgram.AddBoogieDeclaration(contractNameConstant);
                foreach(var node in contract.Nodes)
                {
                    if (node is StructDefinition structDefn)
                    {
                        var structTypedIdent = new BoogieTypedIdent(structDefn.CanonicalName, tnameType);
                        context.getProgram.AddBoogieDeclaration(new BoogieConstant(structTypedIdent, true));
                    }
                }
            }
        }

        //Function to build standard global variables for Boogie expressions set.
        private void buildGlobalVariables()
        {
            BoogieTypedIdent balanceId = new BoogieTypedIdent("Balance", new BoogieMapType(BoogieType.Ref, BoogieType.Int));
            BoogieGlobalVariable balanceVar = new BoogieGlobalVariable(balanceId);
            context.getProgram.AddBoogieDeclaration(balanceVar);

            BoogieTypedIdent dtypeId = new BoogieTypedIdent("DType", new BoogieMapType(BoogieType.Ref, new BoogieCtorType("ContractName")));
            BoogieGlobalVariable dtype = new BoogieGlobalVariable(dtypeId);
            context.getProgram.AddBoogieDeclaration(dtype);

            BoogieTypedIdent addrBalanceId = new BoogieTypedIdent("balance_ADDR", new BoogieMapType(BoogieType.Ref, BoogieType.Int));
            BoogieGlobalVariable addrBalance = new BoogieGlobalVariable(addrBalanceId);
            context.getProgram.AddBoogieDeclaration(addrBalance);

            BoogieTypedIdent allocId = new BoogieTypedIdent("Alloc", new BoogieMapType(BoogieType.Ref, BoogieType.Bool));
            BoogieGlobalVariable alloc = new BoogieGlobalVariable(allocId);
            context.getProgram.AddBoogieDeclaration(alloc);

            // generate global variables for each array/mapping type to model memory
            ConstructMemoryVariables();

            BoogieMapType type = new BoogieMapType(BoogieType.Ref, BoogieType.Int);
            BoogieTypedIdent arrayLengthId = new BoogieTypedIdent("Length", type);
            BoogieGlobalVariable arrayLength = new BoogieGlobalVariable(arrayLengthId);
            context.getProgram.AddBoogieDeclaration(arrayLength);

            if (context.TranslateFlags.ModelReverts == true)
            {
                BoogieTypedIdent revertId = new BoogieTypedIdent("revert", BoogieType.Bool);
                BoogieGlobalVariable revert = new BoogieGlobalVariable(revertId);
                context.getProgram.AddBoogieDeclaration(revert);//
            }

            if (Flags_HelperClass.InstrumentGas == true)
            {
                BoogieTypedIdent gasId = new BoogieTypedIdent("gas", BoogieType.Int);
                BoogieGlobalVariable gas = new BoogieGlobalVariable(gasId);
                context.getProgram.AddBoogieDeclaration(gas);
            }
        }


        //Function to generate struct constructors through iteration of contract definition collection set.
        private void buildStructConstructor()
        {
            foreach (ContractDefinition contract in context.ContractDefinitionsMap)
            {
                foreach (var node in contract.Nodes)
                {
                    if (node is StructDefinition structDefn)
                    {
                        GenerateStructConstructors(contract, structDefn);
                    }
                }
            }
        }

        private void GenerateStructConstructors(ContractDefinition contract, StructDefinition structDefn)
        {
            // generate the internal one without base constructors
            string procName = structDefn.CanonicalName + "_ctor";
            List<BoogieVariable> inParams = new List<BoogieVariable>();
            inParams.AddRange(Conversion_Utility_Tool.GetDefaultInParams());
            foreach(var member in structDefn.Members)
            {
                Debug.Assert(!member.TypeDescriptions.IsStruct(), "Do no handle nested structs yet!");
                var formalType = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(member.TypeName);
                var formalName = member.Name;
                inParams.Add(new BoogieFormalParam(new BoogieTypedIdent(formalName, formalType)));
            }

            List<BoogieVariable> outParams = new List<BoogieVariable>();
            List<BoogieAttribute> attributes = new List<BoogieAttribute>();
            if (Flags_HelperClass.GenerateInlineAttributes)
            {
                attributes.Add(new BoogieAttribute("inline", 1));
            };
            BoogieProcedure procedure = new BoogieProcedure(procName, inParams, outParams, attributes);
            context.getProgram.AddBoogieDeclaration(procedure);

            List<BoogieVariable> localVars = new List<BoogieVariable>();
            BoogieStmtList procBody = new BoogieStmtList();

            foreach (var member in structDefn.Members)
            {
                Debug.Assert(!member.TypeDescriptions.IsStruct(), "Do no handle nested structs yet!");
                var mapName = member.Name + "_" + structDefn.CanonicalName;
                var formalName = member.Name;
                var mapSelectExpr = new BoogieMapSelect(new BoogieIdentifierExpr(mapName), new BoogieIdentifierExpr("this"));
                procBody.AddStatement(new BoogieAssignCmd(mapSelectExpr, new BoogieIdentifierExpr(member.Name)));
            }

            BoogieImplementation implementation = new BoogieImplementation(procName, inParams, outParams, localVars, procBody);
            context.getProgram.AddBoogieDeclaration(implementation);
        }


        private void BuildBoogieRecordType(string typeName, BoogieType btype)
        {

            // generate the internal one without base constructors
            string procName = "boogie_si_record_solidity2Boogie_" + typeName;
            var inVar = new BoogieFormalParam(new BoogieTypedIdent("x", btype));
            List<BoogieVariable> inParams = new List<BoogieVariable>() { inVar };
            List<BoogieVariable> outParams = new List<BoogieVariable>();

            BoogieProcedure procedure = new BoogieProcedure(procName, inParams, outParams, null);
            context.getProgram.AddBoogieDeclaration(procedure);
        }

        //Generate Fresh Procedure Boogie Type and add as new reference to BoogieProgram
        private void BuildGlobalProcedureFresh()
        {
            // generate the internal one without base constructors
            string procName = "FreshRefGenerator";
            List<BoogieVariable> inParams = new List<BoogieVariable>();

            var outVar = new BoogieFormalParam(new BoogieTypedIdent("newRef", BoogieType.Ref));
            List<BoogieVariable> outParams = new List<BoogieVariable>()
            {
                outVar
            };
            List<BoogieAttribute> attributes = new List<BoogieAttribute>();
            if (Flags_HelperClass.GenerateInlineAttributes)
            {
                attributes.Add(new BoogieAttribute("inline", 1));
            };
            BoogieProcedure procedure = new BoogieProcedure(procName, inParams, outParams, attributes);
            context.getProgram.AddBoogieDeclaration(procedure);

            List<BoogieVariable> localVars = new List<BoogieVariable>();
            BoogieStmtList procBody = new BoogieStmtList();

            var outVarIdentifier = new BoogieIdentifierExpr("newRef");
            BoogieIdentifierExpr allocIdentExpr = new BoogieIdentifierExpr("Alloc");
            // havoc tmp;
            procBody.AddStatement(new BoogieHavocCmd(outVarIdentifier));
            // assume Alloc[tmp] == false;
            BoogieMapSelect allocMapSelect = new BoogieMapSelect(allocIdentExpr, outVarIdentifier);
            BoogieExpr allocAssumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, allocMapSelect, new BoogieLiteralExpr(false));
            procBody.AddStatement(new BoogieAssumeCmd(allocAssumeExpr));
            // Alloc[tmp] := true;
            procBody.AddStatement(new BoogieAssignCmd(allocMapSelect, new BoogieLiteralExpr(true)));
            // assume tmp != null
            procBody.AddStatement(new BoogieAssumeCmd(
                              new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, outVarIdentifier, new BoogieIdentifierExpr("null"))));

            BoogieImplementation implementation = new BoogieImplementation(procName, inParams, outParams, localVars, procBody);
            context.getProgram.AddBoogieDeclaration(implementation);
        }

        //Function to produce memory allocation as boogie reference types using "alloc"
        private void GenerateGlobalProcedureAllocMany()
        {
            // generate the internal one without base constructors
            string procName = "HavocAllocMany";
            List<BoogieVariable> inParams = new List<BoogieVariable>();
            List<BoogieVariable> outParams = new List<BoogieVariable>();
            List<BoogieAttribute> attributes = new List<BoogieAttribute>();
            if (Flags_HelperClass.GenerateInlineAttributes)
            {
                attributes.Add(new BoogieAttribute("inline", 1));
            };
            BoogieProcedure procedure = new BoogieProcedure(procName, inParams, outParams, attributes);
            context.getProgram.AddBoogieDeclaration(procedure);

            var oldAlloc = new BoogieLocalVariable(new BoogieTypedIdent("oldAlloc", new BoogieMapType(BoogieType.Ref, BoogieType.Bool)));
            List<BoogieVariable> localVars = new List<BoogieVariable>() {oldAlloc};
            
            BoogieStmtList procBody = new BoogieStmtList();
            BoogieIdentifierExpr oldAllocIdentExpr = new BoogieIdentifierExpr("oldAlloc");
            BoogieIdentifierExpr allocIdentExpr = new BoogieIdentifierExpr("Alloc");
            // oldAlloc = Alloc
            procBody.AddStatement(new BoogieAssignCmd(oldAllocIdentExpr, allocIdentExpr));            
            // havoc Alloc
            procBody.AddStatement(new BoogieHavocCmd(allocIdentExpr));


            var qVar = QVarGenerator.NewQVar(0, 0);
            BoogieMapSelect allocMapSelect = new BoogieMapSelect(allocIdentExpr, qVar);
            BoogieMapSelect oldAllocMapSelect = new BoogieMapSelect(oldAllocIdentExpr, qVar);
            BoogieExpr allocAssumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.IMP, oldAllocMapSelect, allocMapSelect);
            procBody.AddStatement(new BoogieAssumeCmd(new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() {qVar}, new List<BoogieType>() { BoogieType.Ref }, allocAssumeExpr)));

            BoogieImplementation implementation = new BoogieImplementation(procName, inParams, outParams, localVars, procBody);
            context.getProgram.AddBoogieDeclaration(implementation);
        }


    
        //Function to construct memory variables for ArrayMap.keys and Mapping Frame
        private void ConstructMemoryVariables()
        {
            HashSet<KeyValuePair<BoogieType, BoogieType>> generatedTypes = new HashSet<KeyValuePair<BoogieType, BoogieType>>();
            // mappings
            foreach (ContractDefinition contract in context.MappingFrame.Keys)
            {
                foreach (VariableDeclaration variableDeclaration in context.MappingFrame[contract])
                {
                    //Iteratve over MappingFrame and generate keys 
                    Debug.Assert(variableDeclaration.TypeName is Mapping);
                    Mapping mapping = variableDeclaration.TypeName as Mapping;
                    ConstructMappingMemoryArray(mapping, generatedTypes);
                }
            }
            //Iterate over array maps and construct appropriate memory maps from this.
            foreach (ContractDefinition contract in context.constructArrayMaps.Keys)
            {
                foreach (VariableDeclaration variableDeclaration in context.constructArrayMaps[contract])
                {
                    Debug.Assert(variableDeclaration.TypeName is ArrayTypeName);
                    ArrayTypeName array = variableDeclaration.TypeName as ArrayTypeName;
                    ConstructArrayMemoryMap(array, generatedTypes);
                }
            }
        }

        //Construct Memory Maps for Arrays for Mapping and ArrayTypeName types.
        private void ConstructMappingMemoryArray(Mapping mapping, HashSet<KeyValuePair<BoogieType, BoogieType>> generatedTypes)
        {
            BoogieType boogieKeyType = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(mapping.KeyType);
            BoogieType boogieValueType = null;
            if (mapping.ValueType is Mapping submapping)
            {
                boogieValueType = BoogieType.Ref;
                ConstructMappingMemoryArray(submapping, generatedTypes);
            }
            else if (mapping.ValueType is ArrayTypeName array)
            {
                boogieValueType = BoogieType.Ref;
                ConstructArrayMemoryMap(array, generatedTypes);
            }
            else { boogieValueType = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(mapping.ValueType); }

            KeyValuePair<BoogieType, BoogieType> pair = new KeyValuePair<BoogieType,BoogieType>(boogieKeyType, boogieValueType);
            if (!generatedTypes.Contains(pair))
            {
                generatedTypes.Add(pair);
                BuildSingleMapMemoryName(boogieKeyType, boogieValueType);
            }
        }

        //Build Memory Map for Array types, 
        private void ConstructArrayMemoryMap(ArrayTypeName array, HashSet<KeyValuePair<BoogieType, BoogieType>> generatedTypes)
        {
            BoogieType boogieKeyType = BoogieType.Int;
            BoogieType boogieValueType = null;
            if (array.BaseType is ArrayTypeName subarray)
            {
                boogieValueType = BoogieType.Ref;
                ConstructArrayMemoryMap(subarray, generatedTypes); //construct array memory map with sub array
            }
            else if (array.BaseType is Mapping mapping)
            {
                boogieValueType = BoogieType.Ref;
                ConstructMappingMemoryArray(mapping, generatedTypes); // construct array memory map using mapping type
            }
            else {
                boogieValueType = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(array.BaseType); //type cannot be resolve, request type declaration 
            }

            KeyValuePair<BoogieType, BoogieType> pair = new KeyValuePair<BoogieType, BoogieType>(boogieKeyType, boogieValueType);
            if (generatedTypes.Contains(pair) == false)
            {
                generatedTypes.Add(pair);
                BuildSingleMapMemoryName(boogieKeyType, boogieValueType);
            }
        }

        //Build Single Memory Map using key and value types
        private void BuildSingleMapMemoryName(BoogieType keyTypes, BoogieType valueTypes)
        {
            BoogieMapType map = new BoogieMapType(keyTypes, valueTypes);
            map = new BoogieMapType(BoogieType.Ref, map);

            string name = Flags_HelperClass.generateMemoryMapName(keyTypes, valueTypes);
            context.getProgram.AddBoogieDeclaration(new BoogieGlobalVariable(new BoogieTypedIdent(name, map)));
        }
    }
}
