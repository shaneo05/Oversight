

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    /**
     * accumulaltor function to handle adding contract definition node to overall translator context
     */
    public class accumulator_SOL_Contract : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classTranslatorContext;

        public override bool ContractDefinition_VisitNode(ContractDefinition currentNode)
        {
            if(classTranslatorContext != null)
                classTranslatorContext.AddContract(currentNode);
            return false;
        }

        //provides base context, called after instantiation
        public void setContext(TranslatorContext tempContext)
        {
            this.classTranslatorContext = tempContext;
        }
    }
}
