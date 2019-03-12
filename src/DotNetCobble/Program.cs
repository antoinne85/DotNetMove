using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using DotNetSolutionTools;
using Microsoft.DotNet.Tools.Common;

namespace DotNetCobble
{
    class Program
    {
        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<FromFolderOptions, FromProjectOptions>(args)
                .MapResult(
                    (FromFolderOptions o) => CreateFromFolder(o),
                    (FromProjectOptions o) => CreateFromProject(o),
                    (errs) => 1);

            Console.ReadLine();

            return result;
        }

        private static int CreateFromProject(FromProjectOptions options)
        {
            var folder = Directory.GetCurrentDirectory();
            
            CliProxy.CreateSolution(options.Output);

            var foundProjects = DiskUtilities.FindFullPathToProjects(folder, options.TargetProject);
            var projectFile = foundProjects.Single();
            var currentDir = Directory.GetCurrentDirectory() + "\\";
            var relativePath = PathUtility.GetRelativePath(currentDir, projectFile);
            var dirName = Path.GetDirectoryName(relativePath);
            CliProxy.AddProjectToSolution(options.Output, dirName);

            var slnAbsolutePath = Path.Combine(folder, options.Output);
            var sln = new DotNetSolution(slnAbsolutePath);

            sln.AddExistingProject(projectFile);

            var projectsToProcess = new Queue<string>();
            var firstProject = sln.AllProjects.Single();

            var absolutePathsInSolution = new HashSet<string>();
            absolutePathsInSolution.Add(firstProject.ProjectFileAbsolutePath);

            var firstRound = firstProject.AbsolutePathsToProjectReferences;
            foreach(var proj in firstRound)
            {
                projectsToProcess.Enqueue(proj);
            }

            while(projectsToProcess.Count > 0)
            {
                //Get the project to add.
                var absolutePath = projectsToProcess.Dequeue();

                if (absolutePathsInSolution.Contains(absolutePath))
                {
                    continue;
                }

                //Add it.
                var newProject = sln.AddExistingProject(absolutePath);
                absolutePathsInSolution.Add(absolutePath);
                var dependencies = newProject.AbsolutePathsToProjectReferences;
                foreach (var dependency in dependencies)
                {
                    if (!absolutePathsInSolution.Contains(dependency))
                    {
                        projectsToProcess.Enqueue(dependency);
                    }
                }
            }

            sln.Save();

            return 0;
        }

        private static int CreateFromFolder(FromFolderOptions options)
        {
            if (!options.ExamineReferences)
            {
                return CreateFromFolderWithoutReferences(options);
            }

            return CreateFromFolderWithReferences(options);
        }

        private static int CreateFromFolderWithReferences(FromFolderOptions options)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), options.Source);
            var projectFiles = DiskUtilities.FindProjectFilesInFolder(folder);

            var slnAbsolutePath = Path.Combine(folder, options.Output);
            CliProxy.CreateSolution(options.Output);

            var sln = new DotNetSolution(slnAbsolutePath);
            var dependenciesFolder = sln.GetOrCreateSolutionFolder("Dependencies");

            var projectsToProcess = new Queue<string>();
            foreach (var file in projectFiles)
            {
                projectsToProcess.Enqueue(file);
            }

            //var firstProject = sln.AllProjects.Single();

            var absolutePathsInSolution = new HashSet<string>();
            //absolutePathsInSolution.Add(firstProject.ProjectFileAbsolutePath);

            while (projectsToProcess.Count > 0)
            {
                //Get the project to add.
                var absolutePath = projectsToProcess.Dequeue();
                

                if (absolutePathsInSolution.Contains(absolutePath))
                {
                    continue;
                }

                //Add it.
                var newProject = sln.AddExistingProject(absolutePath);
                absolutePathsInSolution.Add(absolutePath);
                var dependencies = newProject.AbsolutePathsToProjectReferences;
                foreach (var dependency in dependencies)
                {
                    if (!absolutePathsInSolution.Contains(dependency))
                    {
                        projectsToProcess.Enqueue(dependency);
                    }
                }

                var isDependency = !projectFiles.Contains(absolutePath);
                if (isDependency)
                {
                    sln.AddProjectToSolutionFolder(newProject, dependenciesFolder);
                }
            }

            sln.Save();

            return 0;
        }

        private static int CreateFromFolderWithoutReferences(FromFolderOptions options)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), options.Source);
            var projectFiles = DiskUtilities.FindProjectFilesInFolder(folder);

            CliProxy.CreateSolution(options.Output);
            foreach (var projectFile in projectFiles)
            {
                var currentDir = Directory.GetCurrentDirectory() + "\\";
                var relativePath = PathUtility.GetRelativePath(currentDir, projectFile);
                var dirName = Path.GetDirectoryName(relativePath);
                CliProxy.AddProjectToSolution(options.Output, dirName);
            }

            return 0;
        }
    }

    [Verb("folder", HelpText = "Cobbles together a solution from the projects in a folder.")]
    class FromFolderOptions
    {
        [Option('s', "source", Required = true,
            HelpText = "The path to the folder containing the target project files.")]
        public string Source { get; set; }

        [Option('o', "output", Required = true,
            HelpText = @"The filename to give the newly created solution.")]
        public string Output { get; set; }

        [Option('r', "examine-references", Default = false)]
        public bool ExamineReferences { get; set; }
    }

    [Verb("project", HelpText = "Cobbles together a solution, beginning with a particular project and including its dependencies.")]
    class FromProjectOptions
    {
        [Option('p', "project", Required = true,
            HelpText = "The name of the project or path to the project.")]
        public string TargetProject { get; set; }

        [Option('o', "output", Required = true,
            HelpText = @"The filename to give the newly created solution.")]
        public string Output { get; set; }
    }
}
