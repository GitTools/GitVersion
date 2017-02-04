import tl = require('vsts-task-lib/task');
import path = require('path');
import q = require('q');

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
            toolRunner.line(additionalArguments);
        }

        var result = await toolRunner.exec();
        if (result) {
            tl.setResult(tl.TaskResult.Failed, "An error occured during GitVersion execution")
        } else {
            tl.setResult(tl.TaskResult.Succeeded, "GitVersion executed successfully")
        }
    }
    catch (err) {
        tl.debug(err.stack)
        tl.setResult(tl.TaskResult.Failed, err);
    }
})();






