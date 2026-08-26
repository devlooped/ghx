namespace Tests;

public class CliTests
{
    [Fact]
    public void ResolvePath_finds_payload_under_gh_bin()
    {
        var root = Path.Combine(Path.GetTempPath(), "ghx-resolve-" + Guid.NewGuid().ToString("n"));
        var bin = Path.Combine(root, "gh", "bin");
        Directory.CreateDirectory(bin);
        var name = OperatingSystem.IsWindows() ? "gh.exe" : "gh";
        var expected = Path.Combine(bin, name);
        File.WriteAllText(expected, "fake");

        try
        {
            Assert.Equal(Path.GetFullPath(expected), GitHub.Cli.ResolvePath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolvePath_throws_when_missing()
    {
        var root = Path.Combine(Path.GetTempPath(), "ghx-missing-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(root);
        try
        {
            var ex = Assert.Throws<FileNotFoundException>(() => GitHub.Cli.ResolvePath(root));
            Assert.Contains("gh", ex.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("gh.cli", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolvePath_finds_project_payload_and_gh_version_matches_pin()
    {
        string path;
        try
        {
            path = GitHub.Cli.ResolvePath();
        }
        catch (FileNotFoundException)
        {
            return;
        }

        Assert.True(File.Exists(path), path);
        if (!OperatingSystem.IsWindows())
        {
            Assert.True(
                File.GetUnixFileMode(path).HasFlag(UnixFileMode.UserExecute),
                path);
        }
        var pin = File.ReadAllText(Path.Combine(FindRepoRoot(), "github-cli.version")).Trim();
        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(path),
        };
        start.ArgumentList.Add("--version");

        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start gh.");
        var output = process.StandardOutput.ReadToEnd();
        Assert.True(process.WaitForExit(60_000));
        Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
        Assert.Contains(pin, output, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvePath_sets_unix_execute_bits()
    {
        if (OperatingSystem.IsWindows())
            return;

        var root = Path.Combine(Path.GetTempPath(), "ghx-chmod-" + Guid.NewGuid().ToString("n"));
        var gh = Path.Combine(root, "gh", "bin", "gh");
        Directory.CreateDirectory(Path.GetDirectoryName(gh)!);
        File.WriteAllText(gh, "#!/bin/sh\n");
        var readWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite |
                        UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        File.SetUnixFileMode(gh, readWrite);

        try
        {
            Assert.Equal(Path.GetFullPath(gh), GitHub.Cli.ResolvePath(root));
            Assert.True(File.GetUnixFileMode(gh).HasFlag(UnixFileMode.UserExecute), gh);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ghx.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find ghx.slnx from " + AppContext.BaseDirectory);
    }
}
