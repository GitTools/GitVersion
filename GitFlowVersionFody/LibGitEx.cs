using LibGit2Sharp;

public static class LibGitEx
{
	public static bool IsClean(this Repository repository)
	{
		var repositoryStatus = repository.Index.RetrieveStatus();
		return 
			repositoryStatus.Added.IsEmpty() &&
			repositoryStatus.Missing.IsEmpty() &&
			repositoryStatus.Modified.IsEmpty() &&
			repositoryStatus.Removed.IsEmpty() &&
			repositoryStatus.Staged.IsEmpty();	
	}
}