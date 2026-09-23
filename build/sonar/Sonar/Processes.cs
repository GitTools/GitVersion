using System.Diagnostics;

namespace GitVersion.Sonar;

public static class Processes
{
    public static async Task<string> Run(string executable, IEnumerable<string> arguments, string directory, string? secret = null)
    {
        var start = new ProcessStartInfo(executable) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        // Do not pass Actions/1Password credentials, host tool overrides, or arbitrary runtime
        // startup hooks to processes that inspect PR data.
        var inherited = new[] { "PATH", "HOME", "DOTNET_ROOT", "JAVA_HOME", "LANG", "TMPDIR" }
            .ToDictionary(k => k, Environment.GetEnvironmentVariable);
        start.Environment.Clear();
        foreach (var pair in inherited.Where(p => p.Value is not null))
        {
            start.Environment[pair.Key] = pair.Value;
        }

        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "/dev/null";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_ROLL_FORWARD"] = "Major";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start trusted tool");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw new TimeoutException("Trusted tool timed out"); }
        var output = await stdout;
        var error = await stderr;
        if (!string.IsNullOrEmpty(secret))
        {
            output = output.Replace(secret, "***", StringComparison.Ordinal);
            error = error.Replace(secret, "***", StringComparison.Ordinal);
        }

        SafeFiles.Require(process.ExitCode == 0, $"{Path.GetFileName(executable)} failed ({process.ExitCode}): {output}{error}");
        return output;
    }

    public static async Task Materialize(RunIdentity identity, string repository)
    {
        SafeFiles.Require(!Directory.Exists(repository) || !Directory.EnumerateFileSystemEntries(repository).Any(), "Source destination must be empty");
        Directory.CreateDirectory(repository);
        async Task<string> Git(params string[] args) => await Run("/usr/bin/git", new[] { "-c", "core.hooksPath=/dev/null", "-c", "protocol.file.allow=never" }.Concat(args), repository);
        await Git("init", ".");
        await Git("remote", "add", "origin", "https://github.com/GitTools/GitVersion.git");
        await Git("fetch", "--no-tags", "origin", identity.AnalyzedSha, $"+refs/heads/{identity.BaseBranch}:refs/remotes/origin/{identity.BaseBranch}");
        await WriteTree(repository, identity.AnalyzedSha);
    }

    public static async Task WriteTree(string repository, string revision)
    {
        async Task<string> Git(params string[] args) => await Run("/usr/bin/git", new[] { "-c", "core.hooksPath=/dev/null", "-c", "protocol.file.allow=never" }.Concat(args), repository);
        // No checkout: write only regular git blobs. This runs no attributes, filters or hooks,
        // and excludes symlinks/submodules rather than allowing them to redirect later reads.
        var tree = await Git("ls-tree", "-rlz", "--full-tree", revision);
        var entries = tree.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        SafeFiles.Require(entries.Length <= SafeFiles.MaxFiles, "Too many source files");
        long total = 0;
        foreach (var entry in entries)
        {
            var separator = entry.IndexOf('\t');
            SafeFiles.Require(separator > 0, "Invalid git tree");
            var fields = entry[..separator].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields[0] == "120000" || fields[0] == "160000")
            {
                continue;
            }

            SafeFiles.Require(fields[0] is "100644" or "100755", "Unsupported source entry");
            var size = long.Parse(fields[3], System.Globalization.CultureInfo.InvariantCulture);
            total += size;
            SafeFiles.Require(size <= SafeFiles.MaxFileBytes && total <= SafeFiles.MaxTotalBytes, "Source tree exceeds limits");
            var relative = SafeFiles.Relative(entry[(separator + 1)..]);
            SafeFiles.Require(!relative.Split('/').Any(p => p.Equals(".git", StringComparison.OrdinalIgnoreCase) || p.Equals(".sonarqube", StringComparison.OrdinalIgnoreCase)), "Reserved source path");
            var target = SafeFiles.Resolve(repository, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            // Preserve binary files: git writes the blob directly via a trusted process.
            var start = new ProcessStartInfo("/usr/bin/git") { WorkingDirectory = repository, RedirectStandardOutput = true, RedirectStandardError = true };
            start.Environment.Clear();
            start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
            start.Environment["GIT_CONFIG_GLOBAL"] = "/dev/null";
            start.ArgumentList.Add("cat-file"); start.ArgumentList.Add("blob"); start.ArgumentList.Add(fields[2]);
            await using var output = new FileStream(target, FileMode.CreateNew);
            using var process = Process.Start(start)!;
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.StandardOutput.BaseStream.CopyToAsync(output, timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                await error;
            }
            catch
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }

                throw;
            }
            SafeFiles.Require(process.ExitCode == 0 && output.Length == size, "Unable to materialize source blob");
        }
        await Git("update-ref", "HEAD", revision);
        await Git("read-tree", revision);
    }
}
