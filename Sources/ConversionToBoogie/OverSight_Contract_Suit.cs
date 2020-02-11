

namespace ConversionToBoogie
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Boogie_Syntax_Tree;
    using Sol_Syntax_Tree;

    /**
     * Generate harness for each contract
     */
    public class OverSight_Contract_Suit
    {
        private TranslatorContext classTranslatorContext;
        private Dictionary<string, List<BoogieExpr>> contractInvariants;

        public void setTranslatorContext(TranslatorContext translatorContext)
        {
            this.classTranslatorContext = translatorContext;
        }

        public void setContractInvariants(Dictionary<string, List<BoogieExpr>> givenInvariants)
        {
            this.contractInvariants = givenInvariants;
        }

        /**
         * Creates harness for contract translated to boogie.
         */
        public void createHarness()
        {
            foreach (ContractDefinition contract in classTranslatorContext.ContractDefinitions)
            {
                Dictionary<int, BoogieExpr> houdiniVariableMap = HoudiniHelper.GenerateHoudiniVarMapping(contract, classTranslatorContext);
                GenerateHoudiniVarsForContract(contract, houdiniVariableMap);
                createContractBoogieHarness(contract, houdiniVariableMap);
            }

            createModificationProperties();

            foreach (ContractDefinition contract in classTranslatorContext.ContractDefinitions)
            {
                GenerateCorralChoiceProcForContract(contract);
                GenerateCorralHarnessForContract(contract);
            }
        }

        private void GenerateHoudiniVarsForContract(ContractDefinition contract, Dictionary<int, BoogieExpr> houdiniVarMap)
        {
            foreach (int id in houdiniVarMap.Keys)
            {
                string varName = GetHoudiniVarName(id, contract);
                BoogieConstant houdiniVar = new BoogieConstant(new BoogieTypedIdent(varName, BoogieType.Bool));
                houdiniVar.Attributes = new List<BoogieAttribute>()
                {
                    new BoogieAttribute("existential", true)
                };
                classTranslatorContext.Program.AddDeclaration(houdiniVar);
            }
        }

        private void createModificationProperties()
        {
            foreach (string modifier in classTranslatorContext.ModifierToBoogiePreProc.Keys)
            {
                if (classTranslatorContext.ModifierToBoogiePreImpl.ContainsKey(modifier))
                {
                    classTranslatorContext.Program.AddDeclaration(classTranslatorContext.ModifierToBoogiePreProc[modifier]);
                    classTranslatorContext.Program.AddDeclaration(classTranslatorContext.ModifierToBoogiePreImpl[modifier]);
                }
            }

            foreach (string modifier in classTranslatorContext.ModifierToBoogiePostProc.Keys)
            {
                if (classTranslatorContext.ModifierToBoogiePostImpl.ContainsKey(modifier))
                {
                    classTranslatorContext.Program.AddDeclaration(classTranslatorContext.ModifierToBoogiePostProc[modifier]);
                    classTranslatorContext.Program.AddDeclaration(classTranslatorContext.ModifierToBoogiePostImpl[modifier]);
                }
            }
        }

        private void createContractBoogieHarness(ContractDefinition contract, Dictionary<int, BoogieExpr> houdiniVarMap)
        {
            string harnessName = "BoogieEntry_" + contract.Name;
            List<BoogieVariable> inParams = new List<BoogieVariable>();
            List<BoogieVariable> outParams = new List<BoogieVariable>();
            BoogieProcedure harness = new BoogieProcedure(harnessName, inParams, outParams);
            classTranslatorContext.Program.AddDeclaration(harness);

            List<BoogieVariable> localVars = TranslatorUtilities.CollectLocalVars(new List<ContractDefinition>() { contract }, classTranslatorContext);
            BoogieStmtList harnessBody = new BoogieStmtList();
            harnessBody.AddStatement(GenerateDynamicTypeAssumes(contract));
            GenerateConstructorCall(contract).ForEach(x => harnessBody.AddStatement(x));
            if (classTranslatorContext.TranslateFlags.ModelReverts)
            {
                BoogieExpr assumePred = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, new BoogieIdentifierExpr("revert"));
                if (classTranslatorContext.TranslateFlags.InstrumentGas)
                {
                    assumePred = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, assumePred, new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, new BoogieIdentifierExpr("gas"), new BoogieLiteralExpr(0)));    
                }
                
                harnessBody.AddStatement(new BoogieAssumeCmd(assumePred));
            }
            harnessBody.AddStatement(GenerateWhileLoop(contract, houdiniVarMap, localVars));
            BoogieImplementation harnessImpl = new BoogieImplementation(harnessName, inParams, outParams, localVars, harnessBody);
            classTranslatorContext.Program.AddDeclaration(harnessImpl);
        }

        private BoogieAssumeCmd GenerateDynamicTypeAssumes(ContractDefinition contract)
        {
            BoogieExpr assumeLhs = new BoogieMapSelect(new BoogieIdentifierExpr("DType"), new BoogieIdentifierExpr("this"));

            List<ContractDefinition> subtypes = new List<ContractDefinition>(classTranslatorContext.GetSubTypesOfContract(contract));
            Debug.Assert(subtypes.Count > 0);

            BoogieExpr assumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, assumeLhs,
                new BoogieIdentifierExpr(subtypes[0].Name));
            for (int i = 1; i < subtypes.Count; ++i)
            {
                BoogieExpr rhs = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, assumeLhs,
                    new BoogieIdentifierExpr(subtypes[i].Name));
                assumeExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.OR, assumeExpr, rhs);
            }

            return new BoogieAssumeCmd(assumeExpr);
        }

      
        private List<BoogieCmd> GenerateConstructorCall(ContractDefinition contract)
        {
            List<BoogieCmd> localStmtList = new List<BoogieCmd>();
            string callee = TranslatorUtilities.GetCanonicalConstructorName(contract);
            List<BoogieExpr> inputs = new List<BoogieExpr>()
            {
                new BoogieIdentifierExpr("this"),
                new BoogieIdentifierExpr("msgsender_MSG"),
                new BoogieIdentifierExpr("msgvalue_MSG"),
            };
            if (classTranslatorContext.IsConstructorDefined(contract))
            {
                FunctionDefinition ctor = classTranslatorContext.GetConstructorByContract(contract);
                foreach (VariableDeclaration param in ctor.Parameters.Parameters)
                {
                    string name = TranslatorUtilities.GetCanonicalLocalVariableName(param, classTranslatorContext);
                    inputs.Add(new BoogieIdentifierExpr(name));

                    if (param.TypeName is ArrayTypeName array)
                    {
                        localStmtList.Add(new BoogieCallCmd(
                            "FreshRefGenerator",
                            new List<BoogieExpr>(), new List<BoogieIdentifierExpr>() {new BoogieIdentifierExpr(name)}));
                    }
                }
            }

            if (classTranslatorContext.TranslateFlags.InstrumentGas)
            {
                TranslatorUtilities.havocGas(localStmtList);
            }

            localStmtList.Add(new BoogieCallCmd(callee, inputs, null));
            return localStmtList;
        }

        private BoogieWhileCmd GenerateWhileLoop(ContractDefinition contract, Dictionary<int, BoogieExpr> houdiniVarMap, List<BoogieVariable> localVars)
        {
            // havoc all local variables except `this'
            BoogieStmtList body = GenerateHavocBlock(localVars);

            // generate the choice block
            body.AddStatement(TranslatorUtilities.GenerateChoiceBlock(new List<ContractDefinition>() { contract }, classTranslatorContext));

            // generate candidate invariants for Houdini
            List<BoogiePredicateCmd> candidateInvs = new List<BoogiePredicateCmd>();
            foreach (int id in houdiniVarMap.Keys)
            {
                BoogieIdentifierExpr houdiniVar = new BoogieIdentifierExpr(GetHoudiniVarName(id, contract));
                BoogieExpr candidateInv = houdiniVarMap[id];
                BoogieExpr invExpr = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.IMP, houdiniVar, candidateInv);
                BoogieLoopInvCmd invCmd = new BoogieLoopInvCmd(invExpr);
                candidateInvs.Add(invCmd);
            }

            // add the contract invariant if present
            if (contractInvariants.ContainsKey(contract.Name))
            {
                contractInvariants[contract.Name].ForEach(x => candidateInvs.Add(new BoogieLoopInvCmd(x)));
            }

            return new BoogieWhileCmd(new BoogieLiteralExpr(true), body, candidateInvs);
        }

        private BoogieStmtList GenerateHavocBlock(List<BoogieVariable> localVars)
        {
            BoogieStmtList stmtList = new BoogieStmtList();
            foreach (BoogieVariable localVar in localVars)
            {
                string varName = localVar.TypedIdent.Name;
                if (!varName.Equals("this"))
                {
                    stmtList.AddStatement(new BoogieHavocCmd(new BoogieIdentifierExpr(varName)));
                }
            }
            return stmtList;
        }

        
        private string GetHoudiniVarName(int id, ContractDefinition contract)
        {
            return "HoudiniB" + id.ToString() + "_" + contract.Name;
        }

        private void GenerateCorralChoiceProcForContract(ContractDefinition contract)
        {
            string procName = "CorralChoice_" + contract.Name;
            List<BoogieVariable> inParams = new List<BoogieVariable>()
            {
                new BoogieFormalParam(new BoogieTypedIdent("this", BoogieType.Ref)),
            };
            List<BoogieVariable> outParams = new List<BoogieVariable>();
            BoogieProcedure harness = new BoogieProcedure(procName, inParams, outParams);
            classTranslatorContext.Program.AddDeclaration(harness);

            List<BoogieVariable> localVars = RemoveThisFromVariables(TranslatorUtilities.CollectLocalVars(new List<ContractDefinition>() { contract }, classTranslatorContext));
            BoogieStmtList procBody = GenerateHavocBlock(localVars);
            procBody.AddStatement(TranslatorUtilities.GenerateChoiceBlock(new List<ContractDefinition>() { contract }, classTranslatorContext));
            BoogieImplementation procImpl = new BoogieImplementation(procName, inParams, outParams, localVars, procBody);
            classTranslatorContext.Program.AddDeclaration(procImpl);
        }

        private void GenerateCorralHarnessForContract(ContractDefinition contract)
        {
            string harnessName = "CorralEntry_" + contract.Name;
            List<BoogieVariable> inParams = new List<BoogieVariable>();
            List<BoogieVariable> outParams = new List<BoogieVariable>();
            BoogieProcedure harness = new BoogieProcedure(harnessName, inParams, outParams);
            classTranslatorContext.Program.AddDeclaration(harness);

            List<BoogieVariable> localVars = new List<BoogieVariable>
            {
                new BoogieLocalVariable(new BoogieTypedIdent("this", BoogieType.Ref)),
                new BoogieLocalVariable(new BoogieTypedIdent("msgsender_MSG", BoogieType.Ref)),
                new BoogieLocalVariable(new BoogieTypedIdent("msgvalue_MSG", BoogieType.Int)),
            };
            if (classTranslatorContext.IsConstructorDefined(contract))
            {
                FunctionDefinition ctor = classTranslatorContext.GetConstructorByContract(contract);
                localVars.AddRange(GetParamsOfFunction(ctor));
            }
            BoogieStmtList harnessBody = new BoogieStmtList();
            harnessBody.AddStatement(GenerateDynamicTypeAssumes(contract));
            GenerateConstructorCall(contract).ForEach(x => harnessBody.AddStatement(x));
            if (classTranslatorContext.TranslateFlags.ModelReverts)
            {
                BoogieExpr assumePred = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, new BoogieIdentifierExpr("revert"));
                if (classTranslatorContext.TranslateFlags.InstrumentGas)
                {
                    assumePred = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, assumePred, new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, new BoogieIdentifierExpr("gas"), new BoogieLiteralExpr(0)));    
                }
                
                harnessBody.AddStatement(new BoogieAssumeCmd(assumePred));
            }
            harnessBody.AddStatement(GenerateCorralWhileLoop(contract));
            BoogieImplementation harnessImpl = new BoogieImplementation(harnessName, inParams, outParams, localVars, harnessBody);
            classTranslatorContext.Program.AddDeclaration(harnessImpl);
        }

        private BoogieWhileCmd GenerateCorralWhileLoop(ContractDefinition contract)
        {
            BoogieStmtList body = new BoogieStmtList();
            string callee = "CorralChoice_" + contract.Name;
            List<BoogieExpr> inputs = new List<BoogieExpr>()
            {
                new BoogieIdentifierExpr("this"),
            };
            body.AddStatement(new BoogieCallCmd(callee, inputs, null));

            List<BoogiePredicateCmd> candidateInvs = new List<BoogiePredicateCmd>();
            // add the contract invariant if present
            if (contractInvariants.ContainsKey(contract.Name))
            {
                contractInvariants[contract.Name].ForEach(x => candidateInvs.Add(new BoogieLoopInvCmd(x)));
            }

            return new BoogieWhileCmd(new BoogieLiteralExpr(true), body, candidateInvs);
        }

        private List<BoogieVariable> RemoveThisFromVariables(List<BoogieVariable> variables)
        {
            List<BoogieVariable> ret = new List<BoogieVariable>();
            foreach (BoogieVariable variable in variables)
            {
                if (!variable.TypedIdent.Name.Equals("this"))
                {
                    ret.Add(variable);
                }
            }
            return ret;
        }

        private List<BoogieVariable> GetParamsOfFunction(FunctionDefinition funcDef)
        {
            List<BoogieVariable> parameters = new List<BoogieVariable>();

            var inpParamCount = 0;
            foreach (VariableDeclaration param in funcDef.Parameters.Parameters)
            {
                string name = $"__arg1_{inpParamCount++}_" + funcDef.Name;
                if (!string.IsNullOrEmpty(param.Name))
                {
                    name = TranslatorUtilities.GetCanonicalLocalVariableName(param, classTranslatorContext);
                }
                BoogieType type = TranslatorUtilities.GetBoogieTypeFromSolidityTypeName(param.TypeName);
                BoogieVariable localVar = new BoogieLocalVariable(new BoogieTypedIdent(name, type));
                parameters.Add(localVar);
            }

            var retParamCount = 0;
            foreach (VariableDeclaration param in funcDef.ReturnParameters.Parameters)
            {
                string name = $"__ret1_{retParamCount++}_" + funcDef.Name;

                if (!string.IsNullOrEmpty(param.Name))
                {
                    name = TranslatorUtilities.GetCanonicalLocalVariableName(param, classTranslatorContext);
                }
                BoogieType type = TranslatorUtilities.GetBoogieTypeFromSolidityTypeName(param.TypeName);
                BoogieVariable localVar = new BoogieLocalVariable(new BoogieTypedIdent(name, type));
                parameters.Add(localVar);
            }

            return parameters;
        }
    }
}
