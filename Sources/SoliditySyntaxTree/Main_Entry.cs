// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Sol_Syntax_Tree
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Logging;

    class Main_Entry
    {
        public static void Main(string[] args)
        {
            DirectoryInfo debugDirectoryInfo = Directory.GetParent(Directory.GetCurrentDirectory());
            string workingDirectory = debugDirectoryInfo.Parent.Parent.Parent.Parent.FullName;
            string solcPath = workingDirectory + "\\Tool\\solc.exe";
            string testDir = workingDirectory + "\\Test\\regression";

            ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Information);
            ILogger logger = loggerFactory.CreateLogger("SolidityAST.RegressionExecutor");

            RegressionExecutor executor = new RegressionExecutor(solcPath, testDir, logger);
            executor.BatchExecute();
            Console.ReadLine();
        }
    }
}
