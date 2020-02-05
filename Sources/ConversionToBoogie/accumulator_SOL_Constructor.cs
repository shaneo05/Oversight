

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    /**
     * SubInheritance of AST Visittor 
     * Collect all constructor definitions and put them in the translator context.
     * Overrides 
     */
    public class accumulator_SOL_Constructor : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classTransContext;

        public override bool TreeNodeVisitor(ContractDefinition nodeCollection)
        {
            bool completion = false;

            if (nodeCollection.Nodes != null)
            {
                //search the node structure, with each child no encountered evaluate its compatibility as to whether its a  constructor or a fallback
                foreach (ASTNode currentNode in nodeCollection.Nodes)
                {
                    if (currentNode is FunctionDefinition expectedFunction)
                    {
                        if (expectedFunction.IsConstructor)
                        {
                            classTransContext.AddConstructorToContract(nodeCollection, expectedFunction);
                        }
                        if (expectedFunction.IsFallback)
                        {
                            classTransContext.AddFallbackToContract(nodeCollection, expectedFunction);
                        }
                        if (expectedFunction.IsDeclaredConst)
                        {
                            //not implemented yet
                        }
                    }
                }
            }
            completion = false;
            return completion;
        }
        public void setContext(TranslatorContext context)
        {
            this.classTransContext = context;
        }
    }
}
