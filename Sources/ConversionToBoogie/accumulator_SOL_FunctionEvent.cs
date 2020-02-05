

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

        public override bool ContractDefinition_VisitNode(ContractDefinition node)
        {
            currentContract = node;
            return true;
        }

        public override void ContractDefinition_VisitCompletion(ContractDefinition node)
        {
            currentContract = null;
        }

        public override bool EventDefinition_VisitNode(EventDefinition node)
        {
            if (classContext != null)
            {
                classContext.AddEventToContract(currentContract, node);
            }
            return false;
        }

        public override bool FunctionDefinition_VisiNode(FunctionDefinition node)
        {
            if (classContext != null)
            {
                classContext.AddFunctionToContract(currentContract, node);
            }
            return false;
        }
    }
}
