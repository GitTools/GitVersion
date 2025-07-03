namespace GitVersion.Core.Tests.Extensions;

public static class ShouldlyExtensions
{
    /// <summary>
    /// Asserts that the action throws an exception of type TException
    /// with the expected message.
    /// </summary>
    public static void ShouldThrowWithMessage<TException>(this Action action, string expectedMessage) where TException : Exception
    {
        var ex = Should.Throw<TException>(action);
        ex.Message.ShouldBe(expectedMessage);
    }

    /// <summary>
    /// Asserts that the action throws an exception of type TException,
    /// and allows further assertion on the exception instance.
    /// </summary>
    public static void ShouldThrow<TException>(this Action action, Action<TException> additionalAssertions) where TException : Exception
    {
        var ex = Should.Throw<TException>(action);
        additionalAssertions(ex);
    }
}
