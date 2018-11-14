namespace GitTools.Testing.Tests
{
    using NUnit.Framework;

    public class Example
    {
        [Test]
        public void TheReadmeSample()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.MakeACommit();
                fixture.MakeATaggedCommit("1.0.0");
                fixture.BranchTo("develop");
                fixture.MakeACommit();
                fixture.Checkout("master");
                fixture.MergeNoFF("develop");
            }
        }
    }
}