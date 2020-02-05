

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    /*
     * Accumulator class to handle Mappings located within the given sol contrat 
     */
    public class accumulator_SOL_MapArray : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classTranslationContext;

        // current contract that the visitor is visiting
        private ContractDefinition currentContract = null;

        public void setContext(TranslatorContext context)
        {
            this.classTranslationContext = context;
        }
        public override bool ContractDefinition_VisitNode(ContractDefinition node)
        {
            currentContract = node;
            return true;
        }

        public override void ContractDefinition_VisitCompletion(ContractDefinition node)
        {
            currentContract = null;
        }

        public override bool VariableDeclaration_VisitNode(VariableDeclaration node)
        {
            if (currentContract != null)
            {
                if (node.TypeName is Mapping)
                {
                    classTranslationContext.AddMappingtoContract(currentContract, node);
                }
                else if (node.TypeName is ArrayTypeName)
                {
                    classTranslationContext.AddArrayToContract(currentContract, node);
                }
            }
            return false;
        }
    }
}
