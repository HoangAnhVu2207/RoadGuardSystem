# RF-05 activation and recovery plan

**Local activation executed 2026-09-30.** Owner acceptance identified package `rf05-prepare-correction-3` and target manifest SHA-256 `764bec3cac0450fe4219fa0e41168795efb0bc7120569c691354e587540c0448`. Exact target, retirement and check results are in `activation-result.json`; the seven-section report is `planning/refactor/reports/RF-05-activation.md`. The procedure below is retained as the recovery and audit plan. GitHub Actions remains NOT RUN because no push was authorized.

## Package to accept

The exact file list, old-source SHA-256 and proposed SHA-256 is target-manifest.json, package version `rf05-prepare-correction-3`: 16 target files (eight guidance/manifest files, three readiness/decision files, five replacements) and the same 22 old guidance files proposed for relocation. The extra target is `planning/refactor/04-decision-index.json`; it starts empty. The full replacement contents are in drafts/ and patches/targets/; the five unified diffs are in patches/. checks/staging-result.json records the external staging simulation. The new root guide is intentionally short; no new SKILL.md is proposed.

Owner acceptance must identify this manifest hash/version and the metadata-only activation transform below. Until acceptance, all draft files remain in planning/refactor/rf05/ and the current AGENTS.md/.agents/ and CI/validators remain byte-for-byte unchanged.

## Preflight after acceptance

1. Recheck branch anh, HEAD, all dirty paths, current-scope writer and any new owner instructions. Compare every source_sha256 in target-manifest.json with current bytes. If one differs, stop that file, rebase its exact patch against current content and obtain review of the new output/hash. Do not overwrite dirty user changes.
2. Reserve one writer for root AGENTS.md, .agents/, both PowerShell validators, RF-04 generator/verifier and CI. Confirm no other active task depends on old discoverable skills. Recheck docs, FE lock and Postman guards separately.
3. Review 22 old guidance paths and archive destination in target-manifest.json. Ensure docs/history/agent-pre-rf05/ has no conflicting files and archive hashes will match. Do not delete old docs/ADR or rewrite historical evidence.

## Atomic activation order

1. In an isolated integration checkout, archive only the 22 enumerated old .agents rules/references/skills to their manifest destinations. Preserve .agents/mcp_config.json.
2. Apply the 16 manifest targets only when each source hash matches. For the .agents/manifest.json target, set status ACTIVE_AFTER_OWNER_ACCEPTANCE and fill accepted_at and acceptance_source with the actual owner message; this metadata transform changes its draft hash and must be recorded as final hash. No other content change is implied.
3. Apply the generator/verifier readiness patch, empty decision index and empty registry together; rerun the generator so the currently mapped 148 entries remain BLOCKED with reasons/checkpoints. This count may grow only with an explicit source-to-task mapping. Existing recorded READY/DEFERRED/partial-scope decisions, if any arose since preparation, must be translated into sourced decision-index entries and registry scopes before rerun. Keep the 32-44 table intact; its overlap hints do not certify historical claim text. Never overwrite an unmapped decision.
4. Apply the two validator wrappers and CI patch in the same integration change as the guidance files. Keep check_alignment.py, FE check_contracts.py, FE lock, Postman, production and migrations unchanged. Confirm no second AGENTS.md, .agent/ or discoverable old SKILL.md remains on active paths.
5. Run manifest/link/path/authority/ownership guard, RF-04 generator/verifier, the three RF-05 unit scripts, the disposable `test_readiness_flow.py` fixture, `python planning/refactor/rf05/checks/verify_repository_readiness.py --root .` read-only on actual data, existing CI security verifier and all previously invoked unaffected guards. Check missing-file, metadata and retired-guidance negative self-tests. Inspect proposed CI diff and run the CI verify job on the branch when available. Python 3.11 is specified in the CI patch. Record baseline FE lock mismatch separately; do not auto-relock.

## Rollback

If the coordinated switch fails, stop CI/guidance use of the new set. Restore only the 16 package targets from their recorded pre-activation bytes or remove only package-created paths, and return the 22 archived old guide files to their original paths after verifying their archive hashes. Restore the old CI and validator invocations in the same rollback change. Never reset the whole working tree or discard unrelated dirty edits. Re-run the old applicable checks and document any pre-existing failures. Database/data recovery is not applicable: this package has no schema or data effects.

The next activation turn must record the owner acceptance source, final manifest hash, active version/date, exact changed files/commit if any, static checks and GitHub CI result. RF-05 PREPARE does not satisfy the activation gate; RF-06 starts only after an accepted, verified activation.
