using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using DotNetSolutionTools;

namespace DotNetComply
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ComplianceOptions>(args)
                .MapResult(
                    (ComplianceOptions o) => Comply(o),
                    (errs) => 1);
        }

        private static string FindProjectFileFullPath(string targetProject)
        {
            var pwd = Directory.GetCurrentDirectory();
            var extensionsToSearch = new List<string>();

            if (KnownProjectFileExtensions.IsKnownProjectFile(targetProject))
            {
                extensionsToSearch = new List<string>
                {
                    Path.GetExtension(targetProject).Replace(".", string.Empty)
                };

                if (Path.IsPathRooted(targetProject))
                {
                    return targetProject;
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

            foreach (var extension in extensionsToSearch)
            {
                var searchFilter = $"*.{extension}";
                var projectFiles = Directory.EnumerateFiles(pwd, searchFilter, SearchOption.AllDirectories).ToList();
                projectFiles = projectFiles.Where(f => Path.GetFileNameWithoutExtension(f) == targetProject).ToList();

                var match = projectFiles.SingleOrDefault();
                if (match != null)
                {
                    return match;
                }

            }

            return null;
        }

        private static int Comply(ComplianceOptions options)
        {
            var nugetRegex = new Regex(@"\<\s*PackageReference[^>]+\>");
            var projectPath = FindProjectFileFullPath(options.Project);
            var text = File.ReadAllText(projectPath);
            var nugetLines = nugetRegex.Matches(text);
            var includeRegex = new Regex(@"Include\s*=\s*""[^""]+""");
            var versionRegex = new Regex(@"Version\s*=\s*""[^""]+""");

            string Extract(string input)
            {
                var start = input.IndexOf("\"") + 1;
                var lastQuote = input.LastIndexOf("\"");
                return input.Substring(start, lastQuote - start);
            }

            var dependencies = new List<PackageReference>();
            for (var i = 0; i < nugetLines.Count; i++)
            {
                var line = nugetLines[i].ToString();
                var include = includeRegex.Match(line).ToString();
                include = Extract(include);
                var version = versionRegex.Match(line).ToString();
                version = Extract(version);

                dependencies.Add(new PackageReference(include, version));
            }
            return 0;
        }


    }

    class PackageReference
    {
        public PackageReference(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }

    [Verb("project", HelpText = "Extract NuGet dependencies for a project.")]
    class ComplianceOptions
    {
        [Option('p', "project", Required = true,
            HelpText = "The path to the folder or .csproj file for a project.")]
        public string Project { get; set; }

    }
}
