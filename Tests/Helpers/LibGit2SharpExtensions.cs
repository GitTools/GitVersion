using LibGit2Sharp;

public static class LibGit2SharpExtensions
{
    public static void ForceCheckout(this Branch branch)
    {
        branch.Checkout(new CheckoutOptions()
        {
            CheckoutModifiers = CheckoutModifiers.Force
        });
    }
}