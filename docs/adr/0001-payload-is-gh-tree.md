# Payload is `gh/`, not `runtimes/{rid}/native/`

The .NET SDK flattens anything under `runtimes/{rid}/native/` when copying to output. GitHub CLI is a directory tree (`bin/gh` plus supporting files), so a RID package ships that tree as `gh/` and `buildTransitive` copies it with paths intact.
