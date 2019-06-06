namespace GitVersionTask
{
    public static class UpdateAssemblyInfo
    {
        public static Output Execute(Input input) => GitVersionTasks.UpdateAssemblyInfo(input);

        public sealed class Input : InputWithCommonAdditionalProperties
        {
            public string[] CompileFiles { get; set; }

            protected override bool ValidateInput()
            {
                return base.ValidateInput()
                    && CompileFiles != null;
            }
        }

        public sealed class Output
        {
            public string AssemblyInfoTempFilePath { get; set; }
        }
    }
}
