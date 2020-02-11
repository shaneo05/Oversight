

namespace ConversionToBoogie
{
    using System.Diagnostics;
    using Sol_Syntax_Tree;

    public class accumulator_SOL_StateCollector : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classTranslatorContext;

        public void setContext(TranslatorContext context)
        {
            this.classTranslatorContext = context;
        }

        public override bool ContractDefinition_VisitNode(ContractDefinition node)
        {
            foreach (ASTNode child in node.Nodes)
            {
                if (child is VariableDeclaration variableDeclarationObj)
                {
                    Debug.Assert(variableDeclarationObj.StateVariable, $"{variableDeclarationObj.Name} is not a state variable");
                    // add all state variables to the context
                    classTranslatorContext.AddStateVarToContract(node, variableDeclarationObj);
                }
            }
            return false;
        }
    }
}
