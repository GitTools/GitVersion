namespace GitTools.Git
{
    using LibGit2Sharp;
    using Logging;

    public static class AuthenticationInfoExtensions
    {
        static readonly ILog Log = LogProvider.GetLogger(typeof(AuthenticationInfoExtensions));

        public static FetchOptions ToFetchOptions(this AuthenticationInfo authenticationInfo)
        {
            var fetchOptions = new FetchOptions();

            if (authenticationInfo != null)
            {
                if (!string.IsNullOrEmpty(authenticationInfo.Username))
                {
                    fetchOptions.CredentialsProvider = (url, user, types) => new UsernamePasswordCredentials
                    {
                        Username = authenticationInfo.Username,
                        Password = authenticationInfo.Password
                    };
                }
            }

            return fetchOptions;
        }

        public static bool IsEmpty(this AuthenticationInfo authenticationInfo)
        {
            if (authenticationInfo == null)
            {
                return true;
            }

            if (IsUsernameAndPasswordAuthentication(authenticationInfo))
            {
                return false;
            }

            if (IsTokenAuthentication(authenticationInfo))
            {
                return false;
            }

            return true;
        }

        public static bool IsUsernameAndPasswordAuthentication(this AuthenticationInfo authenticationInfo)
        {
            if (authenticationInfo == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(authenticationInfo.Username))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(authenticationInfo.Password))
            {
                return false;
            }

            return true;
        }

        public static bool IsTokenAuthentication(this AuthenticationInfo authenticationInfo)
        {
            if (authenticationInfo == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(authenticationInfo.Token))
            {
                return false;
            }

            return true;
        }
    }
}
