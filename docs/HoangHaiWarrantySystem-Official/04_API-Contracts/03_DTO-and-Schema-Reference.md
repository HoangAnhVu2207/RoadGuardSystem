# DTO and schema reference

**CURRENT_VERIFIED — documentation/source discovery, 2026-10-27; BE `01cc4725b178199215b02f4ddf7093491a16f2f7` (`huy-review`).**

259 controller operations / 229 paths / 50 controllers. Canonical catalog retains 260 IDs: API-001–API-241 and ADD-01–ADD-19; ADD-19 is conditional operational `/ready`. Source-built Swagger excludes operational minimal endpoints. No runtime/consumer/full-contract acceptance is granted by this inventory.

Exact Swagger facets below; absent nullable is not nullable=true. Implementation annotations and manual source-backed operation filters can add constraints; no accepted runtime examples are invented.

## Schema AcceptInvitationRequestDto
```json
{
  "required": [
    "displayName",
    "invitationToken",
    "password"
  ],
  "type": "object",
  "properties": {
    "invitationToken": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "displayName": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "password": {
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema AccountUpdateRequestDto
```json
{
  "required": [
    "reason",
    "role",
    "status"
  ],
  "type": "object",
  "properties": {
    "status": {
      "minLength": 1,
      "type": "string",
      "description": "Accepted values after trimming and case normalization: ACTIVE, SUSPENDED."
    },
    "role": {
      "minLength": 1,
      "type": "string",
      "description": "Accepted values after trimming and case normalization: SUPERVISOR, PM, OPERATOR, CREW, REPORTER."
    },
    "reason": {
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema ActiveCandidateDisposition
```json
{
  "type": "object",
  "properties": {
    "decisionId": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ActorResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "displayName": {
      "type": "string",
      "nullable": true
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AdminPasswordResetRequestDto
```json
{
  "required": [
    "expectedTargetRowVersion"
  ],
  "type": "object",
  "properties": {
    "expectedTargetRowVersion": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema AdminPasswordResetResponseDto
```json
{
  "type": "object",
  "properties": {
    "userId": {
      "type": "string",
      "format": "uuid"
    },
    "mustChangePassword": {
      "type": "boolean"
    },
    "temporaryPassword": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AdminResetPasswordV2RequestDto
```json
{
  "required": [
    "reason",
    "temporaryPassword"
  ],
  "type": "object",
  "properties": {
    "temporaryPassword": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "reason": {
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema AiCandidateFactsV1
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "detectionId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "datasetVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "runId": {
      "type": "string",
      "format": "uuid"
    },
    "jobId": {
      "type": "string",
      "format": "uuid"
    },
    "attemptId": {
      "type": "string",
      "format": "uuid"
    },
    "resultId": {
      "type": "string",
      "format": "uuid"
    },
    "manifestHash": {
      "type": "string",
      "nullable": true
    },
    "resultHash": {
      "type": "string",
      "nullable": true
    },
    "fixtureVersion": {
      "type": "string",
      "nullable": true
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "sourceVideoFileId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVideoFileVersion": {
      "type": "string",
      "nullable": true
    },
    "frameFileId": {
      "type": "string",
      "format": "uuid"
    },
    "frameFileVersion": {
      "type": "string",
      "nullable": true
    },
    "frameSha256": {
      "type": "string",
      "nullable": true
    },
    "frameSizeBytes": {
      "type": "integer",
      "format": "int64"
    },
    "frameMediaType": {
      "type": "string",
      "nullable": true
    },
    "timestampMs": {
      "type": "integer",
      "format": "int64"
    },
    "typeCode": {
      "type": "string",
      "nullable": true
    },
    "confidence": {
      "type": "number",
      "format": "double"
    },
    "boundingBox": {
      "$ref": "#/components/schemas/NormalizedBoundingBoxV1"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "geometryVersion": {
      "type": "string",
      "nullable": true
    },
    "positionStatus": {
      "type": "string",
      "nullable": true
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "disposition": {
      "$ref": "#/components/schemas/ActiveCandidateDisposition"
    },
    "dispositionVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AiDetectionDto
```json
{
  "required": [
    "bbox",
    "detectionId",
    "typeCode"
  ],
  "type": "object",
  "properties": {
    "detectionId": {
      "type": "string",
      "description": "Parseable UUID in a Guid.TryParse accepted form.",
      "format": "uuid"
    },
    "frameFileId": {
      "type": "string",
      "format": "uuid"
    },
    "timestampMs": {
      "minimum": 0,
      "type": "integer",
      "format": "int32"
    },
    "typeCode": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "confidence": {
      "maximum": 1,
      "minimum": 0,
      "type": "number",
      "format": "double"
    },
    "bbox": {
      "maxItems": 4,
      "minItems": 4,
      "type": "array",
      "items": {
        "maximum": 1,
        "minimum": 0,
        "type": "number",
        "format": "double"
      },
      "description": "Exactly four normalized values [x, y, width, height], each in [0,1]; x + width <= 1 and y + height <= 1.",
      "x-roadguard-cross-element-constraints": [
        "x + width <= 1",
        "y + height <= 1"
      ]
    }
  },
  "additionalProperties": false
}
```

## Schema AiDetectionResultV1
```json
{
  "type": "object",
  "properties": {
    "detectionId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVideoFileId": {
      "type": "string",
      "format": "uuid"
    },
    "frameFileId": {
      "type": "string",
      "format": "uuid"
    },
    "frameFileVersion": {
      "type": "string",
      "nullable": true
    },
    "timestampMs": {
      "type": "integer",
      "format": "int64"
    },
    "typeCode": {
      "type": "string",
      "nullable": true
    },
    "confidence": {
      "type": "number",
      "format": "double"
    },
    "bbox": {
      "type": "array",
      "items": {
        "type": "number",
        "format": "double"
      },
      "nullable": true
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "positionStatus": {
      "type": "string",
      "nullable": true
    },
    "geometry": {
      "nullable": true,
      "readOnly": true
    }
  },
  "additionalProperties": false
}
```

## Schema AiMatchResultV1
```json
{
  "type": "object",
  "properties": {
    "detectionId": {
      "type": "string",
      "format": "uuid"
    },
    "candidateDefectId": {
      "type": "string",
      "format": "uuid"
    },
    "candidateVersion": {
      "type": "string",
      "nullable": true
    },
    "rank": {
      "type": "integer",
      "format": "int32"
    },
    "score": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "reasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AiMockRunView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "stage": {
      "type": "string",
      "nullable": true
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "processingJobId": {
      "type": "string",
      "format": "uuid"
    },
    "attemptId": {
      "type": "string",
      "format": "uuid"
    },
    "manifestHash": {
      "type": "string",
      "nullable": true
    },
    "resultId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "fixtureVersion": {
      "type": "string",
      "nullable": true
    },
    "errorCode": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AiMockScopeRequest
```json
{
  "type": "object",
  "properties": {
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "targetBand": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AiResultV1
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "runId": {
      "type": "string",
      "format": "uuid"
    },
    "stage": {
      "type": "string",
      "nullable": true
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "jobId": {
      "type": "string",
      "format": "uuid"
    },
    "attemptId": {
      "type": "string",
      "format": "uuid"
    },
    "manifestHash": {
      "type": "string",
      "nullable": true
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "fixtureVersion": {
      "type": "string",
      "nullable": true
    },
    "resultHash": {
      "type": "string",
      "nullable": true
    },
    "completedAt": {
      "type": "string",
      "format": "date-time"
    },
    "detections": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/AiDetectionResultV1"
      },
      "nullable": true
    },
    "matches": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/AiMatchResultV1"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AsBuiltLayoutInput
```json
{
  "type": "object",
  "properties": {
    "sourcePlanId": {
      "type": "string",
      "format": "uuid"
    },
    "slabs": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/AsBuiltSlabInput"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AsBuiltSlabInput
```json
{
  "type": "object",
  "properties": {
    "key": {
      "type": "string",
      "nullable": true
    },
    "plannedSequence": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "footprint": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryPoint"
      },
      "nullable": true
    },
    "lengthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "widthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "dimensionUnknownReason": {
      "type": "string",
      "nullable": true
    },
    "source": {
      "type": "string",
      "nullable": true
    },
    "sourceFileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AssessmentEvidenceDto
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "fromMilliseconds": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "toMilliseconds": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AssessmentItemDto
```json
{
  "type": "object",
  "properties": {
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentId": {
      "type": "string",
      "format": "uuid"
    },
    "targetBand": {
      "type": "string",
      "nullable": true
    },
    "positionStatus": {
      "enum": [
        "PASS",
        "FAIL",
        "UNKNOWN"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "qualityStatus": {
      "enum": [
        "PASS",
        "FAIL",
        "UNKNOWN"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "coverageStatus": {
      "enum": [
        "PASS",
        "FAIL",
        "UNKNOWN"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "evidence": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/AssessmentEvidenceDto"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AssessmentView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "datasetId": {
      "type": "string",
      "format": "uuid"
    },
    "methodVersion": {
      "type": "string",
      "nullable": true
    },
    "reviewedBy": {
      "type": "string",
      "format": "uuid"
    },
    "reviewedAt": {
      "type": "string",
      "format": "date-time"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/AssessmentItemDto"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AuthActorResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "displayName": {
      "type": "string",
      "nullable": true
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema AuthTokenResponseDto
```json
{
  "type": "object",
  "properties": {
    "accessToken": {
      "type": "string",
      "nullable": true
    },
    "refreshToken": {
      "type": "string",
      "nullable": true
    },
    "tokenType": {
      "type": "string",
      "nullable": true
    },
    "expiresIn": {
      "type": "integer",
      "format": "int32"
    },
    "mustChangePassword": {
      "type": "boolean"
    },
    "user": {
      "$ref": "#/components/schemas/AuthActorResponseDto"
    }
  },
  "additionalProperties": false
}
```

## Schema BandScopeDto
```json
{
  "required": [
    "targetBand"
  ],
  "type": "object",
  "properties": {
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentIds": {
      "minItems": 1,
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "description": "",
      "nullable": true
    },
    "targetBand": {
      "minLength": 1,
      "enum": [
        "SURFACE",
        "LEFT_EDGE",
        "RIGHT_EDGE"
      ],
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema BaselineItemDto
```json
{
  "type": "object",
  "properties": {
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentId": {
      "type": "string",
      "format": "uuid"
    },
    "targetBand": {
      "type": "string",
      "nullable": true
    },
    "datasetId": {
      "type": "string",
      "format": "uuid"
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid"
    },
    "expectedBaselineSelectionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema BaselineSelectedItemDto
```json
{
  "type": "object",
  "properties": {
    "selectionId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentId": {
      "type": "string",
      "format": "uuid"
    },
    "targetBand": {
      "type": "string",
      "nullable": true
    },
    "datasetId": {
      "type": "string",
      "format": "uuid"
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema BaselineView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BaselineSelectedItemDto"
      },
      "nullable": true
    },
    "selectedBy": {
      "type": "string",
      "format": "uuid"
    },
    "selectedAt": {
      "type": "string",
      "format": "date-time"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema BusinessDutyAppointmentInput
```json
{
  "type": "object",
  "properties": {
    "assigneeId": {
      "type": "string",
      "format": "uuid"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema BusinessDutyView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "scopeId": {
      "type": "string",
      "format": "uuid"
    },
    "responsibleActorId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "responsibleRole": {
      "type": "string",
      "nullable": true
    },
    "requestedAt": {
      "type": "string",
      "format": "date-time"
    },
    "receivingState": {
      "type": "string",
      "nullable": true
    },
    "waitingSeconds": {
      "type": "number",
      "format": "double"
    },
    "acknowledgmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "acknowledgedBy": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "acknowledgedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "claimedDeviceAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "clockId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "originalDueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "currentDueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "completedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "appointments": {
      "type": "array",
      "items": {
        "required": [
          "id",
          "previousActorId",
          "currentActorId",
          "decisionActorId",
          "reason",
          "effectiveAt"
        ],
        "type": "object",
        "properties": {
          "id": {
            "type": "string",
            "format": "uuid"
          },
          "previousActorId": {
            "type": "string",
            "format": "uuid",
            "nullable": true
          },
          "currentActorId": {
            "type": "string",
            "format": "uuid"
          },
          "decisionActorId": {
            "type": "string",
            "format": "uuid"
          },
          "reason": {
            "type": "string"
          },
          "effectiveAt": {
            "type": "string",
            "format": "date-time"
          }
        },
        "additionalProperties": false
      }
    }
  },
  "additionalProperties": false
}
```

## Schema BusinessRequestAckInput
```json
{
  "type": "object",
  "properties": {
    "claimedDeviceAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateClassificationDto
```json
{
  "type": "object",
  "properties": {
    "defectTypeCode": {
      "type": "string",
      "nullable": true
    },
    "causeCategoryCode": {
      "type": "string",
      "nullable": true
    },
    "severity": {
      "enum": [
        "LOW",
        "MEDIUM",
        "HIGH",
        "CRITICAL"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "roadSectionVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateDecisionRequestDto
```json
{
  "type": "object",
  "properties": {
    "sourceKind": {
      "enum": [
        "REPORT",
        "AI_DETECTION",
        "FIELD_OBSERVATION"
      ],
      "type": "string",
      "description": "FIELD_OBSERVATION is recognized but has source-deferred admission; enum does not establish source authority.",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "geometryVersion": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "decision": {
      "enum": [
        "REJECT",
        "KEEP_NEW",
        "LINK_EXISTING"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "targetDefectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "targetVersion": {
      "type": "string",
      "nullable": true
    },
    "classification": {
      "$ref": "#/components/schemas/CandidateClassificationDto"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "supersedesDecisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "previousDecisionVersion": {
      "type": "string",
      "nullable": true
    },
    "recurrence": {
      "$ref": "#/components/schemas/CandidateRecurrenceDto"
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateDecisionResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "decision": {
      "type": "string",
      "nullable": true
    },
    "defectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "supersedesDecisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateMatchHistoryDto
```json
{
  "type": "object",
  "properties": {
    "sourceLinkCount": {
      "type": "integer",
      "format": "int32"
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateMatchItemDto
```json
{
  "type": "object",
  "properties": {
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "priorityGroup": {
      "type": "integer",
      "format": "int32"
    },
    "distanceMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "accuracyMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "reasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "evidenceRefs": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "historySummary": {
      "$ref": "#/components/schemas/CandidateMatchHistoryDto"
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateMatchPageDto
```json
{
  "type": "object",
  "properties": {
    "source": {
      "$ref": "#/components/schemas/CandidateMatchSourceDto"
    },
    "geometryVersion": {
      "type": "string",
      "nullable": true
    },
    "algorithmVersion": {
      "type": "string",
      "nullable": true
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/CandidateMatchItemDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateMatchSourceDto
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "nullable": true
    },
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CandidateRecurrenceDto
```json
{
  "type": "object",
  "properties": {
    "previousDefectId": {
      "type": "string",
      "format": "uuid"
    },
    "priorRepairDecisionId": {
      "type": "string",
      "format": "uuid"
    },
    "evidenceFileIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "expectedLifecycleVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CaseConclusionDto
```json
{
  "type": "object",
  "properties": {
    "outcome": {
      "enum": [
        "CONFIRMED",
        "NO_DEFECT",
        "NEEDS_EVIDENCE"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "defectIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CaseConclusionViewDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "outcome": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "concludedAt": {
      "type": "string",
      "format": "date-time"
    },
    "defectIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CaseLinkDto
```json
{
  "type": "object",
  "properties": {
    "reportIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "sourceCaseVersions": {
      "type": "object",
      "additionalProperties": {
        "type": "string"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CasePageDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/InternalCaseDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CasePublicationDto
```json
{
  "type": "object",
  "properties": {
    "reportIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "defectIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "summary": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CasePublicationResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "caseId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CaseSplitDto
```json
{
  "type": "object",
  "properties": {
    "reportIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CaseTriageDto
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "verificationMethod": {
      "enum": [
        "FIELD",
        "DRONE",
        "EXISTING_EVIDENCE"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "geometryVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ChainageCalibrationInput
```json
{
  "type": "object",
  "properties": {
    "sourceReference": {
      "type": "string",
      "nullable": true
    },
    "sourceChecksum": {
      "type": "string",
      "nullable": true
    },
    "controls": {
      "minItems": 2,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ChainageControl"
      },
      "description": "Sourced selected controls monotonically cover alignment from offset0 to canonical length; not invented calibration.",
      "nullable": true
    },
    "selectedReason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ChainageControl
```json
{
  "type": "object",
  "properties": {
    "geometricOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "stationMeters": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema ChangePasswordRequestDto
```json
{
  "required": [
    "currentPassword",
    "newPassword"
  ],
  "type": "object",
  "properties": {
    "currentPassword": {
      "maxLength": 1024,
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "newPassword": {
      "maxLength": 1024,
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema ClockExtensionInput
```json
{
  "type": "object",
  "properties": {
    "newDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CompletedUploadPartDto
```json
{
  "required": [
    "eTag"
  ],
  "type": "object",
  "properties": {
    "partNumber": {
      "maximum": 2147483647,
      "minimum": 1,
      "type": "integer",
      "description": "",
      "format": "int32"
    },
    "eTag": {
      "maxLength": 512,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema ConfirmRetentionBasisRequest
```json
{
  "type": "object",
  "properties": {
    "warrantyIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "expectedReferenceInventoryVersion": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateAiMockRunRequest
```json
{
  "type": "object",
  "properties": {
    "datasetId": {
      "type": "string",
      "format": "uuid"
    },
    "scope": {
      "$ref": "#/components/schemas/AiMockScopeRequest"
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "preprocessingVersion": {
      "type": "string",
      "nullable": true
    },
    "configVersion": {
      "type": "string",
      "nullable": true
    },
    "fixtureVersion": {
      "type": "string",
      "nullable": true
    },
    "stage": {
      "enum": [
        "VIDEO_ANALYSIS",
        "DUPLICATE_MATCHING"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "expectedGeometryVersion": {
      "type": "string",
      "nullable": true
    },
    "analysisRunId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "candidateSnapshotId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateAssessmentDto
```json
{
  "type": "object",
  "properties": {
    "methodVersion": {
      "enum": [
        "pm-evidence-review.v1"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "items": {
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/AssessmentItemDto"
      },
      "description": "Nonempty review items; unique(route,set,segment,band),valid statuses and conditional evidence for any PASS.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateBaselineDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BaselineItemDto"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateExportRequestDto
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "description": "Accepted values after trimming and case normalization: DOSSIER, TRAINING.",
      "nullable": true
    },
    "format": {
      "type": "string",
      "description": "Accepted values after trimming and case normalization: PDF, ZIP.",
      "nullable": true
    },
    "segmentIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "defectIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "from": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "to": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "includeOriginalFiles": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema CreateHandoverRequestDto
```json
{
  "required": [
    "documentNo",
    "handoverDate"
  ],
  "type": "object",
  "properties": {
    "documentNo": {
      "maxLength": 80,
      "type": "string",
      "description": ""
    },
    "handoverDate": {
      "type": "string",
      "description": "",
      "format": "date"
    },
    "fileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "notes": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateInvitationRequestDto
```json
{
  "required": [
    "email",
    "projectIds",
    "role"
  ],
  "type": "object",
  "properties": {
    "email": {
      "minLength": 1,
      "type": "string",
      "description": "",
      "format": "email"
    },
    "role": {
      "minLength": 1,
      "type": "string",
      "description": "Accepted values after trimming and case normalization: SUPERVISOR, PM, OPERATOR, CREW."
    },
    "projectIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema CreateProcessingJobRequestDto
```json
{
  "required": [
    "configVersion",
    "mode",
    "preprocessingVersion"
  ],
  "type": "object",
  "properties": {
    "datasetId": {
      "type": "string",
      "format": "uuid"
    },
    "scope": {
      "$ref": "#/components/schemas/BandScopeDto"
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "preprocessingVersion": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "configVersion": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "mode": {
      "pattern": "^(MOCK|REAL)$",
      "enum": [
        "MOCK",
        "REAL"
      ],
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema CreateProjectRequestDto
```json
{
  "required": [
    "name"
  ],
  "type": "object",
  "properties": {
    "code": {
      "maxLength": 50,
      "type": "string",
      "description": "",
      "nullable": true
    },
    "name": {
      "maxLength": 255,
      "type": "string",
      "description": ""
    },
    "primaryPmId": {
      "type": "string",
      "format": "uuid"
    },
    "handoverDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "warrantyEndDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "handoverFileIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "projectCode": {
      "type": "string",
      "nullable": true
    },
    "description": {
      "type": "string",
      "nullable": true
    },
    "engineeringUtmSrid": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "startDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "endDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "primaryProjectManagerUserId": {
      "type": "string",
      "format": "uuid"
    },
    "handover": {
      "$ref": "#/components/schemas/CreateHandoverRequestDto"
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema CreateProjectResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "code": {
      "type": "string",
      "nullable": true
    },
    "name": {
      "type": "string",
      "nullable": true
    },
    "primaryPmId": {
      "type": "string",
      "format": "uuid"
    },
    "handoverDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyEndDate": {
      "type": "string",
      "format": "date"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "projectCode": {
      "type": "string",
      "nullable": true
    },
    "primaryProjectManagerUserId": {
      "type": "string",
      "format": "uuid"
    },
    "handoverDocumentId": {
      "type": "string",
      "format": "uuid"
    },
    "rowVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateReporterReportRequestDto
```json
{
  "type": "object",
  "properties": {
    "description": {
      "type": "string",
      "nullable": true
    },
    "evidence": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ReportEvidenceInputDto"
      },
      "description": "Provide valid verified evidence within the allowed count. Unrecognized fields are rejected.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateRetentionEvaluationRequest
```json
{
  "type": "object",
  "properties": {
    "fileIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateRetentionHoldRequest
```json
{
  "type": "object",
  "properties": {
    "scopeType": {
      "enum": [
        "PROJECT",
        "FILE"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "scopeId": {
      "type": "string",
      "format": "uuid"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateRoadSectionRequestDto
```json
{
  "required": [
    "changeReason",
    "code",
    "coordinates"
  ],
  "type": "object",
  "properties": {
    "code": {
      "maxLength": 80,
      "type": "string",
      "description": ""
    },
    "name": {
      "maxLength": 255,
      "type": "string",
      "description": "",
      "nullable": true
    },
    "srid": {
      "maximum": 32649,
      "minimum": 32648,
      "type": "integer",
      "description": "",
      "format": "int32"
    },
    "coordinates": {
      "minItems": 2,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RoadSectionCoordinateDto"
      },
      "description": ""
    },
    "effectiveFrom": {
      "type": "string",
      "format": "date-time"
    },
    "changeReason": {
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema CreateRoadSectionVersionRequestDto
```json
{
  "required": [
    "changeReason",
    "coordinates"
  ],
  "type": "object",
  "properties": {
    "srid": {
      "maximum": 32649,
      "minimum": 32648,
      "type": "integer",
      "description": "",
      "format": "int32"
    },
    "coordinates": {
      "minItems": 2,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RoadSectionCoordinateDto"
      },
      "description": ""
    },
    "effectiveFrom": {
      "type": "string",
      "format": "date-time"
    },
    "changeReason": {
      "type": "string",
      "description": ""
    },
    "expectedCurrentVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema CreateSurveyPlanRequestDto
```json
{
  "required": [
    "outputRequirements"
  ],
  "type": "object",
  "properties": {
    "plannedStartAt": {
      "type": "string",
      "format": "date-time"
    },
    "plannedEndAt": {
      "type": "string",
      "format": "date-time"
    },
    "roadSectionVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "surveyType": {
      "$ref": "#/components/schemas/SurveyType"
    },
    "outputRequirements": {
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema CreateSurveyPlanResponseDto
```json
{
  "type": "object",
  "properties": {
    "planId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "plannedStartAt": {
      "type": "string",
      "format": "date-time"
    },
    "plannedEndAt": {
      "type": "string",
      "format": "date-time"
    },
    "surveyType": {
      "$ref": "#/components/schemas/SurveyType"
    },
    "status": {
      "$ref": "#/components/schemas/SurveyPlanStatus"
    },
    "outputRequirements": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateSurveyPlanV2RequestDto
```json
{
  "required": [
    "scope",
    "surveyType"
  ],
  "type": "object",
  "properties": {
    "scope": {
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BandScopeDto"
      },
      "description": ""
    },
    "plannedAt": {
      "type": "string",
      "format": "date-time"
    },
    "surveyType": {
      "type": "string",
      "description": "Accepted values after trimming and case normalization: BASELINE, PERIODIC, AD_HOC."
    }
  },
  "additionalProperties": false
}
```

## Schema CreateSurveyRequestRequestDto
```json
{
  "required": [
    "outputRequirements"
  ],
  "type": "object",
  "properties": {
    "surveyPlanId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "roadSectionVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "surveyType": {
      "$ref": "#/components/schemas/SurveyType"
    },
    "dueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "outputRequirements": {
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema CreateSurveyRequestResponseDto
```json
{
  "type": "object",
  "properties": {
    "requestId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "surveyPlanId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "requestedByUserId": {
      "type": "string",
      "format": "uuid"
    },
    "surveyType": {
      "$ref": "#/components/schemas/SurveyType"
    },
    "status": {
      "$ref": "#/components/schemas/SurveyRequestStatus"
    },
    "requestedAt": {
      "type": "string",
      "format": "date-time"
    },
    "dueAt": {
      "type": "string",
      "format": "date-time"
    },
    "outputRequirements": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateSurveyTaskV2RequestDto
```json
{
  "required": [
    "scope",
    "surveyType"
  ],
  "type": "object",
  "properties": {
    "scope": {
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BandScopeDto"
      },
      "description": ""
    },
    "surveyType": {
      "type": "string",
      "description": "Accepted values after trimming and case normalization: BASELINE, PERIODIC, AD_HOC."
    },
    "operatorId": {
      "type": "string",
      "format": "uuid"
    },
    "dueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "accessPoint": {
      "$ref": "#/components/schemas/PositionDto"
    },
    "planId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateTrainingLabelDto
```json
{
  "type": "object",
  "properties": {
    "sourceKind": {
      "enum": [
        "REPORT",
        "AI_DETECTION"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "annotation": {
      "$ref": "#/components/schemas/LabelAnnotationDto"
    },
    "defectTypeCode": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CreateValidationRunRequestDto
```json
{
  "required": [
    "measurementType",
    "pairs",
    "unit"
  ],
  "type": "object",
  "properties": {
    "pairs": {
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ValidationPairDto"
      },
      "description": ""
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "datasetSplitId": {
      "type": "string",
      "format": "uuid"
    },
    "measurementType": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "unit": {
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema CreateWarrantyRequestDto
```json
{
  "type": "object",
  "properties": {
    "roadSectionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyStartDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyEndDate": {
      "type": "string",
      "format": "date"
    },
    "retainedValue": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "scope": {
      "type": "string",
      "description": "Accepted values after trimming and case normalization: PROJECT, ROAD_SECTION, CONTRACT_ITEM, OTHER.",
      "nullable": true
    },
    "terms": {
      "type": "string",
      "nullable": true
    },
    "sourceDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "status": {
      "type": "string",
      "description": "Accepted values after trimming and case normalization: PLANNED, ACTIVE, EXPIRED, SUSPENDED.",
      "nullable": true
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema CreateWarrantyResponseDto
```json
{
  "type": "object",
  "properties": {
    "warrantyId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyStartDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyEndDate": {
      "type": "string",
      "format": "date"
    },
    "retainedValue": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "scope": {
      "type": "string",
      "nullable": true
    },
    "terms": {
      "type": "string",
      "nullable": true
    },
    "sourceDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CrewRepairContextFact
```json
{
  "type": "object",
  "properties": {
    "packageId": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "bindingId": {
      "type": "string",
      "format": "uuid"
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "itemVersion": {
      "type": "string",
      "nullable": true
    },
    "repairPlan": {
      "type": "string",
      "nullable": true
    },
    "checklistVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CrsControlPoint
```json
{
  "type": "object",
  "properties": {
    "native": {
      "$ref": "#/components/schemas/GeometryPoint"
    },
    "wgs84": {
      "$ref": "#/components/schemas/GeometryPoint"
    },
    "independentSource": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CrsEllipsoidDefinition
```json
{
  "type": "object",
  "properties": {
    "semiMajorAxisMeters": {
      "type": "number",
      "format": "double"
    },
    "inverseFlattening": {
      "type": "number",
      "format": "double"
    },
    "sourceReference": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CrsProfileInput
```json
{
  "type": "object",
  "properties": {
    "code": {
      "maxLength": 80,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "sourceSrid": {
      "minimum": 0,
      "type": "integer",
      "description": "Nonnegative SRID; does not establish a configured official transform.",
      "format": "int32"
    },
    "datum": {
      "type": "string",
      "nullable": true
    },
    "projection": {
      "type": "string",
      "nullable": true
    },
    "axisOrder": {
      "enum": [
        "EN",
        "NE"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged. Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "metresPerUnit": {
      "minimum": 0,
      "exclusiveMinimum": true,
      "type": "number",
      "description": "Finite positive unit conversion; real profile provenance still required.",
      "format": "double"
    },
    "sourceReference": {
      "type": "string",
      "nullable": true
    },
    "sourceChecksum": {
      "type": "string",
      "nullable": true
    },
    "operation": {
      "$ref": "#/components/schemas/CrsTransformOperation"
    },
    "independentControls": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/CrsControlPoint"
      },
      "nullable": true
    },
    "sourcedToleranceMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "toleranceSource": {
      "type": "string",
      "nullable": true
    },
    "sampleOnly": {
      "type": "boolean"
    },
    "ellipsoid": {
      "$ref": "#/components/schemas/CrsEllipsoidDefinition"
    },
    "projectionDefinition": {
      "$ref": "#/components/schemas/CrsProjectionDefinition"
    }
  },
  "additionalProperties": false
}
```

## Schema CrsProfileView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "code": {
      "type": "string",
      "nullable": true
    },
    "revision": {
      "type": "integer",
      "format": "int32"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "definition": {
      "$ref": "#/components/schemas/CrsProfileInput"
    },
    "createdBy": {
      "type": "string",
      "format": "uuid"
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema CrsProjectionDefinition
```json
{
  "type": "object",
  "properties": {
    "parameters": {
      "type": "object",
      "additionalProperties": {
        "type": "number",
        "format": "double"
      },
      "nullable": true
    },
    "sourceReference": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema CrsTransformOperation
```json
{
  "type": "object",
  "properties": {
    "method": {
      "type": "string",
      "nullable": true
    },
    "convention": {
      "type": "string",
      "nullable": true
    },
    "direction": {
      "type": "string",
      "nullable": true
    },
    "parameters": {
      "type": "array",
      "items": {
        "type": "number",
        "format": "double"
      },
      "nullable": true
    },
    "sourceReference": {
      "type": "string",
      "nullable": true
    },
    "areaOfUse": {
      "type": "string",
      "nullable": true
    },
    "publishedAccuracyMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "accuracySource": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DatasetCoverageItemDto
```json
{
  "type": "object",
  "properties": {
    "scope": {
      "$ref": "#/components/schemas/BandScopeDto"
    },
    "positionCoverage": {
      "type": "string",
      "nullable": true
    },
    "qualityCoverage": {
      "type": "string",
      "nullable": true
    },
    "overallCoverage": {
      "type": "string",
      "nullable": true
    },
    "reasons": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "coverageStatus": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DatasetCoverageResponseDto
```json
{
  "type": "object",
  "properties": {
    "datasetId": {
      "type": "string",
      "format": "uuid"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/DatasetCoverageItemDto"
      },
      "nullable": true
    },
    "methodVersion": {
      "type": "string",
      "nullable": true
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DatasetDetailView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "surveyTaskId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "dataVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "submittedBy": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "submittedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "recordedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "deviceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "scope": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BandScopeDto"
      },
      "nullable": true
    },
    "integrityStatus": {
      "type": "string",
      "nullable": true
    },
    "telemetryStatus": {
      "type": "string",
      "nullable": true
    },
    "sourceFiles": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/DatasetSourceFileDto"
      },
      "nullable": true
    },
    "pairs": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/DatasetPairDto"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DatasetPairDto
```json
{
  "type": "object",
  "properties": {
    "videoFileId": {
      "type": "string",
      "format": "uuid"
    },
    "telemetryFileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "timeOffsetMilliseconds": {
      "type": "integer",
      "format": "int64"
    }
  },
  "additionalProperties": false
}
```

## Schema DatasetResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "surveyTaskId": {
      "type": "string",
      "format": "uuid"
    },
    "dataVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "integrityStatus": {
      "type": "string",
      "nullable": true
    },
    "telemetryStatus": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DatasetSourceFileDto
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "checksumSha256": {
      "type": "string",
      "nullable": true
    },
    "sizeBytes": {
      "type": "integer",
      "format": "int64"
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "purpose": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectAssessmentRequestDto
```json
{
  "type": "object",
  "properties": {
    "defectTypeCode": {
      "type": "string",
      "nullable": true
    },
    "causeCategoryCode": {
      "type": "string",
      "nullable": true
    },
    "severity": {
      "enum": [
        "LOW",
        "MEDIUM",
        "HIGH",
        "CRITICAL"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectGeometryDto
```json
{
  "type": "object",
  "properties": {
    "srid": {
      "type": "integer",
      "format": "int32"
    },
    "wkt": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectPageDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/DefectViewDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectStatisticsInputDto
```json
{
  "type": "object",
  "properties": {
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "scopeHash": {
      "type": "string",
      "nullable": true
    },
    "defectVersion": {
      "type": "string",
      "nullable": true
    },
    "sharedPartId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "quantities": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StatisticsQuantityInputDto"
      },
      "nullable": true
    },
    "provenance": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "supersedesId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectStatisticsRead
```json
{
  "type": "object",
  "properties": {
    "scopes": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StatisticsScope"
      },
      "nullable": true
    },
    "segments": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StatisticsSegment"
      },
      "nullable": true
    },
    "measurements": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StatisticsMeasurement"
      },
      "nullable": true
    },
    "history": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/DefectStatisticsSource"
      },
      "nullable": true
    },
    "statistics": {
      "$ref": "#/components/schemas/DefectStatisticsSnapshot"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectStatisticsSnapshot
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defects": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StatisticsDefect"
      },
      "nullable": true
    },
    "projectDistinctDefects": {
      "type": "integer",
      "format": "int64"
    },
    "matchedDistinctDefects": {
      "type": "integer",
      "format": "int64"
    },
    "sharedParts": {
      "type": "integer",
      "format": "int64"
    },
    "unmappedDefects": {
      "type": "integer",
      "format": "int64"
    },
    "segments": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StatisticsSegmentCount"
      },
      "nullable": true
    },
    "quantities": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StatisticsQuantity"
      },
      "nullable": true
    },
    "missingReasons": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "sources": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/DefectStatisticsSource"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectStatisticsSource
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectVersion": {
      "type": "string",
      "nullable": true
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "scopeHash": {
      "type": "string",
      "nullable": true
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "locationVersion": {
      "type": "string",
      "nullable": true
    },
    "from": {
      "type": "number",
      "format": "double"
    },
    "to": {
      "type": "number",
      "format": "double"
    },
    "offsetFrom": {
      "type": "number",
      "format": "double"
    },
    "offsetTo": {
      "type": "number",
      "format": "double"
    },
    "sharedPartId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentIdsJson": {
      "type": "string",
      "nullable": true
    },
    "quantitiesJson": {
      "type": "string",
      "nullable": true
    },
    "sourceFactsJson": {
      "type": "string",
      "nullable": true
    },
    "provenance": {
      "type": "string",
      "nullable": true
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "confirmedAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "supersedesId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectVerificationRequestDto
```json
{
  "type": "object",
  "properties": {
    "decision": {
      "enum": [
        "CONFIRM",
        "REJECT"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "verificationMethod": {
      "enum": [
        "EXISTING_EVIDENCE",
        "FIELD",
        "DRONE"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "fieldTaskId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "fieldSubmissionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "fieldContentHash": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema DefectViewDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "defectTypeCode": {
      "type": "string",
      "nullable": true
    },
    "causeCategoryCode": {
      "type": "string",
      "nullable": true
    },
    "severity": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "geometry": {
      "$ref": "#/components/schemas/DefectGeometryDto"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportFileDto
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "fileVersion": {
      "type": "string",
      "nullable": true
    },
    "sha256": {
      "type": "string",
      "nullable": true
    },
    "sizeBytes": {
      "type": "integer",
      "format": "int64"
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "archivePath": {
      "type": "string",
      "nullable": true
    },
    "included": {
      "type": "boolean"
    },
    "reasonCode": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportJobViewDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "format": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "snapshotId": {
      "type": "string",
      "format": "uuid"
    },
    "snapshotHash": {
      "type": "string",
      "nullable": true
    },
    "artifactId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "completedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "expiresAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "errorCode": {
      "type": "string",
      "nullable": true
    },
    "completeness": {
      "type": "string",
      "nullable": true
    },
    "missingSections": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportLabelDto
```json
{
  "type": "object",
  "properties": {
    "labelId": {
      "type": "string",
      "format": "uuid"
    },
    "revision": {
      "type": "integer",
      "format": "int32"
    },
    "revisionId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "typeCode": {
      "type": "string",
      "nullable": true
    },
    "x": {
      "type": "number",
      "format": "double"
    },
    "y": {
      "type": "number",
      "format": "double"
    },
    "width": {
      "type": "number",
      "format": "double"
    },
    "height": {
      "type": "number",
      "format": "double"
    },
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "fileVersion": {
      "type": "string",
      "nullable": true
    },
    "sha256": {
      "type": "string",
      "nullable": true
    },
    "sizeBytes": {
      "type": "integer",
      "format": "int64"
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "approvalId": {
      "type": "string",
      "format": "uuid"
    },
    "approvedBy": {
      "type": "string",
      "format": "uuid"
    },
    "approvedAt": {
      "type": "string",
      "format": "date-time"
    },
    "processingJobId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "datasetVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportManifestV1Dto
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "snapshotId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "format": {
      "type": "string",
      "nullable": true
    },
    "requestedBy": {
      "type": "string",
      "format": "uuid"
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "snapshotAt": {
      "type": "string",
      "format": "date-time"
    },
    "definitionVersions": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "filters": {
      "$ref": "#/components/schemas/CreateExportRequestDto"
    },
    "sourceRevisions": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ExportSourceRevisionDto"
      },
      "nullable": true
    },
    "files": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ExportFileDto"
      },
      "nullable": true
    },
    "sections": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ExportSectionDto"
      },
      "nullable": true
    },
    "labels": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ExportLabelDto"
      },
      "nullable": true
    },
    "snapshotHash": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportSectionDto
```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "nullable": true
    },
    "availability": {
      "type": "string",
      "nullable": true
    },
    "reasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportSourceRevisionDto
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "nullable": true
    },
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "format": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "completedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "expiresAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "errorCode": {
      "type": "string",
      "nullable": true
    },
    "completeness": {
      "type": "string",
      "nullable": true
    },
    "missingSections": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ExportSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ExportSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldEvidenceDeclaration
```json
{
  "type": "object",
  "properties": {
    "captureOriginId": {
      "type": "string",
      "format": "uuid"
    },
    "fileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "purpose": {
      "enum": [
        "BEFORE",
        "AFTER",
        "MEASUREMENT"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "checksumSha256": {
      "type": "string",
      "nullable": true
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "capturedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "attemptChecklist": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldEvidenceDeclarationFact
```json
{
  "type": "object",
  "properties": {
    "captureOriginId": {
      "type": "string",
      "format": "uuid"
    },
    "fileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "purpose": {
      "type": "string",
      "nullable": true
    },
    "checksumSha256": {
      "type": "string",
      "nullable": true
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "capturedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "attemptChecklist": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldEvidenceReuseInput
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceEvidenceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "expectedChecksum": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldHandoverInput
```json
{
  "type": "object",
  "properties": {
    "performedPortionState": {
      "type": "string",
      "nullable": true
    },
    "summary": {
      "type": "string",
      "nullable": true
    },
    "startOriginId": {
      "type": "string",
      "format": "uuid"
    },
    "submissionIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "recipientUserId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldLocationImpactActionInput
```json
{
  "type": "object",
  "properties": {
    "impactId": {
      "type": "string",
      "format": "uuid"
    },
    "action": {
      "enum": [
        "STOP",
        "REASSIGN",
        "CONTINUE",
        "VERIFY"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "assignedToUserId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handover": {
      "$ref": "#/components/schemas/FieldHandoverInput"
    }
  },
  "additionalProperties": false
}
```

## Schema FieldMeasurementInput
```json
{
  "type": "object",
  "properties": {
    "sampleId": {
      "type": "string",
      "nullable": true
    },
    "type": {
      "type": "string",
      "nullable": true
    },
    "value": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "unknownReason": {
      "type": "string",
      "nullable": true
    },
    "dimension": {
      "type": "string",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "longitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "latitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "locationReason": {
      "type": "string",
      "nullable": true
    },
    "instrument": {
      "type": "string",
      "nullable": true
    },
    "method": {
      "type": "string",
      "nullable": true
    },
    "notes": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldMeasurementInputFact
```json
{
  "type": "object",
  "properties": {
    "sampleId": {
      "type": "string",
      "nullable": true
    },
    "type": {
      "type": "string",
      "nullable": true
    },
    "value": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "unknownReason": {
      "type": "string",
      "nullable": true
    },
    "dimension": {
      "type": "string",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "longitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "latitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "locationReason": {
      "type": "string",
      "nullable": true
    },
    "instrument": {
      "type": "string",
      "nullable": true
    },
    "method": {
      "type": "string",
      "nullable": true
    },
    "notes": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldPositionProofInput
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "nullable": true
    },
    "checklist": {
      "type": "string",
      "nullable": true
    },
    "longitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "latitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "observedSlabId": {
      "type": "string",
      "nullable": true
    },
    "observedSegmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "observedRouteVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "observedChainageMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldReviewInput
```json
{
  "type": "object",
  "properties": {
    "submissionId": {
      "type": "string",
      "format": "uuid"
    },
    "decision": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldReviewViewFact
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "submissionId": {
      "type": "string",
      "format": "uuid"
    },
    "decision": {
      "type": "string",
      "nullable": true
    },
    "receiptActivation": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldStartInput
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "claimedAt": {
      "type": "string",
      "format": "date-time"
    },
    "deviceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "monotonicMilliseconds": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "bootId": {
      "type": "string",
      "nullable": true
    },
    "offlineProof": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldSubmissionInput
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "startOriginId": {
      "type": "string",
      "format": "uuid"
    },
    "parentSubmissionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "measurements": {
      "maxItems": 200,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/FieldMeasurementInput"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "evidence": {
      "maxItems": 100,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/FieldEvidenceDeclaration"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "locationProof": {
      "$ref": "#/components/schemas/FieldPositionProofInput"
    },
    "captureType": {
      "enum": [
        "MEASUREMENT",
        "REPAIR_CLAIM"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "repaired": {
      "type": "boolean",
      "nullable": true
    },
    "unrepairedReason": {
      "type": "string",
      "nullable": true
    },
    "deviceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldSubmissionViewFact
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "rootId": {
      "type": "string",
      "format": "uuid"
    },
    "parentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "revision": {
      "type": "integer",
      "format": "int32"
    },
    "serverReceivedAt": {
      "type": "string",
      "format": "date-time"
    },
    "originalReviewDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "readiness": {
      "type": "string",
      "nullable": true
    },
    "missingReasons": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "contentHash": {
      "type": "string",
      "nullable": true
    },
    "measurements": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/FieldMeasurementInputFact"
      },
      "nullable": true
    },
    "evidence": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/FieldEvidenceDeclarationFact"
      },
      "nullable": true
    },
    "captureType": {
      "type": "string",
      "nullable": true
    },
    "repaired": {
      "type": "boolean",
      "nullable": true
    },
    "unrepairedReason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldTaskActionInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    },
    "assignedToUserId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handover": {
      "$ref": "#/components/schemas/FieldHandoverInput"
    }
  },
  "additionalProperties": false
}
```

## Schema FieldTaskCreateInput
```json
{
  "type": "object",
  "properties": {
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectVersion": {
      "type": "string",
      "nullable": true
    },
    "surveyId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "layoutRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "slabId": {
      "type": "string",
      "nullable": true
    },
    "purpose": {
      "enum": [
        "PRE_MEASUREMENT",
        "POST_REPAIR",
        "VERIFICATION"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "requiredMeasurementType": {
      "type": "integer",
      "format": "int32"
    },
    "measurementScope": {
      "type": "string",
      "nullable": true
    },
    "instructions": {
      "type": "string",
      "nullable": true
    },
    "assignedToUserId": {
      "type": "string",
      "format": "uuid"
    },
    "dueAt": {
      "type": "string",
      "format": "date-time"
    },
    "mapPublicationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema FieldTaskEvidenceFile
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "checksum": {
      "type": "string",
      "nullable": true
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "sizeBytes": {
      "type": "integer",
      "format": "int64"
    }
  },
  "additionalProperties": false
}
```

## Schema FieldTaskViewFact
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "surveyId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "layoutRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "slabId": {
      "type": "string",
      "nullable": true
    },
    "purpose": {
      "type": "string",
      "nullable": true
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "assignmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "assignedToUserId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "firstStartOriginId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "latestSubmissionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "mapPublicationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "requiredMeasurementType": {
      "type": "integer",
      "format": "int32"
    },
    "measurementScope": {
      "type": "string",
      "nullable": true
    },
    "instructions": {
      "type": "string",
      "nullable": true
    },
    "dueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "repairContext": {
      "$ref": "#/components/schemas/CrewRepairContextFact"
    }
  },
  "additionalProperties": false
}
```

## Schema FileMetadataResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "checksumSha256": {
      "type": "string",
      "nullable": true
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "sizeBytes": {
      "type": "integer",
      "format": "int64"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ForcedPasswordChangeRequestDto
```json
{
  "required": [
    "confirmPassword",
    "currentPassword",
    "newPassword",
    "username"
  ],
  "type": "object",
  "properties": {
    "username": {
      "maxLength": 100,
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "currentPassword": {
      "maxLength": 1024,
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "newPassword": {
      "maxLength": 1024,
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "confirmPassword": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryAffectedReferenceFact
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "nullable": true
    },
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "readiness": {
      "type": "string",
      "nullable": true
    },
    "missingReasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryConfirmInput
```json
{
  "type": "object",
  "properties": {
    "expectedCurrentVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "effectiveFrom": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryDraftInput
```json
{
  "type": "object",
  "properties": {
    "sourceKind": {
      "enum": [
        "COORDINATES",
        "NATIVE_ALIGNMENT"
      ],
      "type": "string",
      "description": "COORDINATES is supported by legacy metric preview; NATIVE_ALIGNMENT takes the separate native path. GPX is not an accepted preview source.",
      "nullable": true
    },
    "sourceCrs": {
      "type": "integer",
      "format": "int32"
    },
    "stationOriginMeters": {
      "type": "number",
      "format": "double"
    },
    "changeReason": {
      "type": "string",
      "nullable": true
    },
    "coordinates": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryPoint"
      },
      "nullable": true
    },
    "sourceFileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "trackIndex": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "trackSegmentIndex": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "widthProfile": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/WidthProfileItem"
      },
      "nullable": true
    },
    "surveyWidthMeters": {
      "type": "number",
      "format": "double"
    },
    "branchCode": {
      "type": "string",
      "nullable": true
    },
    "roadCode": {
      "type": "string",
      "nullable": true
    },
    "roadName": {
      "type": "string",
      "nullable": true
    },
    "nativeAlignment": {
      "$ref": "#/components/schemas/NativeAlignmentInput"
    },
    "routeSystemId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "routeKind": {
      "enum": [
        "MAIN",
        "BRANCH"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged. Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged. Conditional native-only value; do not make this optional property globally required.",
      "nullable": true
    },
    "parentRouteVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "junctionOffsetMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "declaredLengthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "chainageCalibration": {
      "$ref": "#/components/schemas/ChainageCalibrationInput"
    },
    "tessellationToleranceMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "resolvedCrsProfile": {
      "$ref": "#/components/schemas/CrsProfileInput"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryDraftReadiness
```json
{
  "type": "object",
  "properties": {
    "ready": {
      "type": "boolean"
    },
    "missingFields": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "errors": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "crsStatus": {
      "type": "string",
      "nullable": true
    },
    "sampleOnly": {
      "type": "boolean"
    },
    "warnings": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryDraftView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "roadCode": {
      "type": "string",
      "nullable": true
    },
    "roadName": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceCrs": {
      "type": "integer",
      "format": "int32"
    },
    "sourceFileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sourceChecksum": {
      "type": "string",
      "nullable": true
    },
    "trackIndex": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "trackSegmentIndex": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "originalCoordinates": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryPoint"
      },
      "nullable": true
    },
    "editedCoordinates": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryPoint"
      },
      "nullable": true
    },
    "stationOriginMeters": {
      "type": "number",
      "format": "double"
    },
    "widthProfile": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/WidthProfileItem"
      },
      "nullable": true
    },
    "surveyWidthMeters": {
      "type": "number",
      "format": "double"
    },
    "createdBy": {
      "type": "string",
      "format": "uuid"
    },
    "updatedBy": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "nativeAlignment": {
      "$ref": "#/components/schemas/NativeAlignmentInput"
    },
    "readiness": {
      "$ref": "#/components/schemas/GeometryDraftReadiness"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryImpactDecisionInput
```json
{
  "type": "object",
  "properties": {
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "action": {
      "enum": [
        "VERIFY",
        "CONTINUE",
        "STOP",
        "REASSIGN"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryImpactDecisionView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "impactId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "action": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "occurredAt": {
      "type": "string",
      "format": "date-time"
    },
    "executionStatus": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryImpactInput
```json
{
  "type": "object",
  "properties": {
    "previousRouteVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "newRouteVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryImpactView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "previousRouteVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "newRouteVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "references": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryAffectedReferenceFact"
      },
      "nullable": true
    },
    "recordedAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryMapFeature
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "nullable": true
    },
    "layer": {
      "type": "string",
      "nullable": true
    },
    "geometry": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "bbox": {
      "type": "array",
      "items": {
        "type": "number",
        "format": "double"
      },
      "nullable": true
    },
    "properties": {
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryMapLayer
```json
{
  "type": "object",
  "properties": {
    "key": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "featureKind": {
      "type": "string",
      "nullable": true
    },
    "count": {
      "type": "integer",
      "format": "int32"
    },
    "reasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "spatialSrid": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sampleOnly": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryMapManifest
```json
{
  "type": "object",
  "properties": {
    "publicationId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "layoutRevisionId": {
      "type": "string",
      "format": "uuid"
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "nativeSrid": {
      "type": "integer",
      "format": "int32"
    },
    "profileStatus": {
      "type": "string",
      "nullable": true
    },
    "sampleOnly": {
      "type": "boolean"
    },
    "displayToleranceMeters": {
      "type": "number",
      "format": "double"
    },
    "contentHash": {
      "type": "string",
      "nullable": true
    },
    "layers": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryMapLayer"
      },
      "nullable": true
    },
    "routeSystemId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "canonicalLengthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "declaredLengthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "chainageStatus": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryMapPage
```json
{
  "type": "object",
  "properties": {
    "publicationId": {
      "type": "string",
      "format": "uuid"
    },
    "contentHash": {
      "type": "string",
      "nullable": true
    },
    "layer": {
      "type": "string",
      "nullable": true
    },
    "features": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryMapFeature"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryMapPublishInput
```json
{
  "type": "object",
  "properties": {
    "layoutRevisionId": {
      "type": "string",
      "format": "uuid"
    },
    "publicationMode": {
      "enum": [
        "SAMPLE",
        "OFFICIAL"
      ],
      "type": "string",
      "description": "SAMPLE may be admitted; OFFICIAL is explicitly recognized but currently rejected409 official_crs_acceptance_pending. Document this gate; never promise OFFICIAL success.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryPackageView
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "route": {
      "$ref": "#/components/schemas/RoadGeometryVersionView"
    },
    "segmentSet": {
      "$ref": "#/components/schemas/SegmentSetView"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryPoint
```json
{
  "type": "object",
  "properties": {
    "x": {
      "type": "number",
      "format": "double"
    },
    "y": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryPointFact
```json
{
  "type": "object",
  "properties": {
    "x": {
      "type": "number",
      "format": "double"
    },
    "y": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryPreview
```json
{
  "type": "object",
  "properties": {
    "sourceCrs": {
      "type": "integer",
      "format": "int32"
    },
    "engineeringSrid": {
      "type": "integer",
      "format": "int32"
    },
    "lengthMeters": {
      "type": "number",
      "format": "double"
    },
    "metricCenterline": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "wgs84Centerline": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "roadSurface": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "surveyArea": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "warnings": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sampleOnly": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema GeometryShape
```json
{
  "type": "object",
  "properties": {
    "type": {
      "type": "string",
      "nullable": true
    },
    "coordinates": {
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema H5EncryptedPackageDto
```json
{
  "type": "object",
  "properties": {
    "header": {
      "$ref": "#/components/schemas/H5PackageHeaderDto"
    },
    "payloadNonce": {
      "type": "string",
      "nullable": true
    },
    "ciphertext": {
      "type": "string",
      "nullable": true
    },
    "payloadTag": {
      "type": "string",
      "nullable": true
    },
    "recipients": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/H5RecipientKeyWrapDto"
      },
      "nullable": true
    },
    "senderSigningPublicKey": {
      "type": "string",
      "nullable": true
    },
    "signature": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema H5PackageHeaderDto
```json
{
  "type": "object",
  "properties": {
    "version": {
      "type": "integer",
      "format": "int32"
    },
    "payloadSchema": {
      "type": "string",
      "nullable": true
    },
    "packageId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "originalActorId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceDeviceId": {
      "type": "string",
      "nullable": true
    },
    "originIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "payloadSha256": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema H5RecipientKeyWrapDto
```json
{
  "type": "object",
  "properties": {
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "deviceId": {
      "type": "string",
      "nullable": true
    },
    "recipientKeyFingerprint": {
      "type": "string",
      "nullable": true
    },
    "ephemeralPublicKey": {
      "type": "string",
      "nullable": true
    },
    "nonce": {
      "type": "string",
      "nullable": true
    },
    "wrappedKey": {
      "type": "string",
      "nullable": true
    },
    "tag": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema H6ClockBreachView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "dueAt": {
      "type": "string",
      "format": "date-time"
    },
    "observedAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema H6ClockCursor
```json
{
  "type": "object",
  "properties": {
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "afterDueAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "afterClockId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema H6ClockExtensionView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "previousDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "newDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "previousDeadlineBreached": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema H6ClockPage
```json
{
  "type": "object",
  "properties": {
    "status": {
      "type": "string",
      "nullable": true
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/H6ClockView"
      },
      "nullable": true
    },
    "continuation": {
      "$ref": "#/components/schemas/H6ClockCursor"
    }
  },
  "additionalProperties": false
}
```

## Schema H6ClockView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "targetId": {
      "type": "string",
      "format": "uuid"
    },
    "originEventId": {
      "type": "string",
      "format": "uuid"
    },
    "originAt": {
      "type": "string",
      "format": "date-time"
    },
    "originalDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "currentDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "overdue": {
      "type": "boolean"
    },
    "completedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "acknowledgedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "extensions": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/H6ClockExtensionView"
      },
      "nullable": true
    },
    "breaches": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/H6ClockBreachView"
      },
      "nullable": true
    },
    "pendingCapabilities": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "appointedActorId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "appointedRole": {
      "type": "string",
      "nullable": true
    },
    "appointments": {
      "type": "array",
      "items": {},
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema H6NotificationScopeDto
```json
{
  "type": "object",
  "properties": {
    "notificationId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "occurrenceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "classification": {
      "type": "string",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "eventType": {
      "type": "string",
      "nullable": true
    },
    "resolverVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema InspectionTaskPageResponseDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/InspectionTaskResponseDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "asOf": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema InspectionTaskResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "crewId": {
      "type": "string",
      "format": "uuid"
    },
    "policyVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema InternalCaseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "reportIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "defectIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "verificationMethod": {
      "type": "string",
      "nullable": true
    },
    "warrantyRouting": {
      "type": "string",
      "nullable": true
    },
    "conclusion": {
      "$ref": "#/components/schemas/CaseConclusionViewDto"
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema InvitationResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "expiresAt": {
      "type": "string",
      "format": "date-time"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema LD06ActionDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "sourceActionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "defectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "linkedDefectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "obligationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "receivingProjectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "priorRepairDecisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "scopeHash": {
      "type": "string",
      "nullable": true
    },
    "factsJson": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema LD06LifecycleInputDto
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    },
    "evidenceFileIds": {
      "maxItems": 100,
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "sourceActionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "defectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "linkedDefectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "obligationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "receivingProjectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "priorRepairDecisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "scopeHash": {
      "type": "string",
      "nullable": true
    },
    "newDefect": {
      "$ref": "#/components/schemas/CandidateDecisionRequestDto"
    }
  },
  "additionalProperties": false
}
```

## Schema LD06TransferableObligationDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "mandatory": {
      "type": "boolean"
    },
    "scopeId": {
      "type": "string",
      "format": "uuid"
    },
    "physicalRoadId": {
      "type": "string",
      "format": "uuid"
    },
    "locationVersion": {
      "type": "string",
      "nullable": true
    },
    "routeLabel": {
      "type": "string",
      "nullable": true
    },
    "from": {
      "type": "number",
      "format": "double"
    },
    "to": {
      "type": "number",
      "format": "double"
    },
    "offsetFrom": {
      "type": "number",
      "format": "double"
    },
    "offsetTo": {
      "type": "number",
      "format": "double"
    },
    "scopeHash": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema LabelAnnotationDto
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "nullable": true
    },
    "coordinateSpace": {
      "type": "string",
      "nullable": true
    },
    "x": {
      "type": "number",
      "format": "double"
    },
    "y": {
      "type": "number",
      "format": "double"
    },
    "width": {
      "type": "number",
      "format": "double"
    },
    "height": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema LoginRequestDto
```json
{
  "required": [
    "email",
    "password"
  ],
  "type": "object",
  "properties": {
    "email": {
      "maxLength": 100,
      "minLength": 1,
      "type": "string",
      "description": "",
      "format": "email"
    },
    "password": {
      "maxLength": 1024,
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema MockRunSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "stage": {
      "type": "string",
      "nullable": true
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "processingJobId": {
      "type": "string",
      "format": "uuid"
    },
    "resultId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "fixtureVersion": {
      "type": "string",
      "nullable": true
    },
    "errorCode": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema MockRunSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/MockRunSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema NativeAlignmentInput
```json
{
  "type": "object",
  "properties": {
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid"
    },
    "primitives": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/NativeAlignmentPrimitive"
      },
      "nullable": true
    },
    "spatialSrid": {
      "type": "integer",
      "format": "int32"
    }
  },
  "additionalProperties": false
}
```

## Schema NativeAlignmentPrimitive
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "enum": [
        "LINE",
        "ARC"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "start": {
      "$ref": "#/components/schemas/GeometryPoint"
    },
    "end": {
      "$ref": "#/components/schemas/GeometryPoint"
    },
    "center": {
      "$ref": "#/components/schemas/GeometryPoint"
    },
    "radius": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "startAngleRadians": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "sweepRadians": {
      "type": "number",
      "format": "double",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema NormalizedBoundingBoxV1
```json
{
  "type": "object",
  "properties": {
    "x": {
      "type": "number",
      "format": "double"
    },
    "y": {
      "type": "number",
      "format": "double"
    },
    "width": {
      "type": "number",
      "format": "double"
    },
    "height": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema NotificationDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "message": {
      "type": "string",
      "nullable": true
    },
    "resourceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "read": {
      "type": "boolean"
    },
    "occurredAt": {
      "type": "string",
      "format": "date-time"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema NotificationPageDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/NotificationDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "asOf": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema NotificationSupervisorDto
```json
{
  "type": "object",
  "properties": {
    "assignmentId": {
      "type": "string",
      "format": "uuid"
    },
    "supervisorId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "assignedAt": {
      "type": "string",
      "format": "date-time"
    },
    "revokedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ObligationResponsibilityDto
```json
{
  "type": "object",
  "properties": {
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "originProjectId": {
      "type": "string",
      "format": "uuid"
    },
    "currentProjectId": {
      "type": "string",
      "format": "uuid"
    },
    "acceptanceActionId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineBatchResult
```json
{
  "type": "object",
  "properties": {
    "batchId": {
      "type": "string",
      "format": "uuid"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineItemResult"
      },
      "nullable": true
    },
    "localQueueInstruction": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineCaptureArtifactInput
```json
{
  "type": "object",
  "properties": {
    "manifest": {
      "$ref": "#/components/schemas/OfflineCaptureManifest"
    },
    "manifestSignature": {
      "type": "string",
      "nullable": true
    },
    "chunkIndex": {
      "type": "integer",
      "format": "int32"
    },
    "envelope": {
      "$ref": "#/components/schemas/H5EncryptedPackageDto"
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineCaptureChunk
```json
{
  "type": "object",
  "properties": {
    "artifactId": {
      "type": "string",
      "format": "uuid"
    },
    "index": {
      "type": "integer",
      "format": "int32"
    },
    "offset": {
      "type": "integer",
      "format": "int64"
    },
    "length": {
      "type": "integer",
      "format": "int32"
    },
    "plaintextChecksum": {
      "type": "string",
      "nullable": true
    },
    "envelopeFingerprint": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineCaptureManifest
```json
{
  "type": "object",
  "properties": {
    "version": {
      "type": "integer",
      "format": "int32"
    },
    "parentPackageId": {
      "type": "string",
      "format": "uuid"
    },
    "parentManifestHash": {
      "type": "string",
      "nullable": true
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "originalActorId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceDeviceRegistrationId": {
      "type": "string",
      "format": "uuid"
    },
    "captureOriginId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "purpose": {
      "type": "string",
      "nullable": true
    },
    "mediaType": {
      "type": "string",
      "nullable": true
    },
    "fileSize": {
      "type": "integer",
      "format": "int64"
    },
    "fileChecksum": {
      "type": "string",
      "nullable": true
    },
    "chunks": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineCaptureChunk"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineDeviceRegisterInput
```json
{
  "type": "object",
  "properties": {
    "deviceId": {
      "type": "string",
      "format": "uuid"
    },
    "encryptionPublicKey": {
      "type": "string",
      "nullable": true
    },
    "signingPublicKey": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineDeviceRevokeInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineDeviceSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "deviceId": {
      "type": "string",
      "format": "uuid"
    },
    "revision": {
      "type": "integer",
      "format": "int32"
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "registeredAt": {
      "type": "string",
      "format": "date-time"
    },
    "revokedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineDeviceSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineDeviceSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineGrantItemScope
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "enum": [
        "FIELD_ACCEPT",
        "FIELD_START",
        "FIELD_SUBMISSION",
        "REPAIR_ASSESSMENT",
        "REPAIR_EXECUTION_START",
        "REPAIR_EXECUTION_FINISH"
      ],
      "type": "string",
      "description": "Each kind selects one exact typed body; source signatures/device binding/grants remain required. No unconditional requirement added to optional variant bodies.",
      "nullable": true
    },
    "corePayloadHash": {
      "type": "string",
      "nullable": true
    },
    "envelopeHash": {
      "type": "string",
      "nullable": true
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "assignmentId": {
      "type": "string",
      "format": "uuid"
    },
    "snapshotId": {
      "type": "string",
      "format": "uuid"
    },
    "repairResourceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineGrantRevokeInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineGrantSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "packageId": {
      "type": "string",
      "format": "uuid"
    },
    "issuedAt": {
      "type": "string",
      "format": "date-time"
    },
    "expiresAt": {
      "type": "string",
      "format": "date-time"
    },
    "revokedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineGrantSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineGrantSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineHandoverGrantInput
```json
{
  "type": "object",
  "properties": {
    "packageId": {
      "type": "string",
      "format": "uuid"
    },
    "recipientDeviceRegistrationId": {
      "type": "string",
      "format": "uuid"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineGrantItemScope"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineItemResult
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "code": {
      "type": "string",
      "nullable": true
    },
    "effectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "canonicalOriginId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "durableAcknowledgment": {
      "type": "boolean"
    },
    "serverReceivedAt": {
      "type": "string",
      "format": "date-time"
    },
    "timeProvenance": {
      "type": "string",
      "nullable": true
    },
    "syncLateness": {
      "type": "string",
      "nullable": true
    },
    "claimedFinishedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "verifiedFinishedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "verifiedSyncDueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "outcome": {
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineOperationDescriptor
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "effectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "enum": [
        "FIELD_ACCEPT",
        "FIELD_START",
        "FIELD_SUBMISSION",
        "REPAIR_ASSESSMENT",
        "REPAIR_EXECUTION_START",
        "REPAIR_EXECUTION_FINISH"
      ],
      "type": "string",
      "description": "Each kind selects one exact typed body; source signatures/device binding/grants remain required. No unconditional requirement added to optional variant bodies.",
      "nullable": true
    },
    "envelopeHash": {
      "type": "string",
      "nullable": true
    },
    "corePayloadHash": {
      "type": "string",
      "nullable": true
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "assignmentId": {
      "type": "string",
      "format": "uuid"
    },
    "snapshotId": {
      "type": "string",
      "format": "uuid"
    },
    "repairResourceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineOperationInput
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "integer",
      "format": "int32"
    },
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "effectId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "enum": [
        "FIELD_ACCEPT",
        "FIELD_START",
        "FIELD_SUBMISSION",
        "REPAIR_ASSESSMENT",
        "REPAIR_EXECUTION_START",
        "REPAIR_EXECUTION_FINISH"
      ],
      "type": "string",
      "description": "Each kind selects one exact typed body; source signatures/device binding/grants remain required. No unconditional requirement added to optional variant bodies.",
      "nullable": true
    },
    "originalActorId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceDeviceId": {
      "type": "string",
      "format": "uuid"
    },
    "snapshotId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "assignmentId": {
      "type": "string",
      "format": "uuid"
    },
    "taskVersion": {
      "type": "string",
      "nullable": true
    },
    "corePayloadHash": {
      "type": "string",
      "nullable": true
    },
    "dependencies": {
      "maxItems": 1000,
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence. Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "claimedFinishedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "fieldStart": {
      "$ref": "#/components/schemas/FieldStartInput"
    },
    "fieldSubmission": {
      "$ref": "#/components/schemas/FieldSubmissionInput"
    },
    "repair": {
      "$ref": "#/components/schemas/OfflineRepairPayload"
    },
    "fieldAction": {
      "$ref": "#/components/schemas/FieldTaskActionInput"
    }
  },
  "additionalProperties": false
}
```

## Schema OfflinePackageExportInput
```json
{
  "type": "object",
  "properties": {
    "package": {
      "$ref": "#/components/schemas/H5EncryptedPackageDto"
    },
    "manifest": {
      "$ref": "#/components/schemas/OfflineSourceManifestInput"
    },
    "sourceFileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflinePackageImportInput
```json
{
  "type": "object",
  "properties": {
    "batchId": {
      "type": "string",
      "format": "uuid"
    },
    "packageId": {
      "type": "string",
      "format": "uuid"
    },
    "grantId": {
      "type": "string",
      "format": "uuid"
    },
    "recipientDeviceRegistrationId": {
      "type": "string",
      "format": "uuid"
    },
    "operations": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineOperationInput"
      },
      "nullable": true
    },
    "sourceSignature": {
      "type": "string",
      "nullable": true
    },
    "recipientSignature": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflinePackageSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "originalActorId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceDeviceRegistrationId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceBatchId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema OfflinePackageSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflinePackageSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineRepairPayload
```json
{
  "type": "object",
  "properties": {
    "resourceId": {
      "type": "string",
      "format": "uuid"
    },
    "action": {
      "enum": [
        "assessment",
        "execution-start",
        "execution-finish"
      ],
      "type": "string",
      "description": "Exact lowercase action must match selected repair kind and typed body.",
      "nullable": true
    },
    "start": {
      "$ref": "#/components/schemas/RepairExecutionStartInput"
    },
    "finish": {
      "$ref": "#/components/schemas/RepairExecutionFinishInput"
    },
    "assessment": {
      "$ref": "#/components/schemas/RepairMeasurementAssessmentInput"
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineSignedBatchInput
```json
{
  "type": "object",
  "properties": {
    "batchId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceDeviceRegistrationId": {
      "type": "string",
      "format": "uuid"
    },
    "operations": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineOperationInput"
      },
      "nullable": true
    },
    "signature": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineSnapshotInput
```json
{
  "type": "object",
  "properties": {
    "deviceRegistrationId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineSourceManifestInput
```json
{
  "type": "object",
  "properties": {
    "sourceBatchId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceDeviceRegistrationId": {
      "type": "string",
      "format": "uuid"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OfflineOperationDescriptor"
      },
      "nullable": true
    },
    "signature": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineStartReconcileInput
```json
{
  "type": "object",
  "properties": {
    "batchId": {
      "type": "string",
      "format": "uuid"
    },
    "originId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema OfflineUploadCreateRequestDto
```json
{
  "type": "object",
  "properties": {
    "admissionId": {
      "type": "string",
      "format": "uuid"
    },
    "captureOriginId": {
      "type": "string",
      "format": "uuid"
    },
    "upload": {
      "$ref": "#/components/schemas/UploadCreateRequestDto"
    }
  },
  "additionalProperties": false
}
```

## Schema OwnReportDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "description": {
      "type": "string",
      "nullable": true
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "evidence": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OwnReportEvidenceDto"
      },
      "nullable": true
    },
    "routingStatus": {
      "type": "string",
      "nullable": true
    },
    "publicUpdates": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OwnReportUpdateDto"
      },
      "nullable": true
    },
    "supplements": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OwnReportSupplementDto"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OwnReportEvidenceDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "fileVersion": {
      "type": "string",
      "nullable": true
    },
    "supplementId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "capturedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "locationSource": {
      "type": "string",
      "nullable": true
    },
    "location": {
      "$ref": "#/components/schemas/ReportEvidenceLocationDto"
    }
  },
  "additionalProperties": false
}
```

## Schema OwnReportPageDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/OwnReportDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema OwnReportSupplementDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "description": {
      "type": "string",
      "nullable": true
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema OwnReportUpdateDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "summary": {
      "type": "string",
      "nullable": true
    },
    "publishedAt": {
      "type": "string",
      "format": "date-time"
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema PasswordRecoveryRequestDto
```json
{
  "required": [
    "email"
  ],
  "type": "object",
  "properties": {
    "email": {
      "maxLength": 254,
      "minLength": 3,
      "type": "string",
      "description": "",
      "format": "email"
    }
  },
  "additionalProperties": false
}
```

## Schema PavementGeometryPreviewFact
```json
{
  "type": "object",
  "properties": {
    "plan": {
      "$ref": "#/components/schemas/PavementLayoutPreviewFact"
    },
    "slabs": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SlabGeometrySnapshotFact"
      },
      "nullable": true
    },
    "warnings": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "displayToleranceMeters": {
      "type": "number",
      "format": "double"
    },
    "renderedGapAreaSquareMeters": {
      "type": "number",
      "format": "double"
    },
    "renderedOverlapAreaSquareMeters": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema PavementLayoutInput
```json
{
  "type": "object",
  "properties": {
    "stripWidthMeters": {
      "type": "number",
      "format": "double"
    },
    "slabLengthMeters": {
      "type": "number",
      "format": "double"
    },
    "widthProfile": {
      "maxItems": 100000,
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/PavementWidthInterval"
      },
      "description": "Nonempty width intervals, at most100000; multiple intervals emit a transition warning and are not rejected solely for count>1. Genuine sourced widths required.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema PavementLayoutPreviewFact
```json
{
  "type": "object",
  "properties": {
    "analyticLengthMeters": {
      "type": "number",
      "format": "double"
    },
    "stripWidthMeters": {
      "type": "number",
      "format": "double"
    },
    "slabLengthMeters": {
      "type": "number",
      "format": "double"
    },
    "cells": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/PlannedSlabCellFact"
      },
      "nullable": true
    },
    "warnings": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema PavementLayoutView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "sourcePlanId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "contentHash": {
      "type": "string",
      "nullable": true
    },
    "geometry": {
      "$ref": "#/components/schemas/PavementGeometryPreviewFact"
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema PavementPlanCreateInput
```json
{
  "type": "object",
  "properties": {
    "layout": {
      "$ref": "#/components/schemas/PavementLayoutInput"
    },
    "displayToleranceMeters": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema PavementWidthInterval
```json
{
  "type": "object",
  "properties": {
    "fromOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "toOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "widthMeters": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema PlannedSlabCellFact
```json
{
  "type": "object",
  "properties": {
    "sequence": {
      "type": "integer",
      "format": "int32"
    },
    "longitudinalRow": {
      "type": "integer",
      "format": "int32"
    },
    "strip": {
      "type": "integer",
      "format": "int32"
    },
    "fromOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "toOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "fromLateralMeters": {
      "type": "number",
      "format": "double"
    },
    "toLateralMeters": {
      "type": "number",
      "format": "double"
    },
    "isTerminalResidual": {
      "type": "boolean"
    },
    "isWidthTransitionFragment": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema PointDto
```json
{
  "type": "object",
  "properties": {
    "longitude": {
      "maximum": 180,
      "minimum": -180,
      "type": "number",
      "description": "",
      "format": "double"
    },
    "latitude": {
      "maximum": 90,
      "minimum": -90,
      "type": "number",
      "description": "",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema PositionDto
```json
{
  "required": [
    "point",
    "source"
  ],
  "type": "object",
  "properties": {
    "point": {
      "$ref": "#/components/schemas/PointDto"
    },
    "source": {
      "enum": [
        "EXIF",
        "MANUAL",
        "SURVEY",
        "DERIVED"
      ],
      "type": "string",
      "description": ""
    },
    "accuracyMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "capturedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema PostponeSurveyPlanRequestDto
```json
{
  "required": [
    "reason"
  ],
  "type": "object",
  "properties": {
    "newPlannedStartAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "reason": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema PostponeSurveyPlanResponseDto
```json
{
  "type": "object",
  "properties": {
    "planId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "$ref": "#/components/schemas/SurveyPlanStatus"
    },
    "newPlannedStartAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "postponementId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema PostponeSurveyPlanV2RequestDto
```json
{
  "required": [
    "reason"
  ],
  "type": "object",
  "properties": {
    "reason": {
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema PrimaryProjectManagerReadDto
```json
{
  "type": "object",
  "properties": {
    "membershipId": {
      "type": "string",
      "format": "uuid"
    },
    "userId": {
      "type": "string",
      "format": "uuid"
    },
    "displayName": {
      "type": "string",
      "nullable": true
    },
    "validFrom": {
      "type": "string",
      "format": "date"
    },
    "validTo": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "membershipVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema PrimaryProjectManagerResponseDto
```json
{
  "type": "object",
  "properties": {
    "primaryProjectManager": {
      "$ref": "#/components/schemas/PrimaryProjectManagerReadDto"
    }
  },
  "additionalProperties": false
}
```

## Schema ProblemDetails
```json
{
  "type": "object",
  "properties": {
    "type": {
      "type": "string",
      "nullable": true
    },
    "title": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "detail": {
      "type": "string",
      "nullable": true
    },
    "instance": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": {}
}
```

## Schema ProcessingJobResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "resultId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "attemptNumber": {
      "type": "integer",
      "format": "int32"
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "jobType": {
      "type": "string",
      "nullable": true
    },
    "error": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProcessingJobSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "resultId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "attemptNumber": {
      "type": "integer",
      "format": "int32"
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "jobType": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProcessingJobSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ProcessingJobSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProfileResponseDto
```json
{
  "type": "object",
  "properties": {
    "userId": {
      "type": "string",
      "format": "uuid"
    },
    "username": {
      "type": "string",
      "nullable": true
    },
    "displayName": {
      "type": "string",
      "nullable": true
    },
    "email": {
      "type": "string",
      "nullable": true
    },
    "roleCode": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "rowVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProfileUpdateRequestDto
```json
{
  "required": [
    "displayName",
    "expectedRowVersion"
  ],
  "type": "object",
  "properties": {
    "displayName": {
      "maxLength": 200,
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "email": {
      "maxLength": 254,
      "minLength": 0,
      "type": "string",
      "description": "",
      "nullable": true
    },
    "expectedRowVersion": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": {}
}
```

## Schema ProjectLifecycleHistoryDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "recordedAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "obligationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "grantId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "receiverId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "operationalClosureId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "basisReference": {
      "type": "string",
      "nullable": true
    },
    "authoritySourceReference": {
      "type": "string",
      "nullable": true
    },
    "sourceDisposition": {
      "type": "string",
      "nullable": true
    },
    "defectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "linkedDefectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handlingScope": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProjectLifecycleViewDto
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "constructionCompletion": {
      "type": "string",
      "nullable": true
    },
    "operationalClosure": {
      "type": "string",
      "nullable": true
    },
    "warrantyRecorded": {
      "type": "boolean"
    },
    "acceptsNewReports": {
      "type": "boolean"
    },
    "obligationInventory": {
      "type": "string",
      "nullable": true
    },
    "operationalClosureEligibility": {
      "type": "string",
      "nullable": true
    },
    "outstandingMandatoryObligationIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "missingReasons": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "history": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ProjectLifecycleHistoryDto"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "actions": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/LD06ActionDto"
      },
      "nullable": true
    },
    "responsibilities": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ObligationResponsibilityDto"
      },
      "nullable": true
    },
    "transferableObligations": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/LD06TransferableObligationDto"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProjectListItemResponseDto
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "projectCode": {
      "type": "string",
      "nullable": true
    },
    "name": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "primaryProjectManagerUserId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "primaryProjectManagerName": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProjectListResponseDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ProjectListItemResponseDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "asOf": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema ProjectMemberResponseDto
```json
{
  "type": "object",
  "properties": {
    "membershipId": {
      "type": "string",
      "format": "uuid"
    },
    "userId": {
      "type": "string",
      "format": "uuid"
    },
    "displayName": {
      "type": "string",
      "nullable": true
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "isPrimary": {
      "type": "boolean"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "validFrom": {
      "type": "string",
      "format": "date"
    },
    "validTo": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "membershipVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProjectMemberResponseDtoStaffPageResponseDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ProjectMemberResponseDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "asOf": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema ProjectSummaryV1
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "readAt": {
      "type": "string",
      "format": "date-time"
    },
    "definitionVersion": {
      "type": "string",
      "nullable": true
    },
    "filters": {
      "$ref": "#/components/schemas/ReportingFiltersDto"
    },
    "metrics": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ReportingMetricDto"
      },
      "nullable": true
    },
    "warnings": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "isolation": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ProjectWorkPackageResponseDto
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "projectCode": {
      "type": "string",
      "nullable": true
    },
    "name": {
      "type": "string",
      "nullable": true
    },
    "description": {
      "type": "string",
      "nullable": true
    },
    "engineeringUtmSrid": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "startDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "endDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "accessRole": {
      "type": "string",
      "nullable": true
    },
    "rowVersion": {
      "type": "string",
      "nullable": true
    },
    "roadSections": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RoadSectionWorkPackageDto"
      },
      "nullable": true
    },
    "warranties": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/WarrantyWorkPackageDto"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema PublicPublicationDto
```json
{
  "type": "object",
  "properties": {
    "publicId": {
      "type": "string",
      "format": "uuid"
    },
    "damageCategory": {
      "type": "string",
      "nullable": true
    },
    "publicStatus": {
      "type": "string",
      "nullable": true
    },
    "publishedAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema ReassignPrimaryProjectManagerRequestDto
```json
{
  "required": [
    "effectiveFrom",
    "expectedCurrentMembershipRowVersion",
    "reason"
  ],
  "type": "object",
  "properties": {
    "primaryProjectManagerUserId": {
      "type": "string",
      "format": "uuid"
    },
    "effectiveFrom": {
      "type": "string",
      "description": "",
      "format": "date"
    },
    "reason": {
      "maxLength": 1000,
      "type": "string",
      "description": ""
    },
    "expectedCurrentMembershipRowVersion": {
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema ReassignPrimaryProjectManagerResponseDto
```json
{
  "type": "object",
  "properties": {
    "previousMembershipId": {
      "type": "string",
      "format": "uuid"
    },
    "currentMembershipId": {
      "type": "string",
      "format": "uuid"
    },
    "currentMembershipRowVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReassignSurveyTaskV2RequestDto
```json
{
  "required": [
    "reason"
  ],
  "type": "object",
  "properties": {
    "operatorId": {
      "type": "string",
      "format": "uuid"
    },
    "reason": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "dueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReceiveAiResultRequestDto
```json
{
  "required": [
    "checksumSha256",
    "detections",
    "manifestHash",
    "mode"
  ],
  "type": "object",
  "properties": {
    "jobAttemptId": {
      "type": "string",
      "format": "uuid"
    },
    "manifestHash": {
      "pattern": "^[a-f0-9]{64}$",
      "type": "string",
      "description": ""
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "mode": {
      "pattern": "^(MOCK|REAL)$",
      "enum": [
        "MOCK",
        "REAL"
      ],
      "type": "string",
      "description": ""
    },
    "rawResultFileId": {
      "type": "string",
      "format": "uuid"
    },
    "checksumSha256": {
      "pattern": "^[a-f0-9]{64}$",
      "type": "string",
      "description": ""
    },
    "detections": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/AiDetectionDto"
      },
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema RefreshRequestDto
```json
{
  "required": [
    "refreshToken"
  ],
  "type": "object",
  "properties": {
    "refreshToken": {
      "maxLength": 2048,
      "minLength": 16,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema RegisterReporterRequestDto
```json
{
  "required": [
    "displayName",
    "email",
    "password",
    "reporterType"
  ],
  "type": "object",
  "properties": {
    "email": {
      "minLength": 1,
      "type": "string",
      "description": "",
      "format": "email"
    },
    "password": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "displayName": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "reporterType": {
      "minLength": 1,
      "type": "string",
      "description": "Accepted values after trimming and case normalization: CITIZEN, INVESTOR_REPRESENTATIVE."
    }
  },
  "additionalProperties": false
}
```

## Schema RegistrationIntentResponseDto
```json
{
  "type": "object",
  "properties": {
    "intentId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "resendAfterSeconds": {
      "type": "integer",
      "format": "int32"
    }
  },
  "additionalProperties": false
}
```

## Schema ReleaseRetentionHoldRequest
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RenewedHandlingScopeInput
```json
{
  "type": "object",
  "properties": {
    "operationalClosureId": {
      "type": "string",
      "format": "uuid"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "handlingScope": {
      "type": "string",
      "nullable": true
    },
    "basis": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairActualScopeInput
```json
{
  "type": "object",
  "properties": {
    "physicalRoadId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "layoutRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "slabId": {
      "type": "string",
      "nullable": true
    },
    "fromMeters": {
      "type": "number",
      "format": "double"
    },
    "toMeters": {
      "type": "number",
      "format": "double"
    },
    "offsetFromMeters": {
      "type": "number",
      "format": "double"
    },
    "offsetToMeters": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairAssessmentView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "firstStartId": {
      "type": "string",
      "format": "uuid"
    },
    "sessionId": {
      "type": "string",
      "format": "uuid"
    },
    "stage": {
      "type": "string",
      "nullable": true
    },
    "readiness": {
      "type": "string",
      "nullable": true
    },
    "missingReasons": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "locationState": {
      "type": "string",
      "nullable": true
    },
    "contentHash": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairAttemptHistoryView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "contentHash": {
      "type": "string",
      "nullable": true
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "crewId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "assignmentId": {
      "type": "string",
      "format": "uuid"
    },
    "authorizationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "locationVersion": {
      "type": "string",
      "nullable": true
    },
    "policyRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "performed": {
      "type": "boolean"
    },
    "unperformedReason": {
      "type": "string",
      "nullable": true
    },
    "startedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "finishedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "serverReceivedAt": {
      "type": "string",
      "format": "date-time"
    },
    "timeProvenance": {
      "type": "string",
      "nullable": true
    },
    "evidence": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairEvidenceHistoryView"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairAttemptIntakeView
```json
{
  "type": "object",
  "properties": {
    "attemptId": {
      "type": "string",
      "format": "uuid"
    },
    "linkId": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "submissionId": {
      "type": "string",
      "format": "uuid"
    },
    "reviewClockId": {
      "type": "string",
      "format": "uuid"
    },
    "originalReviewDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "intakeReadiness": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairAttemptReviewInput
```json
{
  "type": "object",
  "properties": {
    "fieldSubmissionId": {
      "type": "string",
      "format": "uuid"
    },
    "decision": {
      "enum": [
        "SUPPLEMENT",
        "ACCEPT"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "reason": {
      "maxLength": 2000,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairAttemptReviewView
```json
{
  "type": "object",
  "properties": {
    "reviewId": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "submissionId": {
      "type": "string",
      "format": "uuid"
    },
    "decision": {
      "type": "string",
      "nullable": true
    },
    "receiptActivation": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairAttemptSubmitInput
```json
{
  "type": "object",
  "properties": {
    "fieldSubmissionId": {
      "type": "string",
      "format": "uuid"
    },
    "expectedContentHash": {
      "type": "string",
      "nullable": true
    },
    "executionFinishId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairAttemptSupplementInput
```json
{
  "type": "object",
  "properties": {
    "fieldSubmissionId": {
      "type": "string",
      "format": "uuid"
    },
    "expectedContentHash": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairCancellationInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    },
    "handover": {
      "$ref": "#/components/schemas/RepairLifecycleHandoverInput"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairCorrectionBasisHistoryView
```json
{
  "type": "object",
  "properties": {
    "text": {
      "type": "string",
      "nullable": true
    },
    "evidence": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairEvidenceHistoryView"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairCorrectionBasisInput
```json
{
  "type": "object",
  "properties": {
    "text": {
      "type": "string",
      "nullable": true
    },
    "evidenceReferenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairCorrectionInput
```json
{
  "type": "object",
  "properties": {
    "supersedesDecisionId": {
      "type": "string",
      "format": "uuid"
    },
    "result": {
      "enum": [
        "UNREPAIRED",
        "REPORTED_AWAITING_REVIEW",
        "CONFIRMED"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "reason": {
      "maxLength": 2000,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "basis": {
      "$ref": "#/components/schemas/RepairCorrectionBasisInput"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairCorrectionView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "supersedesDecisionId": {
      "type": "string",
      "format": "uuid"
    },
    "result": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "basis": {
      "type": "string",
      "nullable": true
    },
    "evidenceReferenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "obligationResolved": {
      "type": "boolean"
    },
    "itemState": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairDecisionHistoryView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "supersedesDecisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "result": {
      "type": "string",
      "nullable": true
    },
    "basis": {
      "$ref": "#/components/schemas/RepairCorrectionBasisHistoryView"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairDecisionInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "maxLength": 2000,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairEligibilityReadView
```json
{
  "type": "object",
  "properties": {
    "eligible": {
      "type": "boolean"
    },
    "activation": {
      "type": "string",
      "nullable": true
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "bindingId": {
      "type": "string",
      "format": "uuid"
    },
    "policyRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "policyContentHash": {
      "type": "string",
      "nullable": true
    },
    "policyState": {
      "type": "string",
      "nullable": true
    },
    "rules": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairMeasurementRule"
      },
      "nullable": true
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "measurementIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "originalVerifiedStart": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "executionExpiresAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "sourceMapping": {
      "type": "string",
      "nullable": true
    },
    "warrantySources": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairWarrantySource"
      },
      "nullable": true
    },
    "handoverSources": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairHandoverSource"
      },
      "nullable": true
    },
    "missingReasons": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "coverageMapping": {
      "$ref": "#/components/schemas/RoadCoverageMapping"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairEvidenceHistoryView
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "fileVersion": {
      "type": "string",
      "nullable": true
    },
    "sha256": {
      "type": "string",
      "nullable": true
    },
    "purpose": {
      "type": "string",
      "nullable": true
    },
    "verified": {
      "type": "boolean"
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "capturedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "reuseDecisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "reusedSource": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairExecutionFinishInput
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "executionStartId": {
      "type": "string",
      "format": "uuid"
    },
    "claimedAt": {
      "type": "string",
      "format": "date-time"
    },
    "deviceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "monotonicMilliseconds": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "bootId": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairExecutionFinishView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "executionStartId": {
      "type": "string",
      "format": "uuid"
    },
    "claimedAt": {
      "type": "string",
      "format": "date-time"
    },
    "serverReceivedAt": {
      "type": "string",
      "format": "date-time"
    },
    "verifiedOriginalAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "timeProvenance": {
      "type": "string",
      "nullable": true
    },
    "syncClockId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "originalSyncDueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairExecutionStartInput
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "fieldFirstStartId": {
      "type": "string",
      "format": "uuid"
    },
    "claimedAt": {
      "type": "string",
      "format": "date-time"
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid"
    },
    "deviceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "monotonicMilliseconds": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "bootId": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairExecutionStartView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid"
    },
    "firstStartId": {
      "type": "string",
      "format": "uuid"
    },
    "claimedAt": {
      "type": "string",
      "format": "date-time"
    },
    "serverReceivedAt": {
      "type": "string",
      "format": "date-time"
    },
    "verifiedOriginalAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "timeProvenance": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairHandoverSource
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "documentNo": {
      "type": "string",
      "nullable": true
    },
    "handoverDate": {
      "type": "string",
      "format": "date"
    },
    "acceptedByUserId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "fileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "rowVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairHistoryView
```json
{
  "type": "object",
  "properties": {
    "item": {
      "$ref": "#/components/schemas/RepairItemView"
    },
    "decisions": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairDecisionHistoryView"
      },
      "nullable": true
    },
    "attempts": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairAttemptHistoryView"
      },
      "nullable": true
    },
    "reviewRequests": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairReviewRequestHistoryView"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairItemAssignInput
```json
{
  "type": "object",
  "properties": {
    "task": {
      "$ref": "#/components/schemas/FieldTaskCreateInput"
    },
    "policyRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairItemProposeInput
```json
{
  "type": "object",
  "properties": {
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "mode": {
      "enum": [
        "NORMAL",
        "FAST_TRACK"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "repairPlan": {
      "type": "string",
      "nullable": true
    },
    "checklistVersion": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairItemSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "result": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairItemSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairItemSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairItemView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "result": {
      "type": "string",
      "nullable": true
    },
    "effectiveDecisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "attemptIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "decisionIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairLifecycleHandoverInput
```json
{
  "type": "object",
  "properties": {
    "firstStartId": {
      "type": "string",
      "format": "uuid"
    },
    "recipientUserId": {
      "type": "string",
      "format": "uuid"
    },
    "performedPortion": {
      "type": "string",
      "nullable": true
    },
    "safetyState": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairLifecycleView
```json
{
  "type": "object",
  "properties": {
    "sourceItemId": {
      "type": "string",
      "format": "uuid"
    },
    "successorItemId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "cancellationEventId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverEventId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "continuationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sourceState": {
      "type": "string",
      "nullable": true
    },
    "successorState": {
      "type": "string",
      "nullable": true
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "successorVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairMeasurementAssessmentInput
```json
{
  "type": "object",
  "properties": {
    "originId": {
      "type": "string",
      "format": "uuid"
    },
    "fieldFirstStartId": {
      "type": "string",
      "format": "uuid"
    },
    "measurements": {
      "maxItems": 200,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/FieldMeasurementInput"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "evidence": {
      "maxItems": 100,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/FieldEvidenceDeclaration"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "locationProof": {
      "$ref": "#/components/schemas/FieldPositionProofInput"
    },
    "deviceId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "stopConditions": {
      "type": "object",
      "additionalProperties": {
        "type": "boolean",
        "nullable": true
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairMeasurementRule
```json
{
  "type": "object",
  "properties": {
    "code": {
      "type": "string",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "minimum": {
      "type": "number",
      "format": "double"
    },
    "maximum": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairNormalContinuationInput
```json
{
  "type": "object",
  "properties": {
    "repairPlan": {
      "type": "string",
      "nullable": true
    },
    "checklistVersion": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "handover": {
      "$ref": "#/components/schemas/RepairLifecycleHandoverInput"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairObligationInput
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "enum": [
        "FORMAL_REPAIR",
        "TEMPORARY_SAFETY"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "mandatory": {
      "type": "boolean"
    },
    "scope": {
      "$ref": "#/components/schemas/RepairActualScopeInput"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairPackageCreateInput
```json
{
  "type": "object",
  "properties": {
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectVersion": {
      "type": "string",
      "nullable": true
    },
    "obligations": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairObligationInput"
      },
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairPackageSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "allMandatoryResolved": {
      "type": "boolean"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairPackageSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairPackageSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairPackageView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "allMandatoryResolved": {
      "type": "boolean"
    },
    "unresolvedMandatoryObligationIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairItemView"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairPolicyDefinitionInput
```json
{
  "type": "object",
  "properties": {
    "defectTypeCode": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "checklistVersion": {
      "maxLength": 200,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "measurements": {
      "maxItems": 200,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairMeasurementRule"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "stopConditions": {
      "maxItems": 100,
      "type": "array",
      "items": {
        "type": "string"
      },
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    },
    "reason": {
      "maxLength": 2000,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairPolicyReasonInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairPolicyView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "currentChangeId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "publishedRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "revision": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "defectTypeCode": {
      "type": "string",
      "nullable": true
    },
    "checklistVersion": {
      "type": "string",
      "nullable": true
    },
    "measurements": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairMeasurementRule"
      },
      "nullable": true
    },
    "stopConditions": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairReviewRequestHistoryView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "decisionId": {
      "type": "string",
      "format": "uuid"
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "at": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairReviewRequestInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "maxLength": 2000,
      "type": "string",
      "description": "Raw field length/count limit from typed validator; does not force property presence.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairReviewRequestView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "decisionId": {
      "type": "string",
      "format": "uuid"
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyAcknowledgementInput
```json
{
  "type": "object",
  "properties": {
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyAcknowledgementView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "warningId": {
      "type": "string",
      "format": "uuid"
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "afterOriginalDue": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyCheckInput
```json
{
  "type": "object",
  "properties": {
    "result": {
      "enum": [
        "Safe",
        "NeedsReplacement",
        "Danger"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "findings": {
      "type": "string",
      "nullable": true
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyCheckView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "at": {
      "type": "string",
      "format": "date-time"
    },
    "result": {
      "type": "string",
      "nullable": true
    },
    "findings": {
      "type": "string",
      "nullable": true
    },
    "evidenceLinkIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyCreateInput
```json
{
  "type": "object",
  "properties": {
    "formalRepairObligationId": {
      "type": "string",
      "format": "uuid"
    },
    "safetyObligationId": {
      "type": "string",
      "format": "uuid"
    },
    "responsibleActorId": {
      "type": "string",
      "format": "uuid"
    },
    "checkSchedule": {
      "type": "string",
      "nullable": true
    },
    "replacementCondition": {
      "type": "string",
      "nullable": true
    },
    "removalCondition": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyInstallInput
```json
{
  "type": "object",
  "properties": {
    "firstCheckDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "formalObligationId": {
      "type": "string",
      "format": "uuid"
    },
    "safetyObligationId": {
      "type": "string",
      "format": "uuid"
    },
    "responsibleActorId": {
      "type": "string",
      "format": "uuid"
    },
    "installedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "firstCheckDueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "currentCheckId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "checks": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairSafetyCheckView"
      },
      "nullable": true
    },
    "warnings": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairSafetyWarningView"
      },
      "nullable": true
    },
    "acknowledgements": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RepairSafetyAcknowledgementView"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairSafetyWarningView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "sourceCheckId": {
      "type": "string",
      "format": "uuid"
    },
    "intendedActorId": {
      "type": "string",
      "format": "uuid"
    },
    "serverReceivedAt": {
      "type": "string",
      "format": "date-time"
    },
    "originalDueAt": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairTaskBindingView
```json
{
  "type": "object",
  "properties": {
    "itemId": {
      "type": "string",
      "format": "uuid"
    },
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "taskId": {
      "type": "string",
      "format": "uuid"
    },
    "assignmentId": {
      "type": "string",
      "format": "uuid"
    },
    "mode": {
      "type": "string",
      "nullable": true
    },
    "authorizationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "policyRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "layoutRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "mapPublicationId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "slabId": {
      "type": "string",
      "nullable": true
    },
    "firstStartId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RepairWarrantySource
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyStartDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyEndDate": {
      "type": "string",
      "format": "date"
    },
    "scope": {
      "$ref": "#/components/schemas/WarrantyScope"
    },
    "status": {
      "$ref": "#/components/schemas/WarrantyStatus"
    },
    "sourceDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportEvidenceInputDto
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "fileVersion": {
      "type": "string",
      "nullable": true
    },
    "locationSource": {
      "enum": [
        "UNKNOWN",
        "CAPTURE",
        "EXIF",
        "MANUAL"
      ],
      "type": "string",
      "description": "UNKNOWN requires location omitted; other values require location. Keep location conditional, not globally required.",
      "nullable": true
    },
    "capturedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "location": {
      "$ref": "#/components/schemas/ReportEvidenceLocationDto"
    }
  },
  "additionalProperties": false
}
```

## Schema ReportEvidenceLocationDto
```json
{
  "type": "object",
  "properties": {
    "latitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "longitude": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "accuracyMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReporterEvidenceCreateRequestDto
```json
{
  "type": "object",
  "properties": {
    "fileName": {
      "type": "string",
      "nullable": true
    },
    "mediaType": {
      "type": "string",
      "description": "Accepted values after trimming and case normalization: image/jpeg, image/png.",
      "nullable": true
    },
    "sizeBytes": {
      "maximum": 20971520,
      "minimum": 1,
      "type": "integer",
      "description": "Reporter JPEG/PNG photo admission bound20MiB.",
      "format": "int64"
    },
    "checksumSha256": {
      "pattern": "^[a-f0-9]{64}$",
      "type": "string",
      "description": "Canonical lowercase SHA256; actual bytes must match verification.",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReporterReportResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "description": {
      "type": "string",
      "nullable": true
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "evidenceIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingAvailabilityDto
```json
{
  "type": "object",
  "properties": {
    "availability": {
      "type": "string",
      "nullable": true
    },
    "reasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingDimensionsDto
```json
{
  "type": "object",
  "properties": {
    "status": {
      "type": "string",
      "nullable": true
    },
    "band": {
      "type": "string",
      "nullable": true
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingFiltersDto
```json
{
  "type": "object",
  "properties": {
    "from": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "to": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "segmentIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingItemDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "metric": {
      "type": "string",
      "nullable": true
    },
    "type": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "parentTaskId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "band": {
      "type": "string",
      "nullable": true
    },
    "datasetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sizeBytes": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "bias": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "mae": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "rmse": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "used": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "excluded": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "modelVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "splitId": {
      "type": "string",
      "nullable": true
    },
    "measurementType": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingItemsPageDto
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "metric": {
      "type": "string",
      "nullable": true
    },
    "readAt": {
      "type": "string",
      "format": "date-time"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ReportingItemDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "availability": {
      "$ref": "#/components/schemas/ReportingAvailabilityDto"
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingMetricDto
```json
{
  "type": "object",
  "properties": {
    "code": {
      "type": "string",
      "nullable": true
    },
    "dimensions": {
      "$ref": "#/components/schemas/ReportingDimensionsDto"
    },
    "value": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "numerator": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "denominator": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "periodApplicable": {
      "type": "boolean"
    },
    "availability": {
      "type": "string",
      "nullable": true
    },
    "reasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "sourceRefs": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ReportingSourceRefDto"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingSourceRefDto
```json
{
  "type": "object",
  "properties": {
    "type": {
      "type": "string",
      "nullable": true
    },
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingTimelineItemDto
```json
{
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAt": {
      "type": "string",
      "format": "date-time"
    },
    "actorDisplay": {
      "type": "string",
      "nullable": true
    },
    "action": {
      "type": "string",
      "nullable": true
    },
    "source": {
      "$ref": "#/components/schemas/ReportingSourceRefDto"
    },
    "summary": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReportingTimelinePageDto
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "readAt": {
      "type": "string",
      "format": "date-time"
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/ReportingTimelineItemDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "availability": {
      "$ref": "#/components/schemas/ReportingAvailabilityDto"
    }
  },
  "additionalProperties": false
}
```

## Schema ResendReporterOtpRequestDto
```json
{
  "type": "object",
  "properties": {
    "intentId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionBasisView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "revision": {
      "type": "integer",
      "format": "int32"
    },
    "policyVersion": {
      "type": "string",
      "nullable": true
    },
    "classification": {
      "type": "string",
      "nullable": true
    },
    "referenceInventoryVersion": {
      "type": "string",
      "nullable": true
    },
    "inventoryComplete": {
      "type": "boolean"
    },
    "warrantyRefs": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RetentionWarrantyView"
      },
      "nullable": true
    },
    "confirmedBy": {
      "type": "string",
      "format": "uuid"
    },
    "confirmedAt": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "supersedesId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionEvaluationSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "evaluatedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "policyVersion": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionEvaluationSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RetentionEvaluationSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionEvaluationView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "evaluatedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "policyVersion": {
      "type": "string",
      "nullable": true
    },
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RetentionItemView"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionFileView
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "basisHeadId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "basisVersion": {
      "type": "string",
      "nullable": true
    },
    "inventoryVersion": {
      "type": "string",
      "nullable": true
    },
    "inventoryComplete": {
      "type": "boolean"
    },
    "classification": {
      "type": "string",
      "nullable": true
    },
    "references": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RetentionReferenceView"
      },
      "nullable": true
    },
    "applicableObligations": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RetentionWarrantyView"
      },
      "nullable": true
    },
    "inventoryReasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "activeHoldCount": {
      "type": "integer",
      "format": "int32"
    },
    "evaluation": {
      "$ref": "#/components/schemas/RetentionItemView"
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionHoldView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "scopeType": {
      "type": "string",
      "nullable": true
    },
    "scopeId": {
      "type": "string",
      "format": "uuid"
    },
    "state": {
      "type": "string",
      "nullable": true
    },
    "createdBy": {
      "type": "string",
      "format": "uuid"
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "releasedBy": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "releasedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionItemView
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "eligibility": {
      "type": "string",
      "nullable": true
    },
    "reasonCodes": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    },
    "eligibleAfter": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "basisVersion": {
      "type": "string",
      "nullable": true
    },
    "inventoryVersion": {
      "type": "string",
      "nullable": true
    },
    "holdVersion": {
      "type": "string",
      "nullable": true
    },
    "isCurrent": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionReferenceView
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "nullable": true
    },
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RetentionWarrantyView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "scope": {
      "type": "string",
      "nullable": true
    },
    "warrantyEndDate": {
      "type": "string",
      "format": "date"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RetryProcessingJobRequestDto
```json
{
  "required": [
    "reason"
  ],
  "type": "object",
  "properties": {
    "reason": {
      "maxLength": 1000,
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema ReviewTrainingLabelDto
```json
{
  "type": "object",
  "properties": {
    "decision": {
      "enum": [
        "APPROVE",
        "REJECT"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ReviseTrainingLabelDto
```json
{
  "type": "object",
  "properties": {
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "annotation": {
      "$ref": "#/components/schemas/LabelAnnotationDto"
    },
    "defectTypeCode": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RoadCoverageInputDto
```json
{
  "type": "object",
  "properties": {
    "scopeObligationId": {
      "type": "string",
      "format": "uuid"
    },
    "expectedScopeHash": {
      "type": "string",
      "nullable": true
    },
    "handoverDocumentId": {
      "type": "string",
      "format": "uuid"
    },
    "handoverVersion": {
      "type": "string",
      "nullable": true
    },
    "sourceKind": {
      "enum": [
        "WARRANTY",
        "MAINTENANCE_BASIS"
      ],
      "type": "string",
      "description": "Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "applicableFromUtc": {
      "type": "string",
      "format": "date-time"
    },
    "applicableToUtc": {
      "type": "string",
      "format": "date-time"
    },
    "provenance": {
      "enum": [
        "REAL_SOURCE",
        "TEST_ONLY"
      ],
      "type": "string",
      "description": "TEST_ONLY is explicit test provenance; never promote to REAL_SOURCE authority.",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "supersedesId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RoadCoverageMapping
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "scopeObligationId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "locationVersion": {
      "type": "string",
      "nullable": true
    },
    "from": {
      "type": "number",
      "format": "double"
    },
    "to": {
      "type": "number",
      "format": "double"
    },
    "offsetFrom": {
      "type": "number",
      "format": "double"
    },
    "offsetTo": {
      "type": "number",
      "format": "double"
    },
    "applicableFromUtc": {
      "type": "string",
      "format": "date-time"
    },
    "applicableToUtc": {
      "type": "string",
      "format": "date-time"
    },
    "handoverDocumentId": {
      "type": "string",
      "format": "uuid"
    },
    "handoverVersion": {
      "type": "string",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "handoverFileId": {
      "type": "string",
      "format": "uuid"
    },
    "coverageFileId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceFactsJson": {
      "type": "string",
      "nullable": true
    },
    "provenance": {
      "type": "string",
      "nullable": true
    },
    "actorId": {
      "type": "string",
      "format": "uuid"
    },
    "confirmedAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "reason": {
      "type": "string",
      "nullable": true
    },
    "supersedesId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RoadCoverageReadFact
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "scopes": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RoadCoverageScopeFact"
      },
      "nullable": true
    },
    "sources": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RoadCoverageSourceFact"
      },
      "nullable": true
    },
    "history": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/RoadCoverageMapping"
      },
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RoadCoverageScopeFact
```json
{
  "type": "object",
  "properties": {
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "locationVersion": {
      "type": "string",
      "nullable": true
    },
    "from": {
      "type": "number",
      "format": "double"
    },
    "to": {
      "type": "number",
      "format": "double"
    },
    "offsetFrom": {
      "type": "number",
      "format": "double"
    },
    "offsetTo": {
      "type": "number",
      "format": "double"
    },
    "scopeHash": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RoadCoverageSourceFact
```json
{
  "type": "object",
  "properties": {
    "kind": {
      "type": "string",
      "nullable": true
    },
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "from": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "to": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "provenance": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RoadGeometryVersionView
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "versionNo": {
      "type": "integer",
      "format": "int32"
    },
    "isCurrent": {
      "type": "boolean"
    },
    "effectiveFrom": {
      "type": "string",
      "format": "date-time"
    },
    "sourceDraftId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sourceCrs": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "engineeringSrid": {
      "type": "integer",
      "format": "int32"
    },
    "stationOriginMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "widthProfile": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/WidthProfileItem"
      },
      "nullable": true
    },
    "surveyWidthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "metricCenterline": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "wgs84Centerline": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "lengthMeters": {
      "type": "number",
      "format": "double"
    },
    "metadataStatus": {
      "type": "string",
      "nullable": true
    },
    "approvedBy": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "approvedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "geometryHash": {
      "type": "string",
      "nullable": true
    },
    "crsProfileRevisionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "routeSystemId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "routeKind": {
      "type": "string",
      "nullable": true
    },
    "parentRouteVersionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "junctionOffsetMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "declaredLengthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "chainageCalibration": {
      "$ref": "#/components/schemas/ChainageCalibrationInput"
    },
    "crsStatus": {
      "type": "string",
      "nullable": true
    },
    "sampleOnly": {
      "type": "boolean"
    }
  },
  "additionalProperties": false
}
```

## Schema RoadSectionCoordinateDto
```json
{
  "type": "object",
  "properties": {
    "x": {
      "type": "number",
      "format": "double"
    },
    "y": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema RoadSectionVersionResponseDto
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "code": {
      "type": "string",
      "nullable": true
    },
    "versionNo": {
      "type": "integer",
      "format": "int32"
    },
    "isCurrent": {
      "type": "boolean"
    },
    "effectiveFrom": {
      "type": "string",
      "format": "date-time"
    },
    "changeReason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RoadSectionWorkPackageDto
```json
{
  "type": "object",
  "properties": {
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "code": {
      "type": "string",
      "nullable": true
    },
    "name": {
      "type": "string",
      "nullable": true
    },
    "currentVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "versionNo": {
      "type": "integer",
      "format": "int32"
    },
    "geometryWkt": {
      "type": "string",
      "nullable": true
    },
    "srid": {
      "type": "integer",
      "format": "int32"
    },
    "effectiveFrom": {
      "type": "string",
      "format": "date-time"
    },
    "changeReason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RouteSystemInput
```json
{
  "type": "object",
  "properties": {
    "code": {
      "type": "string",
      "nullable": true
    },
    "name": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema RouteSystemView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "code": {
      "type": "string",
      "nullable": true
    },
    "name": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentDefinition
```json
{
  "type": "object",
  "properties": {
    "targetLengthMeters": {
      "minimum": 0,
      "exclusiveMinimum": true,
      "type": "number",
      "description": "Finite strictly positive target length.",
      "format": "double"
    },
    "remainderMode": {
      "enum": [
        "KEEP",
        "MERGE_PREVIOUS"
      ],
      "type": "string",
      "description": "Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged. Allowed canonical string literals validated by current service/repository; branch-specific presence rules remain unchanged. Canonical accepted strings; current validator normalizes only where documented.",
      "nullable": true
    },
    "boundariesMeters": {
      "type": "array",
      "items": {
        "type": "number",
        "format": "double"
      },
      "nullable": true
    },
    "nativeAlignment": {
      "$ref": "#/components/schemas/NativeAlignmentInput"
    },
    "tessellationToleranceMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "chainageCalibration": {
      "$ref": "#/components/schemas/ChainageCalibrationInput"
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentGeometry
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "sequence": {
      "type": "integer",
      "format": "int32"
    },
    "fromOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "toOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "startStationMeters": {
      "type": "number",
      "format": "double"
    },
    "endStationMeters": {
      "type": "number",
      "format": "double"
    },
    "lengthMeters": {
      "type": "number",
      "format": "double"
    },
    "metricGeometry": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "wgs84Geometry": {
      "$ref": "#/components/schemas/GeometryShape"
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentGeometryView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "sequence": {
      "type": "integer",
      "format": "int32"
    },
    "fromOffsetMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "toOffsetMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "startStationMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "endStationMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "lengthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "metricGeometry": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "wgs84Geometry": {
      "$ref": "#/components/schemas/GeometryShape"
    },
    "missingMetadata": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentPreview
```json
{
  "type": "object",
  "properties": {
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "totalLengthMeters": {
      "type": "number",
      "format": "double"
    },
    "boundariesMeters": {
      "type": "array",
      "items": {
        "type": "number",
        "format": "double"
      },
      "nullable": true
    },
    "segments": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SegmentGeometry"
      },
      "nullable": true
    },
    "definitionHash": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentPublishInput
```json
{
  "type": "object",
  "properties": {
    "expectedPublishedSetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentSetPageDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SegmentSetSummaryDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentSetSummaryDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SegmentSetView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "definition": {
      "$ref": "#/components/schemas/SegmentDefinition"
    },
    "geometryHash": {
      "type": "string",
      "nullable": true
    },
    "segments": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SegmentGeometryView"
      },
      "nullable": true
    },
    "publishedBy": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "publishedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "metadataStatus": {
      "type": "string",
      "nullable": true
    },
    "missingMetadata": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SelectedBaselineItem
```json
{
  "type": "object",
  "properties": {
    "selectionId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentId": {
      "type": "string",
      "format": "uuid"
    },
    "targetBand": {
      "type": "string",
      "nullable": true
    },
    "datasetId": {
      "type": "string",
      "format": "uuid"
    },
    "assessmentId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema SlabGeometrySnapshotFact
```json
{
  "type": "object",
  "properties": {
    "key": {
      "type": "string",
      "nullable": true
    },
    "plannedSequence": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "footprint": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/GeometryPointFact"
      },
      "nullable": true
    },
    "lengthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "widthMeters": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "dimensionUnknownReason": {
      "type": "string",
      "nullable": true
    },
    "source": {
      "type": "string",
      "nullable": true
    },
    "sourceFileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "provenanceStatus": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema StaffUserResponseDto
```json
{
  "type": "object",
  "properties": {
    "userId": {
      "type": "string",
      "format": "uuid"
    },
    "displayName": {
      "type": "string",
      "nullable": true
    },
    "email": {
      "type": "string",
      "nullable": true
    },
    "phoneNumber": {
      "type": "string",
      "nullable": true
    },
    "role": {
      "type": "string",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema StaffUserResponseDtoStaffPageResponseDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/StaffUserResponseDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "asOf": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema StatisticsDefect
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema StatisticsMeasurement
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "type": {
      "type": "string",
      "nullable": true
    },
    "value": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "valueState": {
      "type": "string",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "unknownReason": {
      "type": "string",
      "nullable": true
    },
    "evidenceFileId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema StatisticsQuantity
```json
{
  "type": "object",
  "properties": {
    "sharedPartId": {
      "type": "string",
      "format": "uuid"
    },
    "measurementId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "type": {
      "type": "string",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "value": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "valueState": {
      "type": "string",
      "nullable": true
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "provenance": {
      "type": "string",
      "nullable": true
    },
    "reason": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema StatisticsQuantityInputDto
```json
{
  "type": "object",
  "properties": {
    "measurementId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "segmentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema StatisticsScope
```json
{
  "type": "object",
  "properties": {
    "obligationId": {
      "type": "string",
      "format": "uuid"
    },
    "defectId": {
      "type": "string",
      "format": "uuid"
    },
    "defectVersion": {
      "type": "string",
      "nullable": true
    },
    "scopeHash": {
      "type": "string",
      "nullable": true
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "locationVersion": {
      "type": "string",
      "nullable": true
    },
    "from": {
      "type": "number",
      "format": "double"
    },
    "to": {
      "type": "number",
      "format": "double"
    },
    "offsetFrom": {
      "type": "number",
      "format": "double"
    },
    "offsetTo": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```

## Schema StatisticsSegment
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema StatisticsSegmentCount
```json
{
  "type": "object",
  "properties": {
    "segmentId": {
      "type": "string",
      "format": "uuid"
    },
    "relatedDefects": {
      "type": "integer",
      "format": "int64",
      "nullable": true
    },
    "knownRelatedDefects": {
      "type": "integer",
      "format": "int64"
    },
    "availability": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SubmitDatasetRequestDto
```json
{
  "required": [
    "deviceId",
    "recordedAt",
    "scope",
    "telemetryFileIds",
    "videoFileIds"
  ],
  "type": "object",
  "properties": {
    "videoFileIds": {
      "minItems": 1,
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "description": ""
    },
    "telemetryFileIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "description": ""
    },
    "recordedAt": {
      "type": "string",
      "description": "",
      "format": "date-time"
    },
    "deviceId": {
      "type": "string",
      "description": "Unconditionally required nonempty registered device UUID; schema must preserve Guid nullable representation otherwise.",
      "format": "uuid"
    },
    "scope": {
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BandScopeDto"
      },
      "description": ""
    },
    "pairs": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/DatasetPairDto"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SupplementSurveyTaskV2RequestDto
```json
{
  "required": [
    "reason",
    "scope"
  ],
  "type": "object",
  "properties": {
    "scope": {
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BandScopeDto"
      },
      "description": ""
    },
    "reason": {
      "minLength": 1,
      "type": "string",
      "description": ""
    },
    "operatorId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyGeometryRefDto
```json
{
  "type": "object",
  "properties": {
    "routeVersionId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentSetId": {
      "type": "string",
      "format": "uuid"
    },
    "segmentIds": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "uuid"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyPlanStatus
```json
{
  "enum": [
    0,
    1,
    2,
    3,
    4,
    5
  ],
  "type": "integer",
  "format": "int32"
}
```

## Schema SurveyPlanSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "plannedAt": {
      "type": "string",
      "format": "date-time"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyPlanSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SurveyPlanSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyPlanV2ResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "scope": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BandScopeDto"
      },
      "nullable": true
    },
    "plannedAt": {
      "type": "string",
      "format": "date-time"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyRequestStatus
```json
{
  "enum": [
    0,
    1,
    2,
    3,
    4,
    5,
    6,
    7,
    8,
    9,
    10
  ],
  "type": "integer",
  "format": "int32"
}
```

## Schema SurveyTaskPageV2ResponseDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SurveyTaskV2ResponseDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    },
    "asOf": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyTaskReasonV2RequestDto
```json
{
  "required": [
    "reason"
  ],
  "type": "object",
  "properties": {
    "reason": {
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyTaskSummary
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "operatorId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "supplementTaskId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyTaskSummaryCollectionPage
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SurveyTaskSummary"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyTaskV2ResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "scope": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/BandScopeDto"
      },
      "nullable": true
    },
    "operatorId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    },
    "supplementTaskId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyTaskWorkPackage
```json
{
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "string",
      "nullable": true
    },
    "task": {
      "$ref": "#/components/schemas/SurveyTaskV2ResponseDto"
    },
    "dueAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "accessPoint": {
      "$ref": "#/components/schemas/PositionDto"
    },
    "geometryRefs": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/SurveyGeometryRefDto"
      },
      "nullable": true
    },
    "scopeVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema SurveyType
```json
{
  "enum": [
    0,
    1,
    2,
    3
  ],
  "type": "integer",
  "format": "int32"
}
```

## Schema TrainingLabelPageDto
```json
{
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/TrainingLabelViewDto"
      },
      "nullable": true
    },
    "nextCursor": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema TrainingLabelViewDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "revision": {
      "type": "integer",
      "format": "int32"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "sourceKind": {
      "type": "string",
      "nullable": true
    },
    "sourceId": {
      "type": "string",
      "format": "uuid"
    },
    "sourceVersion": {
      "type": "string",
      "nullable": true
    },
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "fileVersion": {
      "type": "string",
      "nullable": true
    },
    "annotation": {
      "$ref": "#/components/schemas/LabelAnnotationDto"
    },
    "defectTypeCode": {
      "type": "string",
      "nullable": true
    },
    "reviewedBy": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "reviewedAt": {
      "type": "string",
      "format": "date-time",
      "nullable": true
    },
    "reviewReason": {
      "type": "string",
      "nullable": true
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema UpdateMeRequestDto
```json
{
  "required": [
    "displayName"
  ],
  "type": "object",
  "properties": {
    "displayName": {
      "maxLength": 200,
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema UpdateProjectRequestDto
```json
{
  "required": [
    "expectedRowVersion",
    "name"
  ],
  "type": "object",
  "properties": {
    "name": {
      "maxLength": 255,
      "type": "string",
      "description": ""
    },
    "description": {
      "type": "string",
      "nullable": true
    },
    "engineeringUtmSrid": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "startDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "endDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "expectedRowVersion": {
      "type": "string",
      "description": ""
    },
    "operationId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema UpdateProjectResponseDto
```json
{
  "type": "object",
  "properties": {
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "projectCode": {
      "type": "string",
      "nullable": true
    },
    "name": {
      "type": "string",
      "nullable": true
    },
    "description": {
      "type": "string",
      "nullable": true
    },
    "engineeringUtmSrid": {
      "type": "integer",
      "format": "int32",
      "nullable": true
    },
    "startDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "endDate": {
      "type": "string",
      "format": "date",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "rowVersion": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema UploadCompleteRequestDto
```json
{
  "required": [
    "checksumSha256",
    "parts"
  ],
  "type": "object",
  "properties": {
    "parts": {
      "minItems": 1,
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/CompletedUploadPartDto"
      },
      "description": ""
    },
    "checksumSha256": {
      "pattern": "^[a-f0-9]{64}$",
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema UploadCreateRequestDto
```json
{
  "required": [
    "checksumSha256",
    "fileName",
    "mediaType",
    "purpose"
  ],
  "type": "object",
  "properties": {
    "purpose": {
      "maxLength": 40,
      "type": "string",
      "description": "Accepted values after trimming and case normalization: REPORT_PHOTO, BEFORE, AFTER, MEASUREMENT, SURVEY_VIDEO, TELEMETRY, ROUTE_SOURCE, DOCUMENT."
    },
    "projectId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "targetId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "fileName": {
      "maxLength": 255,
      "type": "string",
      "description": ""
    },
    "mediaType": {
      "maxLength": 120,
      "type": "string",
      "description": ""
    },
    "sizeBytes": {
      "maximum": 8589934592,
      "minimum": 1,
      "type": "integer",
      "description": "Global admitted union bound8GiB. Conditional: photos20MiB,video8GiB,telemetry10MiB,DOCUMENT/ROUTE_SOURCE int.MaxValue. Not every purpose can use the global maximum.",
      "format": "int64"
    },
    "checksumSha256": {
      "pattern": "^[a-f0-9]{64}$",
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema UploadPartUrlResponseDto
```json
{
  "type": "object",
  "properties": {
    "partNumber": {
      "type": "integer",
      "format": "int32"
    },
    "url": {
      "type": "string",
      "nullable": true
    },
    "expiresAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema UploadPartUrlsRequestDto
```json
{
  "required": [
    "partNumbers"
  ],
  "type": "object",
  "properties": {
    "partNumbers": {
      "minItems": 1,
      "type": "array",
      "items": {
        "type": "integer",
        "format": "int32"
      },
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema UploadPartUrlsResponseDto
```json
{
  "type": "object",
  "properties": {
    "parts": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/UploadPartUrlResponseDto"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema UploadSessionResponseDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "fileId": {
      "type": "string",
      "format": "uuid"
    },
    "status": {
      "type": "string",
      "nullable": true
    },
    "partSizeBytes": {
      "type": "integer",
      "format": "int32"
    },
    "expiresAt": {
      "type": "string",
      "format": "date-time"
    },
    "version": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema ValidationPairDto
```json
{
  "type": "object",
  "properties": {
    "groundTruthId": {
      "type": "string",
      "format": "uuid"
    },
    "derivedMeasurementId": {
      "type": "string",
      "format": "uuid"
    }
  },
  "additionalProperties": false
}
```

## Schema ValidationResultDto
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "usedCount": {
      "type": "integer",
      "format": "int32"
    },
    "excludedCount": {
      "type": "integer",
      "format": "int32"
    },
    "bias": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "mae": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "rmse": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "unit": {
      "type": "string",
      "nullable": true
    },
    "exclusionReasons": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema VerifyReporterOtpRequestDto
```json
{
  "required": [
    "otp"
  ],
  "type": "object",
  "properties": {
    "intentId": {
      "type": "string",
      "format": "uuid"
    },
    "otp": {
      "minLength": 1,
      "type": "string",
      "description": ""
    }
  },
  "additionalProperties": false
}
```

## Schema WarrantyScope
```json
{
  "enum": [
    0,
    1,
    2,
    3,
    4
  ],
  "type": "integer",
  "format": "int32"
}
```

## Schema WarrantyStatus
```json
{
  "enum": [
    0,
    1,
    2,
    3,
    4
  ],
  "type": "integer",
  "format": "int32"
}
```

## Schema WarrantyWorkPackageDto
```json
{
  "type": "object",
  "properties": {
    "warrantyId": {
      "type": "string",
      "format": "uuid"
    },
    "roadSectionId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "handoverDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyStartDate": {
      "type": "string",
      "format": "date"
    },
    "warrantyEndDate": {
      "type": "string",
      "format": "date"
    },
    "retainedValue": {
      "type": "number",
      "format": "double",
      "nullable": true
    },
    "scope": {
      "type": "string",
      "nullable": true
    },
    "terms": {
      "type": "string",
      "nullable": true
    },
    "sourceDocumentId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "status": {
      "type": "string",
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema WeeklyDigestDutyView
```json
{
  "type": "object",
  "properties": {
    "clockId": {
      "type": "string",
      "format": "uuid"
    },
    "targetId": {
      "type": "string",
      "format": "uuid"
    },
    "kind": {
      "type": "string",
      "nullable": true
    },
    "originEventId": {
      "type": "string",
      "format": "uuid"
    },
    "originAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "dueAtRecoveryUtc": {
      "type": "string",
      "format": "date-time"
    }
  },
  "additionalProperties": false
}
```

## Schema WeeklyDigestView
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "format": "uuid"
    },
    "projectId": {
      "type": "string",
      "format": "uuid"
    },
    "recipientId": {
      "type": "string",
      "format": "uuid",
      "nullable": true
    },
    "scheduledAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "recoveredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "pendingAtRecovery": {
      "type": "array",
      "items": {
        "$ref": "#/components/schemas/WeeklyDigestDutyView"
      },
      "nullable": true
    },
    "recoveryPeriods": {
      "type": "array",
      "items": {
        "type": "string",
        "format": "date-time"
      },
      "nullable": true
    }
  },
  "additionalProperties": false
}
```

## Schema WidthProfileItem
```json
{
  "type": "object",
  "properties": {
    "fromOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "toOffsetMeters": {
      "type": "number",
      "format": "double"
    },
    "widthMeters": {
      "type": "number",
      "format": "double"
    }
  },
  "additionalProperties": false
}
```
