

namespace ConversionToBoogie
{
    using System.Diagnostics;
    using Sol_Syntax_Tree;

    /**
     * Collect all function/event definitions and put them in the translator context.
     * The result map only contains functions/events directly defined in each contract,
     * but does not contain inherited functions/events.
     */
    public class accumulator_SOL_FunctionEvent : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classContext;

        // current contract that the visitor is visiting
        private ContractDefinition currentContract = null;

        public void setContext(TranslatorContext context)
        {
            this.classContext = context;
        }

        public override bool TreeNodeVisitor(ContractDefinition node)
        {
            currentContract = node;
            return true;
        }

        public override void EndVisit(ContractDefinition node)
        {
            currentContract = null;
        }

        public override bool Visit(EventDefinition node)
        {
            Debug.Assert(currentContract != null);
            classContext.AddEventToContract(currentContract, node);
            return false;
        }

        public override bool Visit(FunctionDefinition node)
        {
            Debug.Assert(currentContract != null);
            classContext.AddFunctionToContract(currentContract, node);
            return false;
        }
    }
}
