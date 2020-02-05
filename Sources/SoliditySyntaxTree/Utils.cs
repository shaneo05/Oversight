﻿namespace Sol_Syntax_Tree
{
    using System.Diagnostics;
    using System.Collections.Generic;

    public class Utils
    {
        public static void AcceptList<T>(List<T> list, IASTVisitor visitor) where T : ASTNode
        {
            Debug.Assert(list != null);
            foreach (T element in list)
            {
                element.Accept(visitor);
            }
        }
    }
}
