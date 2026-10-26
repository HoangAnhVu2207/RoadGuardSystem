# Synthetic actor accounts

CURRENT_VERIFIED Task33 source candidate; independent review NOT_PERFORMED. No FULL_CONTRACT promotion.

Passwords are random per deployment and persist only in the private state volume. Read locally with `docker compose exec -T api cat /handoff/demo-accounts.json`; never paste this output into logs, Git, tickets or KnowledgeSpace.

| Actor key | Scope / use |
|---|---|
| supervisor | Supervisor project directory and confirmation; both A/B |
| pm-a / pm-b | Independent current project managers for A/B |
| operator-a / operator-b | Assigned survey tasks and dataset upload for A/B |
| crew-a / crew-b | Existing Crew membership boundaries; no PM catalog privilege |
| reporter-a / reporter-b | Own intake/report/evidence boundary, not project manager authority |

Consult generated account keys for additional suspended/ended negative actors. Do not assign an AI service identity from a human account: issuer parameters remain blocked. Email/credential values are intentionally omitted. Permission is rechecked at command/read time, not inferred from the account label.
