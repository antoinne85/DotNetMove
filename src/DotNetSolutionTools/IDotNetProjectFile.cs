using System.Collections.Generic;

namespace DotNetSolutionTools
{
    public interface IDotNetProjectFile
    {
        string ProjectFileAbsolutePath { get; }
        IReadOnlyList<IDotNetProjectInstance> ProjectInstances { get; }
    }
}