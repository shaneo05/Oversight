﻿

namespace ConversionToBoogie
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using Boogie_Syntax_Tree;
    using Sol_Syntax_Tree;

    /**
     * Leveraged process handler to facilitate greater logic of boogie property translation
     */
    public class OverSight_ProcessHandler : Generic_Syntax_Tree_Visitor
    {
        //context reference containing declarations and current boogie program. 
        private readonly AST_Handler context;

        // used to declare local vars in a Boogie implementation
        private readonly Dictionary<string, List<BoogieVariable>> boogieToLocalVarsMap;

        // current Boogie procedure being translated to
        private string currentBoogieProc = null;

        // update in the visitor for contract definition
        private ContractDefinition currentContract = null;
        // update in the visitor for function definition
        private FunctionDefinition currentFunction = null;

        // information about current file and linenumber
        private string currentSourceFile = null;
        private int currentSourceLine = -1;
        
        // store the Boogie call for modifier postlude
        private BoogieStmtList currentPostlude = null;

        // to generate inline attributes 
        private readonly bool genInlineAttrsInBpl;

        //Constructor to handle assignmen of AST properties amongst intialisation of other attributes.
        public OverSight_ProcessHandler(AST_Handler context, bool _genInlineAttrsInBpl = true)
        {
            this.context = context;
            boogieToLocalVarsMap = new Dictionary<string, List<BoogieVariable>>();
            genInlineAttrsInBpl = _genInlineAttrsInBpl;
            ContractInvariants = new Dictionary<string, List<BoogieExpr>>();
        }

        private static void emitGasCheck(BoogieStmtList newBody)
        {
            BoogieStmtList thenBody = new BoogieStmtList();
            thenBody.AddStatement(new BoogieReturnCmd());

            newBody.AddStatement(new BoogieIfCmd(
                new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LT, new BoogieIdentifierExpr("gas"),
                    new BoogieLiteralExpr(0)), thenBody, null));
        }

        private void preTranslationAction(ASTNode node)
        {
            if (Flags_HelperClass.InstrumentGas)
            {
                if (node.GasCost > 0)
                    // gas := gas - node.GasCost
                    currentStmtList.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr("gas"), 
                        new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.SUB, new BoogieIdentifierExpr("gas"), new BoogieLiteralExpr(node.GasCost))));

                if (context.TranslateFlags.ModelReverts)
                {
                    if (node is Continue)
                    {
                        emitGasCheck(currentStmtList);
                    }
                }
            }
        }

   
        
        public override bool ContractDefinition_ReTraceNode(ContractDefinition node)
        {
            preTranslationAction(node);

            currentContract = node;

            if (currentContract.ContractKind == EnumContractKind.LIBRARY &&
                currentContract.Name.Equals("OverSight"))
                return true;

            // generate default empty constructor if there is no constructor explicitly defined
            if (!context.checkConstructorExists(node))
            {
                GenerateDefaultConstructor(node);
            }

            foreach (ASTNode child in node.Nodes)
            {
                if (child is VariableDeclaration varDecl)
                {
                    TranslateStateVarDeclaration(varDecl);
                }
                else if (child is StructDefinition structDefn)
                {
                    TranslateStructDefinition(structDefn);
                }
                else
                {
                    child.Accept(this);
                }
            }

            return false;
        }

        private void TranslateStructDefinition(StructDefinition structDefn)
        {
            foreach(var member in structDefn.Members)
            {
                OverSightAssert(!member.TypeDescriptions.IsStruct(),
                    "Do no handle nested structs yet!");
                var type = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(member.TypeName);
                var mapType = new BoogieMapType(BoogieType.Ref, type);
                var mapName = member.Name + "_" + structDefn.CanonicalName;
                context.getProgram.AddBoogieDeclaration(new BoogieGlobalVariable(new BoogieTypedIdent(mapName, mapType)));
            }
        }

        private void TranslateStateVarDeclaration(VariableDeclaration varDecl)
        {
            OverSightAssert(varDecl.StateVariable, $"{varDecl} is not a state variable");

            string name = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
            BoogieType type = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(varDecl.TypeName);
            BoogieMapType mapType = new BoogieMapType(BoogieType.Ref, type);

            // Issue a warning for intXX variables in case /useModularArithemtic option is used:
            if (Flags_HelperClass.UseModularArithmetic && varDecl.TypeDescriptions.IsInt())
            {
                Console.WriteLine($"Warning: signed integer arithmetic is not handled with /useModularArithmetic option");
            }

            if (varDecl.TypeName is Mapping)
            {
                context.getProgram.AddBoogieDeclaration(new BoogieGlobalVariable(new BoogieTypedIdent(name, mapType)));
            }
            else if (varDecl.TypeName is ArrayTypeName)
            {
                //array variables can be assigned
                context.getProgram.AddBoogieDeclaration(new BoogieGlobalVariable(new BoogieTypedIdent(name, mapType)));
            }
            else // other type of state variables
            {
                context.getProgram.AddBoogieDeclaration(new BoogieGlobalVariable(new BoogieTypedIdent(name, mapType)));
            }
        }

        public override bool Visit(EnumDefinition node)
        {
            preTranslationAction(node);
            // do nothing
            return false;
        }

        private BoogieCallCmd InstrumentForPrintingData(TypeDescription type, BoogieExpr value, string name)
        {
            // don't emit the instrumentation 
            if (Flags_HelperClass.NoDataValuesInfoFlag)
                return null;

            if (type.IsDynamicArray() || type.IsStaticArray())
                return null;

            if (type.IsAddress() || type.IsContract())
            {
                // Skipping dynamic and static array types:
                var callCmd = new BoogieCallCmd("boogie_si_record_sol2Bpl_ref", new List<BoogieExpr>() { value }, new List<BoogieIdentifierExpr>());
                callCmd.Attributes = new List<BoogieAttribute>
                {
                    new BoogieAttribute("cexpr", $"\"{name}\"")
                };
                return callCmd;
            }
            else if (type.IsInt() || type.IsUint() || type.IsString() || type.IsBytes())
                // TypeString.StartsWith("uint") || type.TypeString.StartsWith("int") || type.TypeString.StartsWith("string ") || type.TypeString.StartsWith("bytes"))
            {
                var callCmd = new BoogieCallCmd("boogie_si_record_sol2Bpl_int", new List<BoogieExpr>() { value }, new List<BoogieIdentifierExpr>());
                callCmd.Attributes = new List<BoogieAttribute>
                {
                    new BoogieAttribute("cexpr", $"\"{name}\"")
                };
                return callCmd;
            }
            else if (type.IsBool())
            {
                var callCmd = new BoogieCallCmd("boogie_si_record_sol2Bpl_bool", new List<BoogieExpr>() { value }, new List<BoogieIdentifierExpr>());
                callCmd.Attributes = new List<BoogieAttribute>
                {
                    new BoogieAttribute("cexpr", $"\"{name}\"")
                };
                return callCmd;
            }

            return null;
        }


        private void PrintArguments(FunctionDefinition node, List<BoogieVariable> inParams, BoogieStmtList currentStmtList)
        {
            // Print dummy first parameter (as a delimeter for parsing corral.txt):
            TypeDescription addrType = new TypeDescription();
            addrType.TypeString = "bool";
            // There's no BoogieLiteralExpr that accepts ref type:
            //BoogieConstant nullConst = new BoogieConstant(new BoogieTypedIdent("null", BoogieType.Ref), true);
            var callCmd = InstrumentForPrintingData(addrType, new BoogieLiteralExpr(false), "_OverSightFirstArg");
            if (callCmd != null)
            {
                currentStmtList.AddStatement(callCmd);
            }

            // Add default parameters "this", "msg.sender", "msg.value"
            addrType = new TypeDescription();
            addrType.TypeString = "address";
            callCmd = InstrumentForPrintingData(addrType, new BoogieIdentifierExpr(inParams[0].Name), "this");
            if (callCmd != null)
            {
                currentStmtList.AddStatement(callCmd);
            }
            callCmd = InstrumentForPrintingData(addrType, new BoogieIdentifierExpr(inParams[1].Name), "msg.sender");
            if (callCmd != null)
            {
                currentStmtList.AddStatement(callCmd);
            }
            var valType = new TypeDescription();
            valType.TypeString = "int";
            callCmd = InstrumentForPrintingData(valType, new BoogieIdentifierExpr(inParams[2].Name), "msg.value");
            if (callCmd != null)
            {
                currentStmtList.AddStatement(callCmd);
            }

            // when we call this for an implicit constructor, we don't have a node, which
            // implies there are no parameters
            if (node == null)
            {
                // Print dummy last parameter (as a delimeter for parsing corral.txt):
                addrType = new TypeDescription();
                addrType.TypeString = "bool";
                callCmd = InstrumentForPrintingData(addrType, new BoogieLiteralExpr(true), "_OverSightLastArg");
                if (callCmd != null)
                {
                    currentStmtList.AddStatement(callCmd);
                }
                return;
            }
                
            foreach (VariableDeclaration param in node.Parameters.Parameters)
            {
                var parType = param.TypeDescriptions ?? null;
                int parIndex = node.Parameters.Parameters.IndexOf(param);
                BoogieVariable parVar = inParams[parIndex + 3];
                string parName = param.Name;
                var parExpr = new BoogieIdentifierExpr(parVar.Name);
                callCmd = InstrumentForPrintingData(parType, parExpr, parName);
                if (callCmd != null)
                {
                    currentStmtList.AddStatement(callCmd);
                }
            }
            // Print dummy last parameter (as a delimeter for parsing corral.txt):
            addrType = new TypeDescription();
            addrType.TypeString = "bool";
            callCmd = InstrumentForPrintingData(addrType, new BoogieLiteralExpr(true), "_OverSightLastArg");
            if (callCmd != null)
            {
                currentStmtList.AddStatement(callCmd);
            }
        }
        public override bool FunctionDefinition_TraceNode(FunctionDefinition node)
        {
            preTranslationAction(node);
            // OverSightAssert(node.IsConstructor || node.Modifiers.Count <= 1, "Multiple Modifiers are not supported yet");
            OverSightAssert(currentContract != null);

            currentFunction = node;

            // procedure name
            string procName = node.Name + "_" + currentContract.Name;

            if (node.ofConstructorType)
            {
                procName += "_NoBaseCtor";
            }
            currentBoogieProc = procName;

            // input parameters
            List<BoogieVariable> inParams = Conversion_Utility_Tool.GetDefaultInParams();
            // initialize statement list to include assumption about parameter types
            currentStmtList = new BoogieStmtList();
            // get all formal input parameters
            node.Parameters.Accept(this);
            inParams.AddRange(currentParamList);

            // Print function argument values to corral.txt for counterexample:
            PrintArguments(node, inParams, currentStmtList);

            // output parameters
            isReturnParameterList = true;
            node.ReturnParameters.Accept(this);
            isReturnParameterList = false;
            List<BoogieVariable> outParams = currentParamList;

            var assumesForParamsAndReturn = currentStmtList;
            currentStmtList = new BoogieStmtList();

            // attributes
            List<BoogieAttribute> attributes = new List<BoogieAttribute>();
            if ((node.Visibility == EnumVisibility.PUBLIC || node.Visibility == EnumVisibility.EXTERNAL)
                && !node.ofConstructorType
                && !node.ofFallBackType) //don't expose fallback for calling directly
            {
                attributes.Add(new BoogieAttribute("public"));
            }
            // generate inline attribute for a function only when /noInlineAttrs is specified
            if (genInlineAttrsInBpl)
                attributes.Add(new BoogieAttribute("inline", 1));

            if (currentContract.ContractKind == EnumContractKind.LIBRARY &&
                currentContract.Name.Equals("OverSight"))
            {
                return false;
            }
            // we add any pre/post conditions after analyzing the boy later
            BoogieProcedure procedure = new BoogieProcedure(procName, inParams, outParams, attributes);
            context.getProgram.AddBoogieDeclaration(procedure);

            // could be just a declaration
            if (!node.Implemented)
            {
                return false;
            }

           
                // local variables and function body
                // TODO: move to earlier
                boogieToLocalVarsMap[currentBoogieProc] = new List<BoogieVariable>();

                // TODO: each local array variable should be distinct and 0 initialized

                BoogieStmtList procBody = new BoogieStmtList();
                currentPostlude = new BoogieStmtList();


                // Add possible assume statements from parameters
                procBody.AppendStmtList(assumesForParamsAndReturn);

                // if payable, then modify the balance
                if (node.StateMutability == EnumStateMutability.PAYABLE)
                {
                    procBody.AddStatement(new BoogieCommentCmd("---- Logic for payable function START "));
                    var balnSender = new BoogieMapSelect(new BoogieIdentifierExpr("Balance"), new BoogieIdentifierExpr("msgsender_MSG"));
                    var balnThis = new BoogieMapSelect(new BoogieIdentifierExpr("Balance"), new BoogieIdentifierExpr("this"));
                    var msgVal = new BoogieIdentifierExpr("msgvalue_MSG");
                    //assume Balance[msg.sender] >= msg.value
                    procBody.AddStatement(new BoogieAssumeCmd(new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, balnSender, msgVal)));
                    //balance[msg.sender] = balance[msg.sender] - msg.value
                    procBody.AddStatement(new BoogieAssignCmd(balnSender, new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.SUB, balnSender, msgVal)));
                    //balance[this] = balance[this] + msg.value
                    procBody.AddStatement(new BoogieAssignCmd(balnThis, new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.ADD, balnThis, msgVal)));
                    procBody.AddStatement(new BoogieCommentCmd("---- Logic for payable function END "));
                }

          
            // generate real constructors
            if (node.ofConstructorType)
            {
                GenerateConstructorWithBaseCalls(node, inParams);
            }

            return false;
        }

        private bool IsOverSightContractInvariantFunction(FunctionDefinition node, BoogieStmtList procBody, out List<BoogieExpr> contractInvs)
        {
            contractInvs = null;
            if (node.Visibility != EnumVisibility.PRIVATE ||
                node.StateMutability != EnumStateMutability.VIEW ||
                node.Parameters.Parameters.Count != 0)
            {
                return false;
            }

            contractInvs = ExtractContractInvariants(procBody);
            return contractInvs.Count > 0;
        }

        public override bool Visit(ModifierDefinition node)
        {
            preTranslationAction(node);
            currentBoogieProc = node.Name + "_pre";
            boogieToLocalVarsMap[currentBoogieProc] = new List<BoogieVariable>();

            Block body = node.Body;
            BoogieStmtList prelude = new BoogieStmtList();
            BoogieStmtList postlude = new BoogieStmtList();

            bool translatingPre = true;
            bool hasPre = false;
            bool hasPost = false;
            foreach (Statement statement in body.Statements)
            {
                if (statement is VariableDeclarationStatement)
                {
                    OverSightAssert(false, "locals within modifiers not supported");
                }
                if (statement is PlaceholderStatement)
                {
                    translatingPre = false;
                    currentBoogieProc = node.Name + "_post";
                    boogieToLocalVarsMap[currentBoogieProc] = new List<BoogieVariable>();
                    continue;
                }
                BoogieStmtList stmtList = TranslateStatement(statement);
                if (translatingPre)
                {
                    prelude.AppendStmtList(stmtList);
                    hasPre = true;
                }
                else
                {
                    postlude.AppendStmtList(stmtList);
                    hasPost = true;
                }
            }
            if (hasPre)
            {
                context.ModifierToBoogiePreImpl[node.Name].LocalVars = boogieToLocalVarsMap[node.Name + "_pre"];
                context.ModifierToBoogiePreImpl[node.Name].StructuredStmts = prelude;
            }
            if (hasPost)
            {
                context.ModifierToBoogiePostImpl[node.Name].LocalVars = boogieToLocalVarsMap[node.Name + "_post"];
                context.ModifierToBoogiePostImpl[node.Name].StructuredStmts = postlude;
            }
            return false;
        }

        // generate the initialization statements for state variables
        // assume message sender is not null
        // assign null to other address variables
        private BoogieStmtList GenerateInitializationStmts(ContractDefinition contract)
        {
            currentStmtList = new BoogieStmtList();
            currentStmtList.AddStatement(new BoogieCommentCmd("start of initialization"));

            // assume msgsender_MSG != null;
            BoogieExpr assumeLhs = new BoogieIdentifierExpr("msgsender_MSG");
            BoogieExpr assumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, assumeLhs, new BoogieIdentifierExpr("null"));
            BoogieAssumeCmd assumeCmd = new BoogieAssumeCmd(assumeExpr);
            currentStmtList.AddStatement(assumeCmd);

            // assign null to other address variables
            foreach (VariableDeclaration varDecl in context.retrieveStateVariables(contract))
            {
                if (varDecl.TypeName is ElementaryTypeName elementaryType)
                {
                    GenerateInitializationForElementaryTypes(varDecl, elementaryType);
                }
            }

            // false/0 initialize mappings
            foreach (VariableDeclaration varDecl in context.retrieveStateVariables(contract))
            {
                if (varDecl.TypeName is Mapping mapping)
                {
                    GenerateInitializationForMappingStateVar(varDecl, mapping);
                }
                else if (varDecl.TypeName is ArrayTypeName array)
                {
                    GenerateInitializationForArrayStateVar(varDecl, array);
                }
            }


            currentStmtList.AddStatement(new BoogieCommentCmd("end of initialization"));

            // TODO: add the initializations outside of constructors

            return currentStmtList;
        }

        private void GenerateInitializationForArrayStateVar(VariableDeclaration varDecl, ArrayTypeName array)
        {
            // Issue a warning for intXX type in case /useModularArithemtic option is used:
            if (Flags_HelperClass.UseModularArithmetic && array.BaseType.ToString().StartsWith("int"))
            {
                Console.WriteLine($"Warning: signed integer arithmetic is not handled with /useModularArithmetic option");
            }

            BoogieMapSelect lhsMap = CreateDistinctArrayMappingAddress(currentStmtList, varDecl);

            // lets also initialize the array Lengths (only for Arrays declared in this class)
            var lengthMapSelect = new BoogieMapSelect(new BoogieIdentifierExpr("Length"), lhsMap);
            var lengthExpr = array.Length == null ? new BoogieLiteralExpr(BigInteger.Zero) : TranslateExpr(array.Length);
            // var lengthEqualsZero = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, lengthMapSelect, new BoogieLiteralExpr(0));
            var lengthConstraint = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, lengthMapSelect, lengthExpr);
            currentStmtList.AddStatement(new BoogieAssumeCmd(lengthConstraint));
        }

        private void GenerateInitializationForMappingStateVar(VariableDeclaration varDecl, Mapping mapping)
        {
            BoogieMapSelect lhsMap = CreateDistinctArrayMappingAddress(currentStmtList, varDecl);

            //nested arrays (only 1 level for now)
            if (mapping.ValueType is ArrayTypeName array)
            {
                Console.WriteLine($"Warning: A mapping with nested array {varDecl.Name} of valuetype {mapping.ValueType.ToString()}");

                InitializeNestedArrayMappingStateVar(varDecl, mapping);
            }
            else if (mapping.ValueType is Mapping mappingNested)
            {
                InitializeNestedArrayMappingStateVar(varDecl, mapping);
                // TODO: add the initialization of m[i][j]
            }
            else if (mapping.ValueType is UserDefinedTypeName userTypeName ||
                mapping.ValueType.ToString().Equals("address") ||
                mapping.ValueType.ToString().Equals("address payable") ||
                mapping.ValueType.ToString().StartsWith("bytes")
                )
            {
                currentStmtList.AddStatement(new BoogieCommentCmd($"Initialize address/contract mapping {varDecl.Name}"));

                GetBoogieTypesFromMapping(varDecl, mapping, out BoogieType mapKeyType, out BoogieMapSelect lhs);
                var qVar = QVarGenerator.NewQVar(0, 0);
                BoogieExpr zeroExpr = new BoogieIdentifierExpr("null");

                if (mapping.ValueType.ToString().StartsWith("bytes"))
                    zeroExpr = new BoogieLiteralExpr(BigInteger.Zero);

                var bodyExpr = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    new BoogieMapSelect(lhs, qVar),
                    zeroExpr);
                var qExpr = new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() { qVar }, new List<BoogieType>() { mapKeyType }, bodyExpr);
                currentStmtList.AddStatement(new BoogieAssumeCmd(qExpr));
            }
            else if (mapping.ValueType.ToString().Equals("bool"))
            {
                currentStmtList.AddStatement(new BoogieCommentCmd($"Initialize Boolean mapping {varDecl.Name}"));

                BoogieType mapKeyType;
                BoogieMapSelect lhs;
                GetBoogieTypesFromMapping(varDecl, mapping, out mapKeyType, out lhs);
                var qVar = QVarGenerator.NewQVar(0, 0);
                var bodyExpr = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, new BoogieMapSelect(lhs, qVar));
                var qExpr = new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() { qVar }, new List<BoogieType>() { mapKeyType }, bodyExpr);
                currentStmtList.AddStatement(new BoogieAssumeCmd(qExpr));
            }
            // TODO: Cleanup, StartsWith("uint") can include uint[12] as well. 
            else if (mapping.ValueType.ToString().StartsWith("uint") ||
                mapping.ValueType.ToString().StartsWith("int"))
            {
                // Issue a warning for intXX type in case /useModularArithemtic option is used:
                if (Flags_HelperClass.UseModularArithmetic && mapping.ValueType.ToString().StartsWith("int"))
                {
                    Console.WriteLine($"Warning: signed integer arithmetic is not handled with /useModularArithmetic option");
                }

                currentStmtList.AddStatement(new BoogieCommentCmd($"Initialize Integer mapping {varDecl.Name}"));

                BoogieType mapKeyType;
                BoogieMapSelect lhs;
                GetBoogieTypesFromMapping(varDecl, mapping, out mapKeyType, out lhs);
                var qVar = QVarGenerator.NewQVar(0, 0);
                var bodyExpr = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    new BoogieMapSelect(lhs, qVar),
                    new BoogieLiteralExpr(0));
                var qExpr = new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() { qVar }, new List<BoogieType>() { mapKeyType }, bodyExpr);
                currentStmtList.AddStatement(new BoogieAssumeCmd(qExpr));
            }
            else
            {
                Console.WriteLine($"Warning: A mapping with complex value type {varDecl.Name} of valuetype {mapping.ValueType.ToString()}");
            }
        }

        private void GenerateInitializationForElementaryTypes(VariableDeclaration varDecl, ElementaryTypeName elementaryType)
        {
            if (elementaryType.TypeDescriptions.TypeString.Equals("address") || elementaryType.TypeDescriptions.TypeString.Equals("address payable"))
            {
                string varName = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
                BoogieExpr lhs = new BoogieMapSelect(new BoogieIdentifierExpr(varName), new BoogieIdentifierExpr("this"));
                BoogieExpr rhs = new BoogieIdentifierExpr("null");
                if (varDecl.Value != null)
                {
                    OverSightAssert(varDecl.Value is FunctionCall, $"We only support initialization of hte form address x = address(...);, found {varDecl.Value.ToString()}");
                    rhs = TranslateExpr(varDecl.Value);
                }
                BoogieAssignCmd assignCmd = new BoogieAssignCmd(lhs, rhs);
                currentStmtList.AddStatement(assignCmd);
            }
            else if (elementaryType.TypeDescriptions.TypeString.Equals("bool"))
            {
                string varName = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
                BoogieExpr lhs = new BoogieMapSelect(new BoogieIdentifierExpr(varName), new BoogieIdentifierExpr("this"));
                bool value = false;
                if (varDecl.Value != null && varDecl.Value.ToString() == "true")
                {
                    value = true;
                }
                BoogieAssignCmd assignCmd = new BoogieAssignCmd(lhs, new BoogieLiteralExpr(value));
                currentStmtList.AddStatement(assignCmd);
            }
            else if (elementaryType.TypeDescriptions.TypeString.Equals("string"))
            {
                string x = "";
                if (varDecl.Value != null)
                {
                    x = varDecl.Value.ToString();
                    x = x.Substring(1, x.Length - 2);  //to strip off the single quotations
                }
                int hashCode = x.GetHashCode();
                BigInteger num = new BigInteger(hashCode);
                string varName = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
                BoogieExpr lhs = new BoogieMapSelect(new BoogieIdentifierExpr(varName), new BoogieIdentifierExpr("this"));
                BoogieAssignCmd assignCmd = new BoogieAssignCmd(lhs, new BoogieLiteralExpr(num));
                currentStmtList.AddStatement(assignCmd);
            }
            else //it is integer valued
            {
                // Issue a warning for intXX variables in case /useModularArithemtic option is used:
                if (Flags_HelperClass.UseModularArithmetic && varDecl.TypeDescriptions.IsInt())
                {
                    Console.WriteLine($"Warning: signed integer arithmetic is not handled with /useModularArithmetic option");
                }

                string varName = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
                BoogieExpr lhs = new BoogieMapSelect(new BoogieIdentifierExpr(varName), new BoogieIdentifierExpr("this"));
                var bigInt = (BoogieExpr)new BoogieLiteralExpr(BigInteger.Zero);
                if (varDecl.Value != null)
                {
                    bigInt = TranslateExpr(varDecl.Value); //TODO: any complex expression will crash
                }
                BoogieAssignCmd assignCmd = new BoogieAssignCmd(lhs, bigInt);
                currentStmtList.AddStatement(assignCmd);
            }
        }

        private void InitializeNestedArrayMappingStateVar(VariableDeclaration varDecl, Mapping mapping)
        {
            currentStmtList.AddStatement(new BoogieCommentCmd($"Initialize length of 1-level nested array in {varDecl.Name}"));
            // Issue with inferring Array[] expressions in GetBoogieTypesFromMapping (TODO: use GetBoogieTypesFromMapping after fix)
            var mapKeyType = Flags_HelperClass.getBoogieExpression(mapping.KeyType.TypeDescriptions.ToString());
            string mapName = Flags_HelperClass.generateMemoryMapName(mapKeyType, BoogieType.Ref);
            string varName = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
            var varExpr = new BoogieIdentifierExpr(varName);
            //lhs is Mem_t_ref[x[this]]
            var lhs0 = new BoogieMapSelect(new BoogieIdentifierExpr(mapName),
                new BoogieMapSelect(varExpr, new BoogieIdentifierExpr("this")));
            var qVar1 = QVarGenerator.NewQVar(0, 0);
            //lhs is Mem_t_ref[x[this]][i]
            var lhs1 = new BoogieMapSelect(lhs0, qVar1);
            //Length[Mem_t_ref[x[this]][i]] == 0
            var bodyExpr = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.EQ,
                new BoogieMapSelect(new BoogieIdentifierExpr("Length"), lhs1),
                new BoogieLiteralExpr(0));
            var qExpr = new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() { qVar1 }, new List<BoogieType>() { mapKeyType }, bodyExpr);
            currentStmtList.AddStatement(new BoogieAssumeCmd(qExpr));

            //Nested arrays are disjoint and disjoint from other addresses
            BoogieExpr allocExpr = new BoogieMapSelect(new BoogieIdentifierExpr("Alloc"), lhs1);
            var negAllocExpr = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, allocExpr);
            var negAllocQExpr = new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() { qVar1 }, new List<BoogieType>() { mapKeyType }, negAllocExpr);
            //assume forall i !Alloc[M_t_ref[x[this]][i]]
            currentStmtList.AddStatement(new BoogieAssumeCmd(negAllocQExpr));
            //call HavocAllocMany()
            currentStmtList.AddStatement(new BoogieCallCmd("HavocAllocMany", new List<BoogieExpr>(), new List<BoogieIdentifierExpr>()));
            //assume forall i. Alloc[M_t_ref[x[this]][i]]
            var allocQExpr = new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() { qVar1 }, new List<BoogieType>() { mapKeyType }, allocExpr);
            currentStmtList.AddStatement(new BoogieAssumeCmd(allocQExpr));

            //Two different keys/indices within the same array are distinct
            //forall i, j: i != j ==> M_t_ref[x[this]][i] != M_t_ref[x[this]][j]
            var qVar2 = QVarGenerator.NewQVar(0, 1);
            var lhs2 = new BoogieMapSelect(lhs0, qVar2);
            var distinctQVars = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, qVar1, qVar2);
            var distinctLhs = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, lhs1, lhs2);
            var triggers = new List<BoogieExpr>() { lhs1, lhs2 };

            var neqExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.OR, distinctQVars, distinctLhs);
            var distinctQExpr = new BoogieQuantifiedExpr(true, new List<BoogieIdentifierExpr>() { qVar1, qVar2 }, new List<BoogieType>() { mapKeyType, mapKeyType }, neqExpr, triggers);
            currentStmtList.AddStatement(new BoogieAssumeCmd(distinctQExpr));
        }

        private BoogieMapSelect CreateDistinctArrayMappingAddress(BoogieStmtList stmtList, VariableDeclaration varDecl)
        {
            // define a local variable to generate a fresh constant
            BoogieLocalVariable tmpVar = new BoogieLocalVariable(context.createFreshIdentifier(BoogieType.Ref));
            boogieToLocalVarsMap[currentBoogieProc].Add(tmpVar);
            BoogieIdentifierExpr tmpVarIdentExpr = new BoogieIdentifierExpr(tmpVar.Name);

            stmtList.AddStatement(new BoogieCommentCmd($"Make array/mapping vars distinct for {varDecl.Name}"));
            var lhs = new BoogieIdentifierExpr(Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context));
            var lhsMap = new BoogieMapSelect(lhs, new BoogieIdentifierExpr("this"));
            stmtList.AddStatement(new BoogieCallCmd(
                "FreshRefGenerator",
                new List<BoogieExpr>(),
                new List<BoogieIdentifierExpr>() {
                            tmpVarIdentExpr
                }
                ));

            stmtList.AddStatement(new BoogieAssignCmd(lhsMap, tmpVarIdentExpr));
            return lhsMap;
        }

        private void GetBoogieTypesFromMapping(VariableDeclaration varDecl, Mapping mapping, out BoogieType mapKeyType, out BoogieMapSelect lhs)
        {
            mapKeyType = Flags_HelperClass.getBoogieExpression(mapping.KeyType.TypeDescriptions.ToString());
            var mapValueTypeString = mapping.ValueType is UserDefinedTypeName ?
                ((UserDefinedTypeName)mapping.ValueType).TypeDescriptions.ToString() :
                mapping.ValueType.ToString();
            // needed as a mapping(int => contract A) only has "A" as the valueType.ToSTring()
            var mapValueType = Flags_HelperClass.getBoogieExpression(mapValueTypeString);
            string mapName = Flags_HelperClass.generateMemoryMapName(mapKeyType, mapValueType);

            string varName = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
            var varExpr = new BoogieIdentifierExpr(varName);
            lhs = new BoogieMapSelect(new BoogieIdentifierExpr(mapName),
                new BoogieMapSelect(varExpr, new BoogieIdentifierExpr("this")));
        }

        // generate the default empty constructors, including an internal one without base ctors, and an actual one with base ctors
        // TODO: refactor this code with the code to generate constructor code when definition is present
        private void GenerateDefaultConstructor(ContractDefinition contract)
        {
            // generate the internal one without base constructors
            string procName = contract.Name + "_" + contract.Name + "_NoBaseCtor";
            currentBoogieProc = procName;
            if (!boogieToLocalVarsMap.ContainsKey(currentBoogieProc))
            {
                boogieToLocalVarsMap[currentBoogieProc] = new List<BoogieVariable>();
            }
            
            List<BoogieVariable> inParams = Conversion_Utility_Tool.GetDefaultInParams();
            List<BoogieVariable> outParams = new List<BoogieVariable>();
            List<BoogieAttribute> attributes = new List<BoogieAttribute>();
            if (Flags_HelperClass.GenerateInlineAttributes)
            {
                attributes.Add(new BoogieAttribute("inline", 1));
            };
            BoogieProcedure procedure = new BoogieProcedure(procName, inParams, outParams, attributes);
            context.getProgram.AddBoogieDeclaration(procedure);

            BoogieStmtList procBody = GenerateInitializationStmts(contract);
            List<BoogieVariable> localVars = boogieToLocalVarsMap[currentBoogieProc];
            BoogieImplementation implementation = new BoogieImplementation(procName, inParams, outParams, localVars, procBody);
            context.getProgram.AddBoogieDeclaration(implementation);

            // generate the actual one with base constructors
            string ctorName = contract.Name + "_" + contract.Name;
            BoogieProcedure ctorWithBaseCalls = new BoogieProcedure(ctorName, inParams, outParams, attributes);
            context.getProgram.AddBoogieDeclaration(ctorWithBaseCalls);

            List<BoogieVariable> ctorLocalVars = new List<BoogieVariable>();
            BoogieStmtList ctorBody = new BoogieStmtList();

            // add the printing for argument
            // Print function argument values to corral.txt for counterexample:
            PrintArguments(null, inParams, ctorBody);

            // print sourcefile, and line of the contract start for
            // forcing Corral to print values consistently
            if (!Flags_HelperClass.NoSourceLineInfoFlag)
                ctorBody.AddStatement(InstrumentSourceFileAndLineInfo(contract));

            List<int> baseContractIds = new List<int>(contract.LinearBaseContracts);
            baseContractIds.Reverse();
            foreach (int id in baseContractIds)
            {
                ContractDefinition baseContract = context.retrieveASTNodethroughID(id) as ContractDefinition;
                OverSightAssert(baseContract != null);

                string callee = Conversion_Utility_Tool.GetCanonicalConstructorName(baseContract);
                if (baseContract.Name == contract.Name)
                {
                    // for current contract, call the body that does not have the base calls
                    // for base contracts, call the wrapper constructor 
                    callee += "_NoBaseCtor";
                }

                List<BoogieExpr> inputs = new List<BoogieExpr>();
                List<BoogieIdentifierExpr> outputs = new List<BoogieIdentifierExpr>();

                InheritanceSpecifier inheritanceSpecifier = GetInheritanceSpecifieWithArgsOfBase(contract, baseContract);
                if (inheritanceSpecifier != null)
                {
                    inputs.Add(new BoogieIdentifierExpr("this"));
                    inputs.Add(new BoogieIdentifierExpr("msgsender_MSG"));
                    inputs.Add(new BoogieIdentifierExpr("msgvalue_MSG"));
                    foreach (Expression argument in inheritanceSpecifier.Arguments)
                    {
                        inputs.Add(TranslateExpr(argument));
                    }
                }
                else // no argument for this base constructor
                {

                    if (baseContract.Name == contract.Name)
                    {
                        // only do this for the derived contract
                        foreach (BoogieVariable param in inParams)
                        {
                            inputs.Add(new BoogieIdentifierExpr(param.TypedIdent.Name));
                        }
                    }
                    else
                    {
                        // Do we call the constructor or assume that it is invoked in teh base contract?
                        // since it needs argument, we cannot invoke it here (Issue #101)
                        var baseCtr = context.checkConstructorExists(baseContract) ? context.retrieveConstructor(baseContract) : null;
                        if (baseCtr != null && baseCtr.Parameters.Length() > 0)
                        {
                            continue;
                        } else if (!currentContract.BaseContracts.Any(x => x.BaseName.Name.Equals(baseContract.Name)))
                        {
                            Console.WriteLine($"Warning!!: Invoking base constructor { callee} that is not explicitly in inheritance list of {currentContract.Name}...");
                        }
                        inputs.Add(new BoogieIdentifierExpr("this"));
                        inputs.Add(new BoogieIdentifierExpr("msgsender_MSG"));
                        inputs.Add(new BoogieIdentifierExpr("msgvalue_MSG"));
                    }
                }
                BoogieCallCmd callCmd = new BoogieCallCmd(callee, inputs, outputs);
                ctorBody.AddStatement(callCmd);
            }
            BoogieImplementation ctorImpl = new BoogieImplementation(ctorName, inParams, outParams, ctorLocalVars, ctorBody);
            context.getProgram.AddBoogieDeclaration(ctorImpl);
        }

        // generate actual constructor procedures that invoke constructors without base in linearized order
        private void GenerateConstructorWithBaseCalls(FunctionDefinition ctor, List<BoogieVariable> inParams)
        {
            OverSightAssert(ctor.ofConstructorType, $"{ctor.Name} is not a constructor");

            ContractDefinition contract = context.GetContractByFunction(ctor);
            string procName = contract.Name + "_" + contract.Name;

            // no output params for constructor
            List<BoogieVariable> outParams = new List<BoogieVariable>();
            List<BoogieAttribute> attributes = new List<BoogieAttribute>()
            {
                new BoogieAttribute("constructor"),
                new BoogieAttribute("public"),
            };
            if (Flags_HelperClass.GenerateInlineAttributes)
            {
                attributes.Add(new BoogieAttribute("inline", 1));
            };
            BoogieProcedure procedure = new BoogieProcedure(procName, inParams, outParams, attributes);
            context.getProgram.AddBoogieDeclaration(procedure);

          
                // no local variables for constructor
                List<BoogieVariable> localVars = new List<BoogieVariable>();
                BoogieStmtList ctorBody = new BoogieStmtList();

                List<int> baseContractIds = new List<int>(contract.LinearBaseContracts);
                baseContractIds.Reverse();

                // Print function argument values to corral.txt for counterexample:
                PrintArguments(ctor, inParams, ctorBody);

                //Note that the current derived contract appears as a baseContractId 
                foreach (int id in baseContractIds)
                {
                    ContractDefinition baseContract = context.retrieveASTNodethroughID(id) as ContractDefinition;
                    OverSightAssert(baseContract != null);

                    // since we are not translating any statements, currentStmtList remains null
                    currentStmtList = new BoogieStmtList();

                    string callee = Conversion_Utility_Tool.GetCanonicalConstructorName(baseContract);
                    if (baseContract.Name == contract.Name)
                    {
                        // for current contract, call the body that does not have the base calls
                        // for base contracts, call the wrapper constructor 
                        callee += "_NoBaseCtor";
                    }
                    List<BoogieExpr> inputs = new List<BoogieExpr>();
                    List<BoogieIdentifierExpr> outputs = new List<BoogieIdentifierExpr>();

                    InheritanceSpecifier inheritanceSpecifier = GetInheritanceSpecifieWithArgsOfBase(contract, baseContract);
                    ModifierInvocation modifierInvocation = GetModifierInvocationOfBase(ctor, baseContract);
                    if (inheritanceSpecifier != null)
                    {
                        inputs.Add(new BoogieIdentifierExpr("this"));
                        inputs.Add(new BoogieIdentifierExpr("msgsender_MSG"));
                        inputs.Add(new BoogieIdentifierExpr("msgvalue_MSG"));
                        foreach (Expression argument in inheritanceSpecifier.Arguments)
                        {
                            inputs.Add(TranslateExpr(argument));
                        }
                    }
                    else if (modifierInvocation != null)
                    {
                        inputs.Add(new BoogieIdentifierExpr("this"));
                        inputs.Add(new BoogieIdentifierExpr("msgsender_MSG"));
                        inputs.Add(new BoogieIdentifierExpr("msgvalue_MSG"));
                        foreach (Expression argument in modifierInvocation.Arguments)
                        {
                            inputs.Add(TranslateExpr(argument));
                        }
                    }
                    else // no argument for this base constructor
                    {
                        if (baseContract.Name == contract.Name)
                        {
                            // only do this for the derived contract
                            foreach (BoogieVariable param in inParams)
                            {
                                inputs.Add(new BoogieIdentifierExpr(param.TypedIdent.Name));
                            }
                        }
                        else
                        {
                            // Do we call the constructor or assume that it is invoked in teh base contract?
                            // Do we call the constructor or assume that it is invoked in teh base contract?
                            // since it needs argument, we cannot invoke it here (Issue #101)
                            var baseCtr = context.checkConstructorExists(baseContract) ? context.retrieveConstructor(baseContract) : null;
                            if (baseCtr != null && baseCtr.Parameters.Length() > 0)
                            {
                                continue;
                            }
                            else if (!currentContract.BaseContracts.Any(x => x.BaseName.Name.Equals(baseContract.Name)))
                            {
                                Console.WriteLine($"Warning!!: Invoking base constructor { callee} that is not explicitly called in the inheritance/modifier list specified in { ctor.Name}...");
                            }
                            inputs.Add(new BoogieIdentifierExpr("this"));
                            inputs.Add(new BoogieIdentifierExpr("msgsender_MSG"));
                            inputs.Add(new BoogieIdentifierExpr("msgvalue_MSG"));
                        }
                    }
                    BoogieCallCmd callCmd = new BoogieCallCmd(callee, inputs, outputs);
                    ctorBody.AppendStmtList(currentStmtList);
                    ctorBody.AddStatement(callCmd);
                    currentStmtList = null;
                


                localVars.AddRange(boogieToLocalVarsMap[currentBoogieProc]);
                BoogieImplementation implementation = new BoogieImplementation(procName, inParams, outParams, localVars, ctorBody);
                context.getProgram.AddBoogieDeclaration(implementation);
            }           
        }

        // get the inheritance specifier of `baseContract' in contract definition `contract' if it specifies arguments
        // return null if there is no matching inheritance specifier
        // NOTE: two inheritance specifiers having the same name leads to a compile error
        private InheritanceSpecifier GetInheritanceSpecifieWithArgsOfBase(ContractDefinition contract, ContractDefinition baseContract)
        {
            foreach (InheritanceSpecifier inheritanceSpecifier in contract.BaseContracts)
            {
                if (inheritanceSpecifier.Arguments != null && inheritanceSpecifier.BaseName.Name.Equals(baseContract.Name))
                {
                    return inheritanceSpecifier;
                }
            }
            return null;
        }

        // get the modifier invocation of `baseContract' in constructor `ctor'
        // return null if there is no matching modifier
        // NOTE: two constructor modifiers having the same name leads to a compile error
        private ModifierInvocation GetModifierInvocationOfBase(FunctionDefinition ctor, ContractDefinition baseContract)
        {
            foreach (ModifierInvocation modifierInvocation in ctor.Modifiers)
            {
                int id = modifierInvocation.ModifierName.ReferencedDeclaration;
                if (context.retrieveASTNodethroughID(id) is ContractDefinition contractDef)
                {
                    if (contractDef == baseContract)
                    {
                        return modifierInvocation;
                    }
                }
            }
            return null;
        }

        // updated in the visitor of parameter list
        private List<BoogieVariable> currentParamList;
        private bool isReturnParameterList = false;
        
        public override bool Visit(ParameterList node)
        {
            preTranslationAction(node);
            currentParamList = new List<BoogieVariable>();
            var retParamCount = 0;
            foreach (VariableDeclaration parameter in node.Parameters)
            {
                // Issue a warning for intXX variables in case /useModularArithemtic option is used:
                if (Flags_HelperClass.UseModularArithmetic && parameter.TypeDescriptions.IsInt())
                {
                    Console.WriteLine($"Warning: signed integer arithmetic is not handled with /useModularArithmetic option");
                }

                string name = null;
                if (String.IsNullOrEmpty(parameter.Name))
                {
                    if (isReturnParameterList)
                        name = $"__ret_{retParamCount++}_" ;
                    else
                        name = $"__param_{retParamCount++}_";
                }
                else
                {
                    name = Conversion_Utility_Tool.GetCanonicalLocalVariableName(parameter, context);
                }
                BoogieType type = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(parameter.TypeName);
                var boogieParam = new BoogieFormalParam(new BoogieTypedIdent(name, type));
                currentParamList.Add(boogieParam);
            }
            return false;
        }

        // update in the visitor of different statements
        // this now accumulates all Boogie stmts generated when they are being generated
        private BoogieStmtList currentStmtList = null;

        /// This the only method that returns a BoogieStmtList (value of currentStmtList)
        
        public BoogieStmtList TranslateStatement(Statement node)
        {
            //push the current Statement
            var oldCurrentStmtList = currentStmtList; 

            //new scope
            currentStmtList = new BoogieStmtList(); // reset before starting to translate a Statement
            node.Accept(this);
            OverSightAssert(currentStmtList != null);

            BoogieStmtList annotatedStmtList = new BoogieStmtList();
            // add source file path and line number
            if (!Flags_HelperClass.NoSourceLineInfoFlag)
            {
                BoogieAssertCmd annotationCmd = InstrumentSourceFileAndLineInfo(node);
                annotatedStmtList = BoogieStmtList.MakeSingletonStmtList(annotationCmd);
            }
            annotatedStmtList.AppendStmtList(currentStmtList);

            currentStmtList = oldCurrentStmtList; // pop the stack of currentStmtList

            return annotatedStmtList;
        }

        private BoogieAssertCmd InstrumentSourceFileAndLineInfo(ASTNode node)
        {
            var srcFileLineInfo = Conversion_Utility_Tool.GenerateSourceInfoAnnotation(node, context);
            currentSourceFile = srcFileLineInfo.Item1;
            currentSourceLine = srcFileLineInfo.Item2;

            List<BoogieAttribute> attributes = new List<BoogieAttribute>()
                {
                new BoogieAttribute("first"),
                new BoogieAttribute("sourceFile", "\"" + srcFileLineInfo.Item1 + "\""),
                new BoogieAttribute("sourceLine", srcFileLineInfo.Item2)
                };
            BoogieAssertCmd annotationCmd = new BoogieAssertCmd(new BoogieLiteralExpr(true), attributes);
            return annotationCmd;
        }

        public override bool Visit(Block node)
        {
            preTranslationAction(node);
            BoogieStmtList block = new BoogieStmtList();
            foreach (Statement statement in node.Statements)
            {
                BoogieStmtList stmtList = TranslateStatement(statement);
                block.AppendStmtList(stmtList);
            }

            currentStmtList = block;
            return false;
        }

        public override bool Visit(PlaceholderStatement node)
        {
            preTranslationAction(node);
            return false;
        }

        public override bool Visit(VariableDeclarationStatement node)
        {
            preTranslationAction(node);
            foreach (VariableDeclaration varDecl in node.Declarations)
            {
                string name = Conversion_Utility_Tool.GetCanonicalLocalVariableName(varDecl, context);
                BoogieType type = Conversion_Utility_Tool.GetBoogieTypeFromSolidityTypeName(varDecl.TypeName);
                // Issue a warning for intXX variables in case /useModularArithemtic option is used:
                if (Flags_HelperClass.UseModularArithmetic && varDecl.TypeDescriptions.IsInt())
                {
                    Console.WriteLine($"Warning: signed integer arithmetic is not handled with /useModularArithmetic option");
                }
                var boogieVariable = new BoogieLocalVariable(new BoogieTypedIdent(name, type));
                 boogieToLocalVarsMap[currentBoogieProc].Add(boogieVariable);
            }

            // handle the initial value of variable declaration
            if (node.InitialValue != null)
            {
                OverSightAssert(node.Declarations.Count == 1, "Invalid multiple variable declarations");

                // de-sugar to variable declaration and an assignment
                VariableDeclaration varDecl = node.Declarations[0];

                Identifier identifier = new Identifier();
                identifier.Name = varDecl.Name;
                identifier.ReferencedDeclaration = varDecl.Id;
                identifier.TypeDescriptions = varDecl.TypeDescriptions;

                Assignment assignment = new Assignment();
                assignment.LeftHandSide = identifier;
                assignment.Operator = "=";
                assignment.RightHandSide = node.InitialValue;

                // call the visitor for assignments
                assignment.Accept(this);
            }
            else
            {
                // havoc the declared variables
                List<BoogieIdentifierExpr> varsToHavoc = new List<BoogieIdentifierExpr>();
                foreach (VariableDeclaration varDecl in node.Declarations)
                {
                    string varIdent = Conversion_Utility_Tool.GetCanonicalLocalVariableName(varDecl, context);
                    varsToHavoc.Add(new BoogieIdentifierExpr(varIdent));
                }
                BoogieHavocCmd havocCmd = new BoogieHavocCmd(varsToHavoc);
                currentStmtList.AppendStmtList(BoogieStmtList.MakeSingletonStmtList(havocCmd));
            }
            return false;
        }


        private BoogieExpr AddModuloOp(Expression srcExpr, BoogieExpr expr, TypeDescription type)
        {
            if (Flags_HelperClass.UseModularArithmetic)
            {
                if (type != null)
                {
                    var isUint = type.IsUintWSize(srcExpr, out uint sz);
                    if (isUint)
                    {
                        OverSightAssert(sz != 0, $"size in AddModuloOp is zero");
                        BigInteger maxUIntValue = (BigInteger)Math.Pow(2, sz);
                        return (BoogieExpr)new BoogieFuncCallExpr("modBpl", new List<BoogieExpr>() { expr, new BoogieLiteralExpr(maxUIntValue) });
                    }
                }
            }
            return expr;
        }

        public override bool Visit(Assignment node)
        {
            preTranslationAction(node);
            List<BoogieExpr> lhs = new List<BoogieExpr>();
            List<BoogieType> lhsTypes = new List<BoogieType>(); //stores types in case of tuples

            bool isTupleAssignment = false;

            if (node.LeftHandSide is TupleExpression tuple)
            {
                // we only handle the case (e1, e2, .., _, _)  = funcCall(...)
                lhs.AddRange(tuple.Components.ConvertAll(x => TranslateExpr(x)));
                isTupleAssignment = true;
                lhsTypes.AddRange(tuple.Components.ConvertAll(x => Flags_HelperClass.getBoogieExpression(x.TypeDescriptions.TypeString)));
            }
            else
            {
                lhs.Add(TranslateExpr(node.LeftHandSide));
            }

            // TODO: this part should go into Translate a function call expression
            if (node.RightHandSide is FunctionCall funcCall)
            {
                // if lhs is not an identifier (e.g. a[i]), then
                // we have to introduce a temporary
                // we do it even when lhs is identifier to keep translation simple
                var tmpVars = new List<BoogieIdentifierExpr>();

                var oldStmtList = currentStmtList;
                currentStmtList = new BoogieStmtList();

                if (!isTupleAssignment) {
                    tmpVars.Add(lhs[0] is BoogieIdentifierExpr ? lhs[0] as BoogieIdentifierExpr : MkNewLocalVariableForFunctionReturn(funcCall));
                } else {
                    // always use temporaries for tuples regardless if lhs[i] is an identifier
                    tmpVars.AddRange(lhsTypes.ConvertAll(x => MkNewLocalVariableWithType(x)));
                }

                var tmpVariableAssumes = currentStmtList;
                currentStmtList = oldStmtList;
        
                // a Boolean to decide is we needed to use tmpVar
                bool usedTmpVar = true;

                if (IsContractConstructor(funcCall))
                {
                    OverSightAssert(!isTupleAssignment, "Not expecting a tuple for Constructors");
                    TranslateNewStatement(funcCall, tmpVars[0]);
                }
                else if (IsStructConstructor(funcCall))
                {
                    OverSightAssert(!isTupleAssignment, "Not expecting a tuple for Constructors");
                    TranslateStructConstructor(funcCall, tmpVars[0]);
                }
                else if (IsKeccakFunc(funcCall))
                {
                    OverSightAssert(!isTupleAssignment, "Not expecting a tuple for Keccak256");
                    TranslateKeccakFuncCall(funcCall, lhs[0]); //this is not a procedure call in Boogie
                    usedTmpVar = false;
                }
                else if (IsAbiEncodePackedFunc(funcCall))
                {
                    TranslateAbiEncodedFuncCall(funcCall, tmpVars[0]); //this is not a procedure call in Boogie
                    OverSightAssert(!isTupleAssignment, "Not expecting a tuple for abi.encodePacked");
                    usedTmpVar = false;
                }
                else if (IsTypeCast(funcCall))
                {
                    // assume the type cast is used as: obj = C(var);
                    OverSightAssert(!isTupleAssignment, "Not expecting a tuple for type cast");
                    bool isElementaryCast; //not used at this site
                    TranslateTypeCast(funcCall, tmpVars[0], out isElementaryCast); //this is not a procedure call in Boogie
                    usedTmpVar = false;
                }
                else // normal function calls
                {
                    OverSightAssert(tmpVars is List<BoogieIdentifierExpr>, $"tmpVar has to be a list of Boogie identifiers: {tmpVars}");
                    TranslateFunctionCalls(funcCall, tmpVars);
                }
                if (!isTupleAssignment)
                {
                    if (usedTmpVar || !(lhs[0] is BoogieIdentifierExpr)) //bad bug: was && before!!  
                        currentStmtList.AddStatement(new BoogieAssignCmd(lhs[0], tmpVars[0]));                       
                } else
                {
                    for (int i = 0; i < lhs.Count; ++i)
                    {
                        currentStmtList.AddStatement(new BoogieAssignCmd(lhs[i], tmpVars[i]));
                    }
                }
                foreach(var block in tmpVariableAssumes.BigBlocks)
                {
                    foreach(var stmt in block.SimpleCmds)
                       currentStmtList.AddStatement(stmt);

                }
            }
            else
            {
                if (isTupleAssignment)
                    OverSightAssert(false, "Not implemented...currently only support assignment of tuples as returns of a function call");

                BoogieExpr rhs = TranslateExpr(node.RightHandSide);
                BoogieStmtList stmtList = new BoogieStmtList();
                BoogieExpr assignedExpr = new BoogieExpr();
                switch (node.Operator)
                {
                    case "=":
                        stmtList.AddStatement(new BoogieAssignCmd(lhs[0], rhs));
                        break;
                    case "+=":
                        assignedExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.ADD, lhs[0], rhs);
                        assignedExpr = AddModuloOp(node.LeftHandSide, assignedExpr, node.LeftHandSide.TypeDescriptions);
                        stmtList.AddStatement(new BoogieAssignCmd(lhs[0], assignedExpr));
                        break;
                    case "-=":
                        assignedExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.SUB, lhs[0], rhs);
                        assignedExpr = AddModuloOp(node.LeftHandSide, assignedExpr, node.LeftHandSide.TypeDescriptions);
                        stmtList.AddStatement(new BoogieAssignCmd(lhs[0], assignedExpr));
                        break;
                    case "*=":
                        assignedExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.MUL, lhs[0], rhs);
                        assignedExpr = AddModuloOp(node.LeftHandSide, assignedExpr, node.LeftHandSide.TypeDescriptions);
                        stmtList.AddStatement(new BoogieAssignCmd(lhs[0], assignedExpr));
                        break;
                    case "/=":
                        assignedExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.DIV, lhs[0], rhs);
                        assignedExpr = AddModuloOp(node.LeftHandSide, assignedExpr, node.LeftHandSide.TypeDescriptions);
                        stmtList.AddStatement(new BoogieAssignCmd(lhs[0], assignedExpr));
                        break;
                    default:
                        OverSightAssert(false,  $"Unknown assignment operator: {node.Operator}");
                        break;
                }
                currentStmtList.AppendStmtList(stmtList);
            }

            var lhsType = node.LeftHandSide.TypeDescriptions ?? node.RightHandSide.TypeDescriptions ?? null;

            if (lhsType != null && !isTupleAssignment)
            {
                var callCmd = InstrumentForPrintingData(lhsType, lhs[0], node.LeftHandSide.ToString());
                if (callCmd != null)
                {
                    currentStmtList.AddStatement(callCmd);
                }
            }

            return false;
        }

        private void TranslateFunctionCalls(FunctionCall funcCall, List<BoogieIdentifierExpr> outParams)
        {
            if (IsExternalFunctionCall(funcCall))
            {
                TranslateExternalFunctionCall(funcCall, outParams);
            }
            else
            {
                TranslateInternalFunctionCall(funcCall, outParams);
            }
        }

        public override bool Visit(Return node)
        {
            preTranslationAction(node);
            if (node.Expression == null)
            {
                //Void
                BoogieReturnCmd returnCmd = new BoogieReturnCmd();
                if (currentPostlude == null)
                {
                    currentStmtList.AppendStmtList(BoogieStmtList.MakeSingletonStmtList(returnCmd));
                }
                else
                {
                    currentStmtList.AppendStmtList(currentPostlude);
                    currentStmtList.AddStatement(returnCmd);
                }
            }
            else
            {
                BoogieExpr retExpr = TranslateExpr(node.Expression); //TODO: handle tuples here?
                var retParamCount = 0;
                if (node.Expression is TupleExpression tuple)
                {
                    //Tuple
                    if (!(retExpr is BoogieTupleExpr))
                    {
                        OverSightAssert(false, "Expecting a Boogie tuple expression here");
                    }
                    var bTupleExpr = retExpr as BoogieTupleExpr;

                    //turn the tuple assignment into a serial assignment [TODO: understand the evaluation semantics]
                    foreach (var retVarDecl in currentFunction.ReturnParameters.Parameters)
                    {
                        string retVarName = String.IsNullOrEmpty(retVarDecl.Name) ?
                            $"__ret_{retParamCount}_" :
                            Conversion_Utility_Tool.GetCanonicalLocalVariableName(retVarDecl, context);
                        BoogieIdentifierExpr retVar = new BoogieIdentifierExpr(retVarName);
                        BoogieAssignCmd assignCmd = new BoogieAssignCmd(retVar, bTupleExpr.Arguments[retParamCount++]);
                        currentStmtList.AppendStmtList(BoogieStmtList.MakeSingletonStmtList(assignCmd)); //TODO: simultaneous updates
                    }
                }
                else
                {
                    //Singleton 
                    var retVarDecl = currentFunction.ReturnParameters.Parameters[0];
                    string retVarName = String.IsNullOrEmpty(retVarDecl.Name) ?
                        $"__ret_{retParamCount++}_" :
                        Conversion_Utility_Tool.GetCanonicalLocalVariableName(retVarDecl, context);
                    BoogieIdentifierExpr retVar = new BoogieIdentifierExpr(retVarName);
                    BoogieAssignCmd assignCmd = new BoogieAssignCmd(retVar, retExpr);
                    currentStmtList.AppendStmtList(BoogieStmtList.MakeSingletonStmtList(assignCmd)); //TODO: simultaneous updates
                }

                if (currentPostlude != null)
                {
                    currentStmtList.AppendStmtList(currentPostlude);
                }
                // add a return command, in case the original return expr is in the middle of the function body
                currentStmtList.AddStatement(new BoogieReturnCmd());
            }
            return false;
        }

        private void AddAssumeForUints(Expression expr, BoogieExpr boogieExpr, TypeDescription typeDesc)
        {
            // skip based on a flag
            if (Flags_HelperClass.NoUnsignedAssumesFlag)
                return;

            // Add positive number assume for uints
            if (typeDesc!=null && typeDesc.IsUint())
            {
                var ge0 = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, boogieExpr, new BoogieLiteralExpr(BigInteger.Zero));

                if (Flags_HelperClass.UseModularArithmetic)
                {
                    var isUint = typeDesc.IsUintWSize(expr, out uint sz);
                    if (isUint)
                    {
                        OverSightAssert(sz != 0, $"size of uint lhs is zero");
                        BigInteger maxUIntValue = (BigInteger)Math.Pow(2, sz);
                        var tmp = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LT, boogieExpr, new BoogieLiteralExpr(maxUIntValue));
                        ge0 = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, ge0, tmp);
                    }
                }


                var assumePositiveCmd = new BoogieAssumeCmd(ge0);
                currentStmtList.AppendStmtList(BoogieStmtList.MakeSingletonStmtList(assumePositiveCmd));
            }
        }
        
        private void emitRevertLogic(BoogieStmtList revertLogic)
        {
            BoogieAssignCmd setRevert = new BoogieAssignCmd(new BoogieIdentifierExpr("revert"), new BoogieLiteralExpr(true));
            revertLogic.AddStatement(setRevert);

            revertLogic.AddStatement(new BoogieReturnCmd());
        }

        public override bool Visit(Throw node)
        {
            preTranslationAction(node);
            if (!context.TranslateFlags.ModelReverts)
            {
                BoogieAssumeCmd assumeCmd = new BoogieAssumeCmd(new BoogieLiteralExpr(false));
                currentStmtList.AppendStmtList(BoogieStmtList.MakeSingletonStmtList(assumeCmd));
            }
            else
            {
                emitRevertLogic(currentStmtList);
            }

            return false;
        }

        public override bool Visit(IfStatement node)
        {
            preTranslationAction(node);
            BoogieExpr guard = TranslateExpr(node.Condition);
            BoogieStmtList thenBody = TranslateStatement(node.TrueBody);
            BoogieStmtList elseBody = null;
            if (node.FalseBody != null)
            {
                elseBody = TranslateStatement(node.FalseBody);
            }
            BoogieIfCmd ifCmd = new BoogieIfCmd(guard, thenBody, elseBody);

            //currentStmtList = new BoogieStmtList();
            //currentStmtList.AppendStmtList(auxStmtList);
            currentStmtList.AddStatement(ifCmd);
            return false;
        }

        public override bool Visit(WhileStatement node)
        {
            preTranslationAction(node);
            BoogieExpr guard = TranslateExpr(node.Condition);
            BoogieStmtList body = TranslateStatement(node.Body);

            BoogieStmtList newBody;
            var invariants = ExtractSpecifications("Invariant_OverSight", body, out newBody);
            BoogieWhileCmd whileCmd = new BoogieWhileCmd(guard, newBody, invariants);

            currentStmtList.AddStatement(whileCmd);

            if (Flags_HelperClass.InstrumentGas &&
                context.TranslateFlags.ModelReverts)
            {
                emitGasCheck(newBody);
            }
            return false;
        }

        public override bool Visit(ForStatement node)
        {
            preTranslationAction(node);
            BoogieStmtList initStmt = TranslateStatement(node.InitializationExpression);
            BoogieExpr guard = TranslateExpr(node.Condition);
            BoogieStmtList loopStmt = TranslateStatement(node.LoopExpression);
            BoogieStmtList body = TranslateStatement(node.Body);

            BoogieStmtList stmtList = new BoogieStmtList();
            stmtList.AppendStmtList(initStmt);

            body.AppendStmtList(loopStmt);

            BoogieStmtList newBody;
            var invariants = ExtractSpecifications("Invariant_OverSight", body, out newBody);
            BoogieWhileCmd whileCmd = new BoogieWhileCmd(guard, newBody, invariants);
            stmtList.AddStatement(whileCmd);

            currentStmtList.AppendStmtList(stmtList);

            if (Flags_HelperClass.InstrumentGas &&
                context.TranslateFlags.ModelReverts)
            {
                emitGasCheck(newBody);
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="specStringCall">"Invariant_OverSight", "Ensures_OverSight", "Requires_OverSight"</param>
        /// <param name="body"></param>
        /// <param name="bodyWithoutSpecNodes"></param>
        /// <returns></returns>
        private List<BoogieExpr> ExtractSpecifications(string specStringCall, BoogieStmtList body, out BoogieStmtList bodyWithoutSpecNodes)
        {
            bodyWithoutSpecNodes = new BoogieStmtList();
            List<BoogieExpr> specArguments = new List<BoogieExpr>();
            foreach (var bigBlock in body.BigBlocks)
            {
                foreach (var stmt in bigBlock.SimpleCmds)
                {
                    var callCmd = stmt as BoogieCallCmd;
                    if (callCmd == null)
                    {
                        bodyWithoutSpecNodes.AddStatement(stmt);
                        continue;
                    }
                    if (callCmd.Callee.Equals(specStringCall))
                    {
                        //first 3 args are {this, msg.sender, msg.value}
                        if (specStringCall.Equals("Modifies_OverSight"))
                        {
                            OverSightAssert(callCmd.Ins.Count == 5, $"For Modifies clause, found {specStringCall}(..) with unexpected number of args (expected 5)");
                            specArguments.Add(TranslateModifiesStmt(callCmd.Ins[3], callCmd.Ins[4]));
                        }
                        else
                        {
                            OverSightAssert(callCmd.Ins.Count == 4, $"Found {specStringCall}(..) with unexpected number of args (expected 4)");
                            specArguments.Add(callCmd.Ins[3]);
                        }
                    } else
                    {
                        bodyWithoutSpecNodes.AddStatement(stmt);
                    }
                }
            }
            return specArguments;
        }

        private BoogieExpr TranslateModifiesStmt(BoogieExpr boogieExpr1, BoogieExpr boogieExpr2)
        {
            //has to be M_ref_int[mapp[this]] instead of mapp[this]
            var mapName = Flags_HelperClass.generateMemoryMapName(BoogieType.Ref, BoogieType.Int);
            var mappingExpr = new BoogieMapSelect(new BoogieIdentifierExpr(mapName), boogieExpr1);

            //boogieExpr2 is a tuple, we need to flatten it into an array
            var boogieTupleExpr = boogieExpr2 as BoogieTupleExpr;
            OverSightAssert(boogieTupleExpr != null, $"Expecting tuple expression, found {boogieExpr2.ToString()}");
            OverSightAssert(boogieTupleExpr.Arguments.Count > 0, $"Expecting non-tuple expression, found {boogieExpr2.ToString()}");
            OverSightAssert(boogieTupleExpr.Arguments.Count < 10, $"Expecting tuple expression of size < 10, found {boogieExpr2.ToString()}");

            // forall x: Ref :: x == m1 || x == m2 ... || x == mi || map[x] == old(map[x])
            var qVar1 = QVarGenerator.NewQVar(0, 0);
            var bodyExpr = (BoogieExpr)new BoogieLiteralExpr(false);
            for (int i = 0; i < boogieTupleExpr.Arguments.Count; i++)
            {
                bodyExpr =
                    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.OR, bodyExpr,
                       (new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, qVar1, boogieTupleExpr.Arguments[i])));
            }
            var mapExpr = new BoogieMapSelect(mappingExpr, (BoogieExpr)qVar1);
            var oldMapExpr = new BoogieFuncCallExpr("old", new List<BoogieExpr>() { mapExpr });
            var eqExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, mapExpr, oldMapExpr);
            bodyExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.OR, bodyExpr, eqExpr);
            var qBodyExpr = new BoogieQuantifiedExpr(true,
                new List<BoogieIdentifierExpr>() { qVar1 },
                new List<BoogieType>() { BoogieType.Ref },
                bodyExpr
                );
            return qBodyExpr;
        }

        private List<BoogieExpr> ExtractContractInvariants(BoogieStmtList body)
        {
            List<BoogieExpr> invariantExprs = new List<BoogieExpr>();
            foreach (var bigBlock in body.BigBlocks)
            {
                foreach (var stmt in bigBlock.SimpleCmds)
                {
                    var callCmd = stmt as BoogieCallCmd;
                    if (callCmd == null)
                    {
                        continue;
                    }
                    if (callCmd.Callee.Equals("ContractInvariant_OverSight"))
                    {
                        OverSightAssert(callCmd.Ins.Count == 4, "Found OverSight.ContractInvariant(..) with unexpected number of args");
                        invariantExprs.Add(callCmd.Ins[3]);
                    }
                }
            }
            return invariantExprs;
        }

        public override bool Visit(DoWhileStatement node)
        {
            preTranslationAction(node);
            BoogieExpr guard = TranslateExpr(node.Condition);
            BoogieStmtList body = TranslateStatement(node.Body);

            BoogieStmtList stmtList = new BoogieStmtList();

            BoogieStmtList newBody;
            var invariants = ExtractSpecifications("Invariant_OverSight", body, out newBody);
            stmtList.AppendStmtList(newBody);

            BoogieWhileCmd whileCmd = new BoogieWhileCmd(guard, newBody, invariants);

            stmtList.AddStatement(whileCmd);

            currentStmtList.AppendStmtList(stmtList);
            
            if (Flags_HelperClass.InstrumentGas &&
                context.TranslateFlags.ModelReverts)
            {
                emitGasCheck(newBody);
            }
            return false;
        }

        public override bool Visit(Break node)
        {
            preTranslationAction(node);
            BoogieBreakCmd breakCmd = new BoogieBreakCmd();
            currentStmtList.AddStatement(breakCmd);
            return false;
        }

        public override bool Visit(Continue node)
        {
            preTranslationAction(node);
            throw new NotImplementedException(node.ToString());
        }

        public override bool Visit(ExpressionStatement node)
        {
            preTranslationAction(node);
            if (node.Expression is UnaryOperation unaryOperation)
            {
                // only handle increment and decrement operators in a separate statement
                OverSightAssert(!(unaryOperation.SubExpression is UnaryOperation));

                BoogieExpr lhs = TranslateExpr(unaryOperation.SubExpression);
                if (unaryOperation.Operator.Equals("++") ||
                    unaryOperation.Operator.Equals("--"))
                {
                    var oper = unaryOperation.Operator.Equals("++") ? BoogieBinaryOperation.Opcode.ADD : BoogieBinaryOperation.Opcode.SUB;
                    BoogieExpr rhs = new BoogieBinaryOperation(oper, lhs, new BoogieLiteralExpr(1));
                    rhs = AddModuloOp(unaryOperation.SubExpression, rhs, unaryOperation.SubExpression.TypeDescriptions);
                    BoogieAssignCmd assignCmd = new BoogieAssignCmd(lhs, rhs);
                    currentStmtList.AddStatement(assignCmd);
                    //print the value
                    if (!Flags_HelperClass.NoDataValuesInfoFlag)
                    {
                        var callCmd = new BoogieCallCmd("boogie_si_record_sol2Bpl_int", new List<BoogieExpr>() { lhs }, new List<BoogieIdentifierExpr>());
                        callCmd.Attributes = new List<BoogieAttribute>
                        {
                            new BoogieAttribute("cexpr", $"\"{unaryOperation.SubExpression.ToString()}\"")
                        };
                        currentStmtList.AddStatement(callCmd);
                    }
                    AddAssumeForUints(unaryOperation, lhs, unaryOperation.TypeDescriptions);
                }
                else if (unaryOperation.Operator.Equals("delete"))
                {
                    var typeDescription = unaryOperation.SubExpression.TypeDescriptions;
                    var isBasicType = typeDescription.IsInt() || typeDescription.IsUint()
                                      || typeDescription.IsBool() || typeDescription.IsString();

                    // var isArrayAccess = unaryOperation.SubExpression is IndexAccess;
                    if (typeDescription.IsDynamicArray())
                    {
                        BoogieExpr element = TranslateExpr(unaryOperation.SubExpression);
                        BoogieExpr lengthMapSelect = new BoogieMapSelect(new BoogieIdentifierExpr("Length"), element);
                        BoogieExpr rhs = new BoogieLiteralExpr(BigInteger.Zero);
                        var assignCmd = new BoogieAssignCmd(lengthMapSelect, rhs);
                        currentStmtList.AddStatement(assignCmd);
                    }
                    else if (typeDescription.IsStaticArray())
                    {
                        // TODO: Handle static arrauy
                        Console.WriteLine($"Warning!!: Currently not handling delete of static arrays");
                    }
                    // This handle cases like delete x with "x" a basic type or delete x[i] when x[i] being a basic type;
                    else if (isBasicType)
                    {
                        BoogieExpr rhs = null;
                        if (typeDescription.IsInt() || typeDescription.IsUint())
                            rhs = new BoogieLiteralExpr(BigInteger.Zero);
                        else if (typeDescription.IsBool())
                            rhs = new BoogieLiteralExpr(false);
                        else if (typeDescription.IsString())
                        {
                            var emptyStr = "";
                            rhs = new BoogieLiteralExpr(new BigInteger(emptyStr.GetHashCode()));
                        }
                        var assignCmd = new BoogieAssignCmd(lhs, rhs);
                        currentStmtList.AddStatement(assignCmd);
                    }
                    else
                    {
                        Console.WriteLine($"Warning!!: Only handle delete for scalars and arrays, found {typeDescription.TypeString}");
                    }
                }
                return false;
            }
            else
            {
                // distribute to different visit functions
                return true;
            }
        }

        // updated in visitors of different expressions
        private BoogieExpr currentExpr;

        public Dictionary<string, List<BoogieExpr>> ContractInvariants { get; } = null;

        private BoogieExpr TranslateExpr(Expression expr)
        {
            currentExpr = null;
            if (expr is FunctionCall && IsTypeCast((FunctionCall) expr))
            {
                expr.Accept(this);
                OverSightAssert(currentExpr != null);
            }
            else if(expr is TupleExpression tuple)
            {
                var transArgs = new List<BoogieExpr>();
                foreach(var e in tuple.Components)
                {
                    e.Accept(this);
                    OverSightAssert(currentExpr != null);
                    transArgs.Add(currentExpr);
                }
                currentExpr = new BoogieTupleExpr(transArgs);
            }
            else
            {
                expr.Accept(this);
                OverSightAssert(currentExpr != null);
            }

            // TODO: Many times the type is unknown...
            if(expr.TypeDescriptions!=null) //  && currentExpr is BoogieIdentifierExpr)
            {
                AddAssumeForUints(expr, currentExpr, expr.TypeDescriptions);
            }

            return currentExpr;
        }

        public override bool Visit(Literal node)
        {
            preTranslationAction(node);
            if (node.Kind.Equals("bool"))
            {
                bool b = Convert.ToBoolean(node.Value);
                currentExpr = new BoogieLiteralExpr(b);
            }
            else if (node.Kind.Equals("number"))
            {
                currentExpr = TranslateNumberToExpr(node);
            }
            else if (node.Kind.Equals("string"))
            {
                int hashCode = node.Value.GetHashCode();
                BigInteger num = new BigInteger(hashCode);
                currentExpr = new BoogieLiteralExpr(num);
            }
            else
            {
               OverSightAssert(false, $"Unknown literal kind: {node.Kind}");
            }
            return false;
        }

        private BoogieExpr TranslateNumberToExpr(Literal node)
        {
            // now any 0x and 000 is treated as uint

            BigInteger num; 
            if (node.Value.StartsWith("0x") || node.Value.StartsWith("0X"))
            {
                num = BigInteger.Parse(node.Value.Substring(2), NumberStyles.AllowHexSpecifier);
            }
            else
            {
                num = BigInteger.Parse(node.Value);
            }

            {
                return new BoogieLiteralExpr(num);
            }
        }

        public override bool Visit(Identifier node)
        {
            preTranslationAction(node);
            if (node.Name.Equals("this"))
            {
                currentExpr = new BoogieIdentifierExpr("this");
            }
            else if (node.Name.Equals("now"))
            {
                currentExpr = GenerateNonDetExpr(node, "now special variable");
            }
            else // explicitly defined identifiers
            {
                OverSightAssert(context.HasASTNodeId(node.ReferencedDeclaration), $"Unknown node: {node}");
                VariableDeclaration varDecl = context.retrieveASTNodethroughID(node.ReferencedDeclaration) as VariableDeclaration;
                OverSightAssert(varDecl != null);

                if (varDecl.StateVariable)
                {
                    string name = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);

                    BoogieIdentifierExpr mapIdentifier = new BoogieIdentifierExpr(name);
                    BoogieMapSelect mapSelect = new BoogieMapSelect(mapIdentifier, new BoogieIdentifierExpr("this"));
                    currentExpr = mapSelect;
                }
                else
                {
                    string name = Conversion_Utility_Tool.GetCanonicalLocalVariableName(varDecl, context);
                    BoogieIdentifierExpr identifier = new BoogieIdentifierExpr(name);
                    currentExpr = identifier;
                }
            }
            return false;
        }

        public override bool Visit(MemberAccess node)
        {
            preTranslationAction(node);
            // length attribute of arrays
            if (node.MemberName.Equals("length"))
            {
                currentExpr = TranslateArrayLength(node);
                return false;
            }

            if (node.MemberName.Equals("balance"))
            {
                currentExpr = TranslateBalance(node);
                return false;
            }

            // only structs will need to use x.f.g notation, since 
            // one can only access functions of nested contracts
            // RESTRICTION: only handle e.f where e is Identifier | IndexExpr | FunctionCall
            OverSightAssert(node.Expression is Identifier || node.Expression is IndexAccess || node.Expression is FunctionCall,
                $"Only handle non-nested structures, found {node.Expression.ToString()}");
            if (node.Expression.TypeDescriptions.IsStruct())
            {
                var baseExpr = TranslateExpr(node.Expression);
                var memberMap = node.MemberName + "_" + node.Expression.TypeDescriptions.TypeString.Split(" ")[1];
                currentExpr = new BoogieMapSelect(
                    new BoogieIdentifierExpr(memberMap),
                    baseExpr);
                return false;
            }


            if (node.Expression is Identifier)
            {
                Identifier identifier = node.Expression as Identifier;
                if (identifier.Name.Equals("msg"))
                {
                    if (node.MemberName.Equals("sender"))
                    {
                        currentExpr = new BoogieIdentifierExpr("msgsender_MSG");
                    }
                    else if (node.MemberName.Equals("value"))
                    {
                        currentExpr = new BoogieIdentifierExpr("msgvalue_MSG");
                    }
                    else
                    {
                        OverSightAssert(false, $"Unknown member for msg: {node}");
                    }
                    return false;
                }
                else if (identifier.Name.Equals("this"))
                {
                    if (node.MemberName.Equals("balance"))
                    {
                        currentExpr = new BoogieMapSelect(new BoogieIdentifierExpr("balance_ADDR"), new BoogieIdentifierExpr("this"));
                    }
                    return false;
                } 
                else if (identifier.Name.Equals("block") || identifier.Name.Equals("tx"))
                {
                    //we will havoc the value
                    currentExpr = GenerateNonDetExpr(node, node.ToString());
                    return false;
                }

                OverSightAssert(context.HasASTNodeId(identifier.ReferencedDeclaration), $"Unknown node: {identifier}");
                ASTNode refDecl = context.retrieveASTNodethroughID(identifier.ReferencedDeclaration);

                if (refDecl is EnumDefinition)
                {
                    int enumIndex = Conversion_Utility_Tool.GetEnumValueIndex((EnumDefinition)refDecl, node.MemberName);
                    currentExpr = new BoogieLiteralExpr(enumIndex);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                OverSightAssert(false, $"Unknown expression type for member access: {node}");
                throw new Exception();
            }
        }

        private BoogieExpr GenerateNonDetExpr(Expression node, string reason)
        {
            BoogieType bType = null;
            if (node.TypeDescriptions.IsInt() || node.TypeDescriptions.IsUint())
            {
                bType = BoogieType.Int;
            }
            else if (node.TypeDescriptions.IsAddress())
            {
                bType = BoogieType.Ref;
            }
            else
            {
                OverSightAssert(false, $"Unhandled expression {node.ToString()} with return type not equal to uint/address");
            }
            var tmpVar = MkNewLocalVariableWithType(bType);
            currentStmtList.AddStatement(new BoogieCommentCmd($"Non-deterministic value to model {reason}"));
            currentStmtList.AddStatement(new BoogieHavocCmd(new BoogieIdentifierExpr(tmpVar.Name)));
            return tmpVar;
        }

        private BoogieExpr TranslateBalance(MemberAccess node)
        {
            BoogieExpr indexExpr = TranslateExpr(node.Expression);
            var mapSelect = new BoogieMapSelect(new BoogieIdentifierExpr("Balance"), indexExpr);
            return mapSelect;
        }

        private BoogieExpr TranslateArrayLength(MemberAccess node)
        {
            OverSightAssert(node.MemberName.Equals("length"));

            BoogieExpr indexExpr = TranslateExpr(node.Expression);
            var mapSelect = new BoogieMapSelect(new BoogieIdentifierExpr("Length"), indexExpr);
            return mapSelect;
        }

        public override bool Visit(FunctionCall node)
        {
            preTranslationAction(node);

            // OverSightAssert(!(node.Expression is NewExpression), $"new expressions should be handled in assignment");
            if (node.Expression is NewExpression)
            {
                BoogieIdentifierExpr tmpVarExpr = MkNewLocalVariableForFunctionReturn(node);
                TranslateNewStatement(node, tmpVarExpr);
                currentExpr = tmpVarExpr;
                return false;
            }

            var functionName = Conversion_Utility_Tool.GetFuncNameFromFuncCall(node);

            if (functionName.Equals("assert"))
            {
                // TODO:
                //countNestedFuncCallsRelExprs--;
                OverSightAssert(node.Arguments.Count == 1);
                BoogieExpr predicate = TranslateExpr(node.Arguments[0]);
                BoogieAssertCmd assertCmd = new BoogieAssertCmd(predicate);
                currentStmtList.AddStatement(assertCmd);
            }
            else if (functionName.Equals("require"))
            {
                // TODO:
                //countNestedFuncCallsRelExprs--;
                OverSightAssert(node.Arguments.Count == 1 || node.Arguments.Count == 2);
                BoogieExpr predicate = TranslateExpr(node.Arguments[0]);

                if (!context.TranslateFlags.ModelReverts)
                {
                    BoogieAssumeCmd assumeCmd = new BoogieAssumeCmd(predicate);
                    currentStmtList.AddStatement(assumeCmd);
                }
                else
                {
                    BoogieStmtList revertLogic = new BoogieStmtList();

                    emitRevertLogic(revertLogic);

                    BoogieIfCmd requierCheck = new BoogieIfCmd(new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, predicate), revertLogic, null);

                    currentStmtList.AddStatement(requierCheck);
                }
            }
            else if (functionName.Equals("revert"))
            {
                OverSightAssert(node.Arguments.Count == 0 || node.Arguments.Count == 1);
                if (!context.TranslateFlags.ModelReverts)
                {
                    BoogieAssumeCmd assumeCmd = new BoogieAssumeCmd(new BoogieLiteralExpr(false));
                    currentStmtList.AddStatement(assumeCmd);
                }
                else
                {
                    emitRevertLogic(currentStmtList);
                }
            }
            else if (IsImplicitFunc(node))
            {
                BoogieIdentifierExpr tmpVarExpr = MkNewLocalVariableForFunctionReturn(node);
                bool isElementaryCast = false;  

                if (IsContractConstructor(node)) {
                    TranslateNewStatement(node, tmpVarExpr);
                } else if (IsTypeCast(node)) {
                    TranslateTypeCast(node, tmpVarExpr, out isElementaryCast);
                } else if (IsAbiEncodePackedFunc(node)) {
                    TranslateAbiEncodedFuncCall(node, tmpVarExpr);
                } else if (IsKeccakFunc(node)) {
                    TranslateKeccakFuncCall(node, tmpVarExpr);
                } else if (IsStructConstructor(node)) {
                    TranslateStructConstructor(node, tmpVarExpr);
                } else
                {
                    OverSightAssert(false, $"Unexpected implicit function {node.ToString()}");
                }

                if (!isElementaryCast)
                {
                    // We should not introduce temporaries for address(this).balance in a specification
                    currentExpr = tmpVarExpr;
                }
            }
            else if (IsOverSightCodeContractFunction(node))
            {
                // we cannot use temporaries as we are translating a specification
                currentExpr = TranslateOverSightCodeContractFuncCall(node);
            }
            else if (context.containsEventName(currentContract, functionName))
            {
                // generate empty statement list to ignore the event call                
                List<BoogieAttribute> attributes = new List<BoogieAttribute>()
                {
                new BoogieAttribute("EventEmitted", "\"" + functionName + "_" + currentContract.Name + "\""),
                };
                currentStmtList.AddStatement(new BoogieAssertCmd(new BoogieLiteralExpr(true), attributes));
            }
            else if (functionName.Equals("call"))
            {
                TranslateCallStatement(node);
            }
            else if (functionName.Equals("delegatecall"))
            {
                OverSightAssert(false, "low-level delegatecall statements not supported...");
            }
            else if (functionName.Equals("transfer"))
            {
                TranslateTransferCallStmt(node);
            }
            else if (functionName.Equals("send"))
            {
                var tmpVarExpr = MkNewLocalVariableForFunctionReturn(node);
                var amountExpr = TranslateExpr(node.Arguments[0]);
                TranslateSendCallStmt(node, tmpVarExpr, amountExpr);
                currentExpr = tmpVarExpr;
            }
            else if (IsDynamicArrayPush(node))
            {
                TranslateDynamicArrayPush(node);
            }
            else if (IsExternalFunctionCall(node))
            {
                // external function calls

                var memberAccess = node.Expression as MemberAccess;
                OverSightAssert(memberAccess != null, $"An external function has to be a member access, found {node.ToString()}");

                // HACK: this is the way to identify the return type is void and hence don't need temporary variable
                if (node.TypeDescriptions.TypeString != "tuple()")
                {
                    var tmpVarExpr = MkNewLocalVariableForFunctionReturn(node);
                    var outParams = new List<BoogieIdentifierExpr>() { tmpVarExpr };
                    TranslateExternalFunctionCall(node, outParams);
                    currentExpr = tmpVarExpr;
                }
                else
                {
                    TranslateExternalFunctionCall(node);
                }
            }
            else // internal function calls
            {
                // HACK: this is the way to identify the return type is void and hence don't need temporary variable
                if (node.TypeDescriptions.TypeString != "tuple()")
                {
                    // internal function calls
                    var tmpVarExpr = MkNewLocalVariableForFunctionReturn(node);
                    var outParams = new List<BoogieIdentifierExpr>() { tmpVarExpr };
                    TranslateInternalFunctionCall(node, outParams);
                    currentExpr = tmpVarExpr;
                }
                else
                {
                    TranslateInternalFunctionCall(node);
                }

            }
            return false;
        }

        private BoogieExpr TranslateOverSightCodeContractFuncCall(FunctionCall node)
        {
            var OverSightFunc = GetOverSightCodeContractFunction(node);
            OverSightAssert(OverSightFunc != null, $"Unknown OverSight code contracts function {node.ToString()}");
            var boogieExprs = node.Arguments.ConvertAll(x => TranslateExpr(x));
            // HACK for Sum
            if (OverSightFunc.Equals("_SumMapping_OverSight"))
            {
                //has to be M_ref_int[mapp[this]] instead of mapp[this]
                var mapName = Flags_HelperClass.generateMemoryMapName(BoogieType.Ref, BoogieType.Int);
                boogieExprs[0] = new BoogieMapSelect(new BoogieIdentifierExpr(mapName), boogieExprs[0]);
            }
            return new BoogieFuncCallExpr(OverSightFunc, boogieExprs);
            }

        private bool IsOverSightCodeContractFunction(FunctionCall node)
        {
            if (node.Expression is MemberAccess member)
            {
                if (member.Expression is Identifier ident)
                {
                    if (ident.Name.Equals("OverSight"))
                    {
                        // ignore the specifiction functions
                        if (member.MemberName.Equals("Invariant") ||
                            member.MemberName.Equals("ContractInvariant") ||
                            member.MemberName.Equals("Requires") ||
                            member.MemberName.Equals("Ensures") ||
                            member.MemberName.Equals("Modifies"))
                            return false;
                        else
                            return true;
                    }
                }
            }
            return false;
        }

        private string GetOverSightCodeContractFunction(FunctionCall node)
        {
            if (node.Expression is MemberAccess member)
            {
                if (member.Expression is Identifier ident)
                {
                    if (ident.Name.Equals("OverSight"))
                    {
                        if (member.MemberName.Equals("SumMapping"))
                            return "_SumMapping_OverSight";
                        if (member.MemberName.Equals("Old"))
                            return "old"; //map it old(..) in Boogie
                        if (member.MemberName.Equals("Modifies"))
                            return "Modifies"; //map it modifies and forall(..) in Boogie
                        else
                            return null;
                    }
                }
            }
            return null;
        }

        private BoogieIdentifierExpr MkNewLocalVariableForFunctionReturn(FunctionCall node)
        {
            var boogieTypeCall = Flags_HelperClass.getBoogieExpression(node.TypeDescriptions.TypeString);


            var newBoogieVar =  MkNewLocalVariableWithType(boogieTypeCall);

            Debug.Assert(currentStmtList != null);
            AddAssumeForUints(node, newBoogieVar, node.TypeDescriptions);

            return newBoogieVar;
        }

        private BoogieIdentifierExpr MkNewLocalVariableWithType(BoogieType boogieTypeCall)
        {
            var tmpVar = new BoogieLocalVariable(context.createFreshIdentifier(boogieTypeCall));
            boogieToLocalVarsMap[currentBoogieProc].Add(tmpVar);

            var tmpVarExpr = new BoogieIdentifierExpr(tmpVar.Name);

            return tmpVarExpr;
        }

        #region implicit functions 
        /// <summary>
        /// Implicit function calls
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool IsImplicitFunc(FunctionCall node)
        {
            return
                IsKeccakFunc(node) ||
                IsAbiEncodePackedFunc(node) ||
                IsTypeCast(node) ||
                IsStructConstructor(node) ||
                IsContractConstructor(node);
         }

        private bool IsContractConstructor(FunctionCall node)
        {
            return node.Expression is NewExpression;
        }

        private bool IsStructConstructor(FunctionCall node)
        {
            return node.Kind.Equals("structConstructorCall");
        }

        private bool IsKeccakFunc(FunctionCall node)
        {
            if (node.Expression is Identifier ident)
            {
                return ident.Name.Equals("keccak256");
            }
            return false;
        }

        private bool IsAbiEncodePackedFunc(FunctionCall node)
        {
            if (node.Expression is MemberAccess member)
            {
                if (member.Expression is Identifier ident)
                {
                    if (ident.Name.Equals("abi"))
                    {
                        if (member.MemberName.Equals("encodePacked"))
                            return true;
                    }
                }
            }
            return false;
        }

        private void TranslateKeccakFuncCall(FunctionCall funcCall, BoogieExpr lhs)
        {
            var expression = funcCall.Arguments[0];
            var boogieExpr = TranslateExpr(expression);
            var keccakExpr = new BoogieFuncCallExpr("keccak256", new List<BoogieExpr>() { boogieExpr });
            currentStmtList.AddStatement(new BoogieAssignCmd(lhs, keccakExpr));
            BoogieExpr nonZeroHashExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, lhs, new BoogieLiteralExpr(BigInteger.Zero));
            currentStmtList.AddStatement(new BoogieAssumeCmd(nonZeroHashExpr));
            return;
        }

        private void TranslateAbiEncodedFuncCall(FunctionCall funcCall, BoogieExpr lhs)
        {
            var arguments = funcCall.Arguments;
            if (arguments.Count > 2)
            {
                OverSightAssert(false, $"Variable argument function abi.encodePacked(...) currently supported only for 1 or 2 arguments, encountered  {arguments.Count} arguments");
            }
            var boogieExprs = arguments.ConvertAll(x => TranslateExpr(x));
            var funcName = $"abiEncodePacked{arguments.Count}";
            // hack
            if(arguments[0].TypeDescriptions.TypeString=="address")
                funcName = funcName + "R";
            var abiEncodeFuncCall = new BoogieFuncCallExpr(funcName, boogieExprs);
            currentStmtList.AddStatement(new BoogieAssignCmd(lhs, abiEncodeFuncCall));
            return;
        }

        private void TranslateCallStatement(FunctionCall node, List<BoogieIdentifierExpr> outParams = null)
        {
            OverSightAssert(outParams == null || outParams.Count == 2, "Number of outPArams for call statement should be 2");
            // only handle call.value(x).gas(y)("") 
            var arg0 = node.Arguments[0].ToString();
            if (!string.IsNullOrEmpty(arg0) && !arg0.Equals("\'\'"))
            {
                currentStmtList.AddStatement(new BoogieSkipCmd(node.ToString()));
                OverSightAssert(false, "low-level call statements with non-empty signature not implemented..");
            }

            // almost identical to send(amount)
            BoogieIdentifierExpr tmpVarExpr = outParams[0]; //bool part of the tuple
            if (tmpVarExpr == null)
            {
                tmpVarExpr = MkNewLocalVariableWithType(BoogieType.Bool);
            }

            var amountExpr = node.MsgValue != null ? TranslateExpr(node.MsgValue) : new BoogieLiteralExpr(BigInteger.Zero);
            TranslateSendCallStmt(node, tmpVarExpr, amountExpr);
            currentExpr = tmpVarExpr;
        }

        private void TranslateTransferCallStmt(FunctionCall node)
        {
            var amountExpr = TranslateExpr(node.Arguments[0]);

            // call FallbackDispatch(from, to, amount)
            BoogieCallCmd callStmt = MkFallbackDispatchCallCmd(node, amountExpr);

            currentStmtList.AddStatement(callStmt);
            return;
        }

        private BoogieCallCmd MkFallbackDispatchCallCmd(FunctionCall node, BoogieExpr amountExpr)
        {
            OverSightAssert(node.Expression is MemberAccess, $"Expecting a call of the form e.send/e.transfer/e.call, but found {node.ToString()}");
            var memberAccess = node.Expression as MemberAccess;
            var baseExpr = memberAccess.Expression;

            var callStmt = new BoogieCallCmd(
                    "FallbackDispatch",
                    new List<BoogieExpr>() { new BoogieIdentifierExpr("this"), TranslateExpr(baseExpr), amountExpr },
                    new List<BoogieIdentifierExpr>()
                    );
            return callStmt;
        }

        private void TranslateSendCallStmt(FunctionCall node, BoogieIdentifierExpr returnExpr, BoogieExpr amountExpr)
        {
            var guard = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE,
                new BoogieMapSelect(new BoogieIdentifierExpr("Balance"), new BoogieIdentifierExpr("this")),
                amountExpr);

            // call FallbackDispatch(from, to, amount)
            var callStmt = MkFallbackDispatchCallCmd(node, amountExpr);

            var thenBody = new BoogieStmtList();
            thenBody.AddStatement(callStmt); 
            thenBody.AddStatement(new BoogieAssignCmd(returnExpr, new BoogieLiteralExpr(true)));

            var elseBody = new BoogieAssignCmd(returnExpr, new BoogieLiteralExpr(false));

            currentStmtList.AddStatement(new BoogieIfCmd(guard, thenBody, BoogieStmtList.MakeSingletonStmtList(elseBody)));
            return;
        }

        private void TranslateNewStatement(FunctionCall node, BoogieExpr lhs)
        {
            OverSightAssert(node.Expression is NewExpression);
            NewExpression newExpr = node.Expression as NewExpression;
            OverSightAssert(newExpr.TypeName is UserDefinedTypeName);
            UserDefinedTypeName udt = newExpr.TypeName as UserDefinedTypeName;

            ContractDefinition contract = context.retrieveASTNodethroughID(udt.ReferencedDeclaration) as ContractDefinition;
            OverSightAssert(contract != null);

            // define a local variable to temporarily hold the object
            BoogieTypedIdent freshAllocTmpId = context.createFreshIdentifier(BoogieType.Ref);
            BoogieLocalVariable allocTmpVar = new BoogieLocalVariable(freshAllocTmpId);
            boogieToLocalVarsMap[currentBoogieProc].Add(allocTmpVar);

            // define a local variable to store the new msg.value
            BoogieTypedIdent freshMsgValueId = context.createFreshIdentifier(BoogieType.Int);
            BoogieLocalVariable msgValueVar = new BoogieLocalVariable(freshMsgValueId);
            boogieToLocalVarsMap[currentBoogieProc].Add(msgValueVar);

            BoogieIdentifierExpr tmpVarIdentExpr = new BoogieIdentifierExpr(freshAllocTmpId.Name);
            BoogieIdentifierExpr msgValueIdentExpr = new BoogieIdentifierExpr(freshMsgValueId.Name);
            BoogieIdentifierExpr allocIdentExpr = new BoogieIdentifierExpr("Alloc");

            // suppose the statement is lhs := new A(args);

            // call tmp := FreshRefGenerator();
            currentStmtList.AddStatement(
                new BoogieCallCmd(
                    "FreshRefGenerator",
                    new List<BoogieExpr>(),
                    new List<BoogieIdentifierExpr>() { tmpVarIdentExpr }
                    ));

            // call constructor of A with this = tmp, msg.sender = this, msg.value = tmpMsgValue, args
            string callee = Conversion_Utility_Tool.GetCanonicalConstructorName(contract);
            List<BoogieExpr> inputs = new List<BoogieExpr>()
            {
                tmpVarIdentExpr,
                new BoogieIdentifierExpr("this"),
                new BoogieLiteralExpr(BigInteger.Zero)//assuming msg.value is 0 for new
            };
            foreach (Expression arg in node.Arguments)
            {
                BoogieExpr argument = TranslateExpr(arg);
                inputs.Add(argument);
            }
            // assume DType[tmp] == A
            BoogieMapSelect dtypeMapSelect = new BoogieMapSelect(new BoogieIdentifierExpr("DType"), tmpVarIdentExpr);
            BoogieIdentifierExpr contractIdent = new BoogieIdentifierExpr(contract.Name);
            BoogieExpr dtypeAssumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, dtypeMapSelect, contractIdent);
            currentStmtList.AddStatement(new BoogieAssumeCmd(dtypeAssumeExpr));
            // The assume DType[tmp] == A is before the call as the constructor may do a dynamic 
            // dispatch and the DType[tmp] is unconstrained before the call
            List<BoogieIdentifierExpr> outputs = new List<BoogieIdentifierExpr>();
            currentStmtList.AddStatement(new BoogieCallCmd(callee, inputs, outputs));
            // lhs := tmp;
            currentStmtList.AddStatement(new BoogieAssignCmd(lhs, tmpVarIdentExpr));
            return;
        }

        private void TranslateStructConstructor(FunctionCall node, BoogieExpr lhs)
        {
            var structString = node.TypeDescriptions.TypeString.Split(' ')[1];

            // define a local variable to temporarily hold the object
            BoogieTypedIdent freshAllocTmpId = context.createFreshIdentifier(BoogieType.Ref);
            BoogieLocalVariable allocTmpVar = new BoogieLocalVariable(freshAllocTmpId);
            boogieToLocalVarsMap[currentBoogieProc].Add(allocTmpVar);

            // define a local variable to store the new msg.value
            BoogieTypedIdent freshMsgValueId = context.createFreshIdentifier(BoogieType.Int);
            BoogieLocalVariable msgValueVar = new BoogieLocalVariable(freshMsgValueId);
            boogieToLocalVarsMap[currentBoogieProc].Add(msgValueVar);

            BoogieIdentifierExpr tmpVarIdentExpr = new BoogieIdentifierExpr(freshAllocTmpId.Name);
            BoogieIdentifierExpr msgValueIdentExpr = new BoogieIdentifierExpr(freshMsgValueId.Name);
            BoogieIdentifierExpr allocIdentExpr = new BoogieIdentifierExpr("Alloc");

            // suppose the statement is lhs := new A(args);

            // call tmp := FreshRefGenerator();
            currentStmtList.AddStatement(
                new BoogieCallCmd(
                    "FreshRefGenerator",
                    new List<BoogieExpr>(),
                    new List<BoogieIdentifierExpr>() { tmpVarIdentExpr }
                    ));

            // call constructor of A with this = tmp, msg.sender = this, msg.value = tmpMsgValue, args
            string callee = structString + "_ctor"; // TransUtils.GetCanonicalConstructorName(contract);
            List<BoogieExpr> inputs = new List<BoogieExpr>()
            {
                tmpVarIdentExpr,
                new BoogieIdentifierExpr("this"),
                new BoogieLiteralExpr(BigInteger.Zero) // msg.value is 0 
            };
            foreach (Expression arg in node.Arguments)
            {
                BoogieExpr argument = TranslateExpr(arg);
                inputs.Add(argument);
            }
            // assume DType[tmp] == A
            BoogieMapSelect dtypeMapSelect = new BoogieMapSelect(new BoogieIdentifierExpr("DType"), tmpVarIdentExpr);
            BoogieIdentifierExpr contractIdent = new BoogieIdentifierExpr(structString); // contract.Name);
            BoogieExpr dtypeAssumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, dtypeMapSelect, contractIdent);
            currentStmtList.AddStatement(new BoogieAssumeCmd(dtypeAssumeExpr));
            // The assume DType[tmp] == A is before the call as the constructor may do a dynamic 
            // dispatch and the DType[tmp] is unconstrained before the call
            List<BoogieIdentifierExpr> outputs = new List<BoogieIdentifierExpr>();
            currentStmtList.AddStatement(new BoogieCallCmd(callee, inputs, outputs));
            // lhs := tmp;
            currentStmtList.AddStatement(new BoogieAssignCmd(lhs, tmpVarIdentExpr));
            return;
        }

        private bool IsDynamicArrayPush(FunctionCall node)
        {
            string functionName = Conversion_Utility_Tool.GetFuncNameFromFuncCall(node);
            if (functionName.Equals("push"))
            {
                OverSightAssert(node.Expression is MemberAccess);
                MemberAccess memberAccess = node.Expression as MemberAccess;
                return Flags_HelperClass.IsArrayTypeString(memberAccess.Expression.TypeDescriptions.TypeString);
            }
            return false;
        }

        private void TranslateDynamicArrayPush(FunctionCall node)
        {
            OverSightAssert(node.Expression is MemberAccess);
            OverSightAssert(node.Arguments.Count == 1);

            MemberAccess memberAccess = node.Expression as MemberAccess;
            BoogieExpr receiver = TranslateExpr(memberAccess.Expression);
            BoogieExpr element = TranslateExpr(node.Arguments[0]);

            BoogieExpr lengthMapSelect = new BoogieMapSelect(new BoogieIdentifierExpr("Length"), receiver);
            // suppose the form is a.push(e)
            // tmp := Length[this][a];
            BoogieTypedIdent tmpIdent = context.createFreshIdentifier(BoogieType.Int);
            boogieToLocalVarsMap[currentBoogieProc].Add(new BoogieLocalVariable(tmpIdent));
            BoogieIdentifierExpr tmp = new BoogieIdentifierExpr(tmpIdent.Name);
            BoogieAssignCmd assignCmd = new BoogieAssignCmd(tmp, lengthMapSelect);
            currentStmtList.AddStatement(assignCmd);

            // M[this][a][tmp] := e;
            BoogieType mapKeyType = BoogieType.Int;
            BoogieType mapValType = Flags_HelperClass.getBoogieExpression(node.Arguments[0].TypeDescriptions.TypeString);
            BoogieExpr mapSelect = Flags_HelperClass.GetMemoryMapSelectExpr(mapKeyType, mapValType, receiver, tmp);
            BoogieAssignCmd writeCmd = new BoogieAssignCmd(mapSelect, element);
            currentStmtList.AddStatement(writeCmd);

            // Length[this][a] := tmp + 1;
            BoogieExpr rhs = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.ADD, tmp, new BoogieLiteralExpr(1));
            BoogieAssignCmd updateLengthCmd = new BoogieAssignCmd(lengthMapSelect, rhs);
            currentStmtList.AddStatement(updateLengthCmd);
            return;
        }
        #endregion

        private bool IsExternalFunctionCall(FunctionCall node)
        {
            if (node.Expression is MemberAccess memberAccess)
            {
                if (memberAccess.Expression is Identifier identifier)
                {
                    if (identifier.Name == "this")
                    {
                        return true;
                    }
                    var contract = context.retrieveASTNodethroughID(identifier.ReferencedDeclaration) as ContractDefinition;
                    if (contract == null)
                    {
                        return true;
                    }
                } else if (memberAccess.Expression.ToString().Equals("msg.sender"))
                {
                    // calls can be of the form "msg.sender.call()" or "msg.sender.send()" or "msg.sender.transfer()"
                    return true;
                }
                else if (memberAccess.Expression is FunctionCall)
                {
                    return true;
                } else if (memberAccess.Expression is IndexAccess)
                {
                    //a[i].foo(..)
                    return true;
                }
            }
            return false;
        }

        private ContractDefinition IsLibraryFunctionCall(FunctionCall node)
        {
            if (node.Expression is MemberAccess memberAccess)
            {
                if (memberAccess.Expression is Identifier identifier)
                {
                    var contract = context.retrieveASTNodethroughID(identifier.ReferencedDeclaration) as ContractDefinition;
                    // a Library is treated as an external function call
                    // we need to do it here as the Lib.Foo, Lib is not an expression but name of a contract
                    if (contract.ContractKind == EnumContractKind.LIBRARY)
                    {
                        return contract;
                    }
                }
            }
            return null;
        }

        private void TranslateExternalFunctionCall(FunctionCall node, List<BoogieIdentifierExpr> outParams = null)
        {
            OverSightAssert(node.Expression is MemberAccess, $"Expecting a member access expression here {node.Expression.ToString()}");

           MemberAccess memberAccess = node.Expression as MemberAccess;
            if(memberAccess.MemberName.Equals("call"))
            {
                TranslateCallStatement(node, outParams);
                return;
            }
            else if (memberAccess.MemberName.Equals("send"))
            {
                TranslateSendCallStmt(node, outParams[0], TranslateExpr(node.Arguments[0]));
                return;
            }
            else if (memberAccess.MemberName.Equals("transfer"))
            {
                TranslateTransferCallStmt(node); // this may be unreachable as we already trap transfer directly
                return;
            }

            if (IsUsingBasedLibraryCall(memberAccess))
            {
                TranslateUsingLibraryCall(node, outParams);
                return;
            }

            BoogieExpr receiver = TranslateExpr(memberAccess.Expression);
            BoogieExpr msgValueExpr = null;
            if (node.MsgValue != null)
            {
                msgValueExpr = TranslateExpr(node.MsgValue);
            }
            else
            {
                var msgIdTmp = context.createFreshIdentifier(BoogieType.Int);
                BoogieLocalVariable msgValueVar = new BoogieLocalVariable(msgIdTmp);
                boogieToLocalVarsMap[currentBoogieProc].Add(msgValueVar);
                msgValueExpr = new BoogieIdentifierExpr(msgIdTmp.Name);
            }

            List<BoogieExpr> arguments = new List<BoogieExpr>()
            {
                receiver,
                new BoogieIdentifierExpr("this"),
                msgValueExpr, 
            };

            foreach (Expression arg in node.Arguments)
            {
                BoogieExpr argument = TranslateExpr(arg);
                arguments.Add(argument);
            }

            // TODO: we need a way to determine type of receiver from "x.Foo()"
            // This additional condition is checked in the loop at this call site
            // and was the reason why the code was not abstracted into a single call
            var guard = memberAccess.Expression.ToString() == "this"; 
            TranslateDynamicDispatchCall(node, outParams, arguments, guard, receiver);

            return;
        }

        private void TranslateUsingLibraryCall(FunctionCall node, List<BoogieIdentifierExpr> outParams)
        {
            var memberAccess = node.Expression as MemberAccess;
            OverSightAssert(memberAccess != null, $"Expecting a member access expression here {node.ToString()}");

            // find the set of libraries that this type is mapped to wiht using
            string typedescr = memberAccess.Expression.TypeDescriptions.TypeString;

            if (memberAccess.Expression.TypeDescriptions.IsStruct() ||
                memberAccess.Expression.TypeDescriptions.IsContract()
                )
            {
                //struct Foo.Bar storage ref
                typedescr = typedescr.Split(" ")[1];
            } 
            if (memberAccess.Expression.TypeDescriptions.IsArray())
            {
                //uint[] storage ref
                typedescr = typedescr.Split(" ")[0];
            }

            //struct Foo.Bar[] storage ref should also work

            OverSightAssert(context.constructDefinitionsMap.ContainsKey(currentContract), $"Expect to see a using A for {typedescr} in this contract {currentContract.Name}");

            HashSet<UserDefinedTypeName> usingRange = new HashSet<UserDefinedTypeName>();

            // may need to look into base contracts as well (UsingInBase.sol)
            foreach (int id in currentContract.LinearBaseContracts)
            {
                ContractDefinition baseContract = context.retrieveASTNodethroughID(id) as ContractDefinition;
                Debug.Assert(baseContract != null);
                if (!context.constructDefinitionsMap.ContainsKey(baseContract)) continue;
                foreach (var kv in context.constructDefinitionsMap[baseContract])
                {
                    if (kv.Value.ToString().Equals(typedescr))
                    {
                        usingRange.Add(kv.Key);
                    }
                }
            }
            OverSightAssert(usingRange.Count > 0, $"Expecting at least one using A for B for {typedescr}");

            string signature = Conversion_Utility_Tool.InferFunctionSignature(context, node);
            OverSightAssert(context.doesContainFunctionSignature(signature), $"Cannot find a function with signature: {signature}");
            var dynamicTypeToFuncMap = context.returnFunctionDefintiions(signature);
            OverSightAssert(dynamicTypeToFuncMap.Count > 0);

            //intersect the types with a matching function with usingRange
            var candidateLibraries = new List<Tuple<ContractDefinition, FunctionDefinition>>();
            foreach(var tf in dynamicTypeToFuncMap)
            {
                if (usingRange.Any(x => x.Name.Equals(tf.Key.Name.ToString())))
                {
                    candidateLibraries.Add(Tuple.Create(tf.Key, tf.Value));
                }
            }

            OverSightAssert(candidateLibraries.Count == 1, $"Expecting a library call to match exactly one function, found {candidateLibraries.Count}");
            var funcDefn = candidateLibraries[0];
            //x.f(y1, y2) in Solidity becomes f_lib(this, this, 0, x, y1, y2)
            var arguments = new List<BoogieExpr>()
            {
                new BoogieIdentifierExpr("this"),
                new BoogieIdentifierExpr("this"),
                new BoogieLiteralExpr(BigInteger.Zero), //msg.value
                TranslateExpr(memberAccess.Expression)
            };
            arguments.AddRange(node.Arguments.Select(x => TranslateExpr(x)));

            var callee = Conversion_Utility_Tool.GetCanonicalFunctionName(funcDefn.Item2, context);
            var callCmd = new BoogieCallCmd(callee, arguments, outParams);
            currentStmtList.AddStatement(callCmd);
            // throw new NotImplementedException("not done implementing using A for B yet");
        }

        private bool IsUsingBasedLibraryCall(MemberAccess memberAccess)
        {
            // since we only permit "using A for B" for non-contract types
            // this is sufficient, but not necessary in general since non
            // contracts (including libraries) do not have support methods
            return !memberAccess.Expression.TypeDescriptions.IsContract();
        }

        private void TranslateInternalFunctionCall(FunctionCall node, List<BoogieIdentifierExpr> outParams = null)
        {
            List<BoogieExpr> arguments = Conversion_Utility_Tool.GetDefaultArguments();

            // a Library is treated as an external function call
            // we need to do it here as the Lib.Foo, Lib is not an expression but name of a contract
            if (IsLibraryFunctionCall(node) != null)
            {
                arguments[1] = arguments[0]; //msg.sender is also this 
            }

            foreach (Expression arg in node.Arguments)
            {
                BoogieExpr argument = TranslateExpr(arg);    
                arguments.Add(argument);
            }

            // Question: why do we have a dynamic dispatch for an internal call?
            if (node.Kind.Equals("structConstructorCall"))
            {
                // assume the structAssignment is used as: s = S(args);
                TranslateStructConstructor(node, outParams[0]);
            }
            else if (IsDynamicDispatching(node))
            {
                TranslateDynamicDispatchCall(node, outParams, arguments, true, new BoogieIdentifierExpr("this"));
            }
            else if (IsStaticDispatching(node))
            {
                ContractDefinition contract = GetStaticDispatchingContract(node);
                string functionName = Conversion_Utility_Tool.GetFuncNameFromFuncCall(node);
                string callee = functionName + "_" + contract.Name;
                BoogieCallCmd callCmd = new BoogieCallCmd(callee, arguments, outParams);

                currentStmtList.AddStatement(callCmd);
            }
            else
            {
                OverSightAssert(false, $"Unknown type of internal function call: {node.Expression}");
            }
            return;
        }

        private void TranslateDynamicDispatchCall(FunctionCall node, List<BoogieIdentifierExpr> outParams, List<BoogieExpr> arguments, bool condition, BoogieExpr receiver)
        {
            ContractDefinition contractDefn;
            VariableDeclaration varDecl;
            // Solidity internally generates foo() getter for any public state 
            // variable foo in a contract. 
            if (IsGetterForPublicVariable(node, out varDecl, out contractDefn))
            {
                BoogieExpr lhs = new BoogieMapSelect(new BoogieIdentifierExpr("DType"), receiver);
                BoogieExpr rhs = new BoogieIdentifierExpr(contractDefn.Name);
                BoogieExpr guard = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, lhs, rhs);
                currentStmtList.AddStatement(new BoogieAssumeCmd(guard));
                OverSightAssert(outParams.Count == 1, $"Do not support getters for tuples yet {node.ToString()} ");
                string varMapName = Conversion_Utility_Tool.GetCanonicalStateVariableName(varDecl, context);
                BoogieMapSelect mapSelect = new BoogieMapSelect(new BoogieIdentifierExpr(varMapName), arguments[0]);
                currentStmtList.AddStatement(new BoogieAssignCmd(outParams[0], mapSelect));
                return;
            }

            Dictionary<ContractDefinition, FunctionDefinition> dynamicTypeToFuncMap;
            string signature = Conversion_Utility_Tool.InferFunctionSignature(context, node);
            OverSightAssert(context.doesContainFunctionSignature(signature), $"Cannot find a function with signature: {signature}");
            dynamicTypeToFuncMap = context.returnFunctionDefintiions(signature);
            OverSightAssert(dynamicTypeToFuncMap.Count > 0);

            BoogieIfCmd ifCmd = null;
            BoogieExpr lastGuard = null;
            BoogieCallCmd lastCallCmd = null;
            // generate a single if-then-else statement
            foreach (ContractDefinition dynamicType in dynamicTypeToFuncMap.Keys)
            {
                //ignore the ones those who do not derive from the current contract
                if (condition && !dynamicType.LinearBaseContracts.Contains(currentContract.Id))
                    continue;

                FunctionDefinition function = dynamicTypeToFuncMap[dynamicType];
                string callee = Conversion_Utility_Tool.GetCanonicalFunctionName(function, context);

                BoogieExpr lhs = new BoogieMapSelect(new BoogieIdentifierExpr("DType"), receiver);
                BoogieExpr rhs = new BoogieIdentifierExpr(dynamicType.Name);
                BoogieExpr guard = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, lhs, rhs);
                lastGuard = guard;
                BoogieCallCmd callCmd = new BoogieCallCmd(callee, arguments, outParams);
                lastCallCmd = callCmd;
                BoogieStmtList thenBody = BoogieStmtList.MakeSingletonStmtList(callCmd);
                // BoogieStmtList elseBody = ifCmd == null ? null : BoogieStmtList.MakeSingletonStmtList(ifCmd);
                BoogieStmtList elseBody = ifCmd == null ? 
                    BoogieStmtList.MakeSingletonStmtList(new BoogieAssumeCmd(new BoogieLiteralExpr(false))) : 
                    BoogieStmtList.MakeSingletonStmtList(ifCmd);

                ifCmd = new BoogieIfCmd(guard, thenBody, elseBody);
            }

            // optimization: if there is only 1 type that we replace the if with a assume
            if (dynamicTypeToFuncMap.Keys.Count == 1)
            {
                // currentStmtList.AddStatement(new BoogieAssumeCmd(lastGuard));
                currentStmtList.AddStatement(lastCallCmd);
            }
            else
            {
                currentStmtList.AddStatement(ifCmd);
            }
        }

        private bool IsGetterForPublicVariable(FunctionCall node, out VariableDeclaration var, out ContractDefinition contractDefinition)
        {
            var = null;
            contractDefinition = null;
            if (node.Expression is MemberAccess memberAccess)
            {
                if (memberAccess.MemberName.Equals("call"))
                    return false;
                OverSightAssert(memberAccess.ReferencedDeclaration != null);
                var contractTypeStr = memberAccess.Expression.TypeDescriptions.TypeString;

                if (!context.ContainsStateVar(memberAccess.MemberName))
                {
                    return false;
                }
                contractDefinition = context.retrieveContractName(contractTypeStr.Substring("contract ".Length));
                OverSightAssert(contractDefinition != null, $"Expecting a contract {contractTypeStr} to exist in context");

                var = context.retrieveStateVarDynamicType(memberAccess.MemberName, contractDefinition);
                return true;
            }

            return false;
        }

        private bool IsStaticDispatching(FunctionCall node)
        {
            if (node.Expression is MemberAccess memberAccess)
            {
                if (memberAccess.Expression is Identifier ident)
                {
                    if (context.retrieveASTNodethroughID(ident.ReferencedDeclaration) is ContractDefinition)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private ContractDefinition GetStaticDispatchingContract(FunctionCall node)
        {
            OverSightAssert(node.Expression is MemberAccess);
            MemberAccess memberAccess = node.Expression as MemberAccess;

            Identifier contractId = memberAccess.Expression as Identifier;
            OverSightAssert(contractId != null, $"Unknown contract name: {memberAccess.Expression}");

            ContractDefinition contract = context.retrieveASTNodethroughID(contractId.ReferencedDeclaration) as ContractDefinition;
            OverSightAssert(contract != null);
            return contract;
        }

        private bool IsDynamicDispatching(FunctionCall node)
        {
            return node.Expression is Identifier;
        }

        private bool IsTypeCast(FunctionCall node)
        {
            return node.Kind.Equals("typeConversion");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="lhs"></param>
        /// <param name="isElemenentaryTypeCast">indicates if the cast of to an elementary type, often used in specifications</param>
        private void TranslateTypeCast(FunctionCall node, BoogieExpr lhs, out bool isElemenentaryTypeCast)
        {
            isElemenentaryTypeCast = false; 

            OverSightAssert(node.Kind.Equals("typeConversion"));
            OverSightAssert(node.Arguments.Count == 1);
            //OverSightAssert(node.Arguments[0] is Identifier || node.Arguments[0] is MemberAccess || node.Arguments[0] is Literal || node.Arguments[0] is IndexAccess,
            //    "Argument to a typecast has to be an identifier, memberAccess, indexAccess or Literal");

            // target: lhs := T(expr);
            BoogieExpr exprToCast = TranslateExpr(node.Arguments[0]);

            if (node.Expression is Identifier) // cast to user defined types
            {
                Identifier contractId = node.Expression as Identifier;
                ContractDefinition contract = context.retrieveASTNodethroughID(contractId.ReferencedDeclaration) as ContractDefinition;
                OverSightAssert(contract != null);

                // assume (DType[var] == T);
                BoogieMapSelect dtype = new BoogieMapSelect(new BoogieIdentifierExpr("DType"), exprToCast);
                BoogieExpr assumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, dtype, new BoogieIdentifierExpr(contract.Name));
                currentStmtList.AddStatement(new BoogieAssumeCmd(assumeExpr));
                // lhs := expr;
                currentStmtList.AddStatement(new BoogieAssignCmd(lhs, exprToCast));
            }
            else if (node.Expression is ElementaryTypeNameExpression elemType) // cast to elementary types
            {
                isElemenentaryTypeCast = true;
                BoogieExpr rhsExpr = exprToCast;
                // most casts are skips, except address cast
                if (elemType.TypeName.Equals("address") || elemType.TypeName.Equals("address payable"))
                {

                    // skip by default, unless we have an integer/hex constant
                    if (exprToCast is BoogieLiteralExpr blit)
                    {
                        if (blit.ToString().Equals("0"))
                        {
                            rhsExpr = (BoogieExpr) new BoogieIdentifierExpr("null");
                        }
                        else
                        {
                            rhsExpr = new BoogieFuncCallExpr("ConstantToRef", new List<BoogieExpr>() { exprToCast });
                        }
                    }
                }

                // We do not handle downcasts between unsigned integers, when /useModularArithmetic option is enabled:
                if (Flags_HelperClass.UseModularArithmetic)
                {
                    bool argTypeIsUint = node.Arguments[0].TypeDescriptions.IsUintWSize(node.Arguments[0], out uint argSz);
                    if (argTypeIsUint && elemType.ToString().StartsWith("uint"))
                    {
                        uint castSz = uint.Parse(Utility.GetNumberFromEnd(elemType.ToString()));
                        if (argSz > castSz)
                        {
                            Console.WriteLine($"Warning: downcasts are not handled with /useModularArithmetic option");
                        }
                    }
                }
                
                // lets update currentExpr with rhsExpr. The caller may update it with the temporary
                currentExpr = rhsExpr; 
                // lhs := expr;
                currentStmtList.AddStatement(new BoogieAssignCmd(lhs, rhsExpr));
            } 
            else
            {
                OverSightAssert(false, $"Unknown type cast: {node.Expression}");
            }
            return;
        }

        private void OverSightAssert(bool cond, string message = "")
        {
            if (!cond)
            {
                var contractName = currentContract != null ? currentContract.Name : "Unknown";
                var funcName = currentFunction != null ? currentFunction.Name : "Unknown";
                throw new Exception ($"File {currentSourceFile}, Line {currentSourceLine}, Contract {contractName}, Function {funcName}:: {message}....");
            }
        }

        public override bool Visit(UnaryOperation node)
        {
            preTranslationAction(node);
            BoogieExpr expr = TranslateExpr(node.SubExpression);

            switch (node.Operator)
            {
                case "-":
                case "!":
                    var op = (node.Operator == "-" ? BoogieUnaryOperation.Opcode.NEG : BoogieUnaryOperation.Opcode.NOT);
                    BoogieUnaryOperation unaryExpr = new BoogieUnaryOperation(op, expr);
                    currentExpr = unaryExpr;
                    break;
                case "++":
                case "--":
                    var oper = (node.Operator == "++" ? BoogieBinaryOperation.Opcode.ADD : BoogieBinaryOperation.Opcode.SUB);
                    BoogieExpr rhs = new BoogieBinaryOperation(oper, expr, new BoogieLiteralExpr(1));
                    if (node.Prefix) // ++x, --x
                    {
                        BoogieAssignCmd assignCmd = new BoogieAssignCmd(expr, rhs);
                        currentStmtList.AddStatement(assignCmd);
                        currentExpr = expr;
                    } else // x++, x--
                    {
                        var boogieType = Flags_HelperClass.getBoogieExpression(node.SubExpression.TypeDescriptions.TypeString);
                        var tempVar = MkNewLocalVariableWithType(boogieType);
                        currentStmtList.AddStatement(new BoogieAssignCmd(tempVar, expr));

                        // Add assume tempVar>=0 for uint
                        AddAssumeForUints(node.SubExpression, tempVar, node.SubExpression.TypeDescriptions);


                        currentExpr = tempVar;
                        BoogieAssignCmd assignCmd = new BoogieAssignCmd(expr, rhs);
                        currentStmtList.AddStatement(assignCmd);
                    }
                    //print the value
                    if (!Flags_HelperClass.NoDataValuesInfoFlag)
                    {
                        var callCmd = new BoogieCallCmd("boogie_si_record_sol2Bpl_int", new List<BoogieExpr>() { expr }, new List<BoogieIdentifierExpr>());
                        callCmd.Attributes = new List<BoogieAttribute>
                        {
                            new BoogieAttribute("cexpr", $"\"{node.SubExpression.ToString()}\"")
                        };
                        currentStmtList.AddStatement(callCmd);
                    }
                    break;
                default:
                    op = BoogieUnaryOperation.Opcode.UNKNOWN;
                    OverSightAssert(false, $"Unknwon unary operator: {node.Operator}");
                    break;
            }


            return false;
        }

        public override bool Visit(BinaryOperation node)
        {
            preTranslationAction(node);
            BoogieExpr leftExpr = TranslateExpr(node.LeftExpression);
            BoogieExpr rightExpr = TranslateExpr(node.RightExpression);

            BoogieBinaryOperation.Opcode op;
            switch (node.Operator)
            {
                case "+":
                    op = BoogieBinaryOperation.Opcode.ADD;
                    break;
                case "-":
                    op = BoogieBinaryOperation.Opcode.SUB;
                    break;
                case "*":
                    op = BoogieBinaryOperation.Opcode.MUL;
                    break;
                case "**":
                    // Handled below for constants only
                    // TODO: Need to introduce opcode for power operation
                    op = BoogieBinaryOperation.Opcode.MUL;
                    break;
                case "/":
                    op = BoogieBinaryOperation.Opcode.DIV;
                    break;
                case "%":
                    op = BoogieBinaryOperation.Opcode.MOD;
                    break;
                case "==":
                    op = BoogieBinaryOperation.Opcode.EQ;
                    break;
                case "!=":
                    op = BoogieBinaryOperation.Opcode.NEQ;
                    break;
                case ">":
                    op = BoogieBinaryOperation.Opcode.GT;
                    break;
                case ">=":
                    op = BoogieBinaryOperation.Opcode.GE;
                    break;
                case "<":
                    op = BoogieBinaryOperation.Opcode.LT;
                    break;
                case "<=":
                    op = BoogieBinaryOperation.Opcode.LE;
                    break;
                case "&&":
                    op = BoogieBinaryOperation.Opcode.AND;
                    break;
                case "||":
                    op = BoogieBinaryOperation.Opcode.OR;
                    break;
                default:
                    op = BoogieBinaryOperation.Opcode.UNKNOWN;
                    OverSightAssert(false, $"Unknown binary operator: {node.Operator}");
                    break;
            }

            BoogieExpr binaryExpr;
            if (node.Operator == "**")
            {
                if (node.LeftExpression.TypeDescriptions.IsUintConst(node.LeftExpression, out BigInteger valueLeft, out uint szLeft) &&
                    node.RightExpression.TypeDescriptions.IsUintConst(node.RightExpression, out BigInteger valueRight, out uint szRight))
                {

                    binaryExpr = new BoogieLiteralExpr((BigInteger)Math.Pow((double)valueLeft, (double)valueRight));
                }
                else
                {
                    Console.WriteLine($"OverSight translation error: power operation for non-constants or with constant subexpressions is not supported; hint: use temps for subexpressions");
                    return false;
                }
            }
            else
            {
                binaryExpr = new BoogieBinaryOperation(op, leftExpr, rightExpr);
            }
            currentExpr = binaryExpr;

            if (Flags_HelperClass.UseModularArithmetic)
            {
                //if (node.Operator == "+" || node.Operator == "-" || node.Operator == "*" || node.Operator == "/" || node.Operator == "**")
                if (node.Operator == "+" || node.Operator == "-" || node.Operator == "*" || node.Operator == "/")
                {
                    if (node.LeftExpression.TypeDescriptions != null && node.RightExpression.TypeDescriptions != null)
                    {
                        var isUintLeft = node.LeftExpression.TypeDescriptions.IsUintWSize(node.LeftExpression, out uint szLeft);
                        var isUintRight = node.RightExpression.TypeDescriptions.IsUintWSize(node.RightExpression, out uint szRight);
                        var isUintConstLeft = node.LeftExpression.TypeDescriptions.IsUintConst(node.LeftExpression, out BigInteger valueLeft, out uint szLeftConst);
                        var isUintConstRight = node.RightExpression.TypeDescriptions.IsUintConst(node.RightExpression, out BigInteger valueRight, out uint szRightConst);
                        // If both operands are literals, do not use "modBpl" for the binary operation:
                        if (isUintLeft && isUintRight)
                        {
                            if (!isUintConstLeft || !isUintConstRight)
                            {
                                OverSightAssert(szLeft != 0, $"size of uint lhs in binary expr is zero");
                                OverSightAssert(szRight != 0, $"size of uint rhs in binary expr is zero");
                                BigInteger maxUIntValue = (BigInteger)Math.Pow(2, Math.Max(szLeft, szRight));
                                currentExpr = new BoogieFuncCallExpr("modBpl", new List<BoogieExpr>() { binaryExpr, new BoogieLiteralExpr(maxUIntValue) });
                            }        
                        }
                    }
                }
            }
            
            return false;
        }

        public override bool Visit(Conditional node)
        {
            preTranslationAction(node);
            BoogieExpr guard = TranslateExpr(node.Condition);
            BoogieExpr thenExpr = TranslateExpr(node.TrueExpression);
            BoogieExpr elseExpr = TranslateExpr(node.FalseExpression);

            BoogieITE iteExpr = new BoogieITE(guard, thenExpr, elseExpr);
            currentExpr = iteExpr;

            return false;
        }

        public override bool Visit(IndexAccess node)
        {
            preTranslationAction(node);
            Expression baseExpression = node.BaseExpression;
            Expression indexExpression = node.IndexExpression;

            BoogieType indexType = Flags_HelperClass.getBoogieExpression(indexExpression.TypeDescriptions.TypeString);
            BoogieExpr indexExpr = TranslateExpr(indexExpression);

            // the baseExpression has an array or mapping type
            BoogieType baseKeyType = Flags_HelperClass.GenerateKeyTypeFromString(baseExpression.TypeDescriptions.TypeString);
            BoogieType baseValType = Flags_HelperClass.GenerateValueTypeFromString(baseExpression.TypeDescriptions.TypeString);
            BoogieExpr baseExpr = null;

            baseExpr = TranslateExpr(baseExpression);
  

            BoogieExpr indexAccessExpr = new BoogieMapSelect(baseExpr, indexExpr);
            currentExpr = Flags_HelperClass.GetMemoryMapSelectExpr(baseKeyType, baseValType, baseExpr, indexExpr);
            return false;
        }

        public override bool Visit(InlineAssembly node)
        {
            preTranslationAction(node);
            OverSightAssert(false, $"Inline assembly unsupported {node.ToString()}");
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return obj is OverSight_ProcessHandler translator &&
                   EqualityComparer<BoogieExpr>.Default.Equals(currentExpr, translator.currentExpr) &&
                   EqualityComparer<Dictionary<string, List<BoogieExpr>>>.Default.Equals(ContractInvariants, translator.ContractInvariants);
        }
    }

    static class QVarGenerator
    {
        public static BoogieIdentifierExpr NewQVar(int level, int pos)
        {
            return new BoogieIdentifierExpr($"__i__{level}_{pos}");
        }
    }
}
