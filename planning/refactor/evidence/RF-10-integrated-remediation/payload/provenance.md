# Provenance and scope

The before snapshot was captured after checking branch, HEAD, dirty status, and the historical A08-01 ZIP hash. `payload/before/` contains byte copies of the source, tests, task definitions, and reports used for this integrated turn. `payload/checks/before-snapshot.json` and `allowlist.json` are generated before task edits.

Production source changed only at the A08-02 mapping expression listed in the implementation summary. No migration, contract, CI, fixture, provider, deployment, or shared database file was changed. The A08-01 characterization test and its historical first failed run are retained unchanged.

New build/test commands, timestamps, exit codes, TRX, stdout/stderr, source hashes before build, assembly hashes after build, and source/assembly hashes after test are stored under `payload/checks/` and `payload/runtime/`. A successful `--no-build` test is authoritative only when its binary hash matches the immediately preceding build.

The package manifest hashes every payload file from bytes on disk and excludes only the root `MANIFEST.json` from its own entries to avoid a self-hash cycle. ZIP SHA-256 is external. Extraction verification checks missing, mismatched, unlisted, and duplicate paths. No secrets or environment credentials are included.

Source-only limitations remain: provider/deployed behavior, late-attempt CG11, dispatcher invocation outside the searched C# tree, old JWT behavior, checkpoint-13 correction-04 linkage, survey checkpoint-08 before snapshot, and inspection provenance limits.
