using LibGit2Sharp;

namespace Tests.Helpers
{
    public static class LibGit2SharpExtensions
    {
        public static void ForceCheckout(this Branch branch)
        {
            branch.Checkout(CheckoutModifiers.Force, null, null);
        }
    }
}
