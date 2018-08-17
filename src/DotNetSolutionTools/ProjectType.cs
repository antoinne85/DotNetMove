namespace DotNetSolutionTools
{
    public struct ProjectType
    {
        public ProjectType(FrameworkType framework, ProjectLanguage language, ProjectClass @class) : this()
        {
            Framework = framework;
            Language = language;
            Class = @class;
        }

        public FrameworkType Framework {get; }
        public ProjectLanguage Language { get; }
        public ProjectClass Class { get; }
    }
}