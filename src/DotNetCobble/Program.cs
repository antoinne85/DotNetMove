using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using DotNetSolutionTools;

namespace DotNetCobble
{
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

    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ProjectOptions>(args)
                .MapResult(
                    (ProjectOptions o) => CobbleFromProject(o),
                    (errs) => 1);
        }

        private static int CobbleFromProject(ProjectOptions projectOptions)
        {
            var csproj = FindCsprojFullPath(projectOptions.Project);

            var dir = Path.GetDirectoryName(csproj);
            Directory.SetCurrentDirectory(dir);
            var file = Path.GetFileNameWithoutExtension(csproj);
            var slnFile = $"{file}.sln";
            ProcessProxy.RunCommands(new []
            {
                $"dotnet new sln -n {file}",
                $"dotnet sln ./{slnFile} add {csproj}"
            });

            var sln = new DotNetSolution(Path.Combine(dir,slnFile));
            var toProcess = new HashSet<IDotNetProjectInstance>();
            var existing = sln.AllProjects.First().ProjectFileAbsolutePath;
            toProcess.Add(sln.AllProjects.First());

            var analyzedProjectFiles = new HashSet<string>();
            analyzedProjectFiles.Add(existing);

            var toAddProjectFiles = new HashSet<string>();

            var dependencies = sln.AllProjects.First().ReferenceProjectPaths;
            foreach (var dependency in dependencies)
            {
                toAddProjectFiles.Add(dependency);
            }

            while (toAddProjectFiles.Count > 0)
            {
                var currentProjectFile = toAddProjectFiles.First();
                
                ProcessProxy.RunCommands(new[]
                {
                    $"dotnet sln ./{slnFile} add {currentProjectFile}"
                });

                sln = new DotNetSolution(slnFile);
                var currentProjectInstance = sln.GetProjectInstanceForProjectFile(currentProjectFile);
                var paths = currentProjectInstance.ReferenceProjectPaths;

                foreach (var path in paths)
                {
                    if (analyzedProjectFiles.Contains(path))
                    {
                        continue;
                    }
                    else
                    {
                        toAddProjectFiles.Add(path);
                    }
                }

                analyzedProjectFiles.Add(currentProjectFile);
                toAddProjectFiles.Remove(currentProjectFile);
            }


            return 0;
        }

        private static string FindCsprojFullPath(string targetProject)
        {
            var pwd = Directory.GetCurrentDirectory();

            if (targetProject.EndsWith(".csproj"))
            {
                if (Path.IsPathRooted(targetProject))
                {
                    return targetProject;
                }
                else
                {
                    targetProject = Path.GetFileNameWithoutExtension(targetProject);
                }
            }

            var csprojFiles = Directory.EnumerateFiles(pwd, "*.csproj", SearchOption.AllDirectories).ToList();
            csprojFiles = csprojFiles.Where(f => Path.GetFileNameWithoutExtension(f) == targetProject).ToList();

            return csprojFiles.SingleOrDefault();
        }
    }

    [Verb("project", HelpText = "Cobbles together a solution from a project origin.")]
    class ProjectOptions
    {
        [Option('p', "project", Required = true,
            HelpText = "The path to the folder or .csproj file for a project.")]
        public string Project { get; set; }
    }
}
