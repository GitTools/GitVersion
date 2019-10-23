namespace GitVersion
{
    public class Authentication
    {
        public Authentication()
        {
            Username = System.Environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
            Password = System.Environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
        }
        public string Username;
        public string Password;
    }
}
