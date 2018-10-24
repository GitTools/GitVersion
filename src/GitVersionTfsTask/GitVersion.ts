import tl = require('vsts-task-lib/task');
import { IExecOptions, ToolRunner } from 'vsts-task-lib/toolrunner';
import path = require('path');
import os = require('os');

var updateAssemblyInfo = tl.getBoolInput('updateAssemblyInfo');
var updateAssemblyInfoFilename = tl.getInput('updateAssemblyInfoFilename');
var additionalArguments = tl.getInput('additionalArguments');
var gitVersionPath = tl.getInput('gitVersionPath');
var preferBundledVersion = tl.getBoolInput('preferBundledVersion');

var currentDirectory = __dirname;

var sourcesDirectory = tl.getVariable("Build.SourcesDirectory") || ".";

if (!gitVersionPath) {
    gitVersionPath = tl.which("GitVersion.exe");
    if (preferBundledVersion || !gitVersionPath) {
        gitVersionPath = path.join(currentDirectory, "GitVersion.exe");
    }
}

(async function execute() {
    try {

        var execOptions: IExecOptions = {
            cwd: undefined,
            env: undefined,
            silent: undefined,
            failOnStdErr: undefined,
            ignoreReturnCode: undefined,
            errStream: undefined,
            outStream: undefined,
            windowsVerbatimArguments: undefined
        };

        var toolRunner: ToolRunner;

        var isWin32 = os.platform() == "win32";

        if (isWin32) {
            toolRunner = tl.tool(gitVersionPath);
        } else {
            toolRunner = tl.tool("mono");
            toolRunner.arg(gitVersionPath);
        }

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

        var result = await toolRunner.exec(execOptions);
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
