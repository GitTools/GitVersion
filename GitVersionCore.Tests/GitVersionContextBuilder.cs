namespace GitVersionCore.Tests
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

        public GitVersionContextBuilder WithTaggedMaster()
        {
            repository = CreateRepository();
            var target = repository.Head.Tip;
            ((MockTagCollection)repository.Tags).Add(new MockTag ("1.0.0", target));
            ((MockBranch)repository.Head).Add(new MockCommit { CommitterEx = SignatureBuilder.SignatureNow() });
            return this;
        }

        public GitVersionContextBuilder AddCommit()
        {
            ((MockBranch)repository.Head).Add(new MockCommit());
            return this;
        }

        public GitVersionContextBuilder WithDevelopBranch()
        {
            return WithBranch("develop");
        }

        public GitVersionContextBuilder WithBranch(string branchName)
        {
            repository = CreateRepository();
            return AddBranch(branchName);
        }

        public GitVersionContextBuilder AddBranch(string branchName)
        {
            var mockBranch = new MockBranch(branchName)
            {
                new MockCommit()
            };
            ((MockBranchCollection)repository.Branches).Add(mockBranch);
            ((MockRepository)repository).Head = mockBranch;
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
                Tags = new MockTagCollection(),
                Head = mockBranch
            };

            return mockRepository;
        }
    }
}