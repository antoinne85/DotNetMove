using System.Collections.Generic;

namespace DotNetSolutionTools
{
    public interface IDotNetSolutionFolder : IDotNetProjectInstance
    {
        string Id { get; }
        string FolderName { get; }
        string HierarchyName { get; }
        IEnumerable<IDotNetSolutionFolder> SubFolders { get; }
        IEnumerable<IDotNetProjectInstance> Projects { get; }
        IDotNetSolution Solution { get; }
        IEnumerable<IDotNetSolutionFolder> Flatten();
        void AddProjectToFolder(IDotNetProjectInstance project);
        void RemoveProjectFromFolder(IDotNetProjectInstance project);
        IDotNetSolutionFolder ParentFolder { get; }
    }
}