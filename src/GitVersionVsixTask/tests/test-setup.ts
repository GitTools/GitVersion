import * as path from 'path';

import * as ma from 'azure-pipelines-task-lib/mock-answer';
import * as tmrm from 'azure-pipelines-task-lib/mock-run';
import * as shared from './test-shared';

let taskDir = path.join(__dirname, '..', 'GitVersionTask');
let taskPath = path.join(taskDir, 'GitVersion.js');
let tmr: tmrm.TaskMockRunner = new tmrm.TaskMockRunner(taskPath);

process.env['BUILD_SOURCESDIRECTORY'] = shared.SharedValues.BUILD_SOURCESDIRECTORY;
process.env["INSTALL_TOOL"] = 'false';

tmr.setInput('versionSpec', process.env[shared.TestEnvVars.versionSpec] || '5.x');
tmr.setInput('includePrerelease', process.env[shared.TestEnvVars.includePrerelease] || 'false');
tmr.setInput('targetPath', process.env[shared.TestEnvVars.targetPath] || '');

tmr.setInput('useConfigFile', process.env[shared.TestEnvVars.useConfigFile] || 'false');
tmr.setInput('configFilePath', process.env[shared.TestEnvVars.configFilePath] || '');

tmr.setInput('updateAssemblyInfo', process.env[shared.TestEnvVars.updateAssemblyInfo] || 'false');
tmr.setInput('updateAssemblyInfoFilename', process.env[shared.TestEnvVars.updateAssemblyInfoFilename] || '');

tmr.setInput('additionalArguments', process.env[shared.TestEnvVars.additionalArguments] || '');

console.log("Inputs have been set");

let a: ma.TaskLibAnswers = <ma.TaskLibAnswers>{
    "which": { "dotnet-gitversion": "dotnet-gitversion" },
    "checkPath": { "dotnet-gitversion": true },
    "exec": {},
    "exist": {
        "src": true,
        "customConfig.yml" : true,
        "GlobalAssemblyInfo.cs" : true
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
        }
    }
};

a.exec["dotnet-gitversion " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with defaults",
    "stderr": ""
};

a.exec["dotnet-gitversion " + shared.SharedValues.BUILD_SOURCESDIRECTORY + "/src /output buildserver /nofetch"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with custom targetPath",
    "stderr": ""
};

a.exec["dotnet-gitversion " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch /config customConfig.yml"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with custom config",
    "stderr": ""
};

a.exec["dotnet-gitversion " + shared.SharedValues.BUILD_SOURCESDIRECTORY + " /output buildserver /nofetch /updateassemblyinfo GlobalAssemblyInfo.cs"] = {
    "code": 0,
    "stdout": "GitVersion run successfully with custom assembly info",
    "stderr": ""
};

var mt = require('azure-pipelines-task-lib/mock-task');
mt.assertAgent = (minimum: string) =>
{
};
tmr.registerMockExport('mt', mt);

tmr.setAnswers(a);

tmr.run();
