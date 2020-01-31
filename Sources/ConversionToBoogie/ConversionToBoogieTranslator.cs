

namespace SolToBoogie
{
    using System;
    using System.Collections.Generic;

    using BoogieAST;
    using SolidityAST;

    //Dedicated class to handle conversion from solidity AST to Boogie AST such that the verification power of Boogie.exe can be leveraged.

    public class BoogieTranslator
    {
        // set of method@contract pairs whose translation is skipped

        SourceUnitList sourceUnits = null;
        TranslatorContext classTranslatorContext;

        bool generateInLineAttributes;

        ProcedureTranslator procedureTranslator;

        public BoogieAST Translate(AST solidityAST, HashSet<Tuple<string, string>> ignoredMethods, TranslatorFlags _translatorFlags = null)
        {
            generateInLineAttributes = _translatorFlags.GenerateInlineAttributes;

            sourceUnits = solidityAST.GetSourceUnits();

            TranslatorContext context = new TranslatorContext(ignoredMethods, generateInLineAttributes, _translatorFlags);
            context.IdToNodeMap = solidityAST.GetIdToNodeMap();
            context.SourceDirectory = solidityAST.SourceDirectory;

            classTranslatorContext = context;

            executeSourceInfoCollector();
            executeSolDesugar();
            executeContratCollection();
            executeInheritanceCollector();
            executeStateVariableCollector();
            executeMapArrayCollector();
            executeConstructorCollector();
            executeFunctionEventCollector();
            executeFunctionEventResolver();
            executeAxiomGenerator();
            executeModifierCollector();
            executeUsingCollector();
            executeProcedureTranslator();
            executeFallBackGenerator();



            // generate harness for each contract
            if (!context.TranslateFlags.NoHarness)
            {
                HarnessGenerator harnessGenerator = new HarnessGenerator(context, this.procedureTranslator.ContractInvariants);
                harnessGenerator.Generate();
            }

            if (context.TranslateFlags.ModelReverts)
            {
                RevertLogicGenerator reverGenerator = new RevertLogicGenerator(context);
                reverGenerator.Generate();
            }

            if (context.TranslateFlags.DoModSetAnalysis)
            {
                ModSetAnalysis modSetAnalysis = new ModSetAnalysis(context);
                modSetAnalysis.PerformModSetAnalysis();
            }

            return new BoogieAST(context.Program);
        }

    private void executeSourceInfoCollector()
    {
        // collect the absolute source path and line number for each AST node
        accumulator_SOL_srcInfo sourceInfoCollector = new accumulator_SOL_srcInfo();
        sourceInfoCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(sourceInfoCollector);

    }

    private void executeSolDesugar()
    {
        // de-sugar the solidity AST
        // will modify the AST
        SolidityDesugaring desugaring = new SolidityDesugaring(classTranslatorContext);
        sourceUnits.Accept(desugaring);
    }

    private void executeContratCollection()
    {
        // collect all contract definitions
        accumulator_SOL_Contract contractCollector = new accumulator_SOL_Contract();
        contractCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(contractCollector);
    }

    private void executeInheritanceCollector()
    {
        // collect all sub types for each contract
        accumulator_SOL_Inheritance inheritanceCollector = new accumulator_SOL_Inheritance();
        inheritanceCollector.setContext(classTranslatorContext);
        inheritanceCollector.Collect();

    }

    private void executeStateVariableCollector()
    {
        // collect explicit state variables
        accumulator_SOL_StateCollector stateVariableCollector = new accumulator_SOL_StateCollector();
        stateVariableCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(stateVariableCollector);
    }

    private void executeMapArrayCollector()
    {
        // collect mappings and arrays
        accumulator_SOL_MapArray mapArrayCollector = new accumulator_SOL_MapArray();
        mapArrayCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(mapArrayCollector);
    }

    private void executeConstructorCollector()
    {
        // collect constructor definitions
        accumulator_SOL_Constructor constructorCollector = new accumulator_SOL_Constructor();
        constructorCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(constructorCollector);
    }

    private void executeFunctionEventCollector()
    {
        // collect explicit function and event definitions
        accumulator_SOL_FunctionEvent functionEventCollector = new accumulator_SOL_FunctionEvent();
        functionEventCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(functionEventCollector);
    }

    private void executeFunctionEventResolver()
    {
        // resolve function and event definitions and determine the actual definition for a dynamic type
        FunctionEventResolver functionEventResolver = new FunctionEventResolver(classTranslatorContext);
        functionEventResolver.Resolve();
    }

    private void executeAxiomGenerator()
    {
        // add types, g;obal ghost variables, and axioms
        GhostVarAndAxiomGenerator generator = new GhostVarAndAxiomGenerator(classTranslatorContext);
        generator.Generate();
    }

    private void executeModifierCollector()
    {
        // collect modifiers information
        accumulator_SOL_Modifiers modifierCollector = new accumulator_SOL_Modifiers();
        modifierCollector.setLocalReferences(classTranslatorContext);
        sourceUnits.Accept(modifierCollector);

    }

    private void executeUsingCollector()
    {
        // collect all using using definitions
        DefinitionsAccumulator usingCollector = new DefinitionsAccumulator();
        usingCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(usingCollector);

    }

    private void executeProcedureTranslator()
    {
        // translate procedures
        ProcedureTranslator procTranslator = new ProcedureTranslator(classTranslatorContext, generateInLineAttributes);
        sourceUnits.Accept(procTranslator);
        this.procedureTranslator = procTranslator;
    }


    private void executeFallBackGenerator()
    {
        // generate fallbacks
        FallbackGenerator fallbackGenerator = new FallbackGenerator(classTranslatorContext);
        fallbackGenerator.Generate();
    }
}
}
