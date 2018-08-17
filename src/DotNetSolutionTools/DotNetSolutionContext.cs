using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Tools.Common;

namespace DotNetSolutionTools
{
    public class DotNetSolutionContext
    {
        private DotNetSolution Solution { get; }
        private ProjectCollection ProjectCollection { get; }

        public DotNetSolutionContext(DotNetSolution solution)
        {
            Solution = solution;
            ProjectCollection = new ProjectCollection();
            //_projects = Solution.BuildableProjects.Select(p => new DotNetProject(SlnProjectExtensions.GetAbsolutePathToProjectFile(p), Solution)).ToList();
        }

        private List<DotNetProject> _projects { get; }
        public IReadOnlyList<DotNetProject> Projects => _projects;
    }
}