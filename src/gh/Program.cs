using System.Diagnostics;
using System.Reflection;

static class Program
{
    static int Main(string[] args)
    {
        var gh = GitHub.Cli.ResolvePath();
        if (args is ["--version"])
        {
            var version = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "0.0.0";
            Console.WriteLine("gh " + version);
        }

        var start = new ProcessStartInfo
        {
            FileName = gh,
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Failed to start '{gh}'.");
        process.WaitForExit();
        return process.ExitCode;
    }
}
