using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    public class MockTagAnnotation : TagAnnotation
    {

        public Signature TaggerEx;
        public override Signature Tagger => TaggerEx;

        public GitObject TargetEx;
        public override GitObject Target => TargetEx;
    }
}