namespace ConversionToBoogie
{

    using Sol_Syntax_Tree;
    using System.Collections.Generic;

    /**
     * Determine the visible functions/events for each contract considering
     * the inheritance hierarchy, and put the information in the context.
     */
    class OverSight_Event_Resolver
    {
        // require the ContractDefinitions member is populated
        // require the ContractToFunctionsMap member is populated
        private TranslatorContext classTranslatorContext;

        public void setTranslatorContext(TranslatorContext givenContext)
        {
            this.classTranslatorContext = givenContext;
        }

        public void filterFunctions()
        {
            OverSight_GenericResolver resolutionHelper = new OverSight_GenericResolver(classTranslatorContext);
            List<ContractDefinition> sortedContracts = resolutionHelper.TopologicalSortByDependency(classTranslatorContext.ContractDefinitions);

            foreach (ContractDefinition contract in sortedContracts)
            {
                // create a deep copy
                List<int> baseContracts = new List<int>(contract.LinearizedBaseContracts);

                if (baseContracts != null)
                {
                    baseContracts.Reverse();
                }

                foreach (int index in baseContracts)
                {
                    ContractDefinition baseContract = classTranslatorContext.GetASTNodeById(index) as ContractDefinition;
                    if (baseContract != null)
                    { 
                        //if baseContract and contract are equal 
                        HashSet<FunctionDefinition> totalFunctions = classTranslatorContext.retrieveFunctionDefinitions(baseContract);
                        foreach (FunctionDefinition function in totalFunctions)
                        {
                            string signature = TranslatorUtilities.ComputeFunctionSignature(function);
                            if (classTranslatorContext != null)
                                classTranslatorContext.AddFunctionToDynamicType(signature, contract, function);
                        }

                        if (baseContract == contract)
                        { 
                            HashSet<EventDefinition> totalEvents = classTranslatorContext.GetEventDefintionsInContract(baseContract);
                            foreach (var singularity in totalEvents)
                            {
                                if (classTranslatorContext != null)
                                    classTranslatorContext.AddEventToContract(contract, singularity);
                            }
                        }

                        //Compute Visible functions in existing function declaration
                        foreach (string funcSig in classTranslatorContext.FuncSigResolutionMap.Keys)
                        {
                            foreach (ContractDefinition tempContract in classTranslatorContext.FuncSigResolutionMap[funcSig].Keys)
                            {
                                FunctionDefinition funcDef = classTranslatorContext.FuncSigResolutionMap[funcSig][tempContract];
                                classTranslatorContext.AddVisibleFunctionToContract(funcDef, tempContract);
                            }
                        }
                    }

                }
            }
        }
    }
}