namespace GitVersionTask
{
    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        public override bool Execute() => GitVersionTasks.WriteVersionInfoToBuildLog(this);
    }
}
