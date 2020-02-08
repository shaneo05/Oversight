

namespace ConversionToBoogie
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Sol_Syntax_Tree;

    public class OverSight_GenericResolver
    {
        private TranslatorContext classTranslatorContext;

        public void setContext(TranslatorContext translatorContext)
        {
            this.classTranslatorContext = translatorContext;
        }

        
        // May be we don't need it
        public List<ContractDefinition> TopologicalSortByDependency(HashSet<ContractDefinition> contracts)
        {
            // reverse order of topological sorting
            List<ContractDefinition> result = new List<ContractDefinition>();

            HashSet<ContractDefinition> visited = new HashSet<ContractDefinition>();
            foreach (ContractDefinition contract in contracts)
            {
                if (!visited.Contains(contract))
                {
                    TopologicalSortImpl(visited, contract, result);
                }
            }

            bool assert = true;

            if(result.Count != contracts.Count)
            {
                assert = false;
            }
            if(assert == false)
            {
                return null;

            }
            return result;
        }

        private void TopologicalSortImpl(HashSet<ContractDefinition> visited, ContractDefinition contractDefinition, List<ContractDefinition> result)
        {
            visited.Add(contractDefinition);
            foreach (int id in contractDefinition.ContractDependencies)
            {
                ContractDefinition dependency = classTranslatorContext.GetASTNodeById(id) as ContractDefinition;
                Debug.Assert(dependency != null);

                bool containsDependency = !visited.Contains(dependency);
                if (containsDependency)
                {
                    TopologicalSortImpl(visited, dependency, result);
                }
            }

            //assuming all dependecies visited, addd contract definition
            result.Add(contractDefinition);
        }

     
    }
}
