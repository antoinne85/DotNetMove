using System.Collections.Generic;

namespace DotNetSolutionTools
{
    public interface IDotNetSolution
    {
        IReadOnlyList<IDotNetProjectInstance> AllProjects { get; }
        IEnumerable<IDotNetProjectInstance> BuildableProjects { get; }
        IEnumerable<IDotNetSolutionFolder> SolutionFolders { get; }
        void RemoveProjectFromSolutionFolder(IDotNetProjectInstance project);
        void AddProjectToSolutionFolder(IDotNetProjectInstance project, IDotNetSolutionFolder solutionFolder);
        IDotNetProjectInstance GetProjectInstanceForProjectFile(string absolutePath);
        IEnumerable<IDotNetProjectInstance> GetProjectsUnderSolutionFolder(IDotNetSolutionFolder dotNetSolutionFolder);
        IDotNetSolutionFolder FindParentSolutionFolder(IDotNetProjectInstance projectInstance);
        IDotNetProjectInstance FindProjectById(string projectId);
        void UpdateProjectAbsolutePath(IDotNetProjectInstance projectInstance, string absolutePath);
        IEnumerable<IDotNetProjectInstance> FindProjectsThatReference(IDotNetProjectInstance projectInstance);
        void Save();
        IDotNetProjectInstance AddExistingProject(string absolutePathToProjectFile);
    }
}