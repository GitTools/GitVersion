import tl = require('azure-pipelines-task-lib/task');
import tr = require('azure-pipelines-task-lib/toolrunner');
import path = require('path');
import os = require('os');

export class GitVersionTask {
    execOptions: tr.IExecOptions;

    preferBundledVersion: boolean;
    configFilePathSupplied: boolean;
    configFilePath: string;
    updateAssemblyInfo: boolean;

    updateAssemblyInfoFilename: string;
    additionalArguments: string;
    targetPath: string;
    sourcesDirectory: string;
    currentDirectory: string;
    workingDirectory: string;
    gitVersionPath: string;
    runtime: string;

    constructor() {
        this.preferBundledVersion       = tl.getBoolInput('preferBundledVersion') || true;
        this.configFilePathSupplied     = tl.filePathSupplied('configFilePath');
        this.configFilePath             = tl.getPathInput('configFilePath');
        this.updateAssemblyInfo         = tl.getBoolInput('updateAssemblyInfo');

        this.updateAssemblyInfoFilename = tl.getInput('updateAssemblyInfoFilename');
        this.additionalArguments        = tl.getInput('additionalArguments');
        this.targetPath                 = tl.getInput('targetPath');
        this.runtime                    = tl.getInput('runtime') || "core";
        this.gitVersionPath             = tl.getInput('gitVersionPath');

        this.sourcesDirectory           = tl.getVariable("Build.SourcesDirectory");

        this.currentDirectory = __dirname;
        this.workingDirectory = !this.targetPath
                ? this.sourcesDirectory
                : path.join(this.sourcesDirectory, this.targetPath);

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
            let exe = this.getExecutable();
            exe.arg([
                this.workingDirectory,
                "/output",
                "buildserver",
                "/nofetch"]);

            if (this.configFilePathSupplied && this.configFilePath) {
                exe.arg(["/config", this.configFilePath]);
            }

            if (this.updateAssemblyInfo) {
                exe.arg("/updateassemblyinfo");
                if (this.updateAssemblyInfoFilename) {
                    exe.arg(this.updateAssemblyInfoFilename);
                } else {
                    exe.arg("true");
                }
            }

            if (this.additionalArguments) {
                exe.line(this.additionalArguments);
            }

            const result = await exe.exec(this.execOptions);
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
        if (this.gitVersionPath){
            return this.gitVersionPath;
        } else if (this.preferBundledVersion) {
            return path.join(this.currentDirectory, this.runtime, exeName);
        }
    }
}

var exe = new GitVersionTask();
exe.execute().catch((reason) => tl.setResult(tl.TaskResult.Failed, reason));
