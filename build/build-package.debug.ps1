$projectPath = "..\src\Krlbr.Optimizely.ImageSharp.Web\Krlbr.Optimizely.ImageSharp.Web.csproj"

dotnet pack $projectPath --configuration Debug --output .\nupkg\debug
