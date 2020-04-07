

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    /**
     * Accumulator for any references to inheritance that may be used in a contract.
     */
    public class accumulator_SOL_Inheritance
    {
        // require the ContractDefinitions is populated
        private AST_Handler classTranslationContext;

        public void setContext(AST_Handler context)
        {
            this.classTranslationContext = context;
        }

        /**
         * Function to filter through the current list of contract definitions stored in the shared translation context.
         * From that point, the for loop will attempt to retrieve the IDs of the base contracts.
         */
        public void checkContractForInheritance()
        {
            foreach (ContractDefinition typeDefinition in classTranslationContext.ContractDefinitionsMap)
            {
                foreach (int baseContractID in typeDefinition.LinearBaseContracts)
                {
                    ContractDefinition baseCurrentContract = classTranslationContext.retrieveASTNodethroughID(baseContractID) as ContractDefinition;

                    //add type to base current contract.
                    if (baseCurrentContract != null)
                    {
                        classTranslationContext.InsertTypeInContract(baseCurrentContract, typeDefinition);
                    }
                }
            }
        }
    }
}
