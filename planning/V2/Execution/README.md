# V2 backend execution plan

This directory contains the active grouped work plan for Anh and Huy. The 133 operation cards under `Person_1/` and `Person_2/` remain the contract and trace index; they are not copied into separate implementation tasks.

## Active files

| File | Owner | Status | Goal |
|---|---|---|---|
| [00-ROADMAP](00-ROADMAP.md) | Anh + Huy | N/A | Shared workflow, wave order, evidence and AI guardrails |
| [ANH-01](ANH-01-auth-identity.md) | Anh | DONE | Identity, sessions and account persistence facts |
| [ANH-02](ANH-02-project-survey.md) | Anh | PARTIAL | Project, route, survey and dataset persistence |
| [ANH-03](ANH-03-report-inspection-repair.md) | Anh | PARTIAL | Report, case, defect, inspection and repair persistence |
| [ANH-04](ANH-04-processing-files-operations.md) | Anh | PARTIAL | Processing, files, audit, notification, sync and retention persistence |
| [HUY-01](HUY-01-auth-identity-api.md) | Huy | PARTIAL | Identity, sessions and account API behavior |
| [HUY-02](HUY-02-project-survey-api.md) | Huy | PARTIAL | Project, route, survey and dataset API behavior |
| [HUY-03](HUY-03-report-inspection-repair-api.md) | Huy | PARTIAL | Report, case, defect, inspection and repair API behavior |
| [HUY-04](HUY-04-processing-files-operations-api.md) | Huy | PARTIAL | Processing, files, audit, notification, sync and retention API behavior |

## Working rule

Start with `00-ROADMAP.md`, then exactly one owner task. A task is complete when its own approved acceptance gates pass. Cross-owner handoff reports completed work and impact; the sender does not wait for receiver confirmation to continue or close independently verified work. Historical completion records and operation cards are append-only evidence; they must not be rewritten to manufacture a new status.


## Prompt sử dụng

- [PROMPT-NEW-TASK.md](PROMPT-NEW-TASK.md): bắt đầu một task mới.
- [PROMPT-P1.md](PROMPT-P1.md): prompt riêng cho Anh/Person 1, persistence và SQL.
- [PROMPT-P2.md](PROMPT-P2.md): prompt riêng cho Huy/Person 2, Service/API/Postman.
- [PROMPT-REVIEW.md](PROMPT-REVIEW.md): review và auto-fix trước khi báo cáo hoặc push.
