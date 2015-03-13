using LibGit2Sharp;

public static class LibGit2SharpExtensions
{
    public static void ForceCheckout(this IRepository repository, Branch branch)
    {
        repository.Checkout(branch, new CheckoutOptions
        {
            CheckoutModifiers = CheckoutModifiers.Force
        });
    }
}