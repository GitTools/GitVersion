using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using GitVersion.Helpers;

using LibGit2Sharp;

using NSubstitute;

public class TestFileSystem : IFileSystem
{
    Dictionary<string, string> fileSystem = new Dictionary<string, string>();


    public void Copy(string @from, string to, bool overwrite)
    {
        throw new NotImplementedException();
    }


    public void Move(string @from, string to)
    {
        throw new NotImplementedException();
    }


    public bool Exists(string file)
    {
        return this.fileSystem.ContainsKey(file);
    }


    public void Delete(string path)
    {
        throw new NotImplementedException();
    }


    public string ReadAllText(string path)
    {
        return this.fileSystem[path];
    }


    public void WriteAllText(string file, string fileContents)
    {
        if (this.fileSystem.ContainsKey(file))
        {
            this.fileSystem[file] = fileContents;
        }
        else
        {
            this.fileSystem.Add(file, fileContents);
        }
    }


    public IEnumerable<string> DirectoryGetFiles(string directory, string searchPattern, SearchOption searchOption)
    {
        throw new NotImplementedException();
    }


    public Stream OpenWrite(string path)
    {
        return new TestStream(path, this);
    }


    public Stream OpenRead(string path)
    {
        if (this.fileSystem.ContainsKey(path))
        {
            var content = this.fileSystem[path];
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        throw new FileNotFoundException("File not found.", path);
    }


    public void CreateDirectory(string path)
    {
    }


    public long GetLastDirectoryWrite(string path)
    {
        return 1;
    }


    public IRepository GetRepository(string gitDirectory)
    {
        var repository = Substitute.For<IRepository>();
        var tip = Substitute.For<Commit>();
        tip.Committer.Returns(new Signature("Asbjørn Ulsberg", "asbjorn@ulsberg.no", new DateTimeOffset(2015, 11, 10, 13, 37, 0, TimeSpan.FromHours(1))));
        var commits = new Commits
        {
            tip
        };
        repository.Commits.QueryBy(null).ReturnsForAnyArgs(commits);
        var head = Substitute.For<Branch>();
        head.CanonicalName.Returns("refs/heads/master");
        tip.Sha.Returns("e7da1b19d03394896fb8da8916cd26f0efb1566f");
        head.Tip.Returns(tip);
        repository.Head.Returns(head);
        var branches = new Branches
        {
            { "master", tip }
        };

        repository.Branches.Returns(branches);
        return repository;
    }


    class Branches : BranchCollection
    {
        IList<Branch> branches;


        public Branches()
        {
            this.branches = new List<Branch>();
        }


        public override Branch this[string name]
        {
            get { return this.branches.FirstOrDefault(b => b.Name == name); }
        }


        public override Branch Add(string name, Commit commit, bool allowOverwrite = false)
        {
            var branch = Substitute.For<Branch>();
            branch.Name.Returns(name);
            branch.Tip.Returns(commit);
            this.branches.Add(branch);
            return branch;
        }


        public override Branch Add(string name, Commit commit, Signature signature, string logMessage = null, bool allowOverwrite = false)
        {
            return Add(name, commit, allowOverwrite);
        }


        public override IEnumerator<Branch> GetEnumerator()
        {
            return this.branches.GetEnumerator();
        }
    }

    class Commits : List<Commit>, IQueryableCommitLog
    {
        public CommitSortStrategies SortedBy { get; private set; }


        public ICommitLog QueryBy(CommitFilter filter)
        {
            throw new NotImplementedException();
        }


        public Commit FindMergeBase(Commit first, Commit second)
        {
            throw new NotImplementedException();
        }


        public Commit FindMergeBase(IEnumerable<Commit> commits, MergeBaseFindingStrategy strategy)
        {
            throw new NotImplementedException();
        }
    }
}