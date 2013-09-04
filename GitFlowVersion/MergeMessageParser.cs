namespace GitFlowVersion
{
    using System;
    using System.Linq;

    public class MergeMessageParser
    {

        public static string GetVersionFromMergeCommit(string message)
        {
            var array = message
                .SkipWhile(x => !char.IsNumber(x))
                .TakeWhile(x => x == '.' || Char.IsNumber(x))
                .ToArray();
            return new string(array);
        }
    }
}