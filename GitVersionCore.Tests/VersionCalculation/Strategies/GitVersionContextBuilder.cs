namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using GitVersion;
    using LibGit2Sharp;

    public class GitVersionContextBuilder
    {
        IRepository repository;
        Config config;

        public GitVersionContextBuilder WithRepository(IRepository repository)
        {
            this.repository = repository;
            return this;
        }

        public GitVersionContextBuilder WithConfig(Config config)
        {
            this.config = config;
            return this;
        }

        public GitVersionContext Build()
        {
            return new GitVersionContext(repository ?? CreateRepository(), config ?? new Config());
        }

        IRepository CreateRepository()
        {
            var mockBranch = new MockBranch("master") { new MockCommit { CommitterEx = SignatureBuilder.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    mockBranch
                },
                Head = mockBranch
            };

            return mockRepository;
        }
    }
}