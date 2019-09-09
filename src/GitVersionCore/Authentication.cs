using System;

namespace GitVersion
{
    public class Authentication
    {
        public Authentication()
        {
            Username = Environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
            Password = Environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
        }
        public string Username;
        public string Password;
    }
}
