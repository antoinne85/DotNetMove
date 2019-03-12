using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace DotNetSolutionTools
{
    [DebuggerDisplay("{HierarchyName}")]
    public class DotNetSolutionFolder : IDotNetSolutionFolder
    {
        private readonly IDotNetProjectInstance _projectInstance;

        public DotNetSolutionFolder(IDotNetProjectInstance projectInstance)
        {
            _projectInstance = projectInstance;
        }

        public string Id => _projectInstance.Id;

        public string FolderName => _projectInstance.ProjectName;

        public string HierarchyName => BuildHierarchyName();

        private string BuildHierarchyName()
        {
            if (!string.IsNullOrWhiteSpace(ParentFolder?.HierarchyName))
            {
                return $@"{ParentFolder.HierarchyName}\{FolderName}";
            }

            return FolderName;
        }

        public IEnumerable<IDotNetSolutionFolder> SubFolders => Solution.GetProjectsUnderSolutionFolder(this)
            .OfType<IDotNetSolutionFolder>();
        public IEnumerable<IDotNetProjectInstance> Projects => Solution.GetProjectsUnderSolutionFolder(this)
            .Where(p => p.ProjectType.Class == ProjectClass.Buildable)
            .ToList();


        public string ProjectName => _projectInstance.ProjectName;
        public string ProjectFileAbsolutePath => _projectInstance.ProjectFileAbsolutePath;
        public string ProjectFileRelativePathFromSolution => _projectInstance.ProjectFileRelativePathFromSolution;

        public IEnumerable<IDotNetProjectInstance> ReferencedProjects => _projectInstance.ReferencedProjects;
        public IDotNetSolution Solution => _projectInstance.Solution;

        public IEnumerable<IDotNetSolutionFolder> Flatten()
        {
            return new[] { this }.Union(SubFolders);
        }

        public void AddProjectToFolder(IDotNetProjectInstance project)
        {
            Solution.AddProjectToSolutionFolder(project, this);
        }

        public void RemoveProjectFromFolder(IDotNetProjectInstance project)
        {
            Solution.RemoveProjectFromSolutionFolder(project);
        }

        private IDotNetSolutionFolder _parentFolder;
        public IDotNetSolutionFolder ParentFolder => Solution.FindParentSolutionFolder(this);

        public ProjectType ProjectType => _projectInstance.ProjectType;

        public IEnumerable<string> AbsolutePathsToProjectReferences => ImmutableList<string>.Empty;

        public void UpdateProjectAbsolutePath(string newAbsolutePath)
        {
            throw new NotSupportedException("Solution folders do not have a path to update.");
        }

        public void UpdatePathToReference(string oldAbsolutePath, string newAbsolutePath)
        {
            throw new NotSupportedException("Solution folders cannot reference projects.");
        }

        public void Save()
        {
            throw new NotSupportedException("Solution folders canno be saved.");
        }
    }
}