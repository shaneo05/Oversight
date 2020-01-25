

namespace SolToBoogie
{
    using SolidityAST;

    /**
     * Collect all constructor definitions and put them in the translator context.
     */
    public class accumulator_SOL_Constructor : BasicASTVisitor
    {
        private TranslatorContext context;

        public accumulator_SOL_Constructor(TranslatorContext context)
        {
            this.context = context;
        }

        public override bool Visit(ContractDefinition node)
        {
            foreach (ASTNode child in node.Nodes)
            {
                if (child is FunctionDefinition function)
                {
                    if (function.IsConstructor)
                    {
                        context.AddConstructorToContract(node, function);
                    }
                    else if (function.IsFallback)
                    {
                        context.AddFallbackToContract(node, function);
                    }
                }
            }
            return false;
        }
    }
}
