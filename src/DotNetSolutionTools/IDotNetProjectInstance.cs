using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Common;

namespace DotNetSolutionTools
{
    public interface IDotNetProjectInstance
    {
        string Id { get; }
        string ProjectName { get; }
        string ProjectFileAbsolutePath { get; }
        string ProjectFileRelativePathFromSolution { get; }
        IEnumerable<IDotNetProjectInstance> ReferencedProjects { get; }
        IDotNetSolution Solution { get; }
        ProjectType ProjectType { get; }
        void UpdateProjectAbsolutePath(string newAbsolutePath);
        void UpdatePathToReference(string oldAbsolutePath, string newAbsolutePath);
        void Save();
    }

    public class DotNetProjectInstance : IDotNetProjectInstance
    {
        private readonly SlnProject _slnProject;
        private readonly Lazy<MsbuildProject> _msbuildProject;

        public DotNetProjectInstance(IDotNetSolution solution, SlnProject slnProject)
        {
            _slnProject = slnProject;
            Solution = solution;
            ProjectType = ProjectTypeSniffer.Sniff(slnProject.TypeGuid);
            _msbuildProject = new Lazy<MsbuildProject>(LoadMsbuildProject);
        }

        private MsbuildProject LoadMsbuildProject()
        {
            var collection = new ProjectCollection();
            return MsbuildProject.FromFile(collection, ProjectFileAbsolutePath);
        }

        public string Id => _slnProject.Id;
        public string ProjectName => _slnProject.Name;
        public string ProjectFileAbsolutePath => _slnProject.GetAbsolutePathToProjectFile();
        public string ProjectFileRelativePathFromSolution => PathUtility.GetRelativePath(_slnProject.FilePath, ProjectFileAbsolutePath);

        private IEnumerable<IDotNetProjectInstance> GetReferencedProjects(string fromProjectLocation)
        {
            if (ProjectType.Class != ProjectClass.Buildable)
            {
                yield break;
            }

            //TODO: This won't work for projects that aren't in the solultion at the moment.
            var includes = _msbuildProject.Value.GetProjectToProjectReferences()
                .Select(r => r.Include)
                .Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            
            var absolutePathIncludes = includes
                .Select(relativePath => PathUtility.GetAbsolutePath(fromProjectLocation, relativePath))
                .ToList();

            var referencedProjects = absolutePathIncludes
                    .Select(absolutePath => Solution.GetProjectInstanceForProjectFile(absolutePath))
                    .ToList();

            foreach (var projectInstance in referencedProjects)
            {
                if (projectInstance != null)
                {
                    yield return projectInstance;
                }
            }
        }

        public IEnumerable<IDotNetProjectInstance> ReferencedProjects => GetReferencedProjects(ProjectFileAbsolutePath);
        public IDotNetSolution Solution { get; }
        public ProjectType ProjectType { get; }

        public void UpdateProjectAbsolutePath(string newAbsolutePath)
        {
            if (newAbsolutePath == ProjectFileAbsolutePath)
            {
                return;
            }

            var oldAbsolutePath = ProjectFileAbsolutePath;

            //Update the paths that other projects use to reference this one.
            //DO this first because once we change the path of this project
            //other projects will have a difficult time determining
            //if they reference it because comparisons are done by absolute path.
            var projectsThatReferenceThisOne = Solution.FindProjectsThatReference(this);
            foreach (var referencingProject in projectsThatReferenceThisOne)
            {
                referencingProject.UpdatePathToReference(oldAbsolutePath, newAbsolutePath);
            }

            //Update the path that the solution uses to find this project.
            //BEWARE: Changing the path the solution uses to find this project
            //also has an effect on the path that the MsbuildProject expects
            //to find its referenced projects at.
            //Unfortunately, it doesn't seem to update them correctly, so
            //we need to be mindful of that further down.
            //To do that, we'll go ahead and capture them here.
            //var currentReferencedProjects = ReferencedProjects.Select(r => (r,r.))ToList();
            Solution.UpdateProjectAbsolutePath(this, newAbsolutePath);

            //var msBuildProjectReferences = _msbuildProject.Value.GetProjectToProjectReferences()
            //    .Where(r => !string.IsNullOrWhiteSpace(r.Include))
            //    .ToDictionary(r => PathUtility.GetAbsolutePath(ProjectFileAbsolutePath, r.Include));

            //Update the paths to the projects that this project references.
            var referencedProjects = GetReferencedProjects(oldAbsolutePath);
            foreach (var referencedProject in referencedProjects)
            {
                var oldRelativePathToReference = BuildOldRelativePathToReference(oldAbsolutePath, referencedProject.ProjectFileAbsolutePath);
                var newRelativePathToReference = GetNewRelativePathToReference(oldAbsolutePath, newAbsolutePath, oldRelativePathToReference);
                UpdatePathToReferenceUsingRelativePaths(oldRelativePathToReference, newRelativePathToReference);
            }

            
        }

        private void UpdatePathToReferenceUsingRelativePaths(string oldRelativePathToReference, string newRelativePathToReference)
        {
            var msBuildProjectReferences = _msbuildProject.Value.GetProjectToProjectReferences()
                .Where(r => !string.IsNullOrWhiteSpace(r.Include))
                .ToDictionary(r => r.Include);

            if (msBuildProjectReferences.TryGetValue(oldRelativePathToReference, out var msBuildReference))
            {
                msBuildReference.Include = newRelativePathToReference;
            }
        }

        private string BuildOldRelativePathToReference(string oldAbsolutePath, string referencedProjectAbsolutePath)
        {
            return PathUtility.GetRelativePath(oldAbsolutePath, referencedProjectAbsolutePath);
        }

        public void UpdatePathToReference(string oldAbsolutePath, string newAbsolutePath)
        {
            var msBuildProjectReferences = _msbuildProject.Value.GetProjectToProjectReferences()
                .Where(r => !string.IsNullOrWhiteSpace(r.Include))
                .ToDictionary(r => PathUtility.GetAbsolutePath(ProjectFileAbsolutePath, r.Include));
     
            if (msBuildProjectReferences.TryGetValue(oldAbsolutePath, out var msBuildReference))
            {
                var relativePathToReferenceInNewLocation =
                    PathUtility.GetRelativePath(ProjectFileAbsolutePath, newAbsolutePath);
                msBuildReference.Include = relativePathToReferenceInNewLocation;
            }
        }

        private string GetNewRelativePathToReference(string oldProjectPath, string newProjectPath, string oldRelativePathToReference)
        {
            var oldAbsolutePath = PathUtility.GetAbsolutePath(oldProjectPath, oldRelativePathToReference);
            return PathUtility.GetRelativePath(newProjectPath, oldAbsolutePath);
        }

        public void Save()
        {
            _msbuildProject.Value.ProjectRootElement.Save();
        }
    }
}