using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DotNetSolutionTools
{
    public static class DiskUtilities
    {
        public static IReadOnlyList<string> FindProjectFilesInFolder(string absolutePathOfSearchRoot)
        {
            var extensionsToSearch = KnownProjectFileExtensions.All.ToList();
            var files = new List<string>();
            foreach (var extension in extensionsToSearch)
            {
                var searchFilter = $"*.{extension}";
                var projectFiles = Directory.EnumerateFiles(absolutePathOfSearchRoot, searchFilter, SearchOption.AllDirectories).ToList();

                files.AddRange(projectFiles);
            }

            return files;
        }

        public static IReadOnlyList<string> FindFullPathToProjects(string absolutePathOfSearchRoot, string targetProject)
        {
            var pwd = absolutePathOfSearchRoot;
            var extensionsToSearch = new List<string>();

            if (KnownProjectFileExtensions.IsKnownProjectFile(targetProject))
            {
                extensionsToSearch = new List<string>
                {
                    Path.GetExtension(targetProject).Replace(".", string.Empty)
                };

                if (Path.IsPathRooted(targetProject))
                {
                    return new List<string> { targetProject };
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

            var matches = new List<string>();
            foreach (var extension in extensionsToSearch)
            {
                var searchFilter = $"*.{extension}";
                var projectFiles = Directory.EnumerateFiles(pwd, searchFilter, SearchOption.AllDirectories).ToList();
                projectFiles = projectFiles.Where(f => Path.GetFileNameWithoutExtension(f) == targetProject).ToList();

                matches.AddRange(projectFiles);

            }

            return matches;
        }
    }
}
