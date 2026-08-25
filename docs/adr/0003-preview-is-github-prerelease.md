# Preview is its own GitHub release

The pin is the naked CLI version (`2.98.0`). When `vars.RELEASE` is not `STABLE` (default `PRERELEASE`), the draft is tagged and titled `2.98.0-preview` (`--prerelease`). The org webhook publishes it; nupkgs use that tag. Flipping to `STABLE` creates a new draft `2.98.0` and does not edit the preview. Packaging-only republishes of the same CLI are `{version}-preview.N`. `publish.yml` appends `-preview` only for a naked prerelease tag, so a tag that already contains `-preview` is not doubled. Previews stay on nuget.org.
