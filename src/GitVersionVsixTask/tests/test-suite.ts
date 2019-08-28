import * as os from 'os';
import * as path from 'path';
import * as assert from 'assert';
import * as ttm from 'azure-pipelines-task-lib/mock-test';
import * as shared from './test-shared';

function runValidations(validator: () => void, tr: ttm.MockTestRunner, done: MochaDone) {
    try {
        validator();
        console.log(tr.succeeded);
        done();
    }
    catch (error) {
        console.log("STDERR", tr.stderr);
        console.log("STDOUT", tr.stdout);
        done(error);
    }
}

let taskDir = path.join(__dirname, '..', 'GitVersionTask').replace(/\\/g, '/');

describe('GitVersion Vsix Task tests', function () {

    beforeEach(() => {
        delete process.env[shared.TestEnvVars.preferBundledVersion];
        delete process.env[shared.TestEnvVars.useConfigFile];
        delete process.env[shared.TestEnvVars.configFilePath];
        delete process.env[shared.TestEnvVars.updateAssemblyInfo];
        delete process.env[shared.TestEnvVars.updateAssemblyInfoFilename];
        delete process.env[shared.TestEnvVars.additionalArguments];
        delete process.env[shared.TestEnvVars.targetPath];
        delete process.env[shared.TestEnvVars.runtime];
        delete process.env[shared.TestEnvVars.gitVersionPath];
    });

    after(() => {

    });

    it('Succeeds: Runs GitVersion default configurations', function (done: MochaDone) {
        this.timeout(1000);

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.succeeded, 'should have succeeded');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 0, "should have no errors");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 1, 'should have invoked tool one time. actual: ' + tr.invokedToolCount);
            assert(tr.ran("dotnet " + taskDir + "/core/GitVersion.dll /user/build /output buildserver /nofetch"));

            assert(tr.stdOutContained('GitVersion run successfully with defaults'), "should display 'GitVersion run successfully with defaults'");

        }, tr, done);
    });

    it('Succeeds: Runs GitVersion default configurations', function (done: MochaDone) {
        this.timeout(1000);
        process.env[shared.TestEnvVars.runtime] = 'full';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.succeeded, 'should have succeeded');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 0, "should have no errors");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 1, 'should have invoked tool one time. actual: ' + tr.invokedToolCount);

            var exec = os.platform() != 'win32' ? "mono " : "";
            assert(tr.ran(exec + taskDir + "/full/GitVersion.exe /user/build /output buildserver /nofetch"));

            assert(tr.stdOutContained('GitVersion.exe run successfully with defaults'), "should display 'GitVersion.exe run successfully with defaults'");

        }, tr, done);
    });

    it('Fails: Runs GitVersion with wrong targetPath', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.targetPath] = 'wrongPath';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.failed, 'should have failed');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 1, "should have thrown an error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 0, 'should not have invoked tool. actual: ' + tr.invokedToolCount);

            assert(tr.stdOutContained('Directory not found'), "should display 'Directory not found'");

        }, tr, done);
    });

    it('Succeeds: Runs GitVersion with correct target', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.targetPath] = 'src';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.succeeded, 'should have succeeded');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 0, "should have no errors");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 1, 'should have invoked tool one time. actual: ' + tr.invokedToolCount);
            assert(tr.ran("dotnet " + taskDir + "/core/GitVersion.dll /user/build/src /output buildserver /nofetch"));

            assert(tr.stdOutContained('GitVersion run successfully with custom targetPath'), "should display 'GitVersion run successfully with custom targetPath'");

        }, tr, done);
    });

    it('Fails: Runs GitVersion with no configuration file specified', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.useConfigFile] = 'true';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert.equal(tr.succeeded, false, 'should have failed');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 1, "should have thrown an error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 0, 'should not have invoked tool. actual: ' + tr.invokedToolCount);

            assert(tr.stdOutContained('GitVersion configuration file not found'), "should display 'GitVersion configuration file not found'");

        }, tr, done);
    });

    it('Fails: Runs GitVersion with wrong configuration file specified', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.useConfigFile] = 'true';
        process.env[shared.TestEnvVars.configFilePath] = 'wronfConfig.yml';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.failed, 'should have failed');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 1, "should have thrown an error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 0, 'should not have invoked tool. actual: ' + tr.invokedToolCount);

            assert(tr.stdOutContained('GitVersion configuration file not found'), "should display 'GitVersion configuration file not found'");

        }, tr, done);
    });

    it('Succeeds: Runs GitVersion with correct custom configuration file specified', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.useConfigFile] = 'true';
        process.env[shared.TestEnvVars.configFilePath] = 'customConfig.yml';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.succeeded, 'should have succeeded');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 0, "should have no error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 1, 'should have invoked tool one time. actual: ' + tr.invokedToolCount);
            assert(tr.ran("dotnet " + taskDir + "/core/GitVersion.dll /user/build /output buildserver /nofetch /config customConfig.yml"));

            assert(tr.stdOutContained('GitVersion run successfully with custom config'), "should display 'GitVersion run successfully with custom config'");

        }, tr, done);
    });

    it('Fails: Runs GitVersion with no assembly info file specified', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.updateAssemblyInfo] = 'true';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.failed, 'should have failed');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 1, "should have thrown an error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 0, 'should not have invoked tool. actual: ' + tr.invokedToolCount);

            assert(tr.stdOutContained('AssemblyInfoFilename file not found'), "should display 'AssemblyInfoFilename file not found'");

        }, tr, done);
    });

    it('Fails: Runs GitVersion with wrong assembly info file specified', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.updateAssemblyInfo] = 'true';
        process.env[shared.TestEnvVars.updateAssemblyInfoFilename] = 'wrongAssemblyInfo.cs';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.failed, 'should have failed');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 1, "should have thrown an error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 0, 'should not have invoked tool. actual: ' + tr.invokedToolCount);

            assert(tr.stdOutContained('AssemblyInfoFilename file not found'), "should display 'AssemblyInfoFilename file not found'");

        }, tr, done);
    });

    it('Succeeds: Runs GitVersion with correct custom assembly info file specified', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.updateAssemblyInfo] = 'true';
        process.env[shared.TestEnvVars.updateAssemblyInfoFilename] = 'GlobalAssemblyInfo.cs';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.succeeded, 'should have succeeded');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 0, "should have no errors");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 1, 'should have invoked tool one time. actual: ' + tr.invokedToolCount);
            assert(tr.ran("dotnet " + taskDir + "/core/GitVersion.dll /user/build /output buildserver /nofetch /updateassemblyinfo GlobalAssemblyInfo.cs"));

            assert(tr.stdOutContained('GitVersion run successfully with custom assembly info'), "should display 'GitVersion run successfully with custom  assembly info'");

        }, tr, done);
    });

    it('Fails: Runs GitVersion with no custom executable', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.preferBundledVersion] = 'false';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.failed, 'should have failed');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 1, "should have thrown an error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 0, 'should not have invoked tool. actual: ' + tr.invokedToolCount);

            assert(tr.stdOutContained('GitVersion executable not found'), "should display 'GitVersion executable not found'");

        }, tr, done);
    });

    it('Fails: Runs GitVersion with wrong custom executable', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.preferBundledVersion] = 'false';
        process.env[shared.TestEnvVars.gitVersionPath] = 'wrongGitversion.dll';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.failed, 'should have failed');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 1, "should have thrown an error");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 0, 'should not have invoked tool. actual: ' + tr.invokedToolCount);

            assert(tr.stdOutContained('GitVersion executable not found'), "should display 'GitVersion executable not found'");

        }, tr, done);
    });

    it('Succeeds: Runs GitVersion with correct custom executable', function (done: MochaDone) {
        this.timeout(1000);

        process.env[shared.TestEnvVars.preferBundledVersion] = 'false';
        process.env[shared.TestEnvVars.gitVersionPath] = 'TestGitversion.dll';

        let tp = path.join(__dirname, 'test-setup.js');
        let tr: ttm.MockTestRunner = new ttm.MockTestRunner(tp);

        tr.run();

        runValidations(() => {

            assert(tr.succeeded, 'should have succeeded');
            assert(tr.warningIssues.length == 0, "should have no warnings");
            assert(tr.errorIssues.length == 0, "should have no errors");
            assert(tr.stderr.length == 0, 'should not have written to stderr');
            assert(tr.invokedToolCount == 1, 'should have invoked tool one time. actual: ' + tr.invokedToolCount);
            assert(tr.ran("dotnet TestGitversion.dll /user/build /output buildserver /nofetch"));

            assert(tr.stdOutContained('GitVersion run successfully with custom exe'), "should display 'GitVersion run successfully with custom exe'");
        }, tr, done);
    });

});
