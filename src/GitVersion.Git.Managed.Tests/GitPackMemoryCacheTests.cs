using NSubstitute;

namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitPackMemoryCacheTests
{
    [Test]
    public void ConcurrentAddsAndReadsPreserveIndependentViews()
    {
        using var cache = new GitPackMemoryCache();
        var sources = Enumerable.Range(0, 32)
            .Select(i => new MemoryStream(Encoding.UTF8.GetBytes($"object {i % 4}")))
            .ToArray();

        Parallel.For(0, sources.Length, i =>
        {
            var offset = i % 4;
            using var added = cache.Add(offset, sources[i], "blob");
            added.ReadByte().ShouldBe((int)'o');

            cache.TryOpen(offset, out var opened, out var objectType).ShouldBeTrue();
            objectType.ShouldBe("blob");
            using var reader = new StreamReader(opened.ShouldNotBeNull());
            reader.ReadToEnd().ShouldBe($"object {offset}");
            added.Position.ShouldBe(1);
        });

        cache.Dispose();
        sources.ShouldAllBe(source => !source.CanRead);
        cache.TryOpen(0, out var missing, out var missingType).ShouldBeFalse();
        missing.ShouldBeNull();
        missingType.ShouldBeNull();
    }

    [Test]
    public void ActiveViewKeepsSourceAliveAfterCacheDisposal()
    {
        using var cache = new GitPackMemoryCache();
        var source = new MemoryStream("cached content"u8.ToArray());
        using var view = cache.Add(42, source, "blob");

        cache.Dispose();

        source.CanRead.ShouldBeTrue();
        using var reader = new StreamReader(view);
        reader.ReadToEnd().ShouldBe("cached content");
        view.Dispose();
        source.CanRead.ShouldBeFalse();
    }

    [Test]
    public async Task FailedAddReleasesLockForAnotherThread()
    {
        using var cache = new GitPackMemoryCache();
        using var brokenSource = Substitute.For<Stream>();
        brokenSource.Length.Returns(_ => throw new IOException("Cannot read length."));

        Should.Throw<IOException>(() => cache.Add(1, brokenSource, "blob"));

        await Task.Run(() =>
        {
            using var view = cache.Add(2, new MemoryStream("recovered"u8.ToArray()), "blob");
            using var reader = new StreamReader(view);
            reader.ReadToEnd().ShouldBe("recovered");
            cache.TryOpen(1, out var missing, out var missingType).ShouldBeFalse();
            missing.ShouldBeNull();
            missingType.ShouldBeNull();
        }).WaitAsync(TimeSpan.FromSeconds(10));
    }
}
