# FE discovery workflows

Current integration scope: G01 and the eleven G02–G06 collections are Owner-approved. Exact operation acceptance remains tracked in the frozen R1 ledger; this navigation guide does not grant independent review PASS or FULL_CONTRACT.

1. Authenticate using the private generated actor account. GET /api/v1/projects returns current authorized projects.
2. GET /api/v1/projects/{projectId}/work-package returns roadSections with roadSectionId/currentVersionId.
3. Owner-approved G01: GET /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets. pageSize defaults25, range1..100; cursor is a returned set UUID; optional status DRAFT/PUBLISHED/SUPERSEDED. Follow nextCursor. No implicit default set is selected.
4. GET the existing /segment-sets/{setId} detail to obtain segments. Preserve the returned version for mutations. Geometry-package requires both routeVersionId and segmentSetId.

G01 uses the existing detail authority: Supervisor or current PM project membership. Operator/Reporter/Crew do not gain a new catalog privilege. Parent road/version/project must match. Current authority is checked per request, including after revocation. Source and ZIP HTTP evidence are recorded separately in the external index.

G02 repair packages/items, G03 package items filtered by `mode=FAST_TRACK`, G04 creator-only export history, G05 plans/tasks/processing jobs/mock runs, and G06 devices/packages/grants/retention evaluations use the approved routes and per-row authority in [FE-APPROVED-COLLECTIONS.md](FE-APPROVED-COLLECTIONS.md). No hold catalog or cross-package Fast Track catalog is approved. These eleven new collections use `limit` (default 25, range 1–100) and an opaque `cursor`; G01 above retains its existing `pageSize` and UUID cursor contract.

HISTORICAL: The Task33 guide previously described G02–G06 as pending Owner/public-contract gates. That description was superseded by `G02_G06_COLLECTION_CONTRACT_APPROVAL`, recorded in [G02-G06-OWNER-DECISIONS.md](G02-G06-OWNER-DECISIONS.md). Approval establishes scope, not complete runtime acceptance. Stored command results remain useful for navigation but do not replace the approved collections. Current task and member APIs retain their existing authority.
