# RF-10 integrated remediation handoff

Date: 2026-10-02. Writer: Anh/Codex. Branch `anh`; HEAD remains `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`. This package combines the bounded A08-01 characterization, one in-scope A08-02 production correction, and decision packets for A09-01/A09-02. It does not claim RF-10, RF-11, or R01 completion.

## Outcome

- **A08-01:** characterization accepted in authenticated HTTP + isolated SQL (13/13 final; failed 11/13 run retained). The source-confirmed defect remains **OPEN/BLOCKED for rollout** because callback identity, legacy receipt compatibility, provider key uniqueness, and canonical serialization are not owner-approved. No production A08-01 fingerprint change was made.
- **A08-02:** fixed in `ProcessingV2PersistenceService.CreateValidationAsync`. A replayed receipt now returns `Replayed` only when the stored outcome was success; stored `Conflict` or `InvalidInput` remains that status. This preserves the existing controller's HTTP error mapping and prevents a stored failure with no run from entering the generic invalid fallback. Focused compile/regression evidence is recorded after the edit.
- **A09-01:** **BLOCKED**. Lease cap and expiry behavior are source-confirmed risk, but no approved crash-terminal policy or production dispatcher caller exists in the inspected tree.
- **A09-02:** **BLOCKED**. Owner-string checks lack a generation/token and expiry fence; adding one requires a schema/contract decision explicitly outside this slice.

## Evidence boundary

The prior A08-01 ZIP hash was verified as `7599731ca111e85aa116739736506c05ed038f5d33fa690ac66080afc66b28cd`. Its final TRX remains historical evidence of pre-fix behavior. New verification in this handoff is separate and is never merged into that historical run. No shared database, provider, deployed environment, CI, migration, public contract, or external consumer was used.

## Remaining closure work

RF-10-05 remains Partial because provider protocol/owner, retryable failure transition, late-attempt fencing and external integration are unresolved. RF-10-09 and RF-11 remain Partial. Historical limitations remain: old JWT behavior, checkpoint-13 correction-04 source-to-binary linkage, missing survey checkpoint-08 before snapshot, inspection provenance/runtime constraints, provider/deployed behavior, and CG11 late-attempt evidence.
