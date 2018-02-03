#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=JetBrains.ReSharper.CommandLineTools"
#tool "nuget:?package=gitreleasemanager"

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
    Information("   NuGetVersionV2: {0}", version.NuGetVersionV2);
    Information("   InformationalVersion: {0}", version.InformationalVersion);
    Information("}");

    System.IO.File.WriteAllText(dst.Path + "/VERSION", version.NuGetVersionV2);

    // NOTE: update the vestion of Visual Studio Extension
    foreach(var vsixmanifest in GetFiles(src.Path + "/**/*.vsixmanifest")) {
        Information(vsixmanifest);
        var settings = new XmlPokeSettings {
            Namespaces = new Dictionary<string, string> {
                { "ns", "http://schemas.microsoft.com/developer/vsx-schema/2011" }
            }
        };
        XmlPoke(vsixmanifest, "/ns:PackageManifest/ns:Metadata/ns:Identity/@Version", version.NuGetVersionV2, settings);
    }
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
});

Task("Artifacts").Does(() => {
    Information("copying the NuGetGraph.CLI to artifacts ...");
    Zip(src + Directory("./NuGetGraph.CLI/bin/Release"), dst + File("NuGetGraph.CLI.zip"));
    Information("copying the NuGetGraph.VisualStudio extension to artifacts ...");
    CopyFiles(src + File("./NuGetGraph.VisualStudio/bin/Release/NuGetGraph.VisualStudio.vsix"), dst);
});

Task("Release").Does(() => {
    var nickname = Argument<string>("u");
    var password = Argument<string>("p");

    var settings = new GitReleaseManagerCreateSettings {
        Prerelease        = true,
        Name              = "v" + System.IO.File.ReadAllText(dst.Path + "/VERSION"),
        InputFilePath     = System.IO.Path.GetTempFileName(),
        Assets            = "./artifacts/NuGetGraph.CLI.zip,./artifacts/NuGetGraph.VisualStudio.vsix",
        TargetCommitish   = "master",
        TargetDirectory   = "."
    };
    GitReleaseManagerCreate(nickname, password, "kolbasik", "NuGetGraph", settings);
});

Task("Default")
  .IsDependentOn("Clean")
  .IsDependentOn("Restore")
  .IsDependentOn("Inspect")
  .IsDependentOn("SemVer")
  .IsDependentOn("Build")
  .IsDependentOn("Artifacts");

RunTarget(Argument("target", "Default"));