﻿///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// EXTERNAL NUGET TOOLS
//////////////////////////////////////////////////////////////////////

#Tool "xunit.runner.console"
#Tool "GitVersion.CommandLine"
#Tool "Brutal.Dev.StrongNameSigner"

//////////////////////////////////////////////////////////////////////
// EXTERNAL NUGET LIBRARIES
//////////////////////////////////////////////////////////////////////

#addin "Cake.FileHelpers"
#addin nuget:?package=Cake.Yaml
#addin nuget:?package=YamlDotNet&version=5.2.1

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var projectName = "Polly.Contrib.MutableBulkheadPolicy";
var keyName = projectName + ".snk";

var solutions = GetFiles("./**/*.sln");
var solutionPaths = solutions.Select(solution => solution.GetDirectory());

var srcDir = Directory("./src");
var buildDir = Directory("./build");
var artifactsDir = Directory("./artifacts");
var testResultsDir = artifactsDir + Directory("test-results");

// NuGet
var nuspecFilename = projectName + ".nuspec";
var nuspecSrcFile = srcDir + File(nuspecFilename);
var nuspecDestFile = buildDir + File(nuspecFilename);
var nupkgDestDir = artifactsDir + Directory("nuget-package");
var snkFile = srcDir + File(keyName);

// Gitversion
var gitVersionPath = ToolsExePath("GitVersion.exe");
Dictionary<string, object> gitVersionOutput;
var gitVersionConfigFilePath = "./GitVersionConfig.yaml";

// Versioning
string nugetVersion;
string appveyorBuildNumber;
string assemblyVersion;
string assemblySemver;

// StrongNameSigner
var strongNameSignerPath = ToolsExePath("StrongNameSigner.Console.exe");

///////////////////////////////////////////////////////////////////////////////
// INNER CLASSES
///////////////////////////////////////////////////////////////////////////////
class GitVersionConfigYaml
{
    public string NextVersion { get; set; }
}

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(_ =>
{
    Information("==============================");
    Information("Starting the cake build script");
    Information("Building: " + projectName);
    Information("==============================");
});

Teardown(_ =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
// PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Clean")
    .Does(() =>
{
    DirectoryPath[] cleanDirectories = new DirectoryPath[] {
        buildDir,
        testResultsDir,
        nupkgDestDir,
        artifactsDir
  	};

    CleanDirectories(cleanDirectories);

    foreach(var path in cleanDirectories) { EnsureDirectoryExists(path); }

    foreach(var path in solutionPaths)
    {
        Information("Cleaning {0}", path);
        CleanDirectories(path + "/**/bin/" + configuration);
        CleanDirectories(path + "/**/obj/" + configuration);
    }
});

Task("__RestoreNugetPackages")
    .Does(() =>
{
    foreach(var solution in solutions)
    {
        Information("Restoring NuGet Packages for {0}", solution);
        NuGetRestore(solution);
    }
});

Task("__UpdateAssemblyVersionInformation")
    .Does(() =>
{
    var gitVersionSettings = new ProcessSettings()
        .SetRedirectStandardOutput(true);

    try {
        IEnumerable<string> outputLines;
        StartProcess(gitVersionPath, gitVersionSettings, out outputLines);

        var output = string.Join("\n", outputLines);
        gitVersionOutput = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
    }
    catch
    {
        Information("Error reading git version information. Build may be running outside of a git repo. Falling back to version specified in " + gitVersionConfigFilePath);

        string gitVersionYamlString = System.IO.File.ReadAllText(gitVersionConfigFilePath);
        GitVersionConfigYaml deserialized = DeserializeYaml<GitVersionConfigYaml>(gitVersionYamlString.Replace("next-version", "NextVersion"));
        string gitVersionConfig = deserialized.NextVersion;

        gitVersionOutput = new Dictionary<string, object>{
            { "NuGetVersion", gitVersionConfig + "-NotFromGitRepo" },
            { "FullSemVer", gitVersionConfig },
            { "AssemblySemVer", gitVersionConfig },
            { "Major", gitVersionConfig.Split('.')[0] },
        };

    }

    Information("");
    Information("Obtained raw version info for package versioning:");
    Information("NuGetVersion -> {0}", gitVersionOutput["NuGetVersion"]);
    Information("FullSemVer -> {0}", gitVersionOutput["FullSemVer"]);
    Information("AssemblySemVer -> {0}", gitVersionOutput["AssemblySemVer"]);

    appveyorBuildNumber = gitVersionOutput["FullSemVer"].ToString();
    nugetVersion = gitVersionOutput["NuGetVersion"].ToString();
    assemblyVersion = gitVersionOutput["Major"].ToString() + ".0.0.0";
    assemblySemver = gitVersionOutput["AssemblySemVer"].ToString();

    Information("");
    Information("Mapping versioning information to:");
    Information("Appveyor build number -> {0}", appveyorBuildNumber);
    Information("Nuget package version -> {0}", nugetVersion);
    Information("AssemblyVersion -> {0}", assemblyVersion);
    Information("AssemblyFileVersion -> {0}", assemblySemver);
    Information("AssemblyInformationalVersion -> {0}", assemblySemver);
});

Task("__UpdateDotNetStandardAssemblyVersionNumber")
    .Does(() =>
{
    Information("Updating Assembly Version Information");

    var attributeToValueMap = new Dictionary<string, string>() {
        { "AssemblyVersion", assemblyVersion },
        { "FileVersion", assemblySemver },
        { "InformationalVersion", assemblySemver },
        { "Version", nugetVersion },
        { "PackageVersion", nugetVersion },
    };

    var csproj = File("./src/" + projectName + "/" + projectName + ".csproj");

    foreach(var attributeMap in attributeToValueMap) {
        var attribute = attributeMap.Key;
        var value = attributeMap.Value;

        var replacedFiles = ReplaceRegexInFiles(csproj, $@"\<{attribute}\>[^\<]*\</{attribute}\>", $@"<{attribute}>{value}</{attribute}>");
        if (!replacedFiles.Any())
        {
            throw new Exception($"{attribute} version could not be updated in {csproj}.");
        }
    }

});

Task("__UpdateAppVeyorBuildNumber")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UpdateBuildVersion(appveyorBuildNumber);
});

Task("__BuildSolutions")
    .Does(() =>
{
    foreach(var solution in solutions)
    {
        Information("Building {0}", solution);

        MSBuild(solution, settings =>
            settings
                .SetConfiguration(configuration)
                .WithProperty("TreatWarningsAsErrors", "true")
                .UseToolVersion(MSBuildToolVersion.VS2017)
                .SetVerbosity(Verbosity.Minimal)
                .SetNodeReuse(false));
    }
});

Task("__RunTests")
    .Does(() =>
{
    foreach(var specsProj in GetFiles("./src/**/*.Specs.csproj")) {
        DotNetCoreTest(specsProj.FullPath, new DotNetCoreTestSettings {
            Configuration = configuration,
            NoBuild = true
        });
    }
});

Task("__CopyOutputToNugetFolder")
    .Does(() =>
{
    var sourceDir = srcDir + Directory(projectName) + Directory("bin") + Directory(configuration);

    var destDir = buildDir + Directory("lib");

    Information("Copying {0} -> {1}.", sourceDir, destDir);
    CopyDirectory(sourceDir, destDir);

    CopyFile(nuspecSrcFile, nuspecDestFile);
});

Task("__StronglySignAssemblies")
    .Does(() =>
{
    //see: https://github.com/brutaldev/StrongNameSigner
    var strongNameSignerSettings = new ProcessSettings()
        .WithArguments(args => args
            .Append("-in")
            .AppendQuoted(buildDir)
            .Append("-k")
            .AppendQuoted(snkFile)
            .Append("-l")
            .AppendQuoted("Changes"));

    StartProcess(strongNameSignerPath, strongNameSignerSettings);
});

Task("__CreateSignedNugetPackage")
    .Does(() =>
{
    var packageName = projectName;

    Information("Building {0}.{1}.nupkg", packageName, nugetVersion);

    var nuGetPackSettings = new NuGetPackSettings {
        Id = packageName,
        Title = packageName,
        Version = nugetVersion,
        OutputDirectory = nupkgDestDir
    };

    NuGetPack(nuspecDestFile, nuGetPackSettings);
});

//////////////////////////////////////////////////////////////////////
// BUILD TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("__Clean")
    .IsDependentOn("__RestoreNugetPackages")
    .IsDependentOn("__UpdateAssemblyVersionInformation")
    .IsDependentOn("__UpdateDotNetStandardAssemblyVersionNumber")
    .IsDependentOn("__UpdateAppVeyorBuildNumber")
    .IsDependentOn("__BuildSolutions")
    .IsDependentOn("__RunTests")
    .IsDependentOn("__CopyOutputToNugetFolder")
    .IsDependentOn("__StronglySignAssemblies")
    .IsDependentOn("__CreateSignedNugetPackage");

///////////////////////////////////////////////////////////////////////////////
// PRIMARY TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);

//////////////////////////////////////////////////////////////////////
// HELPER FUNCTIONS
//////////////////////////////////////////////////////////////////////

string ToolsExePath(string exeFileName) {
    var exePath = System.IO.Directory.GetFiles(@".\Tools", exeFileName, SearchOption.AllDirectories).FirstOrDefault();
    return exePath;
}