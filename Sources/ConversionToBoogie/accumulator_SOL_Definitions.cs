

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;
    using System.Collections.Generic;
    using System.Diagnostics;

    /**
     * Collect all using definitions and put them in the translator context.
     */
    public class DefinitionsAccumulator : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classContext;
        private ContractDefinition currentContract;
        public void setContext(TranslatorContext context)
        {
            this.classContext = context;
        }

        public override bool TreeNodeVisitor(ContractDefinition node)
        {
            currentContract = node;
            classContext.UsingMap[currentContract] = new Dictionary<UserDefinedTypeName, TypeName>();
            return true;
        }

        public override void EndVisit(ContractDefinition node)
        {
            currentContract = null;
        }

        public override bool Visit(UsingForDirective node)
        {
            if (node.TypeName is UserDefinedTypeName userType)
            {
                Debug.Assert(!userType.TypeDescriptions.IsContract(), $"OverSight does not support using A for B where B is a contract name, found {userType.ToString()}");
            }
            classContext.UsingMap[currentContract][node.LibraryName] = node.TypeName;
            return true;
        }
    }
}
