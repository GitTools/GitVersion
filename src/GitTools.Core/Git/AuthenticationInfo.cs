namespace GitTools.Git
{
    using LibGit2Sharp;

    public class AuthenticationInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
    }
}