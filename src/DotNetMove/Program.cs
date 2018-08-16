using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Common;

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
            //var solutionFile = @"C:\Projects\Obelisk-stockphotos\Nugets\Kubical.sln";
            //var outputFile = @"C:\Projects\Obelisk-stockphotos\Nugets\Kubical-new.sln";
            //var solution = SlnFile.Read(solutionFile);



            //return 0;
        }

        private static int MoveInSolution(SolutionMoveOptions options)
        {
            var solutions = FindSolutions(options.SolutionPath, options.FindSolutions);
            solutions = FilterSolutions(solutions, options.Project);
            foreach (var solution in solutions)
            {
                var targetProject = FindProjectInSolution(solution, options.Project);
                MoveProjectIntoSolutionFolder(solution, targetProject, options.Destination);
                solution.Write(solution.FullPath);
            }
            return -1;
        }

        private static SlnProject FindProjectInSolution(SlnFile solution, string targetProject)
        {
            bool CheckByPath(SlnProject proj) => Path.GetFileName(proj.GetAbsolutePathToProjectFile()) == targetProject;
            bool CheckByName(SlnProject proj) => proj.Name == targetProject;

            var func = targetProject.EndsWith(".csproj") ? CheckByPath : (Func<SlnProject, bool>)CheckByName;
            return solution.Projects.FirstOrDefault(func);
        }

        private static void MoveProjectIntoSolutionFolder(SlnFile solution, SlnProject targetProject, string solutionFolder)
        {
            var solutionFolderId = GetOrCreateSolutionFolder(solution, solutionFolder);
            var solutionFolderSection = solution.GetSolutionFolderSection();
            solutionFolderSection.Properties[targetProject.Id] = solutionFolderId;
        }

        private static string GetOrCreateSolutionFolder(SlnFile solution, string solutionFolder)
        {
            var existingFolders = solution.GetSolutionFolderPaths();
            if (existingFolders.TryGetValue(solutionFolder, out var solutionFolderId))
            {
                return solutionFolderId;
            }

            var targetSolutionFolderHierarchy = solutionFolder.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            var testFolder = "";

            var solutionFolderSection = solution.GetSolutionFolderSection();

            foreach (var folder in targetSolutionFolderHierarchy)
            {
                testFolder += folder;
                if (!existingFolders.TryGetValue(testFolder, out solutionFolderId))
                {
                    var newSolutionFolderId = Guid.NewGuid().ToString("B").ToUpper();
                    var solutionFolderProject = new SlnProject
                    {
                        Name = testFolder,
                        Id = solutionFolderId,
                        TypeGuid = ProjectTypeGuids.SolutionFolderGuid
                    };
                    solution.Projects.Add(solutionFolderProject);
                    solutionFolderSection.Properties[newSolutionFolderId] = solutionFolderId;
                    solutionFolderId = newSolutionFolderId;
                }

                testFolder += "\\";
            }

            return solutionFolderId;
        }

        private static IReadOnlyList<SlnFile> FilterSolutions(IReadOnlyList<SlnFile> solutions, string targetProject)
        {
            return solutions.Where(sln => FindProjectInSolution(sln, targetProject) != null).ToList();
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
            var csprojFullPath = FindCsprojFullPath(options.Project);

            if (string.IsNullOrWhiteSpace(csprojFullPath))
            {
                return -1;
            }

            var directoryContainingCsproj = Path.GetDirectoryName(csprojFullPath);
            var lastFolderInDirectoryContainingCsproj =
                directoryContainingCsproj.Split(Path.DirectorySeparatorChar).Last();
            var absoluteDirectoryToMoveTo =
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), options.Destination, lastFolderInDirectoryContainingCsproj));
            var newCsprojFullPath = Path.Combine(absoluteDirectoryToMoveTo, Path.GetFileName(csprojFullPath));

            var solutions = FindSolutions(options.SolutionPath, options.FindSolutions);
            solutions = FilterSolutions(solutions, options.Project);
            foreach (var solution in solutions)
            {
                foreach (var slnProject in solution.Projects)
                {
                    var projectName = slnProject.Name;
                    if (projectName == "PureCars.Core.Logging-Standard")
                    {
                        Console.WriteLine("X");
                    }
                    var projectCollection = new ProjectCollection();
                    if (slnProject.TypeGuid != ProjectTypeGuids.CSharpProjectTypeGuid && slnProject.TypeGuid != "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}")
                    {
                        continue;
                    }
                    var msbuildProject = MsbuildProject.FromFile(projectCollection, slnProject.GetAbsolutePathToProjectFile());
                    var projectReferences = msbuildProject.GetProjectToProjectReferences().ToList();
                    var absolutePathToThisProject = slnProject.GetAbsolutePathToProjectFile();

                    if (string.Compare(absolutePathToThisProject, csprojFullPath, CultureInfo.InvariantCulture, CompareOptions.OrdinalIgnoreCase) == 0)
                    {
                        //This is our target project.
                        //We're about to move it, so the project references it has will need to be updated.
                        foreach (var projectReference in projectReferences)
                        {
                            var relativePathFromThisProject =
                                Path.Combine(Path.GetDirectoryName(absolutePathToThisProject), projectReference.Include);
                            var absolutePathFromCurrentLocation = Path.GetFullPath(relativePathFromThisProject);

                            var newRelativePath =
                                PathUtility.GetRelativePath(newCsprojFullPath, absolutePathFromCurrentLocation);
                            projectReference.Include = newRelativePath;
                        }

                        msbuildProject.ProjectRootElement.Save();
                    }
                    else
                    {
                        //This is some other project.
                        //It might reference our target project and so need updating.
                        var referenceToTargetProject = projectReferences.SingleOrDefault(r =>
                        {
                            var relativePathFromThisProject =
                                Path.Combine(Path.GetDirectoryName(absolutePathToThisProject), r.Include);
                            var absolutePathToReferencedProject = Path.GetFullPath(relativePathFromThisProject);
                            //TODO: Other platforms may have case-sensitive file systems. Does that affect how .NET works with them?
                            return string.Compare(absolutePathToReferencedProject, csprojFullPath,
                                       CultureInfo.CurrentCulture, CompareOptions.OrdinalIgnoreCase) == 0;
                        });

                        if (referenceToTargetProject != null)
                        {
                            var relativePathToNewCsprojLocation =
                                PathUtility.GetRelativePath(absolutePathToThisProject, newCsprojFullPath);
                            referenceToTargetProject.Include = relativePathToNewCsprojLocation;
                        }

                        msbuildProject.ProjectRootElement.Save();
                    }
                }

                var targetProject = FindProjectInSolution(solution, options.Project);
                targetProject.FilePath = solution.GetRelativePathToFile(newCsprojFullPath);

                Directory.CreateDirectory(absoluteDirectoryToMoveTo);
                DeleteDirectoryIfEmpty(absoluteDirectoryToMoveTo);
                Directory.Move(directoryContainingCsproj, absoluteDirectoryToMoveTo);

                solution.Write(solution.FullPath);
            }
            return -1;
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

        private static void AddRenames(IPipeline pipeline, DiskMoveOptions options)
        {
            if (Path.GetFileNameWithoutExtension(options.Project) == ".csproj")
            {

            }

            if (Directory.Exists(options.Project))
            {

            }
        }
    }

    internal class FilterSolutionsAction : IAction
    {
        private readonly string _targetProject;

        public FilterSolutionsAction(string targetProject)
        {
            _targetProject = targetProject;
        }

        public IEnumerable<string> Preverify(IPipeline pipeline)
        {
            return new string[] { };
        }

        public IEnumerable<string> Execute(IPipeline pipeline)
        {
            var solutions = pipeline.SolutionsToModify.ToList();
            Func<SlnFile, bool> CheckByPath = (sln) =>
            {
                return sln.Projects.Any(p => Path.GetFileName(p.GetAbsolutePathToProjectFile()) == _targetProject);
            };

            Func<SlnFile, bool> CheckByName = (sln) => { return sln.Projects.Any(p => p.Name == _targetProject); };

            var func = _targetProject.EndsWith(".csproj") ? CheckByPath : CheckByName;
            foreach (var sln in solutions)
            {
                if (!func(sln))
                {
                    pipeline.UnmarkSolutionForModification(sln);
                }
            }

            return new string[] { };
        }
    }

    internal class PlaceProjectInFolderAction : IAction
    {
        private readonly Pipeline _pipeline;

        public PlaceProjectInFolderAction(Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public IEnumerable<string> Preverify(IPipeline pipeline)
        {
            return new string[0];
        }

        public IEnumerable<string> Execute(IPipeline pipeline)
        {
            foreach (var sln in pipeline.SolutionsToModify)
            {
                var absolutePath = pipeline.PathToCsProjBeingMoved;
                foreach (var project in sln.Projects)
                {
                    if (project.GetAbsolutePathToProjectFile() == absolutePath)
                    {
                        Console.WriteLine("Gotcha!");
                    }
                }
            }

            return new string[] { };
        }
    }

    internal class CreateSolutionFolderIfNotExistsAction : IAction
    {
        private readonly IReadOnlyList<string> _expectedFolders;
        public CreateSolutionFolderIfNotExistsAction(string destination)
        {
            _expectedFolders = destination.Split(new[] { '\\', '/' }).ToList();
        }

        public IEnumerable<string> Preverify(IPipeline pipeline)
        {
            return new string[0];
        }

        public IEnumerable<string> Execute(IPipeline pipeline)
        {
            foreach (var sln in pipeline.SolutionsToModify)
            {
                sln.AddSolutionFolders(new SlnProject
                {
                    FilePath = "MyFolder/MySubFolder"
                });
            }
            throw new NotImplementedException();
            //foreach (var sln in pipeline.SolutionsToModify)
            //{
            //    sln.ProjectsInOrder
            //}
        }
    }

    class SmartSolutionFindAction : IAction
    {
        private readonly string _solutionPath;
        private readonly bool _findSolutions;

        public SmartSolutionFindAction(string solutionPath, bool findSolutions)
        {
            _solutionPath = solutionPath;
            _findSolutions = findSolutions;
        }

        public IEnumerable<string> Preverify(IPipeline pipeline)
        {
            return new string[] { };
        }

        public IEnumerable<string> Execute(IPipeline pipeline)
        {
            if (!string.IsNullOrWhiteSpace(_solutionPath))
            {
                pipeline.AddAfterCurrent(new ReadSolutionAction(_solutionPath));
            }
            else if (_findSolutions)
            {
                pipeline.AddAfterCurrent(new FindSolutionsAction());
            }

            return new string[] { };
        }
    }

    internal class FindProjectToMoveAction : IAction
    {
        private readonly string _projectInput;
        private bool _isCsprojFile = false;
        private string _locatedCsprojFile = string.Empty;

        public FindProjectToMoveAction(string projectInput)
        {
            _projectInput = projectInput;
        }

        public IEnumerable<string> Preverify(IPipeline pipeline)
        {
            if (Path.GetExtension(_projectInput) == ".csproj")
            {
                if (Path.IsPathRooted(_projectInput) && File.Exists(_projectInput))
                {
                    _locatedCsprojFile = _projectInput;
                }
                else
                {
                    return new[] { $"No .csproj file could be found with the provided path: {_projectInput}" };
                }
            }
            else
            {
                var findResult = FindCsprojInDirectory(pipeline);
                if (!findResult.found)
                {
                    return new[] { $"No .csproj file could be found with the provided project name: {_projectInput}" };
                }

                _locatedCsprojFile = findResult.path;
            }

            return new string[] { };
        }

        private (bool found, string path) FindCsprojInDirectory(IPipeline pipeline)
        {
            var targetDirectory = _projectInput;
            if (!Path.IsPathRooted(targetDirectory))
            {
                targetDirectory = Path.Combine(pipeline.OriginalWorkingDirectory, targetDirectory);
            }

            if (!Directory.Exists(targetDirectory))
            {
                return (false, string.Empty);
            }

            var projectFolderName = targetDirectory.Split(Path.DirectorySeparatorChar).Last();
            var expectedProjectName = projectFolderName + ".csproj";
            if (!File.Exists(expectedProjectName))
            {
                return (false, string.Empty);
            }

            return (true, Path.Combine(targetDirectory, expectedProjectName));
        }

        public IEnumerable<string> Execute(IPipeline pipeline)
        {
            pipeline.SetTargetCsprojToMove(_locatedCsprojFile);

            //TODO: Remove
            if (_isCsprojFile)
            {
                Console.WriteLine("X");
            }

            return new string[] { };

        }
    }

    internal class FindSolutionsAction : IAction
    {
        private IReadOnlyList<string> _solutionFiles;
        public IEnumerable<string> Preverify(IPipeline pipeline)
        {
            var files = Directory.EnumerateFiles(pipeline.OriginalWorkingDirectory, "*.sln",
                SearchOption.AllDirectories).ToList();

            if (files.Count == 0)
            {
                return new[]
                    {$"No solution files were found in the working directory: {pipeline.OriginalWorkingDirectory}"};
            }

            _solutionFiles = files;

            return new string[] { };
        }

        public IEnumerable<string> Execute(IPipeline pipeline)
        {
            foreach (var file in _solutionFiles)
            {
                pipeline.AddAfterCurrent(new ReadSolutionAction(file));
            }

            return new string[] { };
        }
    }

    class Pipeline : IPipeline
    {
        public Pipeline(string workingDirectory)
        {
            OriginalWorkingDirectory = workingDirectory;
        }

        public IReadOnlyList<SlnFile> SolutionsToModify => _solutionsToModify;
        private List<SlnFile> _solutionsToModify = new List<SlnFile>();
        public void MarkSolutionForModification(SlnFile sln)
        {
            _solutionsToModify.Add(sln);

        }

        public void UnmarkSolutionForModification(SlnFile sln)
        {
            _solutionsToModify.Remove(sln);
        }

        public string PathToCsProjBeingMoved { get; private set; }
        public void SetTargetCsprojToMove(string csprojPath)
        {
            PathToCsProjBeingMoved = csprojPath;
        }

        private readonly IList<IAction> _actions = new List<IAction>();
        private int _currentIndex = 0;

        public IEnumerable<string> Start()
        {
            var errors = new List<string>();
            for (var i = 0; i < _actions.Count; i++)
            {
                _currentIndex = i;
                var actionErrors = _actions[i].Preverify(this).ToList();
                if (actionErrors.Count > 0)
                {
                    errors.AddRange(actionErrors);
                }
            }

            if (errors.Count > 0)
            {
                return errors;
            }

            for (var i = 0; i < _actions.Count; i++)
            {
                _currentIndex = i;
                _actions[i].Execute(this);
            }

            return new string[0];
        }

        public void AddAfterCurrent(IAction action)
        {
            _actions.Insert(_currentIndex + 1, action);
        }

        public void AddToEnd(IAction action)
        {
            _actions.Add(action);
        }

        public string OriginalWorkingDirectory { get; }
    }

    interface IAction
    {
        IEnumerable<string> Preverify(IPipeline pipeline);
        IEnumerable<string> Execute(IPipeline pipeline);
    }

    interface IPipeline : IMoveState
    {
        IEnumerable<string> Start();
        void AddAfterCurrent(IAction action);
        void AddToEnd(IAction action);
        string OriginalWorkingDirectory { get; }
    }

    interface IMoveState
    {
        IReadOnlyList<SlnFile> SolutionsToModify { get; }

        void MarkSolutionForModification(SlnFile sln);
        void UnmarkSolutionForModification(SlnFile sln);

        string PathToCsProjBeingMoved { get; }
        void SetTargetCsprojToMove(string csprojPath);
    }

    class ReadSolutionAction : IAction
    {
        private readonly string _solutionToRead;

        public ReadSolutionAction(string solutionToRead)
        {
            _solutionToRead = solutionToRead;
        }

        public IEnumerable<string> Preverify(IPipeline pipeline)
        {
            if (!File.Exists(_solutionToRead))
            {
                return new[] { $"The solution file did not exist: {_solutionToRead}" };
            }

            return new string[] { };
        }

        public IEnumerable<string> Execute(IPipeline pipeline)
        {
            try
            {
                var sln = SlnFile.Read(_solutionToRead);
                pipeline.MarkSolutionForModification(sln);
            }
            catch
            {
                return new[] { $"Unable to parse the solution file: {_solutionToRead}" };
            }

            return new string[] { };
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

    [Verb("disk", HelpText = "Moves a project on disk from one physical location to another.")]
    class DiskMoveOptions : OptionsBase
    {
        [Option('p', "project", Required = true,
            HelpText = "The path to the folder or .csproj file for a project.")]
        public string Project { get; set; }

        [Option('d', "destination", Required = true,
            HelpText = @"The path on disk that the project should be moved to.")]
        public string Destination { get; set; }
    }

    [Verb("solution", HelpText = "Moves a project into a solution folder.")]
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
