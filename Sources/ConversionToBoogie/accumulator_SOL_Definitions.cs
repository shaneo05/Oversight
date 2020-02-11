

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;
    using System.Collections.Generic;

    /**
     * Collect all using definitions and put them in the translator context.
     */
    public class DefinitionsAccumulator : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classTranslatorContext;
        private ContractDefinition currentContractDefinition;
        public void setContext(TranslatorContext context)
        {
            this.classTranslatorContext = context;
        }

        /**
         * Represents visit t current node
         */
        public override bool ContractDefinition_VisitNode(ContractDefinition contractDefinitionNode)
        {
            currentContractDefinition = contractDefinitionNode;
            classTranslatorContext.UsingMap[currentContractDefinition] = new Dictionary<UserDefinedTypeName, TypeName>();
            return true;
        }
        /**
         * Represents end of current node visiting
         */
        public override void ContractDefinition_VisitCompletion(ContractDefinition node)
        {
            currentContractDefinition = null;
        }

    }
}
