# DotNetMove
A dotnet tool for moving projects around on disk or within a solution while automatically updating solutions and project references.

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

### Help
Executing `dotnet move`, `dotnet move solution` or `dotnet move disk` with no arguments will provide additional help.

### Limitations
This current release has several limitations:
* It can only move `.csproj` files.
* It will not function correctly if your project reference paths and paths on disk have casing differences.
