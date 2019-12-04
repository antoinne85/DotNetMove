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

Capable of creating solution files that contain one or more target projects and all of their project dependencies. See the [tool-specific README](src/DotNetCobble/README.md)  for usage, limitations and more information.

Because .NET Project files use relative paths for specifying their project dependencies, move a project around on disk can be time consuming and error prone. You have to update not only the relative paths to the projects it references, but also the relative paths of other projects that reference the moved project.

DotNetMove simplifies that by providing simple command-line tooling for moving a project around on-disk (or within a solution) while properly maintaining the relative paths of both the moved project as well as the projects that reference it.