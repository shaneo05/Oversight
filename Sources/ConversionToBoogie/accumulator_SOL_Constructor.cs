

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    /**
     * SubInheritance of AST Visittor 
     * Collect all constructor definitions and put them in the translator context.
     * Overrides Generic Sntax tree visitor 
     */
    public class accumulator_SOL_Constructor : Generic_Syntax_Tree_Visitor
    {
        private AST_Handler classTransContext;

        /**
         * Contract deinfition visitor function to determine whether AST Solidity Node is of type constructor or fallback
         * AST_Handler instance is updated accordingly.
         */
        public override bool ContractDefinition_ReTraceNode(ContractDefinition nodeCollection)
        {
            bool completion = false;
            try
            {
                if (nodeCollection.Nodes != null)
                {
                    //search the node structure, with each child node encountered evaluate its compatibility as to whether its a  constructor or a fallback

                    foreach (ASTNode currentNode in nodeCollection.Nodes)
                    {
                        if (currentNode is FunctionDefinition expectedFunction)
                        {
                            //compare value expected argument as to whether its of constructor type
                            if (expectedFunction.ofConstructorType == true)
                            {
                                classTransContext.InsertConstructorInContract(nodeCollection, expectedFunction);
                            }
                            if (expectedFunction.ofFallBackType == true)
                            {
                                classTransContext.InsertFallBackInContract(nodeCollection, expectedFunction);
                            }
                        }
                    }
                    //conclusion of for loop up to this stage suggests successful completion
                    completion = true;
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine(e.Source);
                completion = false;
            }
            return completion;
 
        }
        public void setContext(AST_Handler context)
        {
            //pass the updated AST_Handler instance to the class translative context
            //Reassignment is not neccessary as any additons or modifications to the codw is handled back within the AST_Handler
            this.classTransContext = context;
        }
    }
}
