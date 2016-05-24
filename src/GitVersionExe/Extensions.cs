namespace GitVersion
{
    using System;
    using System.Linq;

    public static class Extensions
    {
        private static string[] trues;
        private static string[] falses;


        static Extensions()
        {
            trues = new[]
            {
                "1",
                "true"
            };

            falses = new[]
            {
                "0",
                "false"
            };
        }

        public static bool IsTrue(this string value)
        {
            return trues.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsFalse(this string value)
        {
            return falses.Contains(value, StringComparer.OrdinalIgnoreCase);
        }
    }
}