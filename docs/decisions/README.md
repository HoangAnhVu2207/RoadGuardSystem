# Decision transition (RF-04 draft, inactive)

The six existing ADRs under `docs/adr/` remain untouched. Their file status is historical evidence, not automatic authority for this refactor. The owner's 2026-09-30 reconfirmation of the **full** 32-44 interpretation is the confirmed product source in [decision register](../../planning/refactor/02-decision-register.md) and [product rules](../product/requirements.md). Original 2026-09-28 question/options wording remains unavailable. No proposed ADR below is Accepted merely because a related product requirement is confirmed.

| Old ADR | Historical decision and RF-02 disposition | Draft successor / gate |
|---|---|---|
| [001 backend boundary](../adr/001-backend-boundary.md) | Five-project backend and external ownership; preserve rationale, review architecture. | [D-ARCH](D-ARCH-proposed.md), carry PR-44; architecture approval. |
| [002 authentication](../adr/002-authentication.md) | Identity/JWT/session and OTP; target transport changes under PR-36A/37. | [D-AUTH](D-AUTH-proposed.md), Q-RF02-02 and consumer window. |
| [003 BE/AI boundary](../adr/003-backend-delivery-and-ai-boundary.md) | Durable jobs and external AI; preserve human decision boundary. | [D-ASYNC](D-ASYNC-proposed.md), Q-RF02-06 provider contract. |
| [004 N-layer structure](../adr/004-n-layer-backend-structure.md) | Current N-layer rationale, old counts stale. | [D-ARCH](D-ARCH-proposed.md), confirm preservation after pilot. |
| [005 product workflow](../adr/005-product-workflow-synchronization.md) | Explicitly Proposed; Reporter/repair/segment ideas need separate authority. | [D-WORKFLOW](D-WORKFLOW-proposed.md), Q-RF02-03..05/07/08. |
| [006 V2 ownership](../adr/006-v2-endpoint-ownership-and-persistence-coordination.md) | Historical workflow only under current user override; one-writer insight retained. | [D-DELIVERY](D-DELIVERY-proposed.md), acceptance of replacement guide at RF-05. |

The detailed old-to-new rationale is [RF-02 ADR transition](../../planning/refactor/02-adr-transition.md); the machine/document source inventory is [RF-04 source crosswalk](../../planning/refactor/04-source-crosswalk.md). Deleting an old ADR later does not cancel a product rule. RF-11 may retire old paths only after accepted successor content, links, validators and user authorization.
