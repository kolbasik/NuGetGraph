#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=JetBrains.ReSharper.CommandLineTools"

var src = Directory("./src");
var dst = Directory("./artifacts");
var reports = dst + Directory("./reports");

Task("Clean").Does(() => {
    CleanDirectories(dst);
    CleanDirectories(src.Path + "/packages");
    CleanDirectories(src.Path + "/**/bin");
    CleanDirectories(src.Path + "/**/obj");
    CleanDirectories(src.Path + "/**/pkg");
});

Task("Restore").Does(() => {
    EnsureDirectoryExists(dst);
    EnsureDirectoryExists(reports);

    foreach(var sln in GetFiles(src.Path + "/*.sln")) {
        NuGetRestore(sln, new NuGetRestoreSettings { NoCache = false });
    }
});

Task("Inspect").Does(() => {
    if (IsRunningOnWindows()) {
        Information("JetBrains.ReSharper: Finding duplicates ...");
        var code = GetFiles(src.Path + "/**/*.cs")
            .Where(path => !path.FullPath.Contains("obj"));
        DupFinder(code, new DupFinderSettings {
            ShowText = true,
            ShowStats = true,
            DiscardCost = 100,
            OutputFile = reports + Directory("code") + File("duplicates.xml"),
            ThrowExceptionOnFindingDuplicates = Argument("ThrowException", true)
        });

        Information("JetBrains.ReSharper: Inspecting code ...");
        var msbuild = new Dictionary<string, string>();
        msbuild.Add("configuration", "Release");
        msbuild.Add("platform", "AnyCPU");
        foreach(var file in GetFiles(src.Path + "/*.sln")) {
            InspectCode(file.FullPath, new InspectCodeSettings {
                SolutionWideAnalysis = true,
                MsBuildProperties = msbuild,
                OutputFile = reports.Path.FullPath + "/code/" + file.GetFilenameWithoutExtension() + ".xml",
                ThrowExceptionOnFindingViolations = Argument("ThrowException", true)
            });
        }
    }
});

Task("SemVer").Does(() => {
    var version = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json,
        UpdateAssemblyInfo = true,
        UpdateAssemblyInfoFilePath = src + File("CommonAssemblyInfo.cs")
    });
    Information("GitVersion = {");
    Information("   FullSemVer: {0}", version.FullSemVer);
    Information("   LegacySemVer: {0}", version.LegacySemVer);
    Information("   MajorMinorPatch: {0}", version.MajorMinorPatch);
    Information("   InformationalVersion: {0}", version.InformationalVersion);
    Information("   Nuget v2 version: {0}", version.NuGetVersionV2);
    Information("}");
});

Task("Build").Does(() => {
    foreach(var sln in GetFiles(src.Path + "/*.sln")) {
        MSBuild(sln, settings => settings
            .SetConfiguration("Release")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetPlatformTarget(PlatformTarget.MSIL));
    }
    CopyFiles(GetFiles(src.Path + "/**/*.vsix"), dst);
});

Task("Default")
  .IsDependentOn("Clean")
  .IsDependentOn("Restore")
  .IsDependentOn("Inspect")
  .IsDependentOn("SemVer")
  .IsDependentOn("Build");

RunTarget(Argument("target", "Default"));