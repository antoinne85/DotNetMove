using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.DotNet.Tools.Common;

namespace DotNetSolutionTools
{
    public static class CliProxy
    {
        public static void CreateSolution(string solutionPath)
        {
            var extension = Path.GetExtension(solutionPath);
            solutionPath = solutionPath.Substring(0, solutionPath.Length - extension.Length);
            ProcessProxy.RunCommands(new []{$"dotnet new sln -n {solutionPath}"});
        }

        public static void AddProjectToSolution(string pathToSolution, string pathToProjectFolder)
        {
            var ext = Path.GetExtension(pathToSolution);
            if (ext != ".sln")
            {
                pathToSolution = Path.Combine(pathToSolution, ".sln");
            }

            ProcessProxy.RunCommands(new []{$"dotnet sln {pathToSolution} add {pathToProjectFolder}"});
        }
    }

    static class ProcessProxy
    {
        public static void RunCommands(IEnumerable<string> commands)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            //* Set your output and error (asynchronous) handlers
            p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            p.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            //* Start process and handlers
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            using (StreamWriter sw = p.StandardInput)
            {
                foreach (var command in commands)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine(command);
                    }
                }
            }

            p.WaitForExit();
        }

        private static void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}
