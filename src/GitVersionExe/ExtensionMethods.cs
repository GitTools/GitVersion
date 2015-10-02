namespace GitVersion
{
    static class ExtensionMethods
    {
        public static bool IsOdd(this int number)
        {
            return number % 2 != 0;
        }
    }
}