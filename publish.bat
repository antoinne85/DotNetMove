set PROJECT=%1
set VERSION=%2
set APIKEY=%3
pushd src\%PROJECT%
dotnet pack --configuration Release
dotnet nuget push .\bin\release\%PROJECT%.%VERSION%.nupkg --source https://api.nuget.org/v3/index.json --api-key %APIKEY%
popd