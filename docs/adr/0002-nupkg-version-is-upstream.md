# Nupkg version is Upstream

`GitHub.Cli` and `ghx` use the GitHub CLI version (`2.98.0`) so `ndnx ghx@2.98.0` is that CLI. The pin is `github-cli.version`. CI dogfood stays `42.42.*`. Packaging-only republishes of the same CLI use a SemVer label, not a different major.
