# D-ASYNC: Durable file, processing and notification boundaries

- **Status:** PROPOSED, RF-04 draft.
- **Sources:** historical ADR 003; TARGET_CONFIRMED PR-34A/38/44; CURRENT_VERIFIED upload/outbox/callback source, CG10/11/13/17.
- **Proposal:** commit SQL business change, audit and outbox intent atomically; treat object storage and external AI as separately retryable with correlated receipts. A job result must carry immutable source/model/attempt provenance and reject stale attempts after an active-attempt rule is approved. AI output stays a candidate for PM action. Preserve source media, and plan size-type expansion for 8 GiB video before admitting it.
- **Unknown:** actual AI provider manifest/receipt, dispatcher owner, retry/timeout policy, storage compensation and dataset quality method. Appendix B is review input, not accepted wire.
- **Gate:** Q-RF02-05/06, RF-09 data and consumer package, isolated SQL/provider-fixture evidence. No deployment implied.
