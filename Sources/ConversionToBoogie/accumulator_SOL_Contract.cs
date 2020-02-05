﻿

namespace ConversionToBoogie
{
    using Sol_Syntax_Tree;

    public class accumulator_SOL_Contract : Generic_Syntax_Tree_Visitor
    {
        private TranslatorContext classContext;

        public override bool TreeNodeVisitor(ContractDefinition node)
        {
            if(classContext != null)
                classContext.AddContract(node);
            return false;
        }

        //provides base context, called after instantiation
        public void setContext(TranslatorContext context)
        {
            this.classContext = context;
        }
    }
}
