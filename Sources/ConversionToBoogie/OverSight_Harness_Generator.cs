

namespace ConversionToBoogie
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Boogie_Syntax_Tree;
    using Sol_Syntax_Tree;

    /**
     * Generate harness for each contract
     */
    public class OverSight_Harness_Generator
    {
        private AST_Handler classTranslatorContext;
        
        /**
         * Set Handler context to class variant.
         */
        public void setTranslatorContext(AST_Handler translatorContext)
        {
            this.classTranslatorContext = translatorContext;
        }
        /**
         * Creates harness for contract translated to Boogie.
         */
        public void createHarness()
        {
            //iterate the collection of contract definitions and generate an implementation harness for each of them
            foreach (ContractDefinition contract in classTranslatorContext.ContractDefinitionsMap)
            {
                //Generate BoogieHarness for each contract that exists.
                createContractBoogieHarness(contract);
            }
        }

        //Create a contract boogie harness for each respective contract parsed through here.
        private void createContractBoogieHarness(ContractDefinition contract)
        {
            //create the contract harness name
            string contractHarnessName = "BoogieEntry_" + contract.Name;

            //input boogie variables
            List<BoogieVariable> inputParameters = new List<BoogieVariable>();
            //output boogie variables
            List<BoogieVariable> outputParameters = new List<BoogieVariable>();

            //Generate the harness procedure declaration.
            BoogieProcedure harnessProcedureDeclaration = new BoogieProcedure(contractHarnessName, inputParameters, outputParameters);
            classTranslatorContext.getProgram.AddBoogieDeclaration(harnessProcedureDeclaration);

            List<BoogieVariable> localVariables = Conversion_Utility_Tool.CollectLocalVars(new List<ContractDefinition>() { contract }, classTranslatorContext);
            BoogieStmtList harnessBody = new BoogieStmtList();
            //add dynamic type assumptions to harness body and generate constructor call fallback.
            harnessBody.AddStatement(BuildDynamicTypeAssumptions(contract));
            GenerateConstructorCall(contract).ForEach(x => harnessBody.AddStatement(x));
            
            //create a boogie implementation type that represents a harness with the above parameters
            BoogieImplementation harnessImpl = new BoogieImplementation(contractHarnessName, inputParameters, outputParameters, localVariables, harnessBody);
            
            //add the implementation as a declaration to the Boogie Program
            classTranslatorContext.getProgram.AddBoogieDeclaration(harnessImpl);
        }
        //Build Dynamic type assumptions for harness body that will be picked up by veirifer.
        private BoogieAssumeCmd BuildDynamicTypeAssumptions(ContractDefinition contract)
        {
            BoogieExpr assumeLhs = new BoogieMapSelect(new BoogieIdentifierExpr("DType"), new BoogieIdentifierExpr("this"));

            List<ContractDefinition> subtypes = new List<ContractDefinition>(classTranslatorContext.returnSubTypesIndex(contract));
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

        //Function to generate constructor call and add IdentifierExpressions from contract users.
        private List<BoogieCmd> GenerateConstructorCall(ContractDefinition contract)
        {
            List<BoogieCmd> localStmtList = new List<BoogieCmd>();
            string callee = Conversion_Utility_Tool.GetCanonicalConstructorName(contract);

            //Identify third part users of the conttract.
            //sender refers external party call.
            List<BoogieExpr> constructorCallInputs = new List<BoogieExpr>();
            constructorCallInputs.Add(new BoogieIdentifierExpr("this"));
            constructorCallInputs.Add(new BoogieIdentifierExpr("msgsender_MSG"));
            constructorCallInputs.Add(new BoogieIdentifierExpr("msgvalue_MSG"));


            if (classTranslatorContext.checkConstructorExists(contract) == true)
            {
                FunctionDefinition constructor = classTranslatorContext.retrieveConstructor(contract);
                foreach (VariableDeclaration param in constructor.Parameters.Parameters)
                {
                    string name = Conversion_Utility_Tool.GetCanonicalLocalVariableName(param, classTranslatorContext);
                    constructorCallInputs.Add(new BoogieIdentifierExpr(name));

                    if (param.TypeName is ArrayTypeName)
                    {
                        localStmtList.Add(new BoogieCallCmd(
                            "FreshRefGenerator",
                            new List<BoogieExpr>(), new List<BoogieIdentifierExpr>() { new BoogieIdentifierExpr(name) }));
                    }
                }
            }

            if (Flags_HelperClass.InstrumentGas)
            {
                Conversion_Utility_Tool.havocGas(localStmtList);
            }

            localStmtList.Add(new BoogieCallCmd(callee, constructorCallInputs, null));
            return localStmtList;
        }
    }
}
