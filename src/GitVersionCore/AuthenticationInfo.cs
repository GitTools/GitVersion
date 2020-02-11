using LibGit2Sharp;

namespace GitVersion
{
    public class AuthenticationInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }

        public FetchOptions ToFetchOptions()
        {
            var fetchOptions = new FetchOptions();

            if (!string.IsNullOrEmpty(Username))
            {
                fetchOptions.CredentialsProvider = (url, user, types) => new UsernamePasswordCredentials
                {
                    Username = Username,
                    Password = Password
                };
            }

            return fetchOptions;
        }
    }
}
