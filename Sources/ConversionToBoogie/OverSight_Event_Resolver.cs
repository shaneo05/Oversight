

namespace SolToBoogie
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using SolidityAST;

    /**
     * Determine the visible functions/events for each contract considering
     * the inheritance hierarchy, and put the information in the context.
     */
    public class OverSight_Event_Resolver
    {
        // require the ContractDefinitions member is populated
        // require the ContractToFunctionsMap member is populated
        private TranslatorContext context;

        public OverSight_Event_Resolver(TranslatorContext context)
        {
            this.context = context;
        }

        // TODO: resolve events
        public void Resolve()
        {
            ResolveFunctions();
            ComputeVisibleFunctions();
        }

        private void ResolveFunctions()
        {
            OverSight_GenericResolver resolutionHelper = new OverSight_GenericResolver(context);
            List<ContractDefinition> sortedContracts = resolutionHelper.TopologicalSortByDependency(context.ContractDefinitions);

            foreach (ContractDefinition contract in sortedContracts)
            {
                // create a deep copy
                List<int> linearizedBaseContractIds = new List<int>(contract.LinearizedBaseContracts);
                linearizedBaseContractIds.Reverse();

                foreach (int id in linearizedBaseContractIds)
                {
                    ContractDefinition baseContract = context.GetASTNodeById(id) as ContractDefinition;
                    Debug.Assert(baseContract != null);

                    if (baseContract == contract)
                    {
                        HashSet<FunctionDefinition> functions = context.GetFuncDefintionsInContract(contract);
                        foreach (FunctionDefinition function in functions)
                        {
                            string signature = TranslatorUtilities.ComputeFunctionSignature(function);
                            context.AddFunctionToDynamicType(signature, contract, function);
                        }
                    }
                    else
                    {
                        HashSet<FunctionDefinition> functions = context.GetFuncDefintionsInContract(baseContract);
                        foreach (FunctionDefinition function in functions)
                        {
                            if (function.Visibility == EnumVisibility.PRIVATE) continue;

                            string signature = TranslatorUtilities.ComputeFunctionSignature(function);
                            context.AddFunctionToDynamicType(signature, contract, function);
                        }
                        // Events
                        // TODO: Do we need to lookup by signature?
                        HashSet<EventDefinition> events = context.GetEventDefintionsInContract(baseContract);
                        foreach (var evt in events)
                        {
                            context.AddEventToContract(contract, evt);
                        }
                    }
                }
            }

            // PrintFunctionResolutionMap();
        }

        private void ComputeVisibleFunctions()
        {
            foreach (string funcSig in context.FuncSigResolutionMap.Keys)
            {
                foreach (ContractDefinition contract in context.FuncSigResolutionMap[funcSig].Keys)
                {
                    FunctionDefinition funcDef = context.FuncSigResolutionMap[funcSig][contract];
                    context.AddVisibleFunctionToContract(funcDef, contract);
                }
            }

            // PrintVisibleFunctions();
        }
    }
}
