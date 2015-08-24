namespace GitVersion.Runners
{
    using System;
    using System.Linq;
    using GitVersion.Helpers;
    using GitVersion.Options;

    class InspectRunner
    {
        public static void Run(InspectOptions opts)
        {
            var inputVariables = new InputVariables()
                {
                    TargetPath = opts.Path,
                };

            var fs = new FileSystem();
            var allVariables = SpecifiedArgumentRunner.GetVariables(fs, inputVariables);

            // TODO: allow more variables
            var showVariable = opts.Variables.First();

            switch (showVariable)
            {
                case null:
                    // TODO: allow more output formatters
                    Console.WriteLine(JsonOutputFormatter.ToJson(allVariables));
                    break;

                default:
                    string part;
                    if (!allVariables.TryGetValue(showVariable, out part))
                    {
                        throw new WarningException(string.Format("'{0}' variable does not exist", showVariable));
                    }
                    Console.WriteLine(part);
                    break;
            }
        }
    }
}