using System.Diagnostics;

namespace Tests;

public class GhxTests
{
    [Fact]
    public void Lone_version_prints_ghx_and_gh()
    {
        var ghx = FindGhx();
        if (ghx is null)
            return;

        var pin = File.ReadAllText(Path.Combine(FindRepoRoot(), "github-cli.version")).Trim();
        var (exit, stdout, stderr) = Run(ghx, "--version");
        Assert.True(exit == 0, stderr);
        Assert.Contains("ghx ", stdout, StringComparison.Ordinal);
        Assert.Contains("gh version", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(pin, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Other_args_are_passthrough()
    {
        var ghx = FindGhx();
        if (ghx is null)
            return;

        var (exit, stdout, stderr) = Run(ghx, "version");
        Assert.True(exit == 0, stderr);
        Assert.DoesNotContain("ghx ", stdout, StringComparison.Ordinal);
        var pin = File.ReadAllText(Path.Combine(FindRepoRoot(), "github-cli.version")).Trim();
        Assert.Contains(pin, stdout, StringComparison.Ordinal);
    }

    static (int Exit, string Stdout, string Stderr) Run(string ghx, params string[] args)
    {
        var start = new ProcessStartInfo(ghx)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(ghx),
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start ghx.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(60_000));
        return (process.ExitCode, stdout, stderr);
    }

    static string? FindGhx()
    {
        var dir = AppContext.BaseDirectory;
        var name = OperatingSystem.IsWindows() ? "ghx.exe" : "ghx";
        var path = Path.Combine(dir, name);
        return File.Exists(path) ? path : null;
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
