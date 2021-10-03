namespace GitVersion.MsBuild.Tasks;

public class WriteVersionInfoToBuildLog : GitVersionTaskBase
{
    protected override bool OnExecute() => GitVersionTasks.WriteVersionInfoToBuildLog(this);
}
