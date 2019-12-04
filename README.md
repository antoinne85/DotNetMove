# DotNetMove & DotNetCobble
A set of dotnet tools for manipulating physical and logical layout of .NET projects and solutions.

## To Install
`dotnet tool install -g DotNetMove`  
or  
`dotnet tool install -g DotNetCobble`

## To Uninstall
`dotnet tool uninstall -g DotNetMove`  
or  
`dotnet tool uninstall -g DotNetCobble`

## DotNetMove
A dotnet tool for moving projects around on disk or within a solution while automatically updating solutions and project references.

Because .NET Project files use relative paths for specifying their project dependencies, move a project around on disk can be time consuming and error prone. You have to update not only the relative paths to the projects it references, but also the relative paths of other projects that reference the moved project.

DotNetMove simplifies that by providing simple command-line tooling for moving a project around on-disk (or within a solution) while properly maintaining the relative paths of both the moved project as well as the projects that reference it.

See the [tool-specific README](src/DotNetMove/README.md) for usage, limitations and more information.

## DotNetCobble
A dotnet tool for cobbling together a solution file from a set of target projects that includes all of their project dependencies.

Sometimes we find ourselves working in a solution that has many projects and complicated dependency graphs when we could get better performance out of Visual Studio and third party plugins if the solution were much smaller. However, Visual Studio has the curious quirk that, while `dotnet build` is fully capable of building a project along with its dependencies, Visual Studio will fail if any project dependency isn't present in the currently loaded solution. Typically, crafting a one-off solution for a particular project to ease these pain points is time-consuming as you must first determine your entire project dependency graph for the target project (or set of projects) and then add them one-at-a-time to the solution.

DotNetCobble simplifies that by providing simple command-line tooling for creating a solution file from a target project (or folder of projects) that contains the target project(s) as well as all of their project dependencies.

This is particularly useful in an microservices environment where core foundational projects are used uniformly across many domains, allowing you to keep (or create) one large solution to perform large-scale refactorings or, if you already have that, create smaller solution files for more nimble development.

See the [tool-specific README](src/DotNetCobble/README.md)  for usage, limitations and more information.