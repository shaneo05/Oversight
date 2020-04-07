

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    //Class to collect StateVariable properties contained in Generic AST and add them to the appropriate map set.
    public class accumulator_SOL_StateVariable : Generic_Syntax_Tree_Visitor
    {
        //local context to be updated
        private AST_Handler classTranslatorContext;

        public void setContext(AST_Handler context)
        {
            //assigned updated context to local context
            this.classTranslatorContext = context;
        }

        /**
         * This version of the function upon invocation loops through each child node and adds it to the contract provided the expected type equality is met
         */
        public override bool ContractDefinition_ReTraceNode(ContractDefinition node)
        {
            //loop through collection nodes and compare each child node to whether it corresponds to Solidity Type Variable Declaration
            foreach (ASTNode childNode in node.Nodes)
            {
                if (childNode is VariableDeclaration stateVariableDeclarationObject)
                {
                    // add each child node state variable to contract node, assuming reference is not null
                    if(classTranslatorContext != null)
                        classTranslatorContext.AddStateVarToContract(node, stateVariableDeclarationObject);
                }
            }
            //return success state.
            return true;
        }
    }
}
