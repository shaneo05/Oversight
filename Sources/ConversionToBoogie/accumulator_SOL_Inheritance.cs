

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    /**
     * Accumulator for any references to inheritance that may be used in a contract.
     */
    public class accumulator_SOL_Inheritance
    {
        // require the ContractDefinitions is populated
        private TranslatorContext classTranslationContext;

        public void setContext(TranslatorContext context)
        {
            this.classTranslationContext = context;
        }

        /**
         * Function to filter through the current list of contract definitions stored in the shared translation context.
         * From that point, the for loop will attempt to retrieve the IDs of the base contracts 
         */
        public void SearchForBaseInheritance()
        {
            foreach (ContractDefinition currentContract in classTranslationContext.ContractDefinitions)
            {
                foreach (int baseContractID in currentContract.LinearizedBaseContracts)
                {
                    ContractDefinition baseCurrentContract = classTranslationContext.GetASTNodeById(baseContractID) as ContractDefinition;

                    if (baseCurrentContract != null)
                    {
                        classTranslationContext.AddSubTypeToContract(baseCurrentContract, currentContract);
                    }
                }
            }
        }
    }
}
