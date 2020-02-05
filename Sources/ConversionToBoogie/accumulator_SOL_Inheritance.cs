

namespace ConversionToBoogie
{
    using System.Diagnostics;
    using Sol_Syntax_Tree;

    public class accumulator_SOL_Inheritance
    {
        // require the ContractDefinitions is populated
        private TranslatorContext classContext;

        public void setContext(TranslatorContext context)
        {
            this.classContext = context;
        }

        public void Collect()
        {
            foreach (ContractDefinition contract in classContext.ContractDefinitions)
            {
                foreach (int baseId in contract.LinearizedBaseContracts)
                {
                    ContractDefinition baseContract = classContext.GetASTNodeById(baseId) as ContractDefinition;
                    Debug.Assert(baseContract != null);
                    classContext.AddSubTypeToContract(baseContract, contract);
                }
            }
        }
    }
}
