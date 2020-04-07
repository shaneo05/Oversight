

namespace ConversionToBoogie
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Boogie_Syntax_Tree;
    using Sol_Syntax_Tree;

    //Dedicated class to handle conversion from solidity AST to Boogie AST such that the verification power of Boogie.exe can be leveraged.

    public class ConversionToBoogie_Main
    {
        // set of method@contract pairs whose translation is skipped

        SourceUnitList sourceUnits = null;
        AST_Handler classTranslatorContext;

        OverSight_ProcessHandler processHandler;

        //returns a boogieAST reference, containing all captured elements of the sol file in question
        public BoogieAST Translate(AST solidityAST, HashSet<Tuple<string, string>> ignoredMethods, Flags_HelperClass _translatorFlags = null)
        {
            //ignored methods and translator flags need to be remove as not enough time to implement these.

            sourceUnits = solidityAST.GetSourceUnits();

            //create an empty AST_Handler Instance with no ignored method, generate InlineAttributes and no translator flags.
            AST_Handler context = new AST_Handler(ignoredMethods, true, _translatorFlags);
            context.IdToNodeMap = solidityAST.GetIdToNodeMap();
            context.SourceDirectory = solidityAST.SourceDirectory;

            //assign class instance to temporary instance and use it throughout the rest of the
            //process to populate it.
            classTranslatorContext = context;

            //Execute Collection process from given sol contract.
            //Will enable boogie conversion to occur once specific states/functions/variables have been retrieved from the contract.
            executeSourceInfoCollector();
            executeContractCollection();
           
            executeInheritanceCollector();
            executeStateVariableCollector();
            executeMapArrayCollector();
            executeConstructorCollector();
            
            executeFunctionEventCollector();
            executeFunctionEventResolver();

            executeBoogieGenerator();
            executeDefinitionsCollector();

            executeProcessHandler();
            // generate harness for contract
            // non specifiable property, harness is always created and cannot be altered in this code
            OverSight_Harness_Generator harnessGenerator = new OverSight_Harness_Generator();
            harnessGenerator.setTranslatorContext(classTranslatorContext);
            harnessGenerator.createHarness();
            

            BoogieAST completeAST = new BoogieAST(classTranslatorContext.getProgram);

            Debug.Assert(completeAST != null);
            
            //returns the BoogieAST containing the root property, where the declarations of the contract are present.
            return completeAST;
        }

    //Function to collect source info
    private void executeSourceInfoCollector()
    {
        // collect the absolute source path and line number for each AST node
        accumulator_SOL_srcInfo sourceInfoCollector = new accumulator_SOL_srcInfo();
        sourceInfoCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(sourceInfoCollector);

    }

    //function to collect contracts.
    private void executeContractCollection()
    {
        // collect all contract definitions
        accumulator_SOL_Contract contractCollector = new accumulator_SOL_Contract();
        contractCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(contractCollector);
    }

    //functino to collect inheritance properties
    private void executeInheritanceCollector()
    {
        // collect all sub types for each contract
        accumulator_SOL_Inheritance inheritanceCollector = new accumulator_SOL_Inheritance();
        inheritanceCollector.setContext(classTranslatorContext);
        inheritanceCollector.checkContractForInheritance();
    }

    //function to collect statevariables
    private void executeStateVariableCollector()
    {
        // collect explicit state variables
        accumulator_SOL_StateVariable stateVariableCollector = new accumulator_SOL_StateVariable();
        stateVariableCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(stateVariableCollector);
    }

    //function to collect maps and arrays
    private void executeMapArrayCollector()
    {
        // collect mappings and arrays
        accumulator_SOL_Map_Arrays mapArrayCollector = new accumulator_SOL_Map_Arrays();
        mapArrayCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(mapArrayCollector);
    }

    //function to collect constructor
    private void executeConstructorCollector()
    {
        // collect constructor definitions
        accumulator_SOL_Constructor constructorCollector = new accumulator_SOL_Constructor();
        constructorCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(constructorCollector);
    }

    //function to collect functions and events 
    private void executeFunctionEventCollector()
    {
        // collect explicit function and event definitions
        accumulator_SOL_Functions_Events functionEventCollector = new accumulator_SOL_Functions_Events();
        functionEventCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(functionEventCollector);
    }

    /**
     * Function to resolve solidity functions and events to boogie
     */
    private void executeFunctionEventResolver()
    {
        // resolve function and event definitions and determine the actual definition for a dynamic type
        OverSight_Function_Event_Resolver functionEventResolver = new OverSight_Function_Event_Resolver();
        functionEventResolver.setTranslatorContext(classTranslatorContext);
        functionEventResolver.filterFunctionsAndEvents();      
    }

    /**
     * Function to generate boogie properties, including maps, type, global implementations and so forth
     */
    private void executeBoogieGenerator()
    {
        OverSight_Boogie_Generate_Properties generator = new OverSight_Boogie_Generate_Properties(classTranslatorContext);
        generator.GenerateBoogieProperties();
    }

    /**
     * Function to collect definitions from soldity AST and parse to Boogie
     */
    private void executeDefinitionsCollector()
    {
        // collect all using definitions
        DefinitionsAccumulator usingCollector = new DefinitionsAccumulator();
        usingCollector.setContext(classTranslatorContext);
        sourceUnits.Accept(usingCollector);
    }
    private void executeProcessHandler()
    {
        // start process handler and generate inlineattributes
        OverSight_ProcessHandler procTranslator = new OverSight_ProcessHandler(classTranslatorContext, true);
        sourceUnits.Accept(procTranslator);
        this.processHandler = procTranslator;
    }
}
}
