import tl = require('vsts-task-lib/task');
import path = require('path');
import q = require('q');

var variables = [
    "GitVersion_Major",
    "GitVersion_Minor",
    "GitVersion_Patch",
    "GitVersion_PreReleaseTag",
    "GitVersion_PreReleaseTagWithDash",
    "GitVersion_PreReleaseLabel",
    "GitVersion_PreReleaseNumber",
    "GitVersion_BuildMetaData",
    "GitVersion_BuildMetaDataPadded",
    "GitVersion_FullBuildMetaData",
    "GitVersion_MajorMinorPatch",
    "GitVersion_SemVer",
    "GitVersion_LegacySemVer",
    "GitVersion_LegacySemVerPadded",
    "GitVersion_AssemblySemVer",
    "GitVersion_FullSemVer",
    "GitVersion_InformationalVersion",
    "GitVersion_BranchName",
    "GitVersion_Sha",
    "GitVersion_NuGetVersionV2",
    "GitVersion_NuGetVersion",
    "GitVersion_NuGetPreReleaseTagV2",
    "GitVersion_NuGetPreReleaseTag",
    "GitVersion_CommitsSinceVersionSource",
    "GitVersion_CommitsSinceVersionSourcePadded",
    "GitVersion_CommitDate"
]

var updateAssemblyInfo = tl.getBoolInput('updateAssemblyInfo');
var updateAssemblyInfoFilename = tl.getInput('updateAssemblyInfoFilename');
var additionalArguments = tl.getInput('additionalArguments');
var gitVersionPath = tl.getInput('gitVersionPath');

var currentDirectory = __dirname;

var sourcesDirectory = tl.getVariable("Build.SourcesDirectory")

if (!gitVersionPath) {
    gitVersionPath = tl.which("GitVersion.exe");
    if (!gitVersionPath) {
        gitVersionPath = path.join(currentDirectory, "GitVersion.exe");
    }
}

(async function execute() {
    try {
        var toolRunner = tl.tool(gitVersionPath);

        toolRunner.arg([
            sourcesDirectory,
            "/output",
            "buildserver",
            "/nofetch"
        ]);

        if (updateAssemblyInfo) {
            toolRunner.arg("/updateassemblyinfo")
            if (updateAssemblyInfoFilename) {
                toolRunner.arg(updateAssemblyInfoFilename);
            } else {
                toolRunner.arg("true");
            }
        }

        if (additionalArguments) {
            toolRunner.arg(additionalArguments);
        }

        var result = await toolRunner.exec();
        if (result) {
            tl.setResult(tl.TaskResult.Failed, "An error occured during GitVersion execution")
        } else {
            // workaround gitversion to make sure variable are available in all vsts build contexts
            for(var i = 0; i < variables.length; i++) {
                var variableName = variables[i];

                tl.setVariable(variableName, tl.getVariable(variableName));
            }

            tl.setResult(tl.TaskResult.Succeeded, "GitVersion executed successfully")
        }
    }
    catch (err) {
        tl.debug(err.stack)
        tl.setResult(tl.TaskResult.Failed, err);
    }
})();






