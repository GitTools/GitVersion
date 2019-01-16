namespace GitTools.Git
{
    public class RepositoryInfo
    {
        public RepositoryInfo()
        {
            Authentication = new AuthenticationInfo();
        }

        public AuthenticationInfo Authentication { get; set; }
        public string Url { get; set; }
    }
}