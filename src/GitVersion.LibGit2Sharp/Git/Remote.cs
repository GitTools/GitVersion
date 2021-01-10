using System.Linq;

namespace GitVersion
{
    public class Remote : IRemote
    {
        private readonly LibGit2Sharp.Remote innerRemote;

        internal Remote(LibGit2Sharp.Remote remote)
        {
            innerRemote = remote;
        }

        protected Remote()
        {
        }
        public virtual string Name => innerRemote.Name;
        public virtual string RefSpecs => string.Join(", ", innerRemote.FetchRefSpecs.Select(r => r.Specification));
    }
}
