

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    /**
     * accumulator function to handle adding contract definition node to overall translator context
     */
    public class accumulator_SOL_Contract : Generic_Syntax_Tree_Visitor
    {
        private AST_Handler classTranslatorContext;

        public override bool ContractDefinition_ReTraceNode(ContractDefinition currentNode)
        {
            //if reference type is not equal to null then add contract to ASTH refrence
            if(classTranslatorContext != null)
                classTranslatorContext.InsertContractInMapping(currentNode);
            return false;
        }

        //provides base context, called after instantiation
        public void setContext(AST_Handler tempContext)
        {
            this.classTranslatorContext = tempContext;
        }
    }
}
