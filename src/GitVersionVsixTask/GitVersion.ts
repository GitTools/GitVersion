import * as path from 'path';
import * as os from 'os';

import * as tl from 'azure-pipelines-task-lib/task';
import * as tr from 'azure-pipelines-task-lib/toolrunner';

export class GitVersionTask {
    execOptions: tr.IExecOptions;

    preferBundledVersion: boolean;
    useConfigFile: boolean;
    configFilePath: string;
    updateAssemblyInfo: boolean;

    updateAssemblyInfoFilename: string;
    additionalArguments: string;
    targetPath: string;
    sourcesDirectory: string;
    gitVersionPath: string;
    runtime: string;

    constructor() {

        this.targetPath                 = tl.getInput('targetPath');

        this.useConfigFile              = tl.getBoolInput('useConfigFile');
        this.configFilePath             = tl.getInput('configFilePath');

        this.updateAssemblyInfo         = tl.getBoolInput('updateAssemblyInfo');
        this.updateAssemblyInfoFilename = tl.getInput('updateAssemblyInfoFilename');

        this.preferBundledVersion       = tl.getBoolInput('preferBundledVersion');
        this.runtime                    = tl.getInput('runtime') || 'core';
        this.gitVersionPath             = tl.getInput('gitVersionPath');

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
            let workingDirectory = this.getWorkingDirectory(this.targetPath);
            let exe = this.getExecutable();
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

    public getExecutable(){
        let exe: tr.ToolRunner;

        switch (this.runtime) {
            case "full":
                const isWin32 = os.platform() == "win32";
                let exePath = this.getExecutablePath("GitVersion.exe") || tl.which("GitVersion.exe", true);
                if (isWin32) {
                    exe = tl.tool(exePath);
                } else {
                    exe = tl.tool("mono");
                    exe.arg(exePath);
                }
                break;
            case "core":
                let assemblyPath = this.getExecutablePath("GitVersion.dll");
                let dotnetPath = tl.which("dotnet", true);
                exe = tl.tool(dotnetPath);
                exe.arg(assemblyPath);
                break;
        }

        return exe;
    }

    public getExecutablePath(exeName:string) {
        let exePath;
        if (this.preferBundledVersion) {
            let currentDirectory = __dirname;
            exePath = path.join(currentDirectory, this.runtime, exeName);
        } else {
            if (tl.filePathSupplied('gitVersionPath') && tl.exist(this.gitVersionPath) && tl.stats(this.gitVersionPath).isFile()) {
                exePath = this.gitVersionPath;
            } else{
                throw new Error('GitVersion executable not found at ' + this.gitVersionPath);
            }
        }

        return exePath.replace(/\\/g, '/');
    }

    public getWorkingDirectory(targetPath: string) {
        let workDir;

        if (!targetPath){
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
