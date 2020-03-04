using System;
using System.Reflection;

namespace App
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write(Assembly.GetEntryAssembly().GetType("GitVersionInformation").GetField("FullSemVer").GetValue(null));
        }
    }
}
