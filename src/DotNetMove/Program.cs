using CommandLine;
using DotNetSolutionTools;
using Microsoft.DotNet.Cli.Sln.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetMove
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<DiskMoveOptions, SolutionMoveOptions>(args)
                .MapResult(
                    (DiskMoveOptions o) => MoveOnDisk(o),
                    (SolutionMoveOptions o) => MoveInSolution(o),
                    (errs) => 1);
        }

        private static int MoveInSolution(SolutionMoveOptions options)
        {
            var solutions = FindSolutions(options.SolutionPath, options.FindSolutions);
            var newSolutions = solutions.Select(s => new DotNetSolution(s.FullPath)).ToList();

            foreach (var solution in newSolutions)
            {
                var targetProject = FindProject(solution, options.Project);

                if (targetProject == null)
                {
                    continue;
                }

                var targetSolutionFolder = solution.GetOrCreateSolutionFolder(options.Destination);
                targetSolutionFolder.AddProjectToFolder(targetProject);

                solution.Save();
            }

            return 0;
        }

        private static IDotNetProjectInstance FindProject(IDotNetSolution solution, string targetProject)
        {
            bool CheckByPath(IDotNetProjectInstance proj) => Path.GetFileName(proj.ProjectFileAbsolutePath) == targetProject;
            bool CheckByName(IDotNetProjectInstance proj) => proj.ProjectName == targetProject;

            Func<IDotNetProjectInstance, bool> func = CheckByName;

            if (KnownProjectFileExtensions.IsKnownProjectFile(targetProject))
            {
                func = CheckByPath;
            }

            return solution.AllProjects.SingleOrDefault(func);
        }

        private static IReadOnlyList<SlnFile> FindSolutions(string solutionPath, bool findSolutions)
        {
            if (!string.IsNullOrWhiteSpace(solutionPath))
            {
                if (File.Exists(solutionPath))
                {
                    return new List<SlnFile> { SlnFile.Read(solutionPath) };
                }
                return new List<SlnFile>();
            }
            else if (findSolutions)
            {
                var files = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.sln",
                    SearchOption.AllDirectories).ToList();
                return files.Select(SlnFile.Read).ToList();
            }

            return new List<SlnFile>();
        }

        private static int MoveOnDisk(DiskMoveOptions options)
        {
            var projectFileFullPath = FindProjectFileFullPath(options.Project);
            var directoryContainingProjectFile = Path.GetDirectoryName(projectFileFullPath);
            var lastFolderInDirectoryContainingProjectFile =
                directoryContainingProjectFile.Split(Path.DirectorySeparatorChar).Last();
            var absoluteDirectoryToMoveTo =
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), options.Destination, lastFolderInDirectoryContainingProjectFile));
            var newProjectFileFullPath = Path.Combine(absoluteDirectoryToMoveTo, Path.GetFileName(projectFileFullPath));

            if (string.IsNullOrWhiteSpace(projectFileFullPath))
            {
                return -1;
            }

            var solutions = FindSolutions(options.SolutionPath, options.FindSolutions);

            var newSolutions = solutions.Select(s => new DotNetSolution(s.FullPath)).ToList();

            foreach (var solution in newSolutions)
            {
                var targetProject = FindProject(solution, options.Project);

                if (targetProject == null)
                {
                    continue;
                }

                targetProject.UpdateProjectAbsolutePath(newProjectFileFullPath);

                solution.Save();
            }

            Directory.CreateDirectory(absoluteDirectoryToMoveTo);
            DeleteDirectoryIfEmpty(absoluteDirectoryToMoveTo);
            Directory.Move(directoryContainingProjectFile, absoluteDirectoryToMoveTo);

            return 0;
        }

        private static void DeleteDirectoryIfEmpty(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            var hasFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Any();
            if (!hasFiles)
            {
                Directory.Delete(path, true);
            }
        }

        private static string FindProjectFileFullPath(string targetProject)
        {
            var pwd = Directory.GetCurrentDirectory();
            var extensionsToSearch = new List<string>();

            if (KnownProjectFileExtensions.IsKnownProjectFile(targetProject))
            {
                extensionsToSearch = new List<string>
                {
                    Path.GetExtension(targetProject).Replace(".", string.Empty)
                };

                if (Path.IsPathRooted(targetProject))
                {
                    return targetProject;
                }
                else
                {
                    targetProject = Path.GetFileNameWithoutExtension(targetProject);
                }
            }
            else
            {
                extensionsToSearch = KnownProjectFileExtensions.All.ToList();
            }

            foreach (var extension in extensionsToSearch)
            {
                var searchFilter = $"*.{extension}";
                var projectFiles = Directory.EnumerateFiles(pwd, searchFilter, SearchOption.AllDirectories).ToList();
                projectFiles = projectFiles.Where(f => Path.GetFileNameWithoutExtension(f) == targetProject).ToList();

                var match = projectFiles.SingleOrDefault();
                if (match != null)
                {
                    return match;
                }

            }

            return null;
        }
    }

    class OptionsBase
    {
        [Option('s', "solution", Required = false,
            HelpText = "The path to the solution file that should be updated as part of the move.")]
        public string SolutionPath { get; set; }

        [Option('f', "find-solutions", Required = false, Default = true,
            HelpText = "Toggles solution search, which will locate solution files referencing the moved project and update them.")]
        public bool FindSolutions { get; set; }
    }

    [Verb("disk", HelpText = "Moves a project on disk from one physical location to another. See https://github.com/antoinne85/DotNetMove for more details and example usages.")]
    class DiskMoveOptions : OptionsBase
    {
        [Option('p', "project", Required = true,
            HelpText = "The path to the folder or .csproj file for a project.")]
        public string Project { get; set; }

        [Option('d', "destination", Required = true,
            HelpText = @"The path on disk that the project should be moved to.")]
        public string Destination { get; set; }
    }

    [Verb("solution", HelpText = "Moves a project into a solution folder. See https://github.com/antoinne85/DotNetMove for more details and example usages.")]
    class SolutionMoveOptions : OptionsBase
    {
        [Option('p', "project", Required = true,
            HelpText = "The name of the project to move.")]
        public string Project { get; set; }

        [Option('d', "destination", Required = true,
            HelpText = @"The solution folder in which to place the project.")]
        public string Destination { get; set; }
    }

    enum MoveType
    {
        Unknown = 0,
        Disk = 1,
        Solution = 2
    }
}
