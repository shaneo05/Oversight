

namespace SolToBoogie
{
    using SolidityAST;

    /**
     * SubInheritance of AST Visittor 
     * Collect all constructor definitions and put them in the translator context.
     */
    public class accumulator_SOL_Constructor : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classContext;

        public override bool Visit(ContractDefinition node)
        {
            foreach (ASTNode child in node.Nodes)
            {
                if (child is FunctionDefinition function)
                {
                    if (function.IsConstructor)
                    {
                        classContext.AddConstructorToContract(node, function);
                    }
                    else if (function.IsFallback)
                    {
                        classContext.AddFallbackToContract(node, function);
                    }
                }
            }
            return false;
        }
        public void setContext(TranslatorContext context)
        {
            this.classContext = context;
        }
    }
}
