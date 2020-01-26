

namespace SolToBoogie
{
    using System.Diagnostics;
    using SolidityAST;

    public class accumulator_SOL_StateCollector : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classContext;

        public void setContext(TranslatorContext context)
        {
            this.classContext = context;
        }

        public override bool Visit(ContractDefinition node)
        {
            foreach (ASTNode child in node.Nodes)
            {
                if (child is VariableDeclaration varDecl)
                {
                    Debug.Assert(varDecl.StateVariable, $"{varDecl.Name} is not a state variable");
                    // add all state variables to the context
                    classContext.AddStateVarToContract(node, varDecl);
                }
            }
            return false;
        }
    }
}
