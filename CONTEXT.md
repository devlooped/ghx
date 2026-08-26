# GitHub.Cli

NuGet pointer and per-RID payloads that put a working GitHub CLI next to a .NET app or tool. Humans run that payload through the **gh** tool package.

## Language

**Pointer**:
The `gh.cli` nupkg: `Cli` plus `runtime.json` mapping each supported RID to a RID package.
_Avoid_: metapackage, tool package, native package

**RID package**:
`gh.cli.{rid}` — one nupkg, one RID, one Payload.
_Avoid_: native package, runtime pack, sidecar package

**Payload**:
The self-contained tree copied to the consuming app as `gh/`, from which `gh` is executed.
_Avoid_: native files, native/, binaries, sidecar

**Cli**:
The managed type in namespace `GitHub` whose `ResolvePath` returns the Payload's `gh` executable (`gh.exe` on Windows). Callers write `GitHub.Cli.ResolvePath()`.
_Avoid_: Gh, Ghx, GitHubCli, ResolveBinaryPath

**gh** (tool package):
The passthrough .NET tool. Package id `gh` so `dnx`/`ndnx gh`. `ToolCommandName` is `dotnet-gh`, so a locally/globally installed tool is `dotnet gh` and does not steal the `gh` command from GitHub's native CLI.
_Avoid_: installing a command named `gh`; wrapping with its own GitHub verbs

**Execute**:
The tool replacing itself with the Payload `gh`. Every argument is forwarded. The only exception is a lone `--version`, which prints the wrapper version line (`gh {version}`) then payload `gh --version`.
_Avoid_: wrap, shell out and wait as the product metaphor (implementation may still spawn)

**Upstream**:
The GitHub CLI GitHub release we track. Tag `v{version}`; that `{version}` is our nupkg version.
_Avoid_: submodule, clone, building `gh` from source

**Preview**:
A separate GitHub release tagged `{upstream}-preview` when `vars.RELEASE` is not `STABLE`. Nupkgs use that tag. A later STABLE release is `{upstream}`, not an edit of the preview.
_Avoid_: flipping the prerelease bit on a published release, `{version}-preview-preview`

**Skip-announce**:
`<!-- !x -->` in every GitHub release body. Notes are copied from cli/cli; SponsorLink must not post them to X.
_Avoid_: `<!-- X -->` (that *forces* an announcement)

**Nosponsors**:
`<!-- nosponsors -->` in every GitHub release body so SponsorLink does not inject a sponsors section.
