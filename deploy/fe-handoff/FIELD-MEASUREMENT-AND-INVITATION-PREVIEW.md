# FIELD measurements and invitation preview

## FIELD measurements

POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions keeps the existing measurements array. Each item uses its own sampleId, type, value, state, dimension, unit, instrument, method and location metadata. GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId} returns the same typed measurements from the immutable submission snapshot.

The accepted length types are DepressionDepth, DepressionLength, DepressionWidth, SlabFaultingHeight and ShoulderErosionExtent. DepressionDepth represents depression depth. DepressionLength and DepressionWidth represent the two separate depression dimensions. SlabFaultingHeight remains the slab height difference. Units for these length types are mm, cm or m and dimension is LENGTH. Area remains the existing AREA and m² type.

~~~json
{
  "measurements": [
    {
      "sampleId": "depth-01",
      "type": "DepressionDepth",
      "value": 12.5,
      "state": "KNOWN",
      "dimension": "LENGTH",
      "unit": "mm",
      "unknownReason": null,
      "instrument": "depth-gauge",
      "method": "manual",
      "longitude": null,
      "latitude": null,
      "locationReason": "GPS unavailable",
      "notes": "Measured at sample point"
    },
    {
      "sampleId": "length-01",
      "type": "DepressionLength",
      "value": 120,
      "state": "KNOWN",
      "dimension": "LENGTH",
      "unit": "cm",
      "unknownReason": null,
      "instrument": "tape",
      "method": "manual",
      "longitude": null,
      "latitude": null,
      "locationReason": "GPS unavailable",
      "notes": "Measured at sample point"
    },
    {
      "sampleId": "width-01",
      "type": "DepressionWidth",
      "value": 0.8,
      "state": "KNOWN",
      "dimension": "LENGTH",
      "unit": "m",
      "unknownReason": null,
      "instrument": "tape",
      "method": "manual",
      "longitude": null,
      "latitude": null,
      "locationReason": "GPS unavailable",
      "notes": "Measured at sample point"
    }
  ]
}
~~~

KNOWN requires a non-null non-negative value. Zero remains a known value. UNKNOWN requires a null value and unknownReason. Every sampleId must be unique within the submission. The server preserves the values and units in SQL and in API-096. Existing signed offline submissions use the same serialized facts and content hash. Repair intake uses the same validation adapter.

The repository has no approved defect-specific matrix that makes length or width mandatory by defect type. The current task-level requiredMeasurementType rule remains the only mandatory rule. FE should show the extra fields when the task instructions or measurement scope request them, while treating the defect-specific requirement as open until a catalog or policy supplies it. AI output is not converted into physical measurements.

Successful submission returns 201 with Location and ETag. Send the current task ETag in If-Match, a new Idempotency-Key for each logical submission, and the existing Bearer or Web cookie plus CSRF headers. Detail reads return 200. Validation failures use 400 validation_error, stale task versions use the existing concurrency error, and authorization follows the current FIELD task guard.

## Web invitation preview

Send the token from the email link in the JSON body over HTTPS.

~~~http
POST /api/v1/invitations/preview
Content-Type: application/json
~~~

~~~json
{ "invitationToken": "<token-from-email-link>" }
~~~

The response is 200 with role, projects containing id and name, expiresAt and inviterName when the inviter is still available. The endpoint is anonymous, read-only and returns Cache-Control: no-store. Invalid, expired, revoked or accepted tokens return the same 404 not_found. A missing or empty body token returns 400 validation_error.

After reading, remove the token from the address bar and keep it only for the explicit accept request. POST /api/v1/invitations/accept remains the account activation endpoint and keeps its current password policy. Preview does not consume the invitation or issue login credentials.
