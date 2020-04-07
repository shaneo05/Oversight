namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;
    using System.Collections.Generic;
    /**
     * Determine the visible functions/events for each contract considering
     * the inheritance hierarchy, and put the information in the context.
     */
    class OverSight_Function_Event_Resolver
    {
        // require the ContractDefinitions member is populated
        // require the ContractToFunctionsMap member is populated
        private AST_Handler classTranslatorContext;

        /**
         * Set the given handler contex to a temporary instance.
         */
        public void setTranslatorContext(AST_Handler givenContext)
        {
            this.classTranslatorContext = givenContext;
        }

        /**
         * Iterates through contract set, filters functions and events from base contracts and adds them to AST_Handler instance
         */
        public void filterFunctionsAndEvents()
        {
            List<ContractDefinition> contractSet = new List<ContractDefinition>(classTranslatorContext.ContractDefinitionsMap);

            foreach (ContractDefinition indexContract in contractSet)
            {
                // create a localised list of integer values containi
                List<int> baseContractSet = new List<int>(indexContract.LinearBaseContracts);

                //for each base contract, return its respective function and event definitiosn and add them to the Handler context.
                foreach (int i in baseContractSet)
                {
                    //return the base contract at index i
                    ContractDefinition baseContract = classTranslatorContext.retrieveASTNodethroughID(i) as ContractDefinition;
                    if (baseContract != null)
                    {
                        //loop through function defs hashset and for each functionDefinition, compute 
                        //the function signature and add the function to the AST_Handler instance.
                        foreach (FunctionDefinition function in classTranslatorContext.returnFunctionDefs(baseContract))
                        {
                            string functionSignature = Conversion_Utility_Tool.ComputeFunctionSignature(function);

                            if (classTranslatorContext != null)
                                classTranslatorContext.AddFunctionToDynamicType(
                                    Conversion_Utility_Tool.ComputeFunctionSignature(function),//Computed signature of function
                                    indexContract,//contract for which function will be added
                                    function); //Function be added
                        }
                        foreach (EventDefinition singleEvent in classTranslatorContext.retrieveEventDefinitionsUsingContract(baseContract))
                        {
                            //if the translator context is not null then add the single event to the contract via addEventToContract
                             if (classTranslatorContext != null)
                                   classTranslatorContext.InsertEventInContract(indexContract, singleEvent);
                        }
                       
                        //Compute Visible functions in existing function declaration
                        foreach (string funcSig in classTranslatorContext.SignatureFunctionMap.Keys)
                        {
                            foreach (ContractDefinition tempContract in classTranslatorContext.SignatureFunctionMap[funcSig].Keys)
                            {
                                FunctionDefinition funcDef = classTranslatorContext.SignatureFunctionMap[funcSig][tempContract];
                                classTranslatorContext.AddVisibleFunctionToContract(funcDef, tempContract);
                            }
                        }
                    }
                }
            }
        }
    }
}