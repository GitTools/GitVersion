import tl = require('azure-pipelines-task-lib/task');
import { IExecOptions, ToolRunner } from 'azure-pipelines-task-lib/toolrunner';
import path = require('path');
import os = require('os');
import { ArgumentParser } from 'argparse';

export class GitVersionTask {

    public static async execute() {
        try {

            const updateAssemblyInfo = tl.getBoolInput('updateAssemblyInfo');
            const updateAssemblyInfoFilename = tl.getInput('updateAssemblyInfoFilename');
            const additionalArguments = tl.getInput('additionalArguments');
            const targetPath = tl.getInput('targetPath');
            const preferBundledVersion = tl.getBoolInput('preferBundledVersion');

            const currentDirectory = __dirname;
            const workingDirectory = !targetPath
                    ? tl.getVariable("Build.SourcesDirectory")
                    : path.join(tl.getVariable("Build.SourcesDirectory"), targetPath);

            let gitVersionPath = tl.getInput('gitVersionPath');
            if (!gitVersionPath) {
                gitVersionPath = tl.which("GitVersion.exe");
                if (preferBundledVersion || !gitVersionPath) {
                    gitVersionPath = path.join(currentDirectory, "GitVersion.exe");
                }
            }

            const execOptions: IExecOptions = {
                cwd: undefined,
                env: undefined,
                silent: undefined,
                failOnStdErr: undefined,
                ignoreReturnCode: undefined,
                errStream: undefined,
                outStream: undefined,
                windowsVerbatimArguments: undefined
            };

            let toolRunner: ToolRunner;

            const parser = new ArgumentParser();
            parser.addArgument(
                [ '-r', '--runtime'],
                {
                    help: '[mono|netcore]',
                    defaultValue: 'mono'
                }
            );

            const args = parser.parseArgs();
            switch (args.runtime) {
                case 'netcore':
                    gitVersionPath = path.join(currentDirectory, "netcore", "GitVersion.dll");
                    toolRunner = tl.tool("dotnet");
                    toolRunner.arg(gitVersionPath);
                    break;

                case 'mono':
                default:
                    const isWin32 = os.platform() == "win32";
                    if (isWin32) {
                        toolRunner = tl.tool(gitVersionPath);
                    } else {
                        toolRunner = tl.tool("mono");
                        toolRunner.arg(gitVersionPath);
                    }

                    break;
            }

            toolRunner.arg([
                workingDirectory,
                "/output",
                "buildserver",
                "/nofetch"]);

            if (updateAssemblyInfo) {
                toolRunner.arg("/updateassemblyinfo");
                if (updateAssemblyInfoFilename) {
                    toolRunner.arg(updateAssemblyInfoFilename);
                } else {
                    toolRunner.arg("true");
                }
            }

            if (additionalArguments) {
                toolRunner.line(additionalArguments);
            }

            const result = await toolRunner.exec(execOptions);
            if (result) {
                tl.setResult(tl.TaskResult.Failed, "An error occured during GitVersion execution")
            } else {
                tl.setResult(tl.TaskResult.Succeeded, "GitVersion executed successfully")
            }
        }
        catch (err) {
            tl.debug(err.stack);
            tl.setResult(tl.TaskResult.Failed, err);
        }
    }
}

GitVersionTask.execute();
