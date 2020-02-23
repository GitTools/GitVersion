namespace GitVersion.MSBuildTask.Tasks
{
    public class WriteVersionInfoToBuildLog : GitVersionTaskBase<WriteVersionInfoToBuildLog>
    {
        public override IAssemblyProvider GetAssemblyProvider()
        {
            return AssemblyProvider.Instance;
        }

        protected override void ExecuteAction(IGitVersionTaskExecutor executor)
        {
            executor.WriteVersionInfoToBuildLog(this);
        }

    }
}
