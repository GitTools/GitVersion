namespace GitVersion.Core.Tests.Extensions;

public static class ShouldlyExtensions
{
    extension(Action action)
    {
        /// <summary>
        /// Asserts that the action throws an exception of type TException
        /// with the expected message.
        /// </summary>
        public void ShouldThrowWithMessage<TException>(string expectedMessage) where TException : Exception
        {
            var ex = Should.Throw<TException>(action);
            ex.Message.ShouldBe(expectedMessage);
        }

        /// <summary>
        /// Asserts that the action throws an exception of type TException,
        /// and allows further assertion on the exception instance.
        /// </summary>
        public void ShouldThrow<TException>(Action<TException> additionalAssertions) where TException : Exception
        {
            var ex = Should.Throw<TException>(action);
            additionalAssertions(ex);
        }
    }
}
