namespace GitHub;

/// <summary>
/// Resolves the Payload's <c>gh</c> executable copied next to the app as <c>gh/bin/gh</c>.
/// </summary>
public static class Cli
{
    /// <summary>
    /// Returns the full path to <c>gh.exe</c> (Windows) or <c>gh</c> under
    /// <paramref name="baseDirectory"/>/<c>gh/bin</c>. Defaults to <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public static string ResolvePath(string? baseDirectory = null)
    {
        var dir = baseDirectory ?? AppContext.BaseDirectory;
        var name = OperatingSystem.IsWindows() ? "gh.exe" : "gh";
        var path = Path.GetFullPath(Path.Combine(dir, "gh", "bin", name));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"GitHub CLI payload was not found at '{path}'. PackageReference gh.cli and publish/pack for your RID.",
                path);
        }

        if (!OperatingSystem.IsWindows())
            EnsureUnixExecuteBits(path);

        return path;
    }

    // NuGet restore does not honor zip Unix modes; chmod on first resolve.
    internal static void EnsureUnixExecuteBits(string ghPath)
        => AddExecute(ghPath);

    static void AddExecute(string path)
    {
        if (OperatingSystem.IsWindows())
            return;

        var mode = File.GetUnixFileMode(path);
        const UnixFileMode exec = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        if ((mode & UnixFileMode.UserExecute) != 0)
            return;

        File.SetUnixFileMode(path, mode | exec);
    }
}
