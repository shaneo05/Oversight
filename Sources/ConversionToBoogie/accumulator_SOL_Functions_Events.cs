

namespace ConversionToBoogie
{
    //Using Leveraged Syntax Tree
    using Sol_Syntax_Tree;

    /**
     * Collect all function/event definitions and put them in the translator context.
     * The result map only contains functions/events directly defined in each contract,
     * but does not contain inherited functions/events.
     */
    public class accumulator_SOL_Functions_Events : Generic_Syntax_Tree_Visitor
    {
        private AST_Handler classContext;

        // current contract that the visitor is visiting
        private ContractDefinition currentContract = null;

        public void setContext(AST_Handler context)
        {
            this.classContext = context;
        }
        /**
         * Adds event defintion node to contract upon invocation as long as instance is not null
         */
        public override bool EventDefinition_TraceNode(EventDefinition givenNode)
        {
            if (classContext != null)
            {
                //Contract for a given contract to be added, the event node to be added to the contract
                classContext.InsertEventInContract(currentContract, givenNode);
            }
            return false;
        }

        /**
         * Adds function definition to contract upon invocation as long as instance is not null
         */
        public override bool FunctionDefinition_TraceNode(FunctionDefinition givenNode)
        {
            if (classContext != null)
            {
                //Contract for a function contract to be added, the function node to be added to the contract
                classContext.InsertFunctionInContract(currentContract, givenNode);
            }
            return false;
        }

        public override bool ContractDefinition_ReTraceNode(ContractDefinition givenNode)
        {
            //called prior to above 2 functions, current contract is assigned the node value given here
            currentContract = givenNode;
            return true;
        }

        public override void ContractDefinition_NullifyNode(ContractDefinition node)
        {
            currentContract = null;//Retrace Node to nullify its use
        }
    }
}
