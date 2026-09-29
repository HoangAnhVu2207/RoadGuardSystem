# V2 backend execution plan

This directory contains the active grouped work plan for Anh and Huy. The 133 operation cards under `Person_1/` and `Person_2/` remain the contract and trace index; they are not copied into separate implementation tasks.

## Active files

| File | Owner | Goal |
|---|---|---|
| [00-ROADMAP](00-ROADMAP.md) | Anh + Huy | Shared workflow, wave order, evidence and AI guardrails |
| [ANH-01](ANH-01-auth-identity.md) | Anh | Identity, sessions and account persistence facts |
| [ANH-02](ANH-02-project-survey.md) | Anh | Project, route, survey and dataset persistence |
| [ANH-03](ANH-03-report-inspection-repair.md) | Anh | Report, case, defect, inspection and repair persistence |
| [ANH-04](ANH-04-processing-files-operations.md) | Anh | Processing, files, audit, notification, sync and retention persistence |
| [HUY-01](HUY-01-auth-identity-api.md) | Huy | Identity, sessions and account API behavior |
| [HUY-02](HUY-02-project-survey-api.md) | Huy | Project, route, survey and dataset API behavior |
| [HUY-03](HUY-03-report-inspection-repair-api.md) | Huy | Report, case, defect, inspection and repair API behavior |
| [HUY-04](HUY-04-processing-files-operations-api.md) | Huy | Processing, files, audit, notification, sync and retention API behavior |

## Working rule

Start with `00-ROADMAP.md`, then exactly one owner task. A task is complete only when its own acceptance evidence and cross-owner handoff are `VERIFIED`. Historical completion records and operation cards are append-only evidence; they must not be rewritten to manufacture a new status.
