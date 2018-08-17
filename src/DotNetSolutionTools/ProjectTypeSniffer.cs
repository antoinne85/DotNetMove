using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
    }
}