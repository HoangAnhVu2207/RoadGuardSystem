# Case Report details for PM and Supervisor

Status: CURRENT_VERIFIED in current source on `huy-review` (2026-11-04). This is an additive read contract. The older packaged `openapi.json` and `FE-API-CATALOG.json` are frozen preview snapshots and do not contain these routes. Use the Development runtime `/swagger/v1/swagger.json` for the current generated OpenAPI; include a newly generated artifact when assembling the next FE package.

## Routes and Web transport

Both routes are API v1, require an authenticated Supervisor or Project Manager, and accept the existing Bearer access token or eligible HTTPS Web session cookie. For cookie requests, send credentials according to the existing Web auth client. Neither GET needs an idempotency key or CSRF header. Responses set `Cache-Control: private, no-store`.

| Method and path | Result |
| --- | --- |
| `GET /api/v1/cases/{caseId}/reports?limit=20&cursor=...` | `200` JSON page of currently linked Reports. `limit` defaults to 20 and accepts 1-100. |
| `GET /api/v1/cases/{caseId}/reports/{reportId}/evidence/{evidenceId}/content` | `200` image bytes (`image/jpeg` or `image/png`) and file-version `ETag`. Use `contentPath` returned by the list. |

The existing `GET /api/v1/cases/{caseId}` retains its response shape. Reporter owner-only Report and file endpoints retain their existing authorization.

## Response example

```json
{
  "items": [
    {
      "caseId": "11111111-1111-1111-1111-111111111111",
      "reportId": "22222222-2222-2222-2222-222222222222",
      "reporterUserId": "33333333-3333-3333-3333-333333333333",
      "reporterDisplayName": "Nguyen Van A",
      "reporterPhoneNumber": null,
      "reporterPhoneNumberConfirmed": false,
      "description": "Noi dung goc cua nguoi gui",
      "receivedAt": "2026-11-04T08:00:00+00:00",
      "originalEvidence": [
        {
          "evidenceId": "44444444-4444-4444-4444-444444444444",
          "fileId": "55555555-5555-5555-5555-555555555555",
          "fileVersion": "AQAAAA==",
          "supplementId": null,
          "capturedAt": null,
          "locationSource": "UNKNOWN",
          "location": null,
          "contentPath": "/api/v1/cases/11111111-1111-1111-1111-111111111111/reports/22222222-2222-2222-2222-222222222222/evidence/44444444-4444-4444-4444-444444444444/content"
        }
      ],
      "supplements": [
        {
          "id": "66666666-6666-6666-6666-666666666666",
          "description": "Noi dung bo sung",
          "receivedAt": "2026-11-04T09:00:00+00:00",
          "evidence": []
        }
      ]
    }
  ],
  "nextCursor": null
}
```

`reporterDisplayName`, `reporterPhoneNumber`, and `reporterPhoneNumberConfirmed` come from the Report owner's **current account**, not a submission snapshot. They may change after submission. Missing phone is `null`; the confirmation flag is the server-held current value, not inferred from phone presence. `receivedAt` and supplement `receivedAt` are server receipt times; `capturedAt` is stored evidence capture time. Do not substitute one for another.

Evidence remains under its original Report or a particular supplement. `location` contains nullable `latitude`, `longitude`, and `accuracyMeters` when stored; otherwise it is `null`. `locationSource` uses the stored enum (`CAPTURE`, etc.) and `UNKNOWN` when metadata is absent. Missing coordinates/time remain null. The API does not derive GPS from a Case, project, address, or another photo.

The page orders Reports by `receivedAt` descending, then `reportId` descending. Supplements within each Report order by `receivedAt` ascending, then `id` ascending; evidence order by `evidenceId` ascending. Follow `nextCursor` with the same Case, actor, and `limit` until null. The cursor is opaque and bound to those values. Each page includes all supplements and evidence for its Reports. A link change between page requests can change the visible result; restart pagination after a merge/split.

## Authorization and images

Supervisor may read an unassigned Case under the current Case guard. PM needs current authority on the Case's project; an unassigned or other-project Case returns `403`. Other roles return `403`. For image content, the server checks current Case authority, active Report-Case link, evidence membership, private Report-photo scope, verified file state, and pinned file version before opening storage. An old Case's `contentPath` can stop working after a merge/split; refresh the list for the new Case. Use an authenticated request for the image and display its returned bytes as a Blob/object URL in Web. Revoke that browser object URL when no longer needed.

This API does **not** issue signed URLs, so there is no signed-URL TTL. Authorization is checked when each content request begins; a response stream already opened cannot be recalled retroactively if rights change during transfer. The image is never made public. Do not persist private image bytes or authenticated URLs in a shared cache.

Errors use `application/problem+json` with `status`, `code`, and `correlationId`: `400 validation_error` (invalid limit/cursor), `401 auth_unauthorized` (missing/invalid identity), `403 access_forbidden`, `404 not_found` (Case, current link, or evidence unavailable), `409 source_not_ready` (missing owner/file or unverified file), `412 concurrency_conflict` (Case scope or file version changed), and `503 dependency_unavailable` (storage unavailable). Auth middleware can produce its own `401` response before the controller runs.

The HTTP/SQL fixture checks backend behavior with fake object storage. It does not verify a real browser, MinIO transfer, or deployed FE integration.
