import * as path from 'path';

import * as tl from 'azure-pipelines-task-lib/task';
import * as tr from 'azure-pipelines-task-lib/toolrunner';

import { ToolInstaller } from './ToolInstaller';

export class GitVersionTask {
    execOptions: tr.IExecOptions;

    versionSpec: string;
    includePrerelease: boolean;
    targetPath: string;

    useConfigFile: boolean;
    configFilePath: string;

    updateAssemblyInfo: boolean;
    updateAssemblyInfoFilename: string;

    additionalArguments: string;

    sourcesDirectory: string;

    constructor() {

        this.versionSpec                = tl.getInput('versionSpec', true);
        this.includePrerelease          = tl.getBoolInput('includePrerelease');
        this.targetPath                 = tl.getInput('targetPath');

        this.useConfigFile              = tl.getBoolInput('useConfigFile');
        this.configFilePath             = tl.getInput('configFilePath');

        this.updateAssemblyInfo         = tl.getBoolInput('updateAssemblyInfo');
        this.updateAssemblyInfoFilename = tl.getInput('updateAssemblyInfoFilename');

        this.additionalArguments        = tl.getInput('additionalArguments');

        this.sourcesDirectory           = tl.getVariable('Build.SourcesDirectory').replace(/\\/g, '/');

        this.execOptions = {
            cwd: undefined,
            env: undefined,
            silent: undefined,
            failOnStdErr: undefined,
            ignoreReturnCode: undefined,
            errStream: undefined,
            outStream: undefined,
            windowsVerbatimArguments: undefined
        };
    }

    public async execute() {
        try {
            let toolPath = await this.installTool(this.versionSpec, this.includePrerelease);

            let workingDirectory = this.getWorkingDirectory(this.targetPath);
            let exe = tl.tool('dotnet-gitversion');
            exe.arg([
                workingDirectory,
                "/output",
                "buildserver",
                "/nofetch"]);

            if (this.useConfigFile) {
                if (tl.filePathSupplied('configFilePath') && tl.exist(this.configFilePath) && tl.stats(this.configFilePath).isFile()) {
                    exe.arg(["/config", this.configFilePath]);
                }
                else {
                    throw new Error('GitVersion configuration file not found at ' + this.configFilePath);
                }
            }

            if (this.updateAssemblyInfo) {
                exe.arg("/updateassemblyinfo");
                if (tl.filePathSupplied('updateAssemblyInfoFilename') && tl.exist(this.updateAssemblyInfoFilename) && tl.stats(this.updateAssemblyInfoFilename).isFile()) {
                    exe.arg(this.updateAssemblyInfoFilename);
                }
                else {
                    throw new Error('AssemblyInfoFilename file not found at ' + this.updateAssemblyInfoFilename);
                }
            }

            if (this.additionalArguments) {
                exe.line(this.additionalArguments);
            }

            const result = await exe.exec(this.execOptions);
            if (result) {
                tl.setResult(tl.TaskResult.Failed, "An error occured during GitVersion execution");
            } else {
                tl.setResult(tl.TaskResult.Succeeded, "GitVersion executed successfully");
            }
        }
        catch (err) {
            tl.debug(err.stack);
            tl.setResult(tl.TaskResult.Failed, err, true);
        }
    }

    async installTool(version: string, includePrerelease: boolean): Promise<string> {
        let installTool = tl.getVariable("INSTALL_TOOL");
        if (installTool === null || installTool === undefined || installTool.toUpperCase() == "TRUE") {
            return await new ToolInstaller().downloadAndInstall("GitVersion.Tool", version, false, includePrerelease);
        }
    }

    getWorkingDirectory(targetPath: string) {
        let workDir;

        if (!targetPath) {
            workDir = this.sourcesDirectory;
        } else {
            if (tl.exist(targetPath) && tl.stats(targetPath).isDirectory()) {
                workDir = path.join(this.sourcesDirectory, targetPath);
            }
            else {
                throw new Error('Directory not found at ' + targetPath);
            }
        }
        return workDir.replace(/\\/g, '/');
    }
}

var task = new GitVersionTask();
task.execute().catch((reason) => tl.setResult(tl.TaskResult.Failed, reason));
