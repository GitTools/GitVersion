namespace GitVersion.MSBuildTask.Tasks
{
    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        protected override bool OnExecute() => TaskProxy.WriteVersionInfoToBuildLog(this);
    }
}
