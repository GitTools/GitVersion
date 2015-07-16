using System;
using GitVersion;
using LibGit2Sharp;

public class CredentialSecuredRepositoryFixture : RepositoryFixtureBase
{
    private string _username;
    private string _password;

    public CredentialSecuredRepositoryFixture(Config configuration, string username, string password) :
        base(CreateNewRepository, configuration)
    {
        _username = username;
        _password = password;
    }

    static IRepository CreateNewRepository(string path)
    {
        LibGit2Sharp.Repository.Init(path,true);
        var repo = new Repository(path);
        Console.WriteLine("Created git repository at '{0}'", path);

        return new Repository(path);
    }
}