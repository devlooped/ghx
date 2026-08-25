# GitHub.Cli

NuGet pointer and per-RID payloads that put a working GitHub CLI next to a .NET app or tool. Humans run that payload through **ghx**.

## Language

**Pointer**:
The `GitHub.Cli` nupkg: `Cli` plus `runtime.json` mapping each supported RID to a RID package.
_Avoid_: metapackage, tool package, native package

**RID package**:
`GitHub.Cli.{rid}` — one nupkg, one RID, one Payload.
_Avoid_: native package, runtime pack, sidecar package

**Payload**:
The self-contained tree copied to the consuming app as `gh/`, from which `gh` is executed.
_Avoid_: native files, native/, binaries, sidecar

**Cli**:
The managed type in namespace `GitHub` whose `ResolvePath` returns the Payload's `gh` executable (`gh.exe` on Windows). Callers write `GitHub.Cli.ResolvePath()`.
_Avoid_: Gh, Ghx, GitHubCli, ResolveBinaryPath

**ghx**:
The passthrough .NET tool that execs the Payload `gh` with the same arguments. Primary human vehicle via `dnx`/`ndnx ghx`.
_Avoid_: GitHub CLI, gh, wrapper with its own GitHub verbs

**Execute**:
ghx replacing itself with the Payload `gh`. Every argument is forwarded. The only exception is a lone `--version`, which prints ghx and `gh`.
_Avoid_: wrap, shell out and wait as the product metaphor (implementation may still spawn)

**Upstream**:
The GitHub CLI GitHub release we track. Tag `v{version}`; that `{version}` is our nupkg version.
_Avoid_: submodule, clone, building `gh` from source

**Preview**:
A separate GitHub release tagged `{upstream}-preview` when `vars.RELEASE` is not `STABLE`. Nupkgs use that tag. A later STABLE release is `{upstream}`, not an edit of the preview.
_Avoid_: flipping the prerelease bit on a published release, `{version}-preview-preview`
