"""Refresh narrative entrypoints while retaining each previous checkpoint verbatim."""
from pathlib import Path
import hashlib
import json
import os
from sync_current_docs import ROOT, BE, PROJECT, write

HEAD='01cc4725b178199215b02f4ddf7093491a16f2f7'


def link(origin, path, label=None):
    return '['+(label or Path(path).name)+']('+os.path.relpath(ROOT/path,origin.parent).replace('\\','/')+')'


def update(path, body):
    text=path.read_text(encoding='utf-8-sig')
    # Repeatable: replace this refresh only; historical content remains intact.
    if '<!-- CURRENT-DOCS-SYNC-BEGIN -->' in text:
        start=text.index('<!-- CURRENT-DOCS-SYNC-BEGIN -->'); end=text.index('<!-- CURRENT-DOCS-SYNC-END -->')+len('<!-- CURRENT-DOCS-SYNC-END -->')
        text=text[:start]+text[end:].lstrip('\n')
    first,_,rest=text.partition('\n')
    panel='<!-- CURRENT-DOCS-SYNC-BEGIN -->\n\n## Current integration — 2026-10-27\n\n'+body+'\n\n## Historical checkpoint (retained)\n\nThe following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.\n\n<!-- CURRENT-DOCS-SYNC-END -->\n\n'
    write(path,first+'\n\n'+panel+rest.lstrip('\n'))


def main():
    d=json.loads((BE/'10_Traceability/current-discovery.json').read_text(encoding='utf-8'))
    common=f'`CURRENT_VERIFIED` static source at BE `{HEAD}`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.\n\n50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.\n\nAPI030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.'
    narrative='''# Current integrated Backend and client boundary

## Evidence and identity

'''+common+'''

## Current source changes and exact authority

- G02–G06: [DiscoveryCollectionsController](../../RoadGuardSystem.API/Controllers/DiscoveryCollectionsController.cs) → [CollectionReadService](../../RoadGuardSystem.Services/Implementations/Collections/CollectionReadService.cs) → [CollectionReadRepository](../../RoadGuardSystem.Repositories/Implementations/Collections/CollectionReadRepository.cs). Eleven GET collections use closed typed summaries. Current actor/project/resource/parent admission runs before query parsing, including empty lists; rows reuse current detail authority. Default limit25, range1–100, protected cursor bound to actor/project/resource/parent/mode; only repair items accept NORMAL/FAST_TRACK mode. Unknown/repeated query keys fail400. Private no-store applies. Exports remain creator-only plus snapshot visibility; no list grants private download. Survey tasks/processing jobs admit scoped Operator; offline lists admit scoped Crew subject to detail checks; other collections PM/Supervisor. A list is not a new action permission.
- B2 ADD-13/14/15: [controller](../../RoadGuardSystem.API/Controllers/NotificationSupervisorController.cs) → [service](../../RoadGuardSystem.Services/Implementations/Repairs/NotificationSupervisorService.cs) → [repository](../../RoadGuardSystem.Repositories/Implementations/Repairs/NotificationSupervisorRepository.cs). Current PM manages an exact project/package/item. Global active Supervisor recipient does not require ProjectMember. GET/PUT/DELETE use current authority before validation/replay, version/ETag and idempotency; serializable locks, generation history and audit preserve reassignment/revocation. [Delivery](../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs) and [retry](../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Retry.cs) recheck pinned recipient authority. SUBMITTED and PM-final are information; **FastTrack gains no Supervisor approval**.
- C1 ADD-16/17/18: [PublicPublicationsController](../../RoadGuardSystem.API/Controllers/PublicPublicationsController.cs) uses action-only HTTP routes. PM create/withdraw reuse [current Case scope and eligible confirmed source/evidence](../../RoadGuardSystem.Repositories/Implementations/Cases/PublicPublicationRepository.cs); [service](../../RoadGuardSystem.Services/Implementations/Cases/PublicPublicationService.cs) performs guarded serializable idempotency before outcome replay. Create201, withdraw200; anonymous read200 or404 after withdrawal. Public DTO is only publicId/damageCategory/publicStatus/publishedAt, not contacts, private evidence or full Case history. K is required on writes; missing K428. This is distinct from Reporter-recipient publication.
- FIELD/Crew P5-G02: API-080 task GET can supply repairContext through [CrewRepairContextAsync](../../RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.CrewRepairContext.cs). It requires assigned current Crew, NORMAL task/item, matching binding/obligation/current item and active project. Context contains packageId/itemId/bindingId/mode/status/itemVersion/repairPlan/checklistVersion; it is absent when not applicable. Task version and itemVersion are distinct. This proves a guarded source bootstrap exists; it does not certify native Crew rendering or all repair journeys. No new Crew projectwide repair catalog is inferred.
- A1: [ProcessingV2PersistenceService](../../RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs) binds new callback receipt identity and checks current attempt before replay; legacy unverifiable receipt409 duplicate_request, without rewriting receipts or business effects. [AI JWT](../../RoadGuardSystem.API/Authentication/AiServiceJwtBearerConfiguration.cs) remains its own service boundary, not a human FE callback. Real external AI acceptance remains open. Historical P1-02 partial-fence text below is not a current code conclusion.
- API-238 [UsersController.Get](../../RoadGuardSystem.API/Controllers/UsersController.cs) maps account to ActorResponseDto with exactly id/displayName/role/version. It does not remove persisted contact data or change other account APIs. Owner-approved optional-null properties, including API019 conclusion/API225 accessPoint, remain property-local serialization rules; non-null data is retained. Schema omission is distinct from nullable acceptance.
- NORMAL remains PM proposal → Supervisor approval → Crew execution → PM review → Supervisor final under current source constraints. FastTrack retains PM final with Supervisor information only. FIELD assessment is not repair completion. Offline envelopes/device signatures/version/ownership stay independent; no invented device fixture or native acceptance.

## Owner principles and remaining compatibility

`TARGET_CONFIRMED` principle does not prove HTTP implementation:

| Principle | Current bounded disposition |
|---|---|
| E1 | Mobile uses reportId as wire identity; #HH- is presentation only. Native consumer verification NOT_VERIFIED. |
| E4 | /me auth should use ProblemDetails without changed auth semantics. [MeController](../../RoadGuardSystem.API/Controllers/MeController.cs), JWT/cookie events and status pages must be considered together. This docs task does not close exact HTTP error parity. |
| P5-G02 | Existing API080 repairContext source proven above; source existence is not native runtime acceptance. |
| API030 | Prefer video/mp4 for real MP4; documented media/runtime mismatch stays NOT_VERIFIED until exact HTTP proof. |
| B11 | BE V2 authoritative roles. A temporary FE role adapter is a compatibility principle; no roleAdapter file was found in the inspected current hoang tree, so current adapter implementation is UNKNOWN. |

## Current FE inspection and acceptance limits

FE `hoang` is not the earlier `Anh-test` acceptance source. `src/api/client.ts` uses cookie credentials, CSRF and a single-flight renew path; `src/api/auth.ts` declares WebAuth DTOs. `src/api/services/authService.ts` still contains an in-memory mock implementation. Source presence does not establish reachability; no global REAL-only or 165-consumer/browser acceptance is claimed for this FE revision. A mutation retry after401 needs preserved K/body/version and current authority; it is not made safe merely by renewing a session. No FE files were changed.

## Tests and deployment provenance

Historical exact-SHA integrated CI at this BE HEAD (five published refs) recorded Unit1117PASS, API1533PASS/0FAIL/1 allowed legacy optional skip, SQL807PASS across four lanes. Retained verification is private in BE-FIVE-BRANCH-PUBLISH-20261025-183103. Those results do not certify all client dimensions or Candidate3 parity. This docs task freshly runs Swagger metadata and documentation validators; SQL/runtime journeys are NOT_RUN here. Earlier aborted local API run113PASS/260FAIL and all older failures remain historical, not converted to PASS.

Source SHA256 and capture identities: [current discovery](10_Traceability/current-discovery.json), [full current C# hash manifest](10_Traceability/current-source-hashes.json), [current API/source map](10_Traceability/api-source-map.json). Original Roslyn source-inventory.json and api-method-evidence.json retain their older embedded baselines and are HISTORICAL navigation only, not current transitive call-path proof.

## Knowledge Space and sharing

Versioned local indexes retain old snapshots. Live text is CURRENT_WORKTREE_UNREVIEWED; historical snapshots remain HISTORICAL_REFERENCE. API-ID mentions are TEXT_MENTION_ONLY, not contract adoption or verified implementation. Activation requires full source hashes, integrity, provenance and search checks before CURRENT changes. No SQLite/raw evidence is uploaded. Current Windows ACL still grants BUILTIN\\Users Modify; **SECURITY_BLOCKED — sharing NOT_GRANTED**. Index correctness does not waive this access-control limitation.
'''
    write(BE/'CURRENT-INTEGRATION-20261027.md',narrative)
    # Make all section entrypoints explicitly date-bound; old paragraphs remain
    # available for historical review and are never silently promoted.
    backend_targets=['README.md','DOCUMENTATION_MANIFEST.md','03_API_Contracts/06_OpenAPI_Runtime_Alignment.md','03_API_Contracts/03_Request_Response_DTOs.md','03_API_Contracts/04_Error_Catalog.md','03_API_Contracts/05_Concurrency_and_Idempotency.md','05_Security/02_Authorization_Matrix.md','06_Integrations/01_BE_FE_Integration_Boundary.md']
    backend_targets += [p.relative_to(BE).as_posix() for folder in ['00_Overview','01_Architecture','02_Business_Workflows','04_Database','09_Testing','11_Audit_and_Gaps'] for p in (BE/folder).glob('*.md') if not p.name.endswith('SPEC.md')]
    for rel in dict.fromkeys(backend_targets):
        p=BE/rel
        if p.is_file(): update(p,common+'\n\nCurrent details: '+link(p,'docs/RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md','integration, authority, compatibility and evidence')+'; '+link(p,'docs/RoadGuard-Backend-Official/03_API_Contracts/02_Endpoint_Catalog.md','exact current endpoint catalog')+'; '+link(p,'docs/HoangHaiWarrantySystem-Official/04_API-Contracts/03_DTO-and-Schema-Reference.md','384 current schema definitions')+'. Historical deep traces below must be rechecked against current SHA before reuse.')
    project_targets=['README.md']
    for folder in ['00_Governance','01_Product-and-Roles','02_Business-Workflows','03_Backend-and-Data','04_API-Contracts','05_Web','06_Mobile-Expo','07_AI-FastAPI','08_Testing-and-Operations/Phase5']:
        project_targets += [p.relative_to(PROJECT).as_posix() for p in (PROJECT/folder).rglob('*.md') if not p.name.startswith(('PHASE','BE-')) and p.name not in ['02_Full-API-Catalog.md','03_DTO-and-Schema-Reference.md','09_Consumer-API-Crosswalk.md']]
    for rel in dict.fromkeys(project_targets):
        p=PROJECT/rel
        update(p,common+'\n\n'+link(p,'docs/RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md','Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles')+'. '+link(p,'docs/HoangHaiWarrantySystem-Official/04_API-Contracts/02_Full-API-Catalog.md','Current 260-ID catalog')+' preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.')
    # Refresh current security matrix and exact error/header navigation, rather
    # than leaving old policies as the sole machine-readable reference.
    rows=json.loads((BE/'10_Traceability/api-source-map.json').read_text(encoding='utf-8'))['operations']
    table=['# Current authorization matrix\n',common,'| ID | Actor scope | Method/path | Authentication |','|---|---|---|---|']
    for r in rows: table.append(f"| {r['id']} | {r['role']} | `{r['method']} {r['path']}` | {r['authentication']} |")
    table.append('\nRole membership is necessary, not sufficient: current actor/resource/assignment/project/receipt guards remain mandatory. B2 recipient is global Supervisor, not a PM management permission. C1 writes PM only; read anonymous. AI callback is service-only. Exact alternatives/headers/error responses are in the current catalog.')
    write(BE/'05_Security/02_Authorization_Matrix.md','\n'.join(table))
    print(json.dumps({'narrative_pages_refreshed':len(set(backend_targets))+len(set(project_targets)),'schemas':d['schemas']}))


if __name__=='__main__': main()
