namespace GitVersionTask.MsBuild.Tasks
{
    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        public override bool Execute() => TaskProxy.WriteVersionInfoToBuildLog(this);
    }
}
