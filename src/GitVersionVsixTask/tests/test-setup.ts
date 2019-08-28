import * as path from 'path';

import * as ma from 'azure-pipelines-task-lib/mock-answer';
import * as tmrm from 'azure-pipelines-task-lib/mock-run';
import * as shared from './test-shared';

let taskDir = path.join(__dirname, '..', 'GitVersionTask');
let taskPath = path.join(taskDir, 'GitVersion.js');
let tmr: tmrm.TaskMockRunner = new tmrm.TaskMockRunner(taskPath);

process.env['BUILD_SOURCESDIRECTORY'] = shared.SharedValues.BUILD_SOURCESDIRECTORY;

let runtime = process.env[shared.TestEnvVars.runtime] || 'core';

tmr.setInput('targetPath', process.env[shared.TestEnvVars.targetPath] || '');

tmr.setInput('useConfigFile', process.env[shared.TestEnvVars.useConfigFile] || 'false');
tmr.setInput('configFilePath', process.env[shared.TestEnvVars.configFilePath] || '');

tmr.setInput('updateAssemblyInfo', process.env[shared.TestEnvVars.updateAssemblyInfo] || 'false');
tmr.setInput('updateAssemblyInfoFilename', process.env[shared.TestEnvVars.updateAssemblyInfoFilename] || '');

tmr.setInput('preferBundledVersion', process.env[shared.TestEnvVars.preferBundledVersion] || 'true');
tmr.setInput('gitVersionPath', process.env[shared.TestEnvVars.gitVersionPath] || '');
tmr.setInput('runtime', runtime);

tmr.setInput('additionalArguments', process.env[shared.TestEnvVars.additionalArguments] || '');

console.log("Inputs have been set");

var gitVersionPath = (path.join(taskDir, runtime, runtime == 'core' ? 'GitVersion.dll' : 'GitVersion.exe')).replace(/\\/g, '/');

let a: ma.TaskLibAnswers = <ma.TaskLibAnswers>{
    "which": { "dotnet": "dotnet" },
    "checkPath": { "dotnet": true },
    "exec": {},
    "exist": {
        "src": true,
        "customConfig.yml" : true,
        "GlobalAssemblyInfo.cs" : true,
        "TestGitversion.dll" : true
    },
    "stats": {
        "src" : {
            "isDirectory" : true
        },
        "customConfig.yml" : {
            "isFile": true
        },
        "GlobalAssemblyInfo.cs" : {
            "isFile": true
        },
        "TestGitversion.dll" : {
            "isFile": true
        },
    }
};

a.exec["dotnet " + gitVersionPath + " /user/build /output buildserver /nofetch"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with defaults",
    "stderr": ""
};

a.exec["dotnet " + gitVersionPath + " " + shared.SharedValues.BUILD_SOURCESDIRECTORY + "/src /output buildserver /nofetch"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with custom targetPath",
    "stderr": ""
};

a.exec["dotnet " + gitVersionPath + " " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch /config customConfig.yml"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with custom config",
    "stderr": ""
};

a.exec["dotnet " + gitVersionPath + " " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch /updateassemblyinfo GlobalAssemblyInfo.cs"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with custom assembly info",
    "stderr": ""
};

a.exec[gitVersionPath + " " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch"] = {
    "code": 0,
    "stdout": "GitVersion.exe run successfully with defaults",
    "stderr": ""
};

a.exec["mono " + gitVersionPath + " " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch"] = {
    "code": 0,
    "stdout": "GitVersion.exe run successfully with defaults",
    "stderr": ""
};

a.exec["dotnet " + "TestGitversion.dll" + " " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with custom exe",
    "stderr": ""
};

tmr.setAnswers(a);

tmr.run();
