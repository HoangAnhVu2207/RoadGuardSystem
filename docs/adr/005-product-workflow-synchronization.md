# ADR 005: Product workflow synchronization for reports, segments, and AI

## Status

Proposed target design — 2026-09-22. This ADR records the synchronized product direction; it does not authorize a schema migration or claim runtime implementation.

### Delivery governance amendment (2026-09-27)

[ADR 006](006-v2-endpoint-ownership-and-persistence-coordination.md) governs delivery ownership for any approved slice of this proposal. The assigned V2 task owner may implement every directly required N-layer and persistence change within the approved scope and shared-hotspot sequence. This amendment does not accept this ADR's proposed product behavior, close an open V2 gate, or authorize schema work outside a selected endpoint task.

## Decision

RoadGuard will use a reporter-to-repair workflow built from two related state views:

- `IncidentCase`: `New -> Assigned -> Open -> Fixed -> Retest -> Verified -> Closed`.
- Reporter-facing `ReportStatusEvent`: receiving, accepted, verifying, defect found, no defect with a PM-entered reason, repair progress, and repaired with PM-reviewed after-repair evidence.

`Defect.VERIFIED` remains the pre-repair domain decision that a defect is confirmed. `IncidentCase.VERIFIED` means that the repair retest passed. The names are intentionally scoped to their aggregates and must not be mapped by a shared enum.

The product adds `REPORTER` as a target role. `ReporterType` distinguishes `CITIZEN` and `INVESTOR_REPRESENTATIVE`; it does not grant either type project-wide access. A submitted report creates a `New` case with a nullable project until PM/Supervisor routing resolves the project. Each report photo carries its own coordinate and source (`DEVICE_CAPTURE`, `EXIF`, or `MANUAL`); the device location at upload is not substituted for the location of an old photo.

Supervisor or PM may establish a route from imported GIS/GeoJSON/GPX data or a MapLibre-edited polyline. A drone GPS track is a reference capture that PM reviews, not an automatic centerline. A published `RoadSegmentSet` belongs to one `RoadSectionVersion` and is immutable. PM may draft 100 m, 250 m, 500 m, 1 km, mixed, split, merge, or boundary-adjusted segments. Publishing a changed set creates a new version and preserves old jobs, video intervals, and AI results.

Survey work declares `TargetBand = SURFACE | LEFT_EDGE | RIGHT_EDGE`, with left/right defined in increasing route station direction. A deliberate lateral flight offset is valid when it is inside the planned corridor and the camera covers the requested band. Coverage is evaluated independently per segment, band, and survey data version. Raw aircraft GPS, projected route station, camera footprint, and defect location remain separate facts.

The external AI system is integrated through a versioned, asynchronous adapter. A processing input manifest identifies project, road version, segment set/segment, target band, source files and immutable video intervals, telemetry/SRT, model/configuration and checksum. Results identify the same input/model versions, return candidate detections plus frame/time/bbox/mask/location-quality evidence, and preserve raw payloads. Processing success or “no detections” never means “no defect”. PM decides whether to retain a detection, request physical inspection, create/confirm a Defect, propose repair, or ask for more coverage. Research validation retains its mandatory ground-truth track and never writes operational Defect/Warranty state.

PM repair input is limited to a general repair method summary for the approved scope. Financial data and detailed construction stages, materials, quantities, labor, equipment, procurement, and field execution planning are out of scope.

## Consequences

- FE can use MapLibre/Terra Draw to edit route geometry and preview segment sets; BE remains the authority for geometry validation, lengths, station offsets, versions and coverage status.
- One original video can support many segment/band analysis blocks. AI jobs are retry-safe and deduplicated by input manifest, scope, model and configuration; old results remain queryable after segment set changes.
- The system can distinguish a missing left-edge view from a clean left-edge view, and a deliberate offset from a GPS failure.
- Existing scheduled baseline/periodic survey planning remains valid. A pre-segment `RouteCapture` task is a separate target extension; it must not be simulated by omitting the current road-version requirement.
- Implementation must add the target role, aggregates, contracts, persistence, permissions, worker/adapter and focused tests in separate approved slices. Until then, documents describe the target only.

## Rejected interpretations

- Treating every report as a confirmed Defect or allowing AI to transition Defect/repair states automatically.
- Requiring physical measurement for every retained AI result even when PM has sufficient verified evidence; PM may require field measurement where the evidence needs it, and the research track remains mandatory separately.
- Changing the canonical route after every flight or snapping every later GPS point to the route to make coverage pass.
- Treating one coordinate per kilometer as a complete curved route description.
- Sending the entire project or a separate copied original video for each segment when a scoped immutable manifest and source interval are sufficient.
