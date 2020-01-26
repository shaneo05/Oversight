

namespace SolToBoogie
{
    using System.Diagnostics;
    using SolidityAST;

    public class accumulator_SOL_MapArray : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classContext;

        // current contract that the visitor is visiting
        private ContractDefinition currentContract = null;

        public void setContext(TranslatorContext context)
        {
            this.classContext = context;
        }
        public override bool Visit(ContractDefinition node)
        {
            currentContract = node;
            return true;
        }

        public override void EndVisit(ContractDefinition node)
        {
            currentContract = null;
        }

        public override bool Visit(VariableDeclaration node)
        {
            Debug.Assert(currentContract != null);

            if (node.TypeName is Mapping)
            {
                classContext.AddMappingtoContract(currentContract, node);
            }
            else if (node.TypeName is ArrayTypeName)
            {
                classContext.AddArrayToContract(currentContract, node);
            }
            return false;
        }
    }
}
