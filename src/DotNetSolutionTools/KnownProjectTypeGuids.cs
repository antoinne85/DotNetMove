using System;

namespace DotNetSolutionTools
{
    public static class KnownProjectTypeGuids
    {
        public static readonly Guid FullFrameworkCsharpProject = new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
        public static readonly Guid NetStandardCSharpProject = new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556");
        public static readonly Guid SolutionFolderProject = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");
        public static readonly Guid FSharpProject = new Guid("F2A71F9B-5D33-465A-A702-920D77279786");
        public static readonly Guid VisualBasicProject = new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F");
    }
}