using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetSolutionTools
{
    public static class ProjectTypeSniffer
    {
        private static readonly Dictionary<string, FrameworkType> _frameworks = new Dictionary<string, FrameworkType>();
        private static readonly Dictionary<string, ProjectLanguage> _languages = new Dictionary<string, ProjectLanguage>();
        private static readonly Dictionary<string, ProjectClass> _classes = new Dictionary<string, ProjectClass>();
        private static readonly Dictionary<string, ProjectType> _cache = new Dictionary<string, ProjectType>();

        static ProjectTypeSniffer()
        {
            SetLanguages();
            SetFrameworks();
            SetClasses();
        }

        private static void SetClasses()
        {
            SetClass(
                BuildSet(
                    KnownProjectTypeGuids.SolutionFolderProject),
                ProjectClass.SolutionFolder);

            SetClass(
                BuildSet(
                    KnownProjectTypeGuids.NetStandardCSharpProject,
                    KnownProjectTypeGuids.FSharpProject,
                    KnownProjectTypeGuids.FullFrameworkCsharpProject,
                    KnownProjectTypeGuids.VisualBasicProject,
                    KnownProjectTypeGuids.VisualBasicProject),
                ProjectClass.Buildable);
        }

        private static void SetClass(IImmutableSet<string> ids, ProjectClass @class)
        {
            foreach (var id in ids)
            {
                _classes[id] = @class;
            }
        }

        private static void SetFrameworks()
        {
            SetFramework(
                BuildSet(
                    KnownProjectTypeGuids.NetStandardCSharpProject),
                FrameworkType.DotNetStandard);

            SetFramework(
                BuildSet(
                    KnownProjectTypeGuids.FSharpProject,
                    KnownProjectTypeGuids.FullFrameworkCsharpProject,
                    KnownProjectTypeGuids.VisualBasicProject),
                FrameworkType.DotNet);
        }

        private static void SetFramework(IImmutableSet<string> ids, FrameworkType framework)
        {
            foreach (var id in ids)
            {
                _frameworks[id] = framework;
            }
        }

        private static void SetLanguages()
        {
            SetLanguage(
                BuildSet(
                    KnownProjectTypeGuids.FullFrameworkCsharpProject,
                    KnownProjectTypeGuids.NetStandardCSharpProject),
                ProjectLanguage.CSharp);

            SetLanguage(
                BuildSet(
                    KnownProjectTypeGuids.FSharpProject),
                ProjectLanguage.FSharp);

            SetLanguage(
                BuildSet(
                    KnownProjectTypeGuids.VisualBasicProject),
                ProjectLanguage.VisualBasic);
        }

        private static void SetLanguage(IImmutableSet<string> ids, ProjectLanguage language)
        {
            foreach (var id in ids)
            {
                _languages[id] = language;
            }
        }

        public static ProjectType Sniff(string projectTypeId)
        {
            if (!_cache.TryGetValue(projectTypeId, out var cachedResult))
            {
                _languages.TryGetValue(projectTypeId, out var language);
                _frameworks.TryGetValue(projectTypeId, out var framework);
                _classes.TryGetValue(projectTypeId, out var @class);

                cachedResult = new ProjectType(framework, language, @class);
                _cache[projectTypeId] = cachedResult;
            }

            return cachedResult;
        }

        public static ProjectType SniffFromFilename(string absolutePathOrFilename)
        {
            var language = SniffLanguageFromExtension(absolutePathOrFilename);
            return new ProjectType(FrameworkType.Unknown, language, ProjectClass.Buildable);
        }

        public static ProjectLanguage SniffLanguageFromExtension(string absolutePathOrFilename)
        {
            var filename = Path.GetFileName(absolutePathOrFilename);
            var ext = Path.GetExtension(filename);

            switch (ext)
            {
                case ".csproj": return ProjectLanguage.CSharp;
                case ".fsproj": return ProjectLanguage.FSharp;
                case ".vbproj": return ProjectLanguage.VisualBasic;
                default: return ProjectLanguage.Unknown;
            }
        }

        private static IImmutableSet<string> BuildSet(params Guid[] ids)
        {
            var set = new HashSet<string>();
            foreach (var id in ids)
            {
                set.Add(id.ToString("N"));
                set.Add(id.ToString("D"));
                set.Add(id.ToString("B"));
                set.Add(id.ToString("P"));

                var current = set.ToList();
                foreach (var lowercase in current)
                {
                    set.Add(lowercase.ToUpperInvariant());
                }
            }

            return ImmutableHashSet<string>.Empty.Union(set);
        }

        public static ProjectType SniffFromProjectFile(string absolutePathToProjectFile)
        {
            var language = SniffLanguageFromExtension(absolutePathToProjectFile);
            var projectFileContents = File.ReadAllText(absolutePathToProjectFile);
            var startIndex = projectFileContents.IndexOf("TargetFramework");
            var endIndex = projectFileContents.IndexOf("/TargetFramework");
            var length = endIndex - startIndex;
            var content = projectFileContents.Substring(startIndex, length);
            startIndex = content.IndexOf(">");
            endIndex = content.IndexOf("<");
            length = endIndex - startIndex;
            content = content.Substring(startIndex, length);

            var standardRegex = new Regex(@"netstandard\d+\.\d+");
            var fullFrameworkRegex = new Regex(@"net\d+");
            var netCoreRegex = new Regex(@"netcoreapp\d+\.\d+");
            var hasStandard = standardRegex.IsMatch(content);
            var hasFullFramework = fullFrameworkRegex.IsMatch(content);
            var hasNetCore = netCoreRegex.IsMatch(content);

            var @class = ProjectClass.Buildable;
            var framework = FrameworkType.Unknown;
            if (hasStandard)
            {
                framework = FrameworkType.DotNetStandard;
            }
            else if (hasNetCore)
            {
                framework = FrameworkType.DotNetCore;
            }
            else if (hasFullFramework)
            {
                framework = FrameworkType.DotNet;
            }

            return new ProjectType(framework, language, @class);
        }
    }
}