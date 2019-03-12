using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetSolutionTools
{
    public static class KnownProjectFileExtensions
    {
        public static readonly string Csproj = "csproj";
        public static readonly string Fsproj = "fsproj";
        public static readonly string Vbproj = "vbproj";

        public static IReadOnlyList<string> All = new List<string>
        {
            Csproj,
            Fsproj,
            Vbproj
        };

        public static string AsFileSearchFilter(this string extension)
        {
            return $"*.{extension}";
        }

        public static bool IsKnownProjectFile(string filename)
        {
            filename = Path.GetFileName(filename);

            foreach (var extension in All)
            {
                if (filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class KnownProjectTypeGuids
    {
        public static readonly Guid FullFrameworkCsharpProject = new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
        public static readonly Guid NetStandardCSharpProject = new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556");
        public static readonly Guid SolutionFolderProject = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");
        public static readonly Guid FSharpProject = new Guid("F2A71F9B-5D33-465A-A702-920D77279786");
        public static readonly Guid VisualBasicProject = new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F");

        internal static Guid FromProjectType(ProjectType projectType)
        {
            if(projectType.Language == ProjectLanguage.CSharp)
            {
                if(projectType.Framework == FrameworkType.DotNetStandard)
                {
                    return NetStandardCSharpProject;
                }

                return FullFrameworkCsharpProject;
            }

            if(projectType.Language == ProjectLanguage.FSharp)
            {
                return FSharpProject;
            }

            if(projectType.Language == ProjectLanguage.VisualBasic)
            {
                return VisualBasicProject;
            }

            return SolutionFolderProject;
        }
    }
}