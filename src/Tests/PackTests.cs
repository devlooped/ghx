using System.IO.Compression;
using System.Text.Json;

namespace Tests;

public class PackTests
{
    static readonly string[] SupportedRids =
    [
        "win-x64",
        "linux-x64",
        "linux-arm64",
        "osx-x64",
        "osx-arm64",
    ];

    [Fact]
    public void Pointer_and_rid_csproj_use_pack_split()
    {
        var repo = FindRepoRoot();
        var slnx = File.ReadAllText(Path.Combine(repo, "ghx.slnx"));
        Assert.Contains("src/GitHub.Cli/GitHub.Cli.csproj", slnx);
        Assert.Contains("src/ghx/ghx.csproj", slnx);

        var githubCli = File.ReadAllText(Path.Combine(repo, "src", "GitHub.Cli", "GitHub.Cli.csproj"));
        Assert.Contains("<PackageId>GitHub.Cli</PackageId>", githubCli);
        Assert.Contains("<RootNamespace>GitHub</RootNamespace>", githubCli);
        Assert.Contains("GitHub.Cli.ResolvePath", githubCli);
        Assert.Contains("<RuntimeIdentifiers>win-x64;linux-x64;linux-arm64;osx-x64;osx-arm64</RuntimeIdentifiers>", githubCli);
        Assert.DoesNotContain("NuGetizer", githubCli, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<PackAsTool", githubCli);
        Assert.Contains("GitHub.Cli.pack.targets", githubCli);
        Assert.Contains("buildTransitive\\GitHub.Cli.targets", githubCli.Replace('/', '\\'));
        Assert.Contains("Readme", githubCli);
        Assert.DoesNotContain("win-arm64", githubCli);

        var payload = File.ReadAllText(Path.Combine(repo, "src", "GitHub.Cli", "payload.ps1"));
        Assert.DoesNotMatch(@"(?im)^\s*\$is(Linux|Windows|MacOS)\s*=", payload);
        Assert.Contains("payload.functions.ps1", payload);
        Assert.Contains("cli/cli/releases/download/v", payload);
        Assert.Contains("Get-GhAssetName", payload);
        Assert.Contains("Publish-GhPayload", payload);
        Assert.Contains("chmod +x", payload);
        Assert.DoesNotContain("pip", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("python-build-standalone", payload);
        Assert.DoesNotContain("Repair-CaseCollisions", payload);

        var unixExec = File.ReadAllText(Path.Combine(repo, "src", "GitHub.Cli", "unix-exec.ps1"));
        Assert.Contains("Test-UnixExecuteEntry", unixExec);
        Assert.Contains("Set-NupkgUnixExecuteBits", unixExec);
        Assert.Contains("Assert-NupkgUnixExecuteBits", unixExec);
        Assert.Contains("Expand-NupkgWithUnixModes", unixExec);
        Assert.Contains("gh/bin/gh", unixExec);
        Assert.Contains("ghx", unixExec);

        var directoryTargets = File.ReadAllText(Path.Combine(repo, "src", "Directory.Build.targets"));
        Assert.Contains("StampUnixExecuteBitsOnNupkg", directoryTargets);
        Assert.Contains("unix-exec.ps1", directoryTargets);
        var buildYml = File.ReadAllText(Path.Combine(repo, ".github", "workflows", "build.yml"));
        Assert.Contains("unix-exec.ps1", buildYml);
        Assert.Contains("-Assert", buildYml);

        var packTargets = File.ReadAllText(Path.Combine(repo, "src", "GitHub.Cli", "GitHub.Cli.pack.targets"));
        Assert.Contains("WriteGitHubCliRuntimeJson", packTargets);
        Assert.Contains("PackGitHubCliPayload", packTargets);
        Assert.Contains("payload.functions.ps1", packTargets);
        Assert.Contains("$(GitHubCliPackageId).$(RuntimeIdentifier)", packTargets);
        Assert.DoesNotContain("runtimes/$(RuntimeIdentifier)/native/", packTargets);

        var consumer = File.ReadAllText(Path.Combine(repo, "src", "GitHub.Cli", "buildTransitive", "GitHub.Cli.targets"));
        Assert.Contains("IncludeGitHubCliPayload", consumer);
        Assert.Contains(@"TargetPath>gh\", consumer.Replace('/', '\\'));
        Assert.DoesNotContain("runtimes/$(RuntimeIdentifier)/native/", consumer);

        var ghx = File.ReadAllText(Path.Combine(repo, "src", "ghx", "ghx.csproj"));
        Assert.Contains("<PackageId>ghx</PackageId>", ghx);
        Assert.Contains("<PackAsTool>true</PackAsTool>", ghx);
        Assert.Contains("<PublishAot>true</PublishAot>", ghx);
        Assert.Contains("<ToolCommandName>ghx</ToolCommandName>", ghx);
        Assert.Contains("<ToolPackageRuntimeIdentifiers>win-x64;linux-x64;linux-arm64;osx-x64;osx-arm64</ToolPackageRuntimeIdentifiers>", ghx);
        Assert.Contains("""<PackageReference Include="GitHub.Cli" Version="$(Version)" />""", ghx);
        Assert.DoesNotContain("NuGetizer", ghx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Readme", ghx);
        var nuget = File.ReadAllText(Path.Combine(repo, "src", "ghx", "nuget.config"));
        Assert.Contains("key=\"local\"", nuget);
        Assert.Contains("../../bin", nuget);
        Assert.Contains("GitHub.Cli", nuget);
        Assert.DoesNotContain("win-arm64", ghx);
        Assert.DoesNotContain("ghx.$(RuntimeIdentifier)", ghx);

        Assert.False(File.Exists(Path.Combine(repo, "src", "GitHub.Cli", "runtime.json")));
        foreach (var rid in SupportedRids)
        {
            Assert.Contains(rid, githubCli);
            Assert.Contains(rid, ghx);
        }
    }

    [Fact]
    public void WriteGitHubCliRuntimeJson_maps_five_rids()
    {
        var repo = FindRepoRoot();
        var project = Path.Combine(repo, "src", "GitHub.Cli", "GitHub.Cli.csproj");
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        var start = new System.Diagnostics.ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("msbuild");
        start.ArgumentList.Add(project);
        start.ArgumentList.Add("-t:WriteGitHubCliRuntimeJson");
        start.ArgumentList.Add("-p:Configuration=" + configuration);
        start.ArgumentList.Add("-p:DesignTimeBuild=true");
        start.ArgumentList.Add("-p:GeneratePackageOnBuild=false");
        start.ArgumentList.Add("-nologo");

        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start dotnet msbuild.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(60_000));
        Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr);

        var runtimeJson = Path.Combine(repo, "src", "GitHub.Cli", "obj", configuration, "net10.0", "runtime.json");
        Assert.True(File.Exists(runtimeJson), runtimeJson);
        using var doc = JsonDocument.Parse(File.ReadAllText(runtimeJson));
        var runtimes = doc.RootElement.GetProperty("runtimes");
        Assert.Equal(SupportedRids.Length, runtimes.EnumerateObject().Count());
        foreach (var rid in SupportedRids)
        {
            var range = runtimes
                .GetProperty(rid)
                .GetProperty("GitHub.Cli")
                .GetProperty("GitHub.Cli." + rid)
                .GetString();
            Assert.False(string.IsNullOrWhiteSpace(range));
            Assert.StartsWith("[", range);
            Assert.EndsWith(", )", range);
        }

        Assert.False(runtimes.TryGetProperty("win-arm64", out _));
    }

    [Fact]
    public void Packed_nupkgs_have_pointer_and_rid_layout()
    {
        var bin = Path.Combine(FindRepoRoot(), "bin");
        if (!Directory.Exists(bin))
            return;

        var nupkgs = Directory.GetFiles(bin, "GitHub.Cli*.nupkg")
            .Where(f => !f.Contains(".symbols.", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (nupkgs.Length == 0)
            return;

        var pointer = nupkgs.FirstOrDefault(f =>
            !SupportedRids.Any(r => Path.GetFileName(f).Contains("." + r + ".", StringComparison.Ordinal)));
        if (pointer is not null)
        {
            var names = ZipNames(pointer);
            Assert.Contains(names, n => n == "runtime.json" || n == "runtime.json/");
            Assert.Contains(names, n => n.Replace('\\', '/').StartsWith("lib/", StringComparison.Ordinal));
            Assert.Contains(names, n => n.Replace('\\', '/').Contains("buildTransitive/GitHub.Cli.targets", StringComparison.Ordinal));
            Assert.DoesNotContain(names, n => n.Replace('\\', '/').StartsWith("gh/", StringComparison.Ordinal));
        }

        var ridPkg = nupkgs.FirstOrDefault(f =>
            SupportedRids.Any(r => Path.GetFileName(f).Contains("." + r + ".", StringComparison.Ordinal)));
        if (ridPkg is not null)
        {
            var names = ZipNames(ridPkg);
            Assert.Contains(names, n => n.Replace('\\', '/').StartsWith("gh/bin/", StringComparison.Ordinal));
            Assert.DoesNotContain(names, n => n.Replace('\\', '/').StartsWith("lib/", StringComparison.Ordinal));
            Assert.DoesNotContain(names, n => n.Replace('\\', '/').Contains("buildTransitive/", StringComparison.Ordinal));
            var fileName = Path.GetFileName(ridPkg);
            if (!fileName.Contains(".win-", StringComparison.Ordinal))
                AssertUnixExecuteBits(ridPkg);
        }

        foreach (var ghxRid in Directory.GetFiles(bin, "ghx.*.nupkg")
            .Where(f => !f.Contains(".symbols.", StringComparison.OrdinalIgnoreCase)
                && SupportedRids.Any(r => Path.GetFileName(f).Contains("." + r + ".", StringComparison.Ordinal)
                    && !Path.GetFileName(f).Contains(".win-", StringComparison.Ordinal))))
        {
            AssertUnixExecuteBits(ghxRid);
        }
    }

    [Fact]
    public void Unix_exec_script_stamps_and_asserts_zip_modes()
    {
        var repo = FindRepoRoot();
        var script = Path.Combine(repo, "src", "GitHub.Cli", "unix-exec.ps1");
        var scratch = Path.Combine(Path.GetTempPath(), "ghx-unix-exec-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(scratch);
        try
        {
            var nupkg = Path.Combine(scratch, "ghx.linux-x64.1.0.0.nupkg");
            using (var archive = ZipFile.Open(nupkg, ZipArchiveMode.Create))
            {
                archive.CreateEntry("tools/any/linux-x64/gh/bin/gh");
                archive.CreateEntry("tools/any/linux-x64/ghx");
                archive.CreateEntry("tools/any/linux-x64/gh/readme.txt");
            }

            using (var zip = ZipFile.OpenRead(nupkg))
            {
                Assert.False(HasUnixExecute(zip.GetEntry("tools/any/linux-x64/gh/bin/gh")!.ExternalAttributes));
            }

            RunPwsh(repo, $"""
                $ErrorActionPreference = 'Stop'
                Set-StrictMode -Version Latest
                . '{script.Replace("'", "''", StringComparison.Ordinal)}'
                Set-NupkgUnixExecuteBits '{nupkg.Replace("'", "''", StringComparison.Ordinal)}'
                Assert-NupkgUnixExecuteBits '{nupkg.Replace("'", "''", StringComparison.Ordinal)}'
                """);

            using (var zip = ZipFile.OpenRead(nupkg))
            {
                Assert.True(HasUnixExecute(zip.GetEntry("tools/any/linux-x64/gh/bin/gh")!.ExternalAttributes));
                Assert.True(HasUnixExecute(zip.GetEntry("tools/any/linux-x64/ghx")!.ExternalAttributes));
                Assert.False(HasUnixExecute(zip.GetEntry("tools/any/linux-x64/gh/readme.txt")!.ExternalAttributes));
            }

            var dest = Path.Combine(scratch, "out");
            RunPwsh(repo, $"""
                $ErrorActionPreference = 'Stop'
                Set-StrictMode -Version Latest
                . '{script.Replace("'", "''", StringComparison.Ordinal)}'
                Expand-NupkgWithUnixModes '{nupkg.Replace("'", "''", StringComparison.Ordinal)}' '{dest.Replace("'", "''", StringComparison.Ordinal)}'
                """);
            var extractedGh = Path.Combine(dest, "tools", "any", "linux-x64", "gh", "bin", "gh");
            var extractedHost = Path.Combine(dest, "tools", "any", "linux-x64", "ghx");
            Assert.True(File.Exists(extractedGh), extractedGh);
            Assert.True(File.Exists(extractedHost), extractedHost);
            if (!OperatingSystem.IsWindows())
            {
                Assert.True(File.GetUnixFileMode(extractedGh).HasFlag(UnixFileMode.UserExecute), extractedGh);
                Assert.True(File.GetUnixFileMode(extractedHost).HasFlag(UnixFileMode.UserExecute), extractedHost);
            }
        }
        finally
        {
            if (Directory.Exists(scratch))
                Directory.Delete(scratch, recursive: true);
        }
    }

    [Fact]
    public void GitHub_cli_version_pin_is_semver()
    {
        var pin = File.ReadAllText(Path.Combine(FindRepoRoot(), "github-cli.version")).Trim();
        Assert.Matches(@"^\d+\.\d+\.\d+$", pin);
    }

    [Fact]
    public void Payload_downloads_official_cli_cli_release_archives()
    {
        var repo = FindRepoRoot();
        var functions = File.ReadAllText(Path.Combine(repo, "src", "GitHub.Cli", "payload.functions.ps1"));
        Assert.Contains("gh_${Version}_windows_amd64.zip", functions);
        Assert.Contains("gh_${Version}_linux_amd64.tar.gz", functions);
        Assert.Contains("gh_${Version}_linux_arm64.tar.gz", functions);
        Assert.Contains("gh_${Version}_macOS_amd64.zip", functions);
        Assert.Contains("gh_${Version}_macOS_arm64.zip", functions);
        Assert.Contains("Expand-TarGz", functions);
        Assert.Contains("Expand-Zip", functions);
        Assert.Contains("Expand-Archive", functions);
        Assert.DoesNotContain("tar -xf $zip", functions);
        Assert.DoesNotContain("Get-SourcesRoot", functions);

        var payload = File.ReadAllText(Path.Combine(repo, "src", "GitHub.Cli", "payload.ps1"));
        Assert.Contains("https://github.com/cli/cli/releases/download/v$GitHubCliVersion/$asset", payload);
        Assert.Contains("Expand-Zip $archive $stage", payload);
        Assert.Contains("Expand-TarGz $archive $stage", payload);
        Assert.DoesNotContain("tar -xf $zip", payload);
        Assert.DoesNotContain("pip", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("azure-cli", payload);
    }

    [Fact]
    public void GetGhAssetName_maps_five_rids_to_official_archives()
    {
        var repo = FindRepoRoot();
        var pin = File.ReadAllText(Path.Combine(repo, "github-cli.version")).Trim();
        var functions = Path.Combine(repo, "src", "GitHub.Cli", "payload.functions.ps1");
        var start = new System.Diagnostics.ProcessStartInfo("pwsh")
        {
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-Command");
        var functionsLit = functions.Replace("'", "''", StringComparison.Ordinal);
        var pinLit = pin.Replace("'", "''", StringComparison.Ordinal);
        start.ArgumentList.Add($$"""
            $ErrorActionPreference = 'Stop'
            Set-StrictMode -Version Latest
            . '{{functionsLit}}'
            foreach ($rid in @('win-x64','linux-x64','linux-arm64','osx-x64','osx-arm64')) {
                Write-Output "$rid=$(Get-GhAssetName $rid '{{pinLit}}')"
            }
            """);

        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(60_000), stdout + Environment.NewLine + stderr);
        Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr);

        Assert.Contains($"win-x64=gh_{pin}_windows_amd64.zip", stdout, StringComparison.Ordinal);
        Assert.Contains($"linux-x64=gh_{pin}_linux_amd64.tar.gz", stdout, StringComparison.Ordinal);
        Assert.Contains($"linux-arm64=gh_{pin}_linux_arm64.tar.gz", stdout, StringComparison.Ordinal);
        Assert.Contains($"osx-x64=gh_{pin}_macOS_amd64.zip", stdout, StringComparison.Ordinal);
        Assert.Contains($"osx-arm64=gh_{pin}_macOS_arm64.zip", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void PublishGhPayload_hoists_version_prefixed_layout()
    {
        var repo = FindRepoRoot();
        var functions = Path.Combine(repo, "src", "GitHub.Cli", "payload.functions.ps1");
        var scratch = Path.Combine(Path.GetTempPath(), "ghx-hoist-" + Guid.NewGuid().ToString("n"));
        var extract = Path.Combine(scratch, "extract");
        var dest = Path.Combine(scratch, "out");
        Directory.CreateDirectory(Path.Combine(extract, "gh_nested_linux_amd64", "bin"));
        File.WriteAllText(Path.Combine(extract, "gh_nested_linux_amd64", "bin", "gh"), "payload");
        File.WriteAllText(Path.Combine(extract, "gh_nested_linux_amd64", "LICENSE"), "mit");
        Directory.CreateDirectory(dest);
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo("pwsh")
            {
                WorkingDirectory = repo,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-Command");
            var functionsLit = functions.Replace("'", "''", StringComparison.Ordinal);
            var extractLit = extract.Replace("'", "''", StringComparison.Ordinal);
            var destLit = dest.Replace("'", "''", StringComparison.Ordinal);
            start.ArgumentList.Add($$"""
                $ErrorActionPreference = 'Stop'
                Set-StrictMode -Version Latest
                . '{{functionsLit}}'
                Publish-GhPayload '{{extractLit}}' '{{destLit}}' 'gh'
                """);

            using var process = System.Diagnostics.Process.Start(start)
                ?? throw new InvalidOperationException("Failed to start pwsh.");
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            Assert.True(process.WaitForExit(60_000), stdout + Environment.NewLine + stderr);
            Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr);
            Assert.True(File.Exists(Path.Combine(dest, "bin", "gh")), dest + Environment.NewLine + stdout + stderr);
            Assert.True(File.Exists(Path.Combine(dest, "LICENSE")), dest);
            Assert.False(Directory.Exists(Path.Combine(dest, "gh_nested_linux_amd64")));
        }
        finally
        {
            if (Directory.Exists(scratch))
                Directory.Delete(scratch, recursive: true);
        }
    }

    [Fact]
    public void ExpandZip_extracts_zip_without_gnu_tar()
    {
        var repo = FindRepoRoot();
        var functions = Path.Combine(repo, "src", "GitHub.Cli", "payload.functions.ps1");
        var scratch = Path.Combine(Path.GetTempPath(), "ghx-expand-zip-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(scratch);
        try
        {
            var zip = Path.Combine(scratch, "payload.zip");
            var dest = Path.Combine(scratch, "out");
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("bin/gh.exe");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("fake");
            }

            var start = new System.Diagnostics.ProcessStartInfo("pwsh")
            {
                WorkingDirectory = repo,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-Command");
            var functionsLit = functions.Replace("'", "''", StringComparison.Ordinal);
            var zipLit = zip.Replace("'", "''", StringComparison.Ordinal);
            var destLit = dest.Replace("'", "''", StringComparison.Ordinal);
            start.ArgumentList.Add($$"""
                $ErrorActionPreference = 'Stop'
                Set-StrictMode -Version Latest
                . '{{functionsLit}}'
                Expand-Zip '{{zipLit}}' '{{destLit}}'
                """);

            using var process = System.Diagnostics.Process.Start(start)
                ?? throw new InvalidOperationException("Failed to start pwsh.");
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            Assert.True(process.WaitForExit(60_000), stdout + Environment.NewLine + stderr);
            Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr);
            Assert.True(File.Exists(Path.Combine(dest, "bin", "gh.exe")), dest + Environment.NewLine + stdout + stderr);
        }
        finally
        {
            if (Directory.Exists(scratch))
                Directory.Delete(scratch, recursive: true);
        }
    }

    static HashSet<string> ZipNames(string nupkg)
    {
        using var zip = ZipFile.OpenRead(nupkg);
        return zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    static bool HasUnixExecute(int externalAttributes)
        => ((externalAttributes >> 16) & Convert.ToInt32("111", 8)) != 0;

    static void AssertUnixExecuteBits(string nupkg)
    {
        using var zip = ZipFile.OpenRead(nupkg);
        var gh = zip.Entries.FirstOrDefault(e =>
        {
            var n = e.FullName.Replace('\\', '/');
            return n == "gh/bin/gh" || n.EndsWith("/gh/bin/gh", StringComparison.Ordinal);
        });
        Assert.True(gh is not null, nupkg + " missing gh/bin/gh");
        Assert.True(HasUnixExecute(gh!.ExternalAttributes), gh.FullName + " in " + nupkg);
    }

    static void RunPwsh(string workingDirectory, string command)
    {
        var start = new System.Diagnostics.ProcessStartInfo("pwsh")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(command);
        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(60_000), stdout + Environment.NewLine + stderr);
        Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr);
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
