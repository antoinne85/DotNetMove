# DotNetCobble
A dotnet tool for cobbling together a solution file from a set of target projects that includes all of their project dependencies.

### Description
Sometimes we find ourselves working in a solution that has many projects and complicated dependency graphs when we could get better performance out of Visual Studio and third party plugins if the solution were much smaller. However, Visual Studio has the curious quirk that, while `dotnet build` is fully capable of building a project along with its dependencies, Visual Studio will fail if any project dependency isn't present in the currently loaded solution. Typically, crafting a one-off solution for a particular project to ease these pain points is time-consuming as you must first determine your entire project dependency graph for the target project (or set of projects) and then add them one-at-a-time to the solution.

DotNetCobble simplifies that by providing simple command-line tooling for creating a solution file from a target project (or folder of projects) that contains the target project(s) as well as all of their project dependencies.

This is particularly useful in an microservices environment where core foundational projects are used uniformly across many domains, allowing you to keep (or create) one large solution to perform large-scale refactorings or, if you already have that, create smaller solution files for more nimble development.

## To Install
`dotnet tool install -g DotNetMove`

## To Uninstall
`dotnet tool uninstall -g DotNetMove`

## Usage
### Moving Projects on Disk
##### To modify a particular solution
`dotnet move disk -p MyCompany.MyProject -d C:\SomeOtherFolder -s C:\Path\To\Solution.sln`

In this case, DotNetMove will recursively scan subfolders of the current folder to find your project (it expects to find `MyCompany.MyProject.csproj` on disk.)

It will then examine `C:\Path\To\Solution.sln` to ensure it includes that project.

If it does, it will then move your project folder to the provided location and modify:
* The solution to reference the project at its new location.
* The project references of the project so that they continue to be valid.
* The project references of any other projects within the solution that referenced the target project so that they point to the new location.

#### To modify any solution that references the target project
`dotnet move disk -p MyCompany.MyProject -d C:\SomeOtherFolder`

This form works in a similar fashion to the above usage, except this one will find all solution files under the current folder (and sub-folders) and perform all of the updates listed above for each solution.

### Moving Into Solution Folders
##### To modify a particular solution
`dotnet move solution -p MyCompany.MyProject -d SomeFolder\SomeSubFolder -s C:\Path\To\Solution.sln`

In this case, DotNetMove will recursively scan subfolders of the current folder to find your project (it expects to find `MyCompany.MyProject.csproj` on disk.)

It will then examine `C:\Path\To\Solution.sln` to ensure it includes that project.

If it does, it will then modify the solution to place that project inside of the `SomeFolder\SomeSubFolder` solution folder, removing it from whatever solution folder it may currently be in (if any).

##### To modify any solution that references the target project
`dotnet move solution -p MyCompany.MyProject -d SomeFolder\SomeSubFolder`

This form works in a similar fashion to the above usage, except this one will find all solution files under the current folder (and sub-folders) and update all of those that reference the target project.

##### Help
Executing `dotnet move`, `dotnet move solution` or `dotnet move disk` with no arguments will provide additional help.

## Limitations
This first release has several limitations, among them:
* It can only operate on `.csproj` files.
* You cannot control the filename of the solution file that gets created (you can rename it afterwards, though).
* You cannot control the behavior around solution folders or manipulate the solution structure as part of the tool.