

namespace ConversionToBoogie
{
    using System.IO;
    using System.Collections.Generic;
    using Sol_Syntax_Tree;

    //Class to add source info regarding AST to mappings.
    public class accumulator_SOL_srcInfo : Generic_Syntax_Tree_Visitor
    {
        // require the AST_Handler instance for adding source info 
        private AST_Handler classTranslatorContext;

        // current source unit the visitor is visiting
        private SourceUnit currentSourceUnit;

        //from srcFile name to the int list of "\n" positions
        private readonly Dictionary<string, List<int>> DictLineBreaks = new Dictionary<string, List<int>>();

        // Set Context instance to class translator.
        public void setContext(AST_Handler context)
        {
            this.classTranslatorContext = context;
        }

        //Add line breaks to Dictionary
        public override bool Visit_SRCINFO(SourceUnitList sourceUnits)
        {
            string srcDirectoryPath = classTranslatorContext.SourceDirectory;
            foreach (KeyValuePair<string, SourceUnit> entry in sourceUnits.FilenameToSourceUnitMap)
            {
                string srcFileName = Path.Combine(srcDirectoryPath, entry.Key);
                List<int> Spaces = calculateLineSpacing(srcFileName);
                DictLineBreaks.Add(entry.Key, Spaces);
            }
            return true;
        }

        //Prepopulate Sourceunit node to be used in common end visit
        public override bool Visit_SourceUnit(SourceUnit node)
        {
            currentSourceUnit = node;
            return true;
        }

        protected override void CommonEndVisit(ASTNode node)
        {
            if ((node is SourceUnitList) == false)
            {
                string relativePath = currentSourceUnit.AbsolutePath;
                string absolutePath = Path.Combine(classTranslatorContext.SourceDirectory, relativePath);

                string srcInfo = node.Src;
                string[] tokens = srcInfo.Split(':');
                int startPosition = int.Parse(tokens[0]);
                int lineNumber = calculateLineNumber(relativePath, startPosition);

                classTranslatorContext.AddSourceInfoForASTNode(node, absolutePath, lineNumber);
            }
        }

        public int calculateLineNumber(string srcFilePathName, int position)
        {
            srcFilePathName = srcFilePathName.Replace("\\", "/"/*, System.StringComparison.CurrentCulture*/);
            List<int> LineBreaks = DictLineBreaks[srcFilePathName];

            int lineNumber = 0; 
            foreach (var lineStart in LineBreaks)
            {
                if (lineStart > position)
                    break;
                else
                    lineNumber++;
            }
            return lineNumber;
        }

        private List<int> calculateLineSpacing(string filePath)
        {
            List<int> LineBreaks = new List<int>();
            int CharCount = 0;
            LineBreaks.Add(0);
            StreamReader file = new StreamReader(filePath);
            string src = file.ReadToEnd();
            file.Close();

           
            int pos_r, pos_n;
            do
            {
                pos_r = src.IndexOf('\r');
                pos_n = src.IndexOf('\n');
                if (pos_r >= 0 && (pos_r < pos_n || pos_n < 0))
                {
                    CharCount += pos_r + 1;
                    LineBreaks.Add(CharCount);
                    if (pos_r + 1 == pos_n)
                    {
                        CharCount++;
                        pos_r++;
                    }
                    src = src.Substring(pos_r + 1);
                }
                else if (pos_n >= 0 && (pos_n < pos_r || pos_r < 0))
                {
                    CharCount += pos_n + 1;
                    LineBreaks.Add(CharCount);
                    if (pos_n + 1 == pos_r)
                    {
                        CharCount++;
                        pos_n++;
                    }
                    src = src.Substring(pos_n + 1);
                }
            } while (pos_r >= 0 || pos_n >= 0);
            LineBreaks.Add(CharCount + src.Length);
            return LineBreaks;
        }
    }
}
