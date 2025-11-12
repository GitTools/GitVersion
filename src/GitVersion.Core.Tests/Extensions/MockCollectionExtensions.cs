namespace GitVersion.Core.Tests;

public static class MockCollectionExtensions
{
    private static IEnumerator<T> GetEnumerator<T>(params T[] itemsToReturn)
    {
        foreach (var item in itemsToReturn)
        {
            yield return item;
        }
    }

    extension<T>(IEnumerable<T> items)
    {
        public void MockCollectionReturn(params T[] itemsToReturn)
        {
            var enumerator = items.GetEnumerator();
            enumerator.Returns(_ => GetEnumerator(itemsToReturn));
            enumerator.Dispose();
        }
    }
}
