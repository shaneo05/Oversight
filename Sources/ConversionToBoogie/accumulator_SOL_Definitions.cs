

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;
    using System.Collections.Generic;

    /**
     * Collect all using definitions and put them in the AST_Handler context.
     */
    public class DefinitionsAccumulator : Generic_Syntax_Tree_Visitor
    {
        private AST_Handler classTranslatorContext;
        private ContractDefinition currentContractDefinition;
        /**
         * Set empty context to given context
         */
        public void setContext(AST_Handler context)
        {
            this.classTranslatorContext = context;
        }

        /**
         * Represents visit t current node, upon invocation generate contract definitions map for the AST
         */
        public override bool ContractDefinition_ReTraceNode(ContractDefinition contractDefinitionNode)
        {
            currentContractDefinition = contractDefinitionNode;
            classTranslatorContext.constructDefinitionsMap[currentContractDefinition] = new Dictionary<UserDefinedTypeName, TypeName>();
            return true;
        }
        /**
         * Represents end of current node visiting
         */
        public override void ContractDefinition_NullifyNode(ContractDefinition node)
        {
            //assign contract definition to null
            currentContractDefinition = null;
        }

    }
}
