namespace GitVersion.MsBuild.Task
{
    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        public override bool Execute() => TaskProxy.WriteVersionInfoToBuildLog(this);
    }
}
