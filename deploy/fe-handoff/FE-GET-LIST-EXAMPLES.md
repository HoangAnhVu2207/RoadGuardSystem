> V9 adoption: G02-G06 collections are owner-approved and implemented. See [FE-APPROVED-COLLECTIONS.md](FE-APPROVED-COLLECTIONS.md) for exact routes/wire, authority, evidence and limits. Older collection-pending language below is historical. Canonical241 coverage unchanged; eleven new GETs are additive. A1/B2/C1 remain gated. No FE_READY or FULL_CONTRACT promotion.

# FE GET list examples — Task32A

CURRENT_VERIFIED: actual HTTP responses from a new disposable `rg-task32a-source` environment. Actor credentials/tokens are generated privately and omitted. GUIDs and row versions are synthetic run-specific values; obtain current IDs from preceding responses. Do not paste these IDs into another environment.

These are bounded assertions, not FULL_CONTRACT or FE_INTEGRATION_READY acceptance. Source HEAD `dd94aa697c411bb11d305973580055216f65ba73`.

## anonymous-projects

`GET /api/v1/projects` → HTTP 401

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required or the supplied credential is no longer valid.",
  "instance": "/api/v1/projects",
  "code": "auth_unauthorized",
  "correlationId": "cff4fdde-35ee-4b5f-aaef-9b36e66ea093"
}
```

## reporter-projects-forbidden

`GET /api/v1/projects` → HTTP 403

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "The project request could not be completed.",
  "instance": "/api/v1/projects",
  "code": "access_forbidden",
  "correlationId": "57166802-b2a0-4f95-8350-99d54c8ac8af"
}
```

## pm-a-projects

`GET /api/v1/projects` → HTTP 200

```json
{
  "items": [
    {
      "projectId": "72f84201-c001-4000-8000-000000000001",
      "projectCode": "SBX-A",
      "name": "Synthetic Project A",
      "status": "ACTIVE",
      "primaryProjectManagerUserId": "72f84201-a001-4000-8000-000000000002",
      "primaryProjectManagerName": "Synthetic pm-a"
    }
  ],
  "nextCursor": null,
  "asOf": "2026-10-20T16:10:40.9179309+00:00"
}
```

## pm-b-projects

`GET /api/v1/projects` → HTTP 200

```json
{
  "items": [
    {
      "projectId": "72f84201-c001-4000-8000-000000000002",
      "projectCode": "SBX-B",
      "name": "Synthetic Project B",
      "status": "ACTIVE",
      "primaryProjectManagerUserId": "72f84201-a001-4000-8000-000000000003",
      "primaryProjectManagerName": "Synthetic pm-b"
    }
  ],
  "nextCursor": null,
  "asOf": "2026-10-20T16:10:42.9719239+00:00"
}
```

## supervisor-global-projects

`GET /api/v1/projects?limit=1` → HTTP 200

```json
{
  "items": [
    {
      "projectId": "72f84201-c001-4000-8000-000000000001",
      "projectCode": "SBX-A",
      "name": "Synthetic Project A",
      "status": "ACTIVE",
      "primaryProjectManagerUserId": "72f84201-a001-4000-8000-000000000002",
      "primaryProjectManagerName": "Synthetic pm-a"
    }
  ],
  "nextCursor": "NjM5MjgxMDkxMzIwMjc4NzAxfDcyZjg0MjAxLWMwMDEtNDAwMC04MDAwLTAwMDAwMDAwMDAwMQ==",
  "asOf": "2026-10-20T16:10:45.0356733+00:00"
}
```

## pm-a-road-discovery

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000001/work-package` → HTTP 200

```json
{
  "projectId": "72f84201-c001-4000-8000-000000000001",
  "projectCode": "SBX-A",
  "name": "Synthetic Project A",
  "description": "Synthetic sandbox; not operational evidence",
  "engineeringUtmSrid": 32648,
  "status": "ACTIVE",
  "startDate": null,
  "endDate": null,
  "accessRole": "PM",
  "rowVersion": "AAAAAAAAB/I=",
  "roadSections": [
    {
      "roadSectionId": "73d83802-2093-4643-b092-c0257a4a718e",
      "code": "DEMO-A",
      "name": "Synthetic demo road A",
      "currentVersionId": "e934ded3-8bfe-46c7-8530-8bde998abd33",
      "versionNo": 1,
      "geometryWkt": "LINESTRING (500000 1200000, 500250 1200250)",
      "srid": 32648,
      "effectiveFrom": "2026-01-01T00:00:00+00:00",
      "changeReason": "Development synthetic scenario"
    }
  ],
  "warranties": []
}
```

## cross-project-road-denied

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000002/work-package` → HTTP 403

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "The current user does not have access to the requested project.",
  "instance": "/api/v1/projects/72f84201-c001-4000-8000-000000000002/work-package",
  "code": "project_access_forbidden",
  "correlationId": "c7049213-3d08-46c1-a4eb-99a832ca4fef"
}
```

## supervisor-road-global

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000002/work-package` → HTTP 200

```json
{
  "projectId": "72f84201-c001-4000-8000-000000000002",
  "projectCode": "SBX-B",
  "name": "Synthetic Project B",
  "description": "Synthetic sandbox; not operational evidence",
  "engineeringUtmSrid": 32648,
  "status": "ACTIVE",
  "startDate": null,
  "endDate": null,
  "accessRole": "SUPERVISOR",
  "rowVersion": "AAAAAAAAB/M=",
  "roadSections": [
    {
      "roadSectionId": "6452aad3-0058-4edf-bb96-519bf1e8fceb",
      "code": "DEMO-B",
      "name": "Synthetic demo road B",
      "currentVersionId": "20556032-46a1-4050-b571-c69b9274970f",
      "versionNo": 1,
      "geometryWkt": "LINESTRING (500000 1200000, 500250 1200250)",
      "srid": 32648,
      "effectiveFrom": "2026-01-01T00:00:00+00:00",
      "changeReason": "Development synthetic scenario"
    }
  ],
  "warranties": []
}
```

## road-geometry-known-id

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000001/road-sections/73d83802-2093-4643-b092-c0257a4a718e/versions/e934ded3-8bfe-46c7-8530-8bde998abd33/geometry` → HTTP 200

```json
{
  "projectId": "72f84201-c001-4000-8000-000000000001",
  "roadSectionId": "73d83802-2093-4643-b092-c0257a4a718e",
  "routeVersionId": "e934ded3-8bfe-46c7-8530-8bde998abd33",
  "versionNo": 1,
  "isCurrent": true,
  "effectiveFrom": "2026-01-01T00:00:00+00:00",
  "sourceDraftId": null,
  "sourceCrs": null,
  "engineeringSrid": 32648,
  "stationOriginMeters": null,
  "widthProfile": null,
  "surveyWidthMeters": null,
  "metricCenterline": {
    "type": "LineString",
    "coordinates": [
      [
        500000,
        1200000
      ],
      [
        500250,
        1200250
      ]
    ]
  },
  "wgs84Centerline": null,
  "lengthMeters": 353.5533905932738,
  "metadataStatus": "LEGACY_INCOMPLETE",
  "approvedBy": null,
  "approvedAt": null,
  "geometryHash": "df0e8325b28a83305da341e4b4d00fe1815750b1682caf891a3b6891a53fb931",
  "crsProfileRevisionId": null,
  "routeSystemId": null,
  "routeKind": null,
  "parentRouteVersionId": null,
  "junctionOffsetMeters": null,
  "declaredLengthMeters": null,
  "chainageCalibration": null,
  "crsStatus": null,
  "sampleOnly": false
}
```

## road-wrong-parent

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000001/road-sections/73d83802-2093-4643-b092-c0257a4a718e/versions/1c4ca7b0-0aa1-4710-81c5-539b0e033400/geometry` → HTTP 404

```json
{
  "title": "Geometry request failed",
  "status": 404,
  "detail": "The geometry request could not be completed.",
  "instance": "/api/v1/projects/72f84201-c001-4000-8000-000000000001/road-sections/73d83802-2093-4643-b092-c0257a4a718e/versions/1c4ca7b0-0aa1-4710-81c5-539b0e033400/geometry",
  "code": "road_section_version_not_found",
  "correlationId": "5f4d3e99-9b7d-412e-8b6e-fda47b2ee492"
}
```

## pm-members

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000001/members` → HTTP 200

```json
{
  "items": [
    {
      "membershipId": "72f84201-b001-4000-8000-000000000002",
      "userId": "72f84201-a001-4000-8000-000000000002",
      "displayName": "Synthetic pm-a",
      "role": "PM",
      "isPrimary": true,
      "status": "ACTIVE",
      "validFrom": "2000-01-01",
      "validTo": null,
      "membershipVersion": "AAAAAAAAB94="
    },
    {
      "membershipId": "72f84201-b001-4000-8000-000000000006",
      "userId": "72f84201-a001-4000-8000-000000000006",
      "displayName": "Synthetic operator-a",
      "role": "OPERATOR",
      "isPrimary": false,
      "status": "ACTIVE",
      "validFrom": "2000-01-01",
      "validTo": null,
      "membershipVersion": "AAAAAAAAB+A="
    },
    {
      "membershipId": "72f84201-b001-4000-8000-000000000008",
      "userId": "72f84201-a001-4000-8000-000000000008",
      "displayName": "Synthetic crew-a",
      "role": "CREW",
      "isPrimary": false,
      "status": "ACTIVE",
      "validFrom": "2000-01-01",
      "validTo": null,
      "membershipVersion": "AAAAAAAAB+I="
    }
  ],
  "nextCursor": null,
  "asOf": "2026-10-20T16:11:10.5720867+00:00"
}
```

## cross-project-members

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000002/members` → HTTP 403

```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "Forbidden",
  "instance": "/api/v1/projects/72f84201-c001-4000-8000-000000000002/members",
  "code": "access_forbidden",
  "correlationId": "f3878dde-c6b9-4b95-9d18-8416b0c65cb0"
}
```

## reporter-own-reports

`GET /api/v1/reports` → HTTP 200

```json
{
  "items": [
    {
      "id": "29f3ab4a-dffd-487f-9843-12af777143e7",
      "description": "Synthetic FE demo incident; no approved diagnosis or repair state.",
      "createdAt": "2026-10-20T16:06:06.0974945+00:00",
      "version": "AAAAAAAAB/s=",
      "evidence": [
        {
          "id": "9eb257b7-11cc-4b48-8fee-80fcee212a0f",
          "fileId": "3a46a9e4-d7ed-4a09-9734-757a8045952b",
          "fileVersion": "AAAAAAAAB/k=",
          "supplementId": null,
          "capturedAt": null,
          "locationSource": "UNKNOWN",
          "location": null
        }
      ],
      "routingStatus": "UNASSIGNED",
      "publicUpdates": [],
      "supplements": []
    }
  ],
  "nextCursor": null
}
```

## defect-project-list

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000001/defects` → HTTP 200

```json
{
  "items": [],
  "nextCursor": null
}
```

## defect-cross-project

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000002/defects` → HTTP 403

```json
{
  "title": "Defect request failed",
  "status": 403,
  "detail": "The defect request could not be completed.",
  "instance": "/api/v1/projects/72f84201-c001-4000-8000-000000000002/defects",
  "code": "access_forbidden",
  "correlationId": "000aef3a-19bc-4fbe-aa95-92c2a2f70a7d"
}
```

## notifications-own-inbox

`GET /api/v1/notifications` → HTTP 200

```json
{
  "items": [],
  "nextCursor": null,
  "asOf": "2026-10-20T16:11:39.5819424+00:00"
}
```

## revoked-road-read

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000001/work-package` → HTTP 403

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "The current user does not have access to the requested project.",
  "instance": "/api/v1/projects/72f84201-c001-4000-8000-000000000001/work-package",
  "code": "project_access_forbidden",
  "correlationId": "39694207-542b-4f6d-ab6e-763cc27928e2"
}
```

## revoked-members-read

`GET /api/v1/projects/72f84201-c001-4000-8000-000000000001/members` → HTTP 403

```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "Forbidden",
  "instance": "/api/v1/projects/72f84201-c001-4000-8000-000000000001/members",
  "code": "access_forbidden",
  "correlationId": "922d2c7c-2adb-40f2-8581-199019ff9caa"
}
```

## Discovery paths without a supported example

| FE need | Current result |
|---|---|
| Segment list from a fresh road selection | BLOCKED_OWNER_POLICY / MISSING_GET_REQUIRED G01. Known-set GET API-194 exists; no Segment Set catalog supplies its required setId. API-104 without segmentSetId returned 400 in this run. Existing geometry SQL test covers known-set read, not unknown-set discovery. |
| FIELD task lists API-037/API-078 | Source inspected. No supported assigned FIELD scenario executed in this handoff; example NOT_RUN. Do not infer nonempty task visibility from Project access. |
| Repair package/item list | MISSING_GET_REQUIRED G02; known-item history and assigned FIELD repairContext do not provide a general catalog. |
| Fast Track list | MISSING_GET_REQUIRED / BLOCKED_OWNER_POLICY G03; no approved collection route. |
| Export jobs list | MISSING_GET_REQUIRED G04; create then retain exportId. GET by known ID/content/manifest is not list discovery. |
| Available assignees | Project members provide a candidate pool with current scope. Actual assignment command rechecks task-specific eligibility; no claim that every member is assignable. |

See FE-DISCOVERY-GAPS.md for route/policy decisions and FE-GET-LIST-AUTH-MATRIX.md for role predicates. Empty inbox/case/defect responses prove those executed empty reads only; they do not prove populated producer workflows. Existing regression results are recorded separately.
