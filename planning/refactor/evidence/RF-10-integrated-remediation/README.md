# RF-10 integrated remediation package

This is the single reviewer handoff for the 2026-10-02 integrated turn. `MANIFEST.json` is generated from every payload file's bytes. The manifest excludes only itself from its own entry list; this prevents a self-hash cycle. The external `.sha256` file hashes the ZIP bytes. `verify-extract.ps1` extracts to a new directory and fails on missing, mismatched, unlisted, or duplicate paths.

The package contains current source needed to review the A08-02 fix, the unchanged A08-01 characterization test and historical evidence, task definitions, closure/design decisions, before snapshots, and focused build/test provenance. It does not contain secrets, credentials, provider data, deployed data, or the full historical archive set.
