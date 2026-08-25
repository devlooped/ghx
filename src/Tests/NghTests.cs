using System.Diagnostics;

namespace Tests;

public class NghTests
{
    [Fact]
    public void Lone_version_prints_gh_wrapper_and_payload()
    {
        var tool = FindDotnetGh();
        if (tool is null)
            return;

        var pin = File.ReadAllText(Path.Combine(FindRepoRoot(), "github-cli.version")).Trim();
        var (exit, stdout, stderr) = Run(tool, "--version");
        Assert.True(exit == 0, stderr);
        var first = stdout.Split(["\r\n", "\n"], StringSplitOptions.None)[0];
        Assert.StartsWith("gh ", first, StringComparison.Ordinal);
        Assert.Contains("gh version", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(pin, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Other_args_are_passthrough()
    {
        var tool = FindDotnetGh();
        if (tool is null)
            return;

        var (exit, stdout, stderr) = Run(tool, "version");
        Assert.True(exit == 0, stderr);
        Assert.StartsWith("gh version", stdout.Trim(), StringComparison.OrdinalIgnoreCase);
        var pin = File.ReadAllText(Path.Combine(FindRepoRoot(), "github-cli.version")).Trim();
        Assert.Contains(pin, stdout, StringComparison.Ordinal);
    }

    static (int Exit, string Stdout, string Stderr) Run(string tool, params string[] args)
    {
        var start = new ProcessStartInfo(tool)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(tool),
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start dotnet-gh.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(60_000));
        return (process.ExitCode, stdout, stderr);
    }

    static string? FindDotnetGh()
    {
        var dir = AppContext.BaseDirectory;
        var name = OperatingSystem.IsWindows() ? "dotnet-gh.exe" : "dotnet-gh";
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
