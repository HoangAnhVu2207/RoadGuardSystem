# Packaging attempt retained

Command: `pwsh -NoProfile -File planning/refactor/evidence/A08-01-characterization/pack-handoff.ps1 -Phase Package`.

First attempt was interrupted (exec session exit1) after observed warning: `WARNING: Resulting JSON is truncated as serialization has exceeded the set depth of 30.` It had completed final source/assembly equality and TRX parsing, source copying and callback search; no ZIP/manifest/static-review file was created at interruption. The original script bytes are preserved beside this note.

Cause: captured git diff --check stderr warning objects were PowerShell ErrorRecord instances; deep serialization of their context produced the warning and excessive traversal. Correction casts each captured line to string before writing JSON. Actual git diff --check exit0 was separately observed, with existing LF/CRLF warnings in unrelated dirty files. No test/source/fixture/production change or runtime rerun. This is packaging metadata correction, not a discarded failed test; both runtime TRXs/logs remain in payload/runtime/.
