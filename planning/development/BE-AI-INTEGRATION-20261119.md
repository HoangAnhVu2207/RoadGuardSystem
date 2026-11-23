# BE–AI integration — design proposal, 2026-11-19

Status: LOCAL IMPLEMENTATION, owner instructed execution on 2026-11-19. The current execution contract below and owner amendments supersede historical proposals/checkpoints in this document. No production deployment is included.

## Current execution contract — 2026-11-20

TARGET_CONFIRMED: Codex/Anh owns the entire BE–AI slice; Huy pauses these shared files. Preserve six distinct FT95 labels; `repair` is an existing patch. AI only proposes, PM alone decides. Same-host Ubuntu is the later deployment target; local mutable storage is D:, with a 40 GiB incremental budget and 30 GiB free floor.

Implementation ruling: private HTTP/JSON plus streamed media, BE pull-and-register, one attempt per verified video, automatic durable intent on operator dataset submission. Additive work/artifact/telemetry/outcome tables preserve existing immutable ProcessingAttempt records. Retry creates a new attempt; terminal outcome history survives it. The dedicated runtime is disabled by default. See [private wire contract](../../contracts/ai/worker-v1.md) and [execution ledger](../../docs/superpowers/plans/2026-11-19-be-ai-integration-execution.md) for actual files, verification and limitations.

Implementation ruling: worker v1 accepts a video only and extracts embedded SRT. Explicitly paired, verified uploaded SRT is resolved by BE, with its committed offset, and takes precedence in the read view; it is not passed through an invented worker wire field. Missing/invalid measured telemetry permits an assigned operator to append SRT or a bounded approximate drone pin. No inference rerun, manifest rewrite or crew ground-location change follows supplementation. External SRT revisions take precedence over paired/embedded telemetry; measured telemetry takes precedence over estimates. An estimate leaves `measuredTelemetryStatus=MISSING_TELEMETRY`.

Implementation ruling: the new bearer API is project/video scoped: discover work IDs for a dataset, read telemetry with its ETag, and append a revision with `If-Match` plus `Idempotency-Key`. Reads allow current assigned DroneOperator, project PM and Supervisor; only the assigned operator appends. Conflicting replay is 409, stale head 412, missing preconditions 428 and invalid payload 422. PM decisions capture the telemetry revision/source version and become stale if the source changes before acceptance. This supplements, rather than replaces, existing candidate-decision APIs.

CURRENT_VERIFIED: actual local BE→HTTP FT95→MinIO→SQL one-minute acceptance passed: 60 frames, 63 proposals, 62 registered artifacts, completed REAL job, zero automatically created Defects, stable replay. The subsequent five-upload repeated-sample pilot also passed: 300 frames, 315 proposals, 310 registered artifacts, all five REAL jobs completed and replay created no new job. Geometry in this vertical harness is explicitly MOCK_VERIFIED; inference, HTTP, MinIO and SQL are actual. This does not establish production deployment, GPU performance, detector accuracy or five diverse samples. The ledger is the current evidence source; old NOT_RUN and writer/mapping gates below are historical.

## Confirmed scope and source baseline

TARGET_CONFIRMED: AI will run on the same Ubuntu host as BE. Use latest fetched develop as the complete backend baseline, work on branch anh, do not merge older local anh work. Add AI to the project workspace, preserve knowledge, prove integration locally first.

CURRENT_VERIFIED: origin/develop fetched 2026-11-19 is 4934103d125be1caca4c6a588eed3768c9cafee4. Branch anh in the isolated be-ai-integration workspace now points to this commit; source tree is clean before this document. Previous deploy workspace changes are preserved. A handoff-document line-ending discrepancy encountered during branch switching was preserved in Git stash before checkout of the requested baseline. No remote branch was pushed.

CURRENT_VERIFIED: AI source D:/Project BE/AI/roadguard-ai-engine is clean at 555e80d7ca0d9b782d66d728920e8eb368c638a8. This is the supplied local source, not a claim that AI remote HEAD was fetched. Production release previously observed is 7e21430886049a98c0df33a6989acbb4fdc42479; latest develop is NOT deployed by this task.

## Findings that prevent URL-only integration

1. AI src/api/worker_service.py calls MockAIAdapter in BOTH mock and nominal real branches. JOB_STORE and idempotency state are memory based. Its callback does not check a non-2xx response and does not send the BE callback credentials/Idempotency-Key. Do not present it as real inference.
2. AI src/api/mvp_video.py uses the actual video pipeline, durable job files and a single executor. It explicitly restricts requests to loopback, local hostnames and same-origin. Docker service-to-service traffic will not satisfy that guard.
3. BE ProcessingV2Controller has authorized processing-job create/read/retry and an internal result endpoint scoped to job + attempt. AiDispatchAdapter prepares an envelope from a committed outbox item; preparation alone does not establish a running HTTP transport.
4. ReceiveAiResultRequestDto requires JobAttemptId, ManifestHash, ModelVersionId, Mode, RawResultFileId, ChecksumSha256 and Detections. Each detection references FrameFileId. AI analysis.json with pixel boxes and class names is not directly this contract.
5. Existing BE callback JWT expires after 300 seconds. A video job can outlive that token. Dispatch-time token reuse after inference is not an acceptable design.
6. No weights directory was found in the supplied AI root. Model checkpoint location and Ubuntu hardware capacity are unverified. Synthetic fixtures cannot certify FT95 inference.
7. Server API/storage were recovered with host loopback bindings. Internal Docker access is separate from those host port bindings. Presigned URLs using the historical LAN IP must not be reused for AI media transfer.

## Architecture options and recommendation

PROPOSED A (recommended): separate BE and AI containers on a private Docker network, HTTP/JSON control messages, durable asynchronous jobs, scoped private object transfer. This reuses existing .NET/FastAPI capabilities and permits direct storage streaming without JSON/base64 video.

PROPOSED B: gRPC/Protobuf for control/results. Requires an additional wire contract and transport implementation on both sides. No benchmark currently demonstrates an end-to-end advantage for this workload.

PROPOSED C: a message broker between services. Useful for several workers/hosts but adds another stateful service. Defer until queue requirements justify it; first use BE durable dispatch and AI durable job admission.

Do not call any option fastest without measurement. Measure upload/download, queue wait, inference and result ingestion separately on the same video/model/hardware. HTTP on the isolated same-host Docker network is proposed; external clients continue HTTPS through BE. AI has no public port, Funnel route or direct FE access.

## Proposed first deliverable

Real FT95 local inference for a verified uploaded video/dataset, returning raw results and evidence frames to BE as AI proposals. Keep human decisions in BE. No automatic defect approval, repair creation, verified coverage, physical dimensions or GPS position invented from pixel detections. Road Mask is optional and must be reported explicitly when absent. Paid Gemini/CKEY and report generation are excluded from the first integration unless separately selected by Owner.

Preserve AI Studio as a local tool. Add a dedicated authenticated worker entrypoint that calls shared pipeline functions; do not remove Studio's local_only guard or expose its review routes remotely.

Repository proposal: keep AI as an independently versioned repository, add AI/roadguard-ai-engine as a pinned submodule in BE after design approval. This avoids copying its .git or maintaining two divergent source trees. Runtime outputs, input videos, model weights, .env and secrets stay outside Git. A different monorepo choice is an Owner design decision, not already approved by this draft.

## Job and data boundaries

BE owns actor/project authorization, verified source file identity, job/attempt state, durable dispatch and accepted business results. AI owns bounded inference execution, durable admission/progress and its raw artifacts. AI receives no SQL credentials and cannot write BE business tables.

Proposed flow: user -> BE authorization and committed job/outbox -> authenticated AI admission -> 202 with stable attempt identity -> stream verified source object -> checksum validation -> inference -> upload raw result/frames into attempt-scoped private staging -> BE validates and ingests -> expose proposals through existing authorized BE APIs.

Admission identity includes contract version, job ID, attempt ID, manifest hash, model/checkpoint hash and preprocessing configuration. Same identity + same payload replays existing acceptance; same identity + changed payload returns 409. A retry has a new attempt ID. Old attempts cannot overwrite the current attempt. Success requires durable BE acceptance, not merely AI completion.

Only configured storage and callback destinations are allowed. No caller-supplied arbitrary URLs/local paths. Tokens, signed URL queries and raw credentials must not enter logs. Input checksum mismatch, unsupported media/model and missing model must fail explicitly; never silently select a mock detector.

Use existing callback authorization semantics. Before coding the wire adapter, freeze one complete credential lifecycle: short-lived callback credential issued when results are ready via an authenticated current-attempt exchange, or BE pulls results and invokes the same validated ingest service internally. Do not extend the five-minute token indefinitely. This is a technical contract checkpoint, not an assumption already implemented.

Artifact registration must produce real BE FileIds and validated checksums before completion. Specify a scoped result-upload/registration mechanism or BE pull-and-register mechanism in the final contract. Do not invent GUIDs to satisfy the existing DTO. Mapping must explicitly define pixel bbox ordering, coordinate space, timestamps, class mapping and unknown class behavior from both producer and consumer tests.

## Reliability and resource policy

Initial proposed limits: one running inference, three waiting jobs, bounded retry/backoff for transient transport faults, no automatic retry for validation/model errors. Preserve durable job and artifact state across restarts. Reconcile running jobs as interrupted unless safe resume is proven. Cancellation is cooperative; never kill SQL or BE to stop inference.

Before Ubuntu rollout, inspect CPU/RAM/GPU/driver and checkpoint size, then choose resource budgets from measured local/host evidence. Existing BE/SQL/MinIO limits stay protected. No promised frame rate, job duration or GPU acceleration until measured. Disk admission checks must account for input, extracted frames, output and reserve; 512 MB from Studio is not an accepted server capacity policy.

## Local acceptance and proposed execution sequence

1. Freeze service contract, artifact registration, credential lifecycle and error/state mapping against pinned develop and AI sources. Update examples/schema and negative fixtures together.
2. Add pinned AI workspace reference and implement authenticated worker admission around real pipeline; durable queue and restart recovery tests.
3. Implement BE transport and scoped result ingestion with outbox lease/idempotency; keep user authorization and attempt checks.
4. Run isolated local BE + SQL + MinIO + AI using separate test data/storage/secrets. Never point these tests at the home server database.
5. Exercise real HTTP: create job -> queue -> real inference -> registered artifacts -> accepted BE result -> authorized result read. Verify hashes, project/attempt ownership and provenance.
6. Negatives: wrong service credential/project/attempt, expired callback credential, duplicate/reordered requests, checksum mismatch, missing model, AI restart, lost acceptance response, callback non-2xx, queue full, cancellation and disk rejection. A mock-only pass is reported separately.
7. Benchmark a fixed authorized short video and pinned model: control p50/p95, transfer bytes/time, queue delay, inference time, CPU/RAM/VRAM and total duration. Record actual numbers rather than an unsupported performance target.
8. Two self-review passes and evidence report. Only then prepare a separate reviewed Ubuntu deployment with health, capacity and rollback.

## Knowledge and unresolved checkpoints

Save this spec and source SHAs into the existing local RoadGuardSystem-Development Knowledge Space. Mark new architecture PROPOSED and same-host deployment TARGET_CONFIRMED. Do not promote old indexes or mock evidence into live verification. A saved note is not equivalent to reindexing the full SQLite catalog.

Owner review needed: accept first-deliverable scope and independent AI submodule layout. Technical investigation still needed: finalize the manifest/result mapping and token/artifact exchange, locate model weights and test video, inspect actual Ubuntu capacity. These are explicit implementation gates; this document is an architecture proposal, not a falsely complete wire specification.

Checks performed at this checkpoint: Git fetch/base/clean-tree inspection and source comparison. Local runtime, inference, API integration and production deployment: NOT_RUN. External peer review: NOT_REQUESTED. Self-review 1 checked mock-vs-real and wire mismatches; self-review 2 checked data ownership, token lifetime, artifact IDs, scope and evidence labels.
## Implementation planning — 2026-11-19

Owner requested a plan based on this spec. See [implementation plan](../../docs/superpowers/plans/2026-11-19-be-ai-integration.md). The plan selects BE pull-and-register as the proposed technical path, preserving external callback semantics. Task 1 freezes class/state/media mapping and determines any shared-schema amendment before dependent implementation. Planning authorization does not relabel proposed wire/schema decisions as accepted business requirements. No implementation or deployment performed at this checkpoint.

## Owner clarification — embedded SRT extraction (2026-11-19)

TARGET_CONFIRMED: owner reports that source MP4 requires FFmpeg extraction to obtain SRT and requests automatic handling after BE upload, preferably by AI. This confirms desired workflow, not the stream format of every input MP4.

PROPOSED implementation: BE verifies/stores MP4 and dispatches its immutable identity. The private AI worker uses ffprobe to enumerate streams, then invokes ffmpeg with an argument list (no shell), an explicitly selected subtitle stream, timeout and output quota to produce SRT without re-encoding video. The existing DJISRTParser validates extracted telemetry before frame association. Do not assume all MP4 contain a supported subtitle stream; data tracks and burned-in captions are not automatically SRT. If multiple supported subtitle tracks exist and no unique DJI telemetry track can be validated, return TELEMETRY_AMBIGUOUS rather than choose silently.

Extraction outcomes: EXTRACTED_VALID, NO_SUBTITLE_STREAM, UNSUPPORTED_STREAM, INVALID_TELEMETRY, TELEMETRY_AMBIGUOUS, EXTRACTION_FAILED. Track presence is not valid GPS proof. Detection-only processing may continue on missing/unsupported telemetry under the existing missing-telemetry policy; results must explicitly mark unavailable location/measurement, never invent it. Tool failure remains a distinct visible error, not disguised as absent telemetry.

Persist extracted SRT as a derived private artifact with source video FileId/hash, stream index, extractor version, SRT hash and parsing status. BE pulls and registers this artifact through the same scoped registrar; preserve the original upload and dataset manifest. Do not append it to a frozen source manifest or mint an unregistered FileId. Define derived-artifact association through the Task 1 schema/ownership checkpoint.

Existing separately uploaded SRT stays supported. Prefer an explicitly paired verified SRT; otherwise attempt embedded extraction. Respect declared time offset and actual media timeline; test timestamp alignment, nonzero start time and variable-frame-rate input rather than assume frameIndex/FPS is exact.

CURRENT_VERIFIED source inspection: src/ingestion/srt_parser.py parses supplied SRT; src/pipeline/pipeline.py consumes supplied srt_path; src/inference/mvp_video.py has no SRT parameter. No FFmpeg/FFprobe invocation or dependency declaration was found in searched Python/dependency/Docker files; executable availability on local/Ubuntu is UNKNOWN. Real MP4 stream inspection and extraction: NOT_RUN.

## Owner answers and fresh main review — 2026-11-19 (supersedes conflicting proposals above)

TARGET_CONFIRMED:
- Return job acknowledgment promptly and progress asynchronously. AI proposes; PM reviews defects. DroneOperator is the video submitter.
- Flights normally produce approximately two-minute segments; start acceptance with the existing one-minute sample, then approximately five one-minute inputs. This is a pilot workload, not a new production upload cap.
- Missing positioning must be explicit and supplementable by the assigned operator. External SRT supplementation is a fallback when automatic extraction fails.
- Display drone position at detection time, not the defect's verified ground position. Crew supplies ground location during existing field work.
- Pinned AI submodule is accepted. Local verification precedes a separately reviewed Ubuntu rollout. Keep original/business artifacts; temporary retention still needs a concrete duration before enabling deletion.
- Storage is on the host running BE; the reported 500 GB disk's identity, mount, existing data and format permission are unverified. No disk mutation authorized by these answers.

CURRENT_VERIFIED fresh source:
- Cloned origin main into D:/Project BE/AI/roadguard-ai-engine-main-review-20261119, HEAD c689e544400fe326683c1c7f45115b3f7ec01daf. Existing AI checkout preserved.
- Compared with 555e80d: only two checkpoint files added; application code unchanged. FT95 SHA256 bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0, 5,467,674 bytes. Road-mask SHA256 1874b4751102a17e697e5747180e5fda17134cbe3bf72c07c5aee5633213eae8, 45,213,110 bytes. Presence/hash does not establish inference accuracy or GPU compatibility. These files are already tracked upstream; do not duplicate them into BE or Knowledge Space.
- AI src/api/video_review.py explicitly represents observations separately; no current tracking/physical identity. Review is available after completion. Keep local AI review UI separate from authoritative BE PM decisions.
- BE CandidateDecisionService restricts DecideAsync to ProjectManager and supports REJECT / KEEP_NEW / LINK_EXISTING. SurveyV2Service.SubmitDatasetAsync requires DroneOperator; upload also checks assigned survey authority. Existing processing job creation is manager-authorized, so operator upload does not itself prove automatic inference dispatch.
- Executed existing new-main DJISRTParser on the extracted one-minute SRT: 60 records, 60 with latitude/longitude, times 0–59000 ms. No coordinate values persisted into knowledge. Actual FT95 inference NOT_RUN.
- Queried local Knowledge Space using search_v3.py telemetry. Catalog is dated 20261027 and uses other worktree sources; its BE_LIVE labels do not establish freshness against this pinned BE workspace.

### Proposed minimal supplementation workflow

Keep supplementation scoped to an assigned survey video, separate from PM decisions and crew field observations. Expose video telemetry status and an operator action only when extraction is missing/unsupported/invalid/failed. An existing valid extracted SRT is not silently replaced; correction needs explicit revision semantics.

Assigned operator uploads SRT via existing verified upload machinery, then attaches its FileId to that video's supplementation record with expected version and idempotency key. Validate project/task/video ownership, content, cue bounds, time offset and coordinate ranges. Filename is a suggestion, not authority. Never overwrite source video/SRT or the frozen dataset/job manifest. Do not relax existing closed-task upload guards globally: introduce a narrowly authorized supplementation action if current guards prohibit it.

Store immutable telemetry revisions with source (embedded or operator-supplied), actor/time, source-video hash, SRT hash, parse outcome and offset. Re-run only parse/timestamp association against existing immutable detections. No GPU rerun or new candidate identity for a telemetry-only update. A read DTO can show latest associated drone position while each PM decision preserves its reviewed source/telemetry revision; no automatic change to approved defect, severity, repair or field location. Resolve exact version/concurrency integration with existing candidate source semantics in contract Task 1.

Separate states: processing state and telemetry state. A completed detector job can have missing/invalid telemetry; attaching SRT does not reset job to QUEUED. DronePosition has telemetry provenance and observation timestamp. GroundDefectPosition belongs to the current crew field workflow. No GPS interpolation beyond defined valid cue coverage; no default 0,0.

TARGET_CONFIRMED (owner selected 2.A, 2026-11-19): if no valid SRT exists, the assigned operator may record an approximate drone reference pin for an explicitly selected video time range plus note. Label OPERATOR_ESTIMATE, never measured GPS or ground-defect position. Preserve missing measured-telemetry status alongside the estimate.

TARGET_CONFIRMED (owner selected 1.A, 2026-11-19): automatically enqueue AI after the assigned operator successfully submits an eligible dataset containing verified uploads. PM reviews results rather than manually starting this normal flow. Implement a dedicated service workflow with operator provenance; do not grant operators the existing manager command permissions. Commit the dispatch intent atomically with eligible submission or through an equally durable transactional outbox, never call AI before the submission commits.

Transport proposal remains internal HTTP/JSON with streamed media and asynchronous polling. No local comparison benchmark supports declaring gRPC faster for this workload. Measure transfer, queue, inference and ingest separately before considering a transport migration.


### Acceptance criteria for confirmed automatic dispatch and manual fallback

- A rolled-back or unauthorized submission creates no AI dispatch. An identical submission replay does not enqueue a second logical run. Distinguish dataset revision, source video and processing configuration in durable identity; changing telemetry alone does not change inference identity.
- AI unavailability or a full worker queue leaves an observable durable BE pending item; successful upload/submission is not falsely reported as failed inference, and no submitted video is silently dropped. The five-clip pilot does not require five concurrent GPU jobs.
- An operator estimate belongs to an authorized video and explicit time interval within its duration. Validate finite latitude/longitude and interval bounds; store author, timestamp, reason and revision. Reject conflicting overlaps within one current revision; corrections create a new revision with concurrency checks. These validation mechanics are proposed implementation details for contract Task 1.
- Show the pin as approximate drone position only for covered observation timestamps. Outside that interval remains missing. Never interpolate it into a flight trajectory or use it for GSD/physical defect measurements.
- Valid parsed telemetry takes precedence in the current read view if later supplied; preserve estimate history. Neither supplementation nor precedence changes rewrite a PM decision's source snapshot or crew-confirmed ground location.
- PM remains the only candidate decision authority. Operator supplementation grants no defect approval, repair mutation or field-verification capability.

Review at decision checkpoint: business selections 1.A and 2.A are now confirmed; exact wire/schema adoption and measured resource limits remain implementation checkpoints. No further approval is required merely to implement these selected business behaviors within the assigned scope.

## Local storage constraint — 2026-11-19

TARGET_CONFIRMED: local testing has approximately 81 GB free on D:, not a spare 500 GB disk. The reported 500 GB device belongs to Ubuntu deployment only. CURRENT_VERIFIED: Get-PSDrive D reported 82.00 GiB free at this check; availability can change.

PROPOSED local budget: isolate mutable test data under D:/RoadGuard-AI-LocalTest, cap total incremental test footprint at 40 GiB and preserve at least 30 GiB free on D:. Include inputs, expanded frames, artifacts, Python/model caches, images, SQL/MinIO and container storage in accounting; do not assume Docker named volumes reside on D:. Preflight must inspect actual Docker/WSL storage placement and free space on every participating filesystem before creating the stack. No automatic Docker disk relocation or unrelated cleanup. Test one one-minute video first, then five clips with one inference at a time. Reuse read-only source/model files where possible; do not create redundant 1.32 GB source copies. Preserve original media and accepted evidence; cleanup only task-owned temporary data.

## Execution checkpoint — 2026-11-19

See [small-task ledger](../../docs/superpowers/plans/2026-11-19-be-ai-integration-execution.md)
for source audit, files, tests and limitations. CURRENT_VERIFIED: submodule pin,
BE coordinate primitive (12 tests), SRT extraction primitive (16 tests), actual
60-cue extraction and local capacity checks. Full integration NOT VERIFIED.

Dependent gates: active six-class FT95-to-BE mapping has no authoritative source
in code; shared-writer reservation/schema adoption is required before altering
the REAL attempt/artifact/telemetry persistence seams. Original Task 1 is still
open. Owner-confirmed PM authority, operator submission and missing-position
business decisions are not reopened by these technical gates.

### Owner checkpoint accepted — 2026-11-19

TARGET_CONFIRMED: owner assigns Codex/Anh the complete BE–AI vertical slice in
this worktree, including DbContext, additive migrations, processing/upload
persistence and DI. Huy temporarily does not edit those shared files. This
supersedes the prior Huy reservation for this slice only; one writer/file still
applies. No production migration/application or deployment is authorized here.

TARGET_CONFIRMED: preserve all six FT95 labels separately: alligator crack,
longitudinal crack, oblique crack, pothole, repair, transverse crack. `repair`
means an already patched region, not a newly confirmed defect. PM remains the
decision maker. Do not collapse crack subclasses or silently discard repair.

Implementation ruling: private worker admission is per source video and includes
datasetId/sourceVideoFileId. Persist exact six source labels and provenance;
explicit released-model mapping must resolve them to six distinct active BE
codes before dispatch. Fail readiness on missing/inactive/duplicate mapping,
rather than silently using the mock CRACK/POTHOLE catalog. Initial isolated
catalog may seed these six types only in the local test database.

## Owner adoption — full slice and FT95 classes

TARGET_CONFIRMED: owner assigns Codex/Anh the entire BE–AI slice, including DbContext, additive migration/snapshot, processing/upload persistence and DI; Huy temporarily does not edit these files. Preserve one writer/file. Owner retains all six FT95 labels individually; repair represents an existing patch and remains an AI proposal for PM review. No production deployment/migration/data write is implied.
