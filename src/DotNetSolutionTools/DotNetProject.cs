using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Common;

namespace DotNetSolutionTools
{
    public class DotNetProject
    {
        private MsbuildProject _project;
        private DotNetSolution _solution;

        public string ProjectFileAbsolutePath { get; }
    
        internal DotNetProject(string projectFileAbsolutePath, DotNetSolution solution)
        {
            _solution = solution;
            ProjectFileAbsolutePath = projectFileAbsolutePath;
        }

        public string GetRelativePath(string relativeTo)
        {
            return PathUtility.GetRelativePath(relativeTo, ProjectFileAbsolutePath);
        }
    }
}