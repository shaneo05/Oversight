

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;
    /*
     * Accumulator class to handle Mappings located within the given sol contract 
     */
    public class accumulator_SOL_Map_Arrays : Generic_Syntax_Tree_Visitor
    {
        private AST_Handler classTranslationContext;

        // current contract that the visitor is visiting
        private ContractDefinition currentContract = null;

        //set the context to the translator context.
        public void setContext(AST_Handler context)
        {
            this.classTranslationContext = context;
        }
        /**
         * Contract assignment occurs here
         * Neccessary for adding mapping and arrays to the contract
         */
        public override bool ContractDefinition_ReTraceNode(ContractDefinition node)
        {
            //Assign current contract the value of node for use later on in populatin the VariableDeclarationMapping
            currentContract = node;
            return true;
        }

        public override void ContractDefinition_NullifyNode(ContractDefinition node)
        {
            //after visitation has completed, terminate reference through null assignment
            currentContract = null;
        }

        public override bool VariableDeclaration_VisitNode(VariableDeclaration currentNode)
        {
            if (currentContract != null)
            {
                //if node instance.TypeName is equal to custom reference type Mapping found in Solidity Syntax tree
                //then add to contract as a mapping.
                if (currentNode.TypeName is Mapping)
                {
                    classTranslationContext.InsertMappingInContract(currentContract, currentNode);
                }
                //if the typename is of array type then add to contract as an array.
                else if (currentNode.TypeName is ArrayTypeName)
                {
                    classTranslationContext.insertArrayInContract(currentContract, currentNode);
                }
                else
                {
                   //Could another declaration type be checked here?, may leave empty 
                }
            }
            return false;
        }
    }
}
