namespace GitTools
{
    using System.Text;
    using JetBrains.Annotations;

    public static class StringExtensions
    {
        [StringFormatMethod("format")]
        public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
        {
            stringBuilder.AppendFormat(format, args);
            stringBuilder.AppendLine();
        }
    }
}