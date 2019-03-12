using System;
using Microsoft.DotNet.Cli.Sln.Internal;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Tools.Common;

namespace DotNetSolutionTools
{
    public static class GuidExtensions
    {
        public static string ToId(this Guid guid)
        {
            return guid.ToString("B").ToUpperInvariant();
        }
    }
    public class DotNetSolution : IDotNetSolution
    {
        private SlnFile SlnFile { get; }

        public static DotNetSolution CreateIfNotExists(string solutionFileAbsolutePath)
        {
            if (File.Exists(solutionFileAbsolutePath))
            {
                throw new Exception("The file already exists.");
            }

            var file = new SlnFile();
            file.FullPath = solutionFileAbsolutePath;
            file.FormatVersion = "12.00";
            file.VisualStudioVersion = "15.0.27130.2036";
            file.MinimumVisualStudioVersion = "10.0.40219.1";
            file.Write();

            return new DotNetSolution(file);
        }

        public DotNetSolution(SlnFile file)
        {
            SlnFile = file;
            _allProjects = SlnFile.Projects.Select<SlnProject, IDotNetProjectInstance>(p =>
            {
                var projectInstance = new DotNetProjectInstance(this, p);

                if (projectInstance.ProjectType.Class == ProjectClass.SolutionFolder)
                {
                    return new DotNetSolutionFolder(projectInstance);
                }

                return projectInstance;
            }).ToList();
        }

        public DotNetSolution(string solutionFileAbsolutePath) : this(SlnFile.Read(solutionFileAbsolutePath))
        {
           
        }

        private readonly List<IDotNetProjectInstance> _allProjects;
        public IReadOnlyList<IDotNetProjectInstance> AllProjects => _allProjects;

        public IEnumerable<IDotNetProjectInstance> BuildableProjects =>
            AllProjects.Where(p => p.ProjectType.Class == ProjectClass.Buildable);

        public IEnumerable<IDotNetSolutionFolder> SolutionFolders =>
            AllProjects.OfType<IDotNetSolutionFolder>().ToList();

        public void RemoveProjectFromSolutionFolder(IDotNetProjectInstance project)
        {
            SlnFile.GetSolutionFolderSection().Properties.Remove(project.Id);
        }

        public void AddProjectToSolutionFolder(IDotNetProjectInstance project, IDotNetSolutionFolder solutionFolder)
        {
            if (project.Id == solutionFolder.Id)
            {
                throw new InvalidOperationException("A solution folder cannot be self-referencing.");
            }

            RemoveProjectFromSolutionFolder(project);
            SlnFile.GetSolutionFolderSection().Properties[project.Id] = solutionFolder.Id;
        }

        public IDotNetProjectInstance GetProjectInstanceForProjectFile(string absolutePath)
        {
            var matches = AllProjects.Where(p => string.Compare(p.ProjectFileAbsolutePath, absolutePath, StringComparison.OrdinalIgnoreCase) == 0);
            return matches.SingleOrDefault();
        }

        public IEnumerable<IDotNetProjectInstance> GetProjectsUnderSolutionFolder(IDotNetSolutionFolder solutionFolder)
        {
            var solutionFolderSection = SlnFile.GetSolutionFolderSection();
            var parentId = solutionFolder.Id;
            var childIds = solutionFolderSection.Properties.Where(p => p.Value == parentId)
                .Select(p => p.Key)
                .ToImmutableHashSet();
            return AllProjects.Where(p => childIds.Contains(p.Id));

        }

        public IDotNetSolutionFolder FindParentSolutionFolder(IDotNetProjectInstance projectInstance)
        {
            if (SlnFile.GetSolutionFolderSection().Properties.TryGetValue(projectInstance.Id, out var parentProjectId))
            {
                var parentProject = FindProjectById(parentProjectId);
                if (parentProject is IDotNetSolutionFolder)
                {
                    return parentProject as IDotNetSolutionFolder;
                }
            }

            return null;
        }

        public IDotNetProjectInstance FindProjectById(string projectId)
        {
            return AllProjects.SingleOrDefault(p => p.Id == projectId);
        }

        public void UpdateProjectAbsolutePath(IDotNetProjectInstance projectInstance, string absolutePath)
        {
            var matchingSlnProject = SlnFile.Projects.SingleOrDefault(p => p.Id == projectInstance.Id);

            if (matchingSlnProject != null)
            {
                var relativePathFromSolution = PathUtility.GetRelativePath(SlnFile.FullPath, absolutePath);
                matchingSlnProject.FilePath = relativePathFromSolution;
            }
        }

        public IEnumerable<IDotNetProjectInstance> FindProjectsThatReference(IDotNetProjectInstance projectInstance)
        {
            //var absolutePathToTargetProject = projectInstance.ProjectFileAbsolutePath;
            foreach (var project in AllProjects)
            {
                if (project.Id == projectInstance.Id)
                {
                    continue;
                }

                if (project.ProjectName == "PureCars.CosmosDb")
                {
                    Console.Write("X");
                }

                var matchingReference = project.ReferencedProjects.SingleOrDefault(p => p.Id == projectInstance.Id);
                if (matchingReference != null)
                {
                    yield return project;
                }
            }
        }

        public void Save()
        {
            SlnFile.Write();

            foreach (var project in BuildableProjects)
            {
                project.Save();
            }
        }

        public IDotNetSolutionFolder GetOrCreateSolutionFolder(string fullPath)
        {
            var existingFolder = SolutionFolders.FirstOrDefault(f => f.HierarchyName == fullPath);
            if (existingFolder != null)
            {
                return existingFolder;
            }

            var pathSegments = fullPath.Split('\\');
            IDotNetSolutionFolder currentFolder = null;

            foreach (var segment in pathSegments)
            {
                if (currentFolder == null)
                {
                    //Try to find the first folder in the path.
                    //If we can't find it, we should create it.
                    //If we do find it, we can start using its navigation properties to fill out the rest of the tree.
                    currentFolder = SolutionFolders.SingleOrDefault(f => f.HierarchyName == segment);

                    if (currentFolder == null)
                    {
                        //The folder didn't exist. 
                        //We should create one at the root.
                        var newFolder = CreateSolutionFolderAtRoot(segment);
                        currentFolder = newFolder;
                    }
                }
                else
                {
                    var matchingChildFolder = currentFolder.SubFolders.SingleOrDefault(f => f.FolderName == segment);
                    if (matchingChildFolder == null)
                    {
                        //This folder did not exist.
                        //We should create it and add it under the current folder.
                        var newFolder = CreateSolutionFolderAtRoot(segment);
                        currentFolder.AddProjectToFolder(newFolder);
                        currentFolder = newFolder;
                    }

                }
            }

            return currentFolder;
        }

        private IDotNetSolutionFolder CreateSolutionFolderAtRoot(string folderName)
        {
            var newProject = new SlnProject
            {
                Id = Guid.NewGuid().ToId(),
                TypeGuid = KnownProjectTypeGuids.SolutionFolderProject.ToId(),
                Name = folderName,
                FilePath = folderName
            };
            SlnFile.Projects.Add(newProject);

            var projectInstance = new DotNetProjectInstance(this, newProject);
            var solutionFolder = new DotNetSolutionFolder(projectInstance);
            _allProjects.Add(solutionFolder);

            return solutionFolder;
        }

        public IDotNetProjectInstance AddExistingProject(string absolutePathToProjectFile)
        {
            var projectType = ProjectTypeSniffer.SniffFromProjectFile(absolutePathToProjectFile);
            var typeGuid = KnownProjectTypeGuids.FromProjectType(projectType);
            var newProject = new SlnProject
            {
                Id = Guid.NewGuid().ToId(),
                TypeGuid = typeGuid.ToId(),
                Name = Path.GetFileNameWithoutExtension(absolutePathToProjectFile),
                FilePath = absolutePathToProjectFile
            };
            SlnFile.Projects.Add(newProject);
            var projectInstance = new DotNetProjectInstance(this, newProject);
            _allProjects.Add(projectInstance);
            return projectInstance;
        }
    }
}