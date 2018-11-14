using NUnit.Framework;

[SetUpFixture]
public class GlobalInitialization
{
    [OneTimeSetUp]
    public static void SetUp()
    {
#if DEBUG
        //LogManager.AddDebugListener(true);
#endif
    }
}