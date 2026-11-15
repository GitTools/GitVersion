using System;
using NUnit.Framework;
using Probe;

namespace GitVersion.MtpForwarding.Tests;

// Compiled only by the isolated CI probe, never included in the product test suite.
public class ForwardingTests
{
    [Test]
    public void PassesAddition() => Assert.That(ProbeMath.Add(2, 2), Is.EqualTo(4));

    [Test]
    public void ExpectedFailure()
    {
        var marker = Environment.GetEnvironmentVariable("MTP_PROBE_MARKER");
        // Intentional failure: the probe requires exit 2 and verifies GitHub's stored annotation.
        Assert.That(ProbeMath.Add(2, 2), Is.EqualTo(5), marker);
    }
}
