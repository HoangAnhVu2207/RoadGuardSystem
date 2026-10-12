# Request, response and DTO catalog

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

All 357 runtime component schemas are reproduced below without promoting V2 types. Required is object-level `required`; nullable is its observed OpenAPI flag and does not imply optional. `allOf`/`oneOf`/`$ref`, defaults, enum values, bounds and formats are retained. Source declarations include internal DTOs not exposed by current Swagger. Credentials are field names only, never values.

## AcceptInvitationRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Invitations/AcceptInvitationRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Invitations/AcceptInvitationRequestDto.cs)

## AccountUpdateRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/AccountUpdateRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Identity/AccountUpdateRequestDto.cs)

## ActiveCandidateDisposition

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

Source matches: [RoadGuardSystem.BusinessObjects/Candidates/ActiveCandidateDisposition.cs:3](../../../RoadGuardSystem.BusinessObjects/Candidates/ActiveCandidateDisposition.cs)

## ActorResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/ActorResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Identity/ActorResponseDto.cs)

## AdminPasswordResetRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/AdminPasswordResetRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Identity/AdminPasswordResetRequestDto.cs)

## AdminPasswordResetResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/AdminPasswordResetResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Identity/AdminPasswordResetResponseDto.cs)

## AdminResetPasswordV2RequestDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/AdminResetPasswordV2RequestDto.cs:6](../../../RoadGuardSystem.DTOs/Identity/AdminResetPasswordV2RequestDto.cs)

## AiCandidateFactsV1

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

Source matches: [RoadGuardSystem.Services/Interfaces/Integration/Anh02Contracts.cs:58](../../../RoadGuardSystem.Services/Interfaces/Integration/Anh02Contracts.cs)

## AiDetectionDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/AiDetectionDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/AiDetectionDto.cs)

## AiDetectionResultV1

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

Source matches: [RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:38](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs)

## AiMatchResultV1

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

Source matches: [RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:44](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs)

## AiMockRunView

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

Source matches: [RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:27](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs)

## AiMockScopeRequest

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

Source matches: [RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:5](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs)

## AiResultV1

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

Source matches: [RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:46](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs)

## AsBuiltLayoutInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:73](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs)

## AsBuiltSlabInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:62](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs)

## AssessmentEvidenceDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:3](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); [RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs:3](../../../RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs)

## AssessmentItemDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:4](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); [RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs:4](../../../RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs)

## AssessmentView

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:7](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## AuthActorResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/AuthActorResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Authentication/AuthActorResponseDto.cs)

## AuthTokenResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/AuthTokenResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Authentication/AuthTokenResponseDto.cs)

## BandScopeDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/BandScopeDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/BandScopeDto.cs)

## BaselineItemDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:9](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); [RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs:8](../../../RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs)

## BaselineSelectedItemDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:12](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## BaselineView

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:14](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## BusinessDutyAppointmentInput

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

Source matches: [RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs:4](../../../RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs)

## BusinessDutyView

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

Source matches: [RoadGuardSystem.Repositories/Interfaces/Messaging/IBusinessDutyRepository.cs:8](../../../RoadGuardSystem.Repositories/Interfaces/Messaging/IBusinessDutyRepository.cs)

## BusinessRequestAckInput

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

Source matches: [RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs:3](../../../RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs)

## CandidateClassificationDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Candidates/CandidateClassification.cs:5](../../../RoadGuardSystem.BusinessObjects/Candidates/CandidateClassification.cs); [RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:5](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs)

## CandidateDecisionRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:8](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs)

## CandidateDecisionResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:14](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs)

## CandidateMatchHistoryDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:4](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs)

## CandidateMatchItemDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:5](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs); [RoadGuardSystem.Services/Interfaces/Candidates/CandidateMatcher.cs:7](../../../RoadGuardSystem.Services/Interfaces/Candidates/CandidateMatcher.cs)

## CandidateMatchPageDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:9](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs)

## CandidateMatchSourceDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:3](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs)

## CandidateRecurrenceDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:13](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs)

## CaseConclusionDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Cases/CaseConclusion.cs:3](../../../RoadGuardSystem.BusinessObjects/Cases/CaseConclusion.cs); [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:12](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## CaseConclusionViewDto

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

Source matches: [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:27](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## CaseLinkDto

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

Source matches: [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:8](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## CasePageDto

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

Source matches: [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:46](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## CasePublicationDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Cases/CasePublication.cs:3](../../../RoadGuardSystem.BusinessObjects/Cases/CasePublication.cs); [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:14](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## CasePublicationResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:37](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## CaseSplitDto

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

Source matches: [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:10](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## CaseTriageDto

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

Source matches: [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:5](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## ChainageCalibrationInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:90](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## ChainageControl

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:81](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## ChangePasswordRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/ChangePasswordRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/ChangePasswordRequestDto.cs)

## ClockExtensionInput

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

Source matches: [RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs:5](../../../RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs)

## CompletedUploadPartDto

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

Source matches: [RoadGuardSystem.DTOs/Files/CompletedUploadPartDto.cs:5](../../../RoadGuardSystem.DTOs/Files/CompletedUploadPartDto.cs)

## ConfirmRetentionBasisRequest

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:4](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## CreateAiMockRunRequest

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

Source matches: [RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:15](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs)

## CreateAssessmentDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:6](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## CreateBaselineDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:11](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## CreateExportRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Exports/ExportDtos.cs:6](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs)

## CreateHandoverRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/CreateHandoverRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateHandoverRequestDto.cs)

## CreateInvitationRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Invitations/CreateInvitationRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Invitations/CreateInvitationRequestDto.cs)

## CreateProcessingJobRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/CreateProcessingJobRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Processing/CreateProcessingJobRequestDto.cs)

## CreateProjectRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/CreateProjectRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateProjectRequestDto.cs)

## CreateProjectResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/CreateProjectResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/CreateProjectResponseDto.cs)

## CreateReporterReportRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:20](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs)

## CreateRetentionEvaluationRequest

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:34](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## CreateRetentionHoldRequest

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:14](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## CreateRoadSectionRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/CreateRoadSectionRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateRoadSectionRequestDto.cs)

## CreateRoadSectionVersionRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/CreateRoadSectionVersionRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateRoadSectionVersionRequestDto.cs)

## CreateSurveyPlanRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanRequestDto.cs)

## CreateSurveyPlanResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanResponseDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanResponseDto.cs)

## CreateSurveyPlanV2RequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanV2RequestDto.cs)

## CreateSurveyRequestRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestRequestDto.cs)

## CreateSurveyRequestResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestResponseDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestResponseDto.cs)

## CreateSurveyTaskV2RequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/CreateSurveyTaskV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyTaskV2RequestDto.cs)

## CreateTrainingLabelDto

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

Source matches: [RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:17](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs)

## CreateValidationRunRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/CreateValidationRunRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/CreateValidationRunRequestDto.cs)

## CreateWarrantyRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Warranties/CreateWarrantyRequestDto.cs:3](../../../RoadGuardSystem.DTOs/Warranties/CreateWarrantyRequestDto.cs)

## CreateWarrantyResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Warranties/CreateWarrantyResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Warranties/CreateWarrantyResponseDto.cs)

## CrsControlPoint

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## CrsEllipsoidDefinition

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:12](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## CrsProfileInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:40](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## CrsProfileView

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:53](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## CrsProjectionDefinition

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:21](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## CrsTransformOperation

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:30](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## DatasetCoverageItemDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/DatasetCoverageItemDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/DatasetCoverageItemDto.cs)

## DatasetCoverageResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/DatasetCoverageResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/DatasetCoverageResponseDto.cs)

## DatasetDetailView

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:16](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## DatasetPairDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs:13](../../../RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs); [RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs:226](../../../RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs)

## DatasetResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/DatasetResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/DatasetResponseDto.cs)

## DatasetSourceFileDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:15](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## DefectAssessmentRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:5](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs)

## DefectGeometryDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:13](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs)

## DefectPageDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:33](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs)

## DefectStatisticsInputDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:10](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs); [RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs:7](../../../RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs)

## DefectStatisticsRead

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:26](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## DefectStatisticsSnapshot

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:23](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## DefectStatisticsSource

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:4](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## DefectVerificationRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:9](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs)

## DefectViewDto

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

Source matches: [RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:22](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs)

## ExportFileDto

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

Source matches: [RoadGuardSystem.DTOs/Exports/ExportDtos.cs:26](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs)

## ExportJobViewDto

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

Source matches: [RoadGuardSystem.DTOs/Exports/ExportDtos.cs:16](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs)

## ExportLabelDto

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

Source matches: [RoadGuardSystem.DTOs/Exports/ExportDtos.cs:44](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs)

## ExportManifestV1Dto

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

Source matches: [RoadGuardSystem.DTOs/Exports/ExportDtos.cs:53](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs)

## ExportSectionDto

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

Source matches: [RoadGuardSystem.DTOs/Exports/ExportDtos.cs:35](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs)

## ExportSourceRevisionDto

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

Source matches: [RoadGuardSystem.DTOs/Exports/ExportDtos.cs:17](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs)

## FieldEvidenceDeclaration

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:71](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldEvidenceDeclarationFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs:5](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs)

## FieldEvidenceReuseInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:114](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldHandoverInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:28](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldLocationImpactActionInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:38](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldMeasurementInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:59](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldMeasurementInputFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs:14](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs)

## FieldPositionProofInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:82](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldReviewInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:104](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldReviewViewFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs:22](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs)

## FieldStartInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:48](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldSubmissionInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:92](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldSubmissionViewFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs:31](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs)

## FieldTaskActionInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:18](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldTaskCreateInput

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

Source matches: [RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:5](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs)

## FieldTaskEvidenceFile

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

Source matches: [RoadGuardSystem.Repositories/Interfaces/Inspections/IFieldInspectionWorkflowRepository.cs:21](../../../RoadGuardSystem.Repositories/Interfaces/Inspections/IFieldInspectionWorkflowRepository.cs)

## FieldTaskViewFact

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
    }
  },
  "additionalProperties": false
}
```

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs:43](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs)

## FileMetadataResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Files/FileMetadataResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/FileMetadataResponseDto.cs)

## ForcedPasswordChangeRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/ForcedPasswordChangeRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Authentication/ForcedPasswordChangeRequestDto.cs)

## GeometryAffectedReferenceFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs:34](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs)

## GeometryConfirmInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:87](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## GeometryDraftInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:21](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## GeometryDraftReadiness

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:100](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## GeometryDraftView

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:37](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## GeometryImpactDecisionInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:72](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs)

## GeometryImpactDecisionView

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

Source matches: [RoadGuardSystem.Repositories/Interfaces/Projects/IPavementWorkflowRepository.cs:16](../../../RoadGuardSystem.Repositories/Interfaces/Projects/IPavementWorkflowRepository.cs)

## GeometryImpactInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:63](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs)

## GeometryImpactView

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

Source matches: [RoadGuardSystem.Repositories/Interfaces/Projects/IPavementWorkflowRepository.cs:14](../../../RoadGuardSystem.Repositories/Interfaces/Projects/IPavementWorkflowRepository.cs)

## GeometryMapFeature

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:26](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs)

## GeometryMapLayer

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs)

## GeometryMapManifest

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:13](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs)

## GeometryMapPage

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:44](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs)

## GeometryMapPublishInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:54](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs)

## GeometryPackageView

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:158](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## GeometryPoint

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## GeometryPointFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs:82](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs)

## GeometryPreview

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:60](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## GeometryShape

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:51](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## H5EncryptedPackageDto

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

Source matches: [RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs:15](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs)

## H5PackageHeaderDto

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

Source matches: [RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs:7](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs)

## H5RecipientKeyWrapDto

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

Source matches: [RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs:11](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs)

## H6ClockBreachView

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:44](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## H6ClockCursor

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:45](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## H6ClockExtensionView

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:42](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## H6ClockPage

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:47](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## H6ClockView

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:33](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## H6NotificationScopeDto

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:16](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## InspectionTaskPageResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Inspections/InspectionTaskPageResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Inspections/InspectionTaskPageResponseDto.cs)

## InspectionTaskResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Inspections/InspectionTaskResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Inspections/InspectionTaskResponseDto.cs)

## InternalCaseDto

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

Source matches: [RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:16](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs)

## InvitationResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Invitations/InvitationResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Invitations/InvitationResponseDto.cs)

## LD06ActionDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:14](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs)

## LD06LifecycleInputDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:20](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); [RoadGuardSystem.Repositories/Interfaces/Projects/IProjectLifecycleRepository.cs:34](../../../RoadGuardSystem.Repositories/Interfaces/Projects/IProjectLifecycleRepository.cs)

## LD06TransferableObligationDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:11](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs)

## LabelAnnotationDto

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

Source matches: [RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:5](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs)

## LoginRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/LoginRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/LoginRequestDto.cs)

## NativeAlignmentInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs:13](../../../RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs)

## NativeAlignmentPrimitive

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

Source matches: [RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs)

## NormalizedBoundingBoxV1

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

Source matches: [RoadGuardSystem.Services/Interfaces/Integration/Anh02Contracts.cs:21](../../../RoadGuardSystem.Services/Interfaces/Integration/Anh02Contracts.cs)

## NotificationDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Messaging/Notification.cs:3](../../../RoadGuardSystem.BusinessObjects/Messaging/Notification.cs); [RoadGuardSystem.DTOs/Messaging/NotificationDto.cs:3](../../../RoadGuardSystem.DTOs/Messaging/NotificationDto.cs)

## NotificationPageDto

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

Source matches: [RoadGuardSystem.DTOs/Messaging/NotificationPageDto.cs:3](../../../RoadGuardSystem.DTOs/Messaging/NotificationPageDto.cs)

## ObligationResponsibilityDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs:78](../../../RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs); [RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:16](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs)

## OfflineBatchResult

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:58](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineCaptureArtifactInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs:7](../../../RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs)

## OfflineCaptureChunk

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

Source matches: [RoadGuardSystem.BusinessObjects/Offline/OfflineCaptureArtifacts.cs:6](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineCaptureArtifacts.cs)

## OfflineCaptureManifest

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

Source matches: [RoadGuardSystem.BusinessObjects/Offline/OfflineCaptureArtifacts.cs:8](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineCaptureArtifacts.cs)

## OfflineDeviceRegisterInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:7](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineDeviceRevokeInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:9](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineGrantItemScope

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:13](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineGrantRevokeInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:19](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineHandoverGrantInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:16](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineItemResult

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:54](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineOperationDescriptor

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:21](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineOperationInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:36](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflinePackageExportInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:27](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflinePackageImportInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:45](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineRepairPayload

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:32](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineSignedBatchInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:42](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineSnapshotInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:11](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineSourceManifestInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:24](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineStartReconcileInput

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

Source matches: [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:49](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs)

## OfflineUploadCreateRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Files/OfflineUploadCreateRequestDto.cs:4](../../../RoadGuardSystem.DTOs/Files/OfflineUploadCreateRequestDto.cs)

## OwnReportDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:31](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs)

## OwnReportEvidenceDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:3](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs)

## OwnReportPageDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:42](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs)

## OwnReportSupplementDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:13](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs)

## OwnReportUpdateDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:22](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs)

## PasswordRecoveryRequestDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Identity/PasswordRecoveryRequest.cs:3](../../../RoadGuardSystem.BusinessObjects/Identity/PasswordRecoveryRequest.cs); [RoadGuardSystem.DTOs/Authentication/PasswordRecoveryRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/PasswordRecoveryRequestDto.cs)

## PavementGeometryPreviewFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs:95](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs)

## PavementLayoutInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:12](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs)

## PavementLayoutPreviewFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs:101](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs)

## PavementLayoutView

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

Source matches: [RoadGuardSystem.Repositories/Interfaces/Projects/IPavementWorkflowRepository.cs:11](../../../RoadGuardSystem.Repositories/Interfaces/Projects/IPavementWorkflowRepository.cs)

## PavementPlanCreateInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:42](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs)

## PavementWidthInterval

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

Source matches: [RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs)

## PlannedSlabCellFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs:108](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs)

## PointDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/PointDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PointDto.cs)

## PositionDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/PositionDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PositionDto.cs)

## PostponeSurveyPlanRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanRequestDto.cs)

## PostponeSurveyPlanResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanResponseDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanResponseDto.cs)

## PostponeSurveyPlanV2RequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanV2RequestDto.cs)

## PrimaryProjectManagerReadDto

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

Source matches: [RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:15](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs)

## PrimaryProjectManagerResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:13](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs)

## ProblemDetails

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

Source matches: Framework/generated/composed schema; inspect runtime $ref and API action/source DTO catalog.

## ProcessingJobResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/ProcessingJobResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Processing/ProcessingJobResponseDto.cs)

## ProfileResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/ProfileResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Identity/ProfileResponseDto.cs)

## ProfileUpdateRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/ProfileUpdateRequestDto.cs:7](../../../RoadGuardSystem.DTOs/Identity/ProfileUpdateRequestDto.cs)

## ProjectLifecycleHistoryDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs)

## ProjectLifecycleViewDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:6](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs)

## ProjectListItemResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs)

## ProjectListResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs:11](../../../RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs)

## ProjectMemberResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:10](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs)

## ProjectMemberResponseDtoStaffPageResponseDto

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

Source matches: Framework/generated/composed schema; inspect runtime $ref and API action/source DTO catalog.

## ProjectSummaryV1

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:53](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ProjectWorkPackageResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectWorkPackageResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/ProjectWorkPackageResponseDto.cs)

## ReassignPrimaryProjectManagerRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerRequestDto.cs)

## ReassignPrimaryProjectManagerResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerResponseDto.cs)

## ReassignSurveyTaskV2RequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/ReassignSurveyTaskV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/ReassignSurveyTaskV2RequestDto.cs)

## ReceiveAiResultRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/ReceiveAiResultRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/ReceiveAiResultRequestDto.cs)

## RefreshRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/RefreshRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/RefreshRequestDto.cs)

## RegisterReporterRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/RegisterReporterRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/RegisterReporterRequestDto.cs)

## RegistrationIntentResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/RegistrationIntentResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Authentication/RegistrationIntentResponseDto.cs)

## ReleaseRetentionHoldRequest

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:24](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RenewedHandlingScopeInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:17](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs)

## RepairActualScopeInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:7](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairAssessmentView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:58](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairAttemptHistoryView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:15](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs)

## RepairAttemptIntakeView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:65](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairAttemptReviewInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:35](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairAttemptReviewView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:67](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairAttemptSubmitInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:31](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairAttemptSupplementInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:33](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairCancellationInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:8](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs)

## RepairCorrectionBasisHistoryView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:10](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs)

## RepairCorrectionBasisInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:5](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs)

## RepairCorrectionInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:8](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs)

## RepairCorrectionView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:17](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs)

## RepairDecisionHistoryView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:6](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs)

## RepairDecisionInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:16](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairEligibilityReadView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairEligibilityReadDtos.cs:4](../../../RoadGuardSystem.DTOs/Repairs/RepairEligibilityReadDtos.cs)

## RepairEvidenceHistoryView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:12](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs)

## RepairExecutionFinishInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:28](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairExecutionFinishView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:62](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairExecutionStartInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:25](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairExecutionStartView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:60](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairHandoverSource

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

Source matches: [RoadGuardSystem.BusinessObjects/Repairs/RepairEligibilitySourceSnapshot.cs:10](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairEligibilitySourceSnapshot.cs)

## RepairHistoryView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:3](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs)

## RepairItemAssignInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:18](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairItemProposeInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:14](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairItemView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:69](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairLifecycleHandoverInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:5](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs)

## RepairLifecycleView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:13](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs)

## RepairMeasurementAssessmentInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:20](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairMeasurementRule

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

Source matches: [RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs:16](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs)

## RepairNormalContinuationInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:10](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs)

## RepairObligationInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:10](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairPackageCreateInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:12](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairPackageView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:71](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairPolicyDefinitionInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs:6](../../../RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs)

## RepairPolicyReasonInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs:9](../../../RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs)

## RepairPolicyView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs:11](../../../RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs)

## RepairReviewRequestHistoryView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:21](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs)

## RepairReviewRequestInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:11](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs)

## RepairReviewRequestView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:14](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs)

## RepairSafetyAcknowledgementInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:7](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs)

## RepairSafetyAcknowledgementView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:17](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs)

## RepairSafetyCheckInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:47](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairSafetyCheckView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:13](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs)

## RepairSafetyCreateInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:44](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairSafetyInstallInput

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:5](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs)

## RepairSafetyView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:9](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs)

## RepairSafetyWarningView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:15](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs)

## RepairTaskBindingView

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:53](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs)

## RepairWarrantySource

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

Source matches: [RoadGuardSystem.BusinessObjects/Repairs/RepairEligibilitySourceSnapshot.cs:7](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairEligibilitySourceSnapshot.cs)

## ReportEvidenceInputDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:16](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs)

## ReportEvidenceLocationDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:5](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs)

## ReporterEvidenceCreateRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Files/ReporterEvidenceCreateRequestDto.cs:4](../../../RoadGuardSystem.DTOs/Files/ReporterEvidenceCreateRequestDto.cs)

## ReporterReportResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:23](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs)

## ReportingAvailabilityDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:94](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingDimensionsDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:32](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingFiltersDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:3](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingItemDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:63](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingItemsPageDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:103](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingMetricDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:22](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingSourceRefDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:13](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingTimelineItemDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:84](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ReportingTimelinePageDto

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

Source matches: [RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:105](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs)

## ResendReporterOtpRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/ResendReporterOtpRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Authentication/ResendReporterOtpRequestDto.cs)

## RetentionBasisView

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:94](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RetentionEvaluationView

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:106](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RetentionFileView

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:82](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RetentionHoldView

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:62](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RetentionItemView

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:72](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RetentionReferenceView

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:44](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RetentionWarrantyView

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

Source matches: [RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:53](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs)

## RetryProcessingJobRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/RetryProcessingJobRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/RetryProcessingJobRequestDto.cs)

## ReviewTrainingLabelDto

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

Source matches: [RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:25](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs)

## ReviseTrainingLabelDto

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

Source matches: [RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:21](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs)

## RoadCoverageInputDto

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

Source matches: [RoadGuardSystem.DTOs/Repairs/RoadCoverageDtos.cs:6](../../../RoadGuardSystem.DTOs/Repairs/RoadCoverageDtos.cs)

## RoadCoverageMapping

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

Source matches: [RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs:5](../../../RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs)

## RoadCoverageReadFact

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

Source matches: [RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs:20](../../../RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs)

## RoadCoverageScopeFact

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

Source matches: [RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs:16](../../../RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs)

## RoadCoverageSourceFact

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

Source matches: [RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs:18](../../../RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs)

## RoadGeometryVersionView

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:71](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## RoadSectionCoordinateDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/RoadSectionCoordinateDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/RoadSectionCoordinateDto.cs); [RoadGuardSystem.Services/Implementations/Projects/RoadSectionCoordinate.cs:3](../../../RoadGuardSystem.Services/Implementations/Projects/RoadSectionCoordinate.cs)

## RoadSectionVersionResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/RoadSectionVersionResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/RoadSectionVersionResponseDto.cs)

## RoadSectionWorkPackageDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/RoadSectionWorkPackageDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/RoadSectionWorkPackageDto.cs)

## RouteSystemInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:63](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## RouteSystemView

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

Source matches: [RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:72](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs)

## SegmentDefinition

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:96](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## SegmentGeometry

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:107](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## SegmentGeometryView

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:127](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## SegmentPreview

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:117](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## SegmentPublishInput

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:149](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## SegmentSetView

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:138](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## SelectedBaselineItem

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

Source matches: [RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs:10](../../../RoadGuardSystem.Repositories/Interfaces/Surveys/AssessmentContracts.cs)

## SlabGeometrySnapshotFact

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

Source matches: [RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs:145](../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/ProjectsFacts.cs)

## StaffUserResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:5](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs)

## StaffUserResponseDtoStaffPageResponseDto

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

Source matches: Framework/generated/composed schema; inspect runtime $ref and API action/source DTO catalog.

## StatisticsDefect

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:22](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## StatisticsMeasurement

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:17](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## StatisticsQuantity

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:20](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## StatisticsQuantityInputDto

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:9](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs); [RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs:5](../../../RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs)

## StatisticsScope

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:14](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## StatisticsSegment

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:16](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## StatisticsSegmentCount

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

Source matches: [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:19](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

## SubmitDatasetRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs)

## SupplementSurveyTaskV2RequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/SupplementSurveyTaskV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/SupplementSurveyTaskV2RequestDto.cs)

## SurveyGeometryRefDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:21](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## SurveyPlanStatus

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

Source matches: [RoadGuardSystem.BusinessObjects/Common/Enums.cs:88](../../../RoadGuardSystem.BusinessObjects/Common/Enums.cs)

## SurveyPlanV2ResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/SurveyPlanV2ResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/SurveyPlanV2ResponseDto.cs)

## SurveyRequestStatus

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

Source matches: [RoadGuardSystem.BusinessObjects/Common/Enums.cs:98](../../../RoadGuardSystem.BusinessObjects/Common/Enums.cs)

## SurveyTaskPageV2ResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/SurveyTaskPageV2ResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/SurveyTaskPageV2ResponseDto.cs)

## SurveyTaskReasonV2RequestDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/SurveyTaskReasonV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/SurveyTaskReasonV2RequestDto.cs)

## SurveyTaskV2ResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Surveys/SurveyTaskV2ResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/SurveyTaskV2ResponseDto.cs)

## SurveyTaskWorkPackage

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

Source matches: [RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:19](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs)

## SurveyType

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

Source matches: [RoadGuardSystem.BusinessObjects/Common/Enums.cs:80](../../../RoadGuardSystem.BusinessObjects/Common/Enums.cs)

## TrainingLabelPageDto

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

Source matches: [RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:43](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs)

## TrainingLabelViewDto

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

Source matches: [RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:28](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs)

## UpdateMeRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Identity/UpdateMeRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Identity/UpdateMeRequestDto.cs)

## UpdateProjectRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/UpdateProjectRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/UpdateProjectRequestDto.cs)

## UpdateProjectResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/UpdateProjectResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/UpdateProjectResponseDto.cs)

## UploadCompleteRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Files/UploadCompleteRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Files/UploadCompleteRequestDto.cs)

## UploadCreateRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Files/UploadCreateRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Files/UploadCreateRequestDto.cs)

## UploadPartUrlResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Files/UploadPartUrlResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/UploadPartUrlResponseDto.cs)

## UploadPartUrlsRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Files/UploadPartUrlsRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Files/UploadPartUrlsRequestDto.cs)

## UploadPartUrlsResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Files/UploadPartUrlsResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/UploadPartUrlsResponseDto.cs)

## UploadSessionResponseDto

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

Source matches: [RoadGuardSystem.DTOs/Files/UploadSessionResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/UploadSessionResponseDto.cs)

## ValidationPairDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/ValidationPairDto.cs:3](../../../RoadGuardSystem.DTOs/Processing/ValidationPairDto.cs)

## ValidationResultDto

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

Source matches: [RoadGuardSystem.DTOs/Processing/ValidationResultDto.cs:3](../../../RoadGuardSystem.DTOs/Processing/ValidationResultDto.cs)

## VerifyReporterOtpRequestDto

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

Source matches: [RoadGuardSystem.DTOs/Authentication/VerifyReporterOtpRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/VerifyReporterOtpRequestDto.cs)

## WarrantyScope

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

Source matches: [RoadGuardSystem.BusinessObjects/Common/Enums.cs:62](../../../RoadGuardSystem.BusinessObjects/Common/Enums.cs)

## WarrantyStatus

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

Source matches: [RoadGuardSystem.BusinessObjects/Common/Enums.cs:71](../../../RoadGuardSystem.BusinessObjects/Common/Enums.cs)

## WarrantyWorkPackageDto

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

Source matches: [RoadGuardSystem.DTOs/Projects/WarrantyWorkPackageDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/WarrantyWorkPackageDto.cs)

## WeeklyDigestDutyView

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:7](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## WeeklyDigestView

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

Source matches: [RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:9](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs)

## WidthProfileItem

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

Source matches: [RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:12](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs)

## Complete source DTO declarations

All declarations physically under RoadGuardSystem.DTOs (including enums/nested/partial records); this is distinct from runtime schema count. Positional record parameters are source properties even when the syntax property list is empty.

### RoadGuardSystem.DTOs.Authentication.AuthActorResponseDto

[RoadGuardSystem.DTOs/Authentication/AuthActorResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Authentication/AuthActorResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     string DisplayName,     string Role,     string Version)`; ``

### RoadGuardSystem.DTOs.Authentication.AuthTokenResponseDto

[RoadGuardSystem.DTOs/Authentication/AuthTokenResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Authentication/AuthTokenResponseDto.cs); `RecordDeclaration`; `(     string AccessToken,     string RefreshToken,     string TokenType,     int ExpiresIn,     bool MustChangePassword,     AuthActorResponseDto User)`; ``

### RoadGuardSystem.DTOs.Authentication.ChangePasswordRequestDto

[RoadGuardSystem.DTOs/Authentication/ChangePasswordRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/ChangePasswordRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| CurrentPassword | `string?` | `[Required] [StringLength(1024, MinimumLength = 1)]` | 9 |
| NewPassword | `string?` | `[Required] [StringLength(1024, MinimumLength = 1)]` | 13 |

### RoadGuardSystem.DTOs.Authentication.ForcedPasswordChangeRequestDto

[RoadGuardSystem.DTOs/Authentication/ForcedPasswordChangeRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Authentication/ForcedPasswordChangeRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Username | `string?` | `[Required] [StringLength(100, MinimumLength = 1)]` | 7 |
| CurrentPassword | `string?` | `[Required] [StringLength(1024, MinimumLength = 1)]` | 11 |
| NewPassword | `string?` | `[Required] [StringLength(1024, MinimumLength = 1)]` | 15 |
| ConfirmPassword | `string?` | `[Required] [Compare(nameof(NewPassword))]` | 19 |
| OperationId | `Guid` | `` | 23 |

### RoadGuardSystem.DTOs.Authentication.LoginRequestDto

[RoadGuardSystem.DTOs/Authentication/LoginRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/LoginRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Email | `string?` | `[Required] [StringLength(100, MinimumLength = 1)] [EmailAddress]` | 9 |
| Password | `string?` | `[Required] [StringLength(1024, MinimumLength = 1)]` | 14 |

### RoadGuardSystem.DTOs.Authentication.PasswordRecoveryRequestDto

[RoadGuardSystem.DTOs/Authentication/PasswordRecoveryRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/PasswordRecoveryRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Email | `string?` | `[Required] [EmailAddress] [StringLength(254, MinimumLength = 3)]` | 9 |

### RoadGuardSystem.DTOs.Authentication.RefreshRequestDto

[RoadGuardSystem.DTOs/Authentication/RefreshRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/RefreshRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| RefreshToken | `string?` | `[Required] [StringLength(2048, MinimumLength = 16)]` | 9 |

### RoadGuardSystem.DTOs.Authentication.RegisterReporterRequestDto

[RoadGuardSystem.DTOs/Authentication/RegisterReporterRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/RegisterReporterRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Email | `string?` | `[Required, EmailAddress]` | 9 |
| Password | `string?` | `[Required, MinLength(1)]` | 12 |
| DisplayName | `string?` | `[Required, MinLength(1)]` | 15 |
| ReporterType | `string?` | `[Required, MinLength(1)]` | 18 |

### RoadGuardSystem.DTOs.Authentication.RegistrationIntentResponseDto

[RoadGuardSystem.DTOs/Authentication/RegistrationIntentResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Authentication/RegistrationIntentResponseDto.cs); `RecordDeclaration`; `(     Guid IntentId,     string Status,     int ResendAfterSeconds)`; ``

### RoadGuardSystem.DTOs.Authentication.ResendReporterOtpRequestDto

[RoadGuardSystem.DTOs/Authentication/ResendReporterOtpRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Authentication/ResendReporterOtpRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| IntentId | `Guid` | `` | 8 |

### RoadGuardSystem.DTOs.Authentication.VerifyReporterOtpRequestDto

[RoadGuardSystem.DTOs/Authentication/VerifyReporterOtpRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Authentication/VerifyReporterOtpRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| IntentId | `Guid` | `` | 9 |
| Otp | `string?` | `[Required, MinLength(1)]` | 11 |

### RoadGuardSystem.DTOs.BoundaryFactMappings

[RoadGuardSystem.DTOs/BoundaryFactMappings.cs:4](../../../RoadGuardSystem.DTOs/BoundaryFactMappings.cs); `ClassDeclaration`; ``; ``

### RoadGuardSystem.DTOs.Cases.CaseTriageDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:5](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(Guid ProjectId, string? VerificationMethod, string? Reason,     Guid? RouteVersionId = null, Guid? SegmentSetId = null, string? GeometryVersion = null)`; ``

### RoadGuardSystem.DTOs.Cases.CaseLinkDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:8](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(IReadOnlyList<Guid>? ReportIds, IReadOnlyDictionary<Guid, string>? SourceCaseVersions, string? Reason)`; ``

### RoadGuardSystem.DTOs.Cases.CaseSplitDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:10](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(IReadOnlyList<Guid>? ReportIds, string? Reason)`; ``

### RoadGuardSystem.DTOs.Cases.CaseConclusionDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:12](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(string? Outcome, IReadOnlyList<Guid>? DefectIds, IReadOnlyList<Guid>? EvidenceIds, string? Reason)`; ``

### RoadGuardSystem.DTOs.Cases.CasePublicationDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:14](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(IReadOnlyList<Guid>? ReportIds, IReadOnlyList<Guid>? DefectIds, IReadOnlyList<Guid>? EvidenceIds, string? Summary)`; ``

### RoadGuardSystem.DTOs.Cases.InternalCaseDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:16](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid? ProjectId, string Status, string Version,     IReadOnlyList<Guid> ReportIds, IReadOnlyList<Guid> DefectIds, string? VerificationMethod, string WarrantyRouting,     CaseConclusionViewDto? Conclusion, DateTimeOffset CreatedAt)`; ``

### RoadGuardSystem.DTOs.Cases.CaseConclusionViewDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:27](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, string Outcome, string Reason, DateTimeOffset ConcludedAt,     IReadOnlyList<Guid> DefectIds, IReadOnlyList<Guid> EvidenceIds)`; ``

### RoadGuardSystem.DTOs.Cases.CasePublicationResponseDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:37](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid CaseId, string Status, string Version)`; ``

### RoadGuardSystem.DTOs.Cases.CasePageDto

[RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs:46](../../../RoadGuardSystem.DTOs/Cases/CaseWorkflowDtos.cs); `RecordDeclaration`; `(IReadOnlyList<InternalCaseDto> Items, string? NextCursor)`; ``

### RoadGuardSystem.DTOs.Commons.BatchIdsRequest

[RoadGuardSystem.DTOs/Commons/BatchIdsRequest.cs:5](../../../RoadGuardSystem.DTOs/Commons/BatchIdsRequest.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Ids | `List<Guid>` | `[Required(ErrorMessage = "Danh sach ID khong duoc rong")] [MinLength(1, ErrorMessage = "Phai co toi thieu 1 ID")]` | 7 |

### RoadGuardSystem.DTOs.Commons.BatchOperationErrorDTO

[RoadGuardSystem.DTOs/Commons/BatchOperationErrorDTO.cs:3](../../../RoadGuardSystem.DTOs/Commons/BatchOperationErrorDTO.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Id | `string` | `` | 5 |
| ErrorMessage | `string` | `` | 7 |
| ErrorCode | `string` | `` | 9 |
| OccurredAt | `DateTime` | `` | 11 |

### RoadGuardSystem.DTOs.Commons.BatchOperationResultDTO

[RoadGuardSystem.DTOs/Commons/BatchOperationResultDTO.cs:3](../../../RoadGuardSystem.DTOs/Commons/BatchOperationResultDTO.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| TotalRequested | `int` | `` | 5 |
| SuccessCount | `int` | `` | 7 |
| FailureCount | `int` | `` | 9 |
| SuccessIds | `List<string>` | `` | 11 |
| Errors | `List<BatchOperationErrorDTO>` | `` | 13 |
| Message | `string` | `` | 15 |

### RoadGuardSystem.DTOs.Commons.ErrorResponse

[RoadGuardSystem.DTOs/Commons/ErrorResponse.cs:3](../../../RoadGuardSystem.DTOs/Commons/ErrorResponse.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| IsSuccess | `bool` | `` | 5 |
| Message | `string` | `` | 7 |
| Details | `string?` | `` | 9 |
| ErrorType | `string?` | `` | 11 |
| ErrorCode | `string?` | `` | 13 |
| Timestamp | `DateTime` | `` | 15 |
| AdditionalData | `Dictionary<string, object>?` | `` | 17 |

### RoadGuardSystem.DTOs.Commons.PagedResponse

[RoadGuardSystem.DTOs/Commons/PagedResponse.cs:3](../../../RoadGuardSystem.DTOs/Commons/PagedResponse.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Data | `List<T>` | `` | 5 |
| CurrentPage | `int` | `` | 7 |
| TotalPages | `int` | `` | 9 |
| PageSize | `int` | `` | 11 |
| TotalCount | `int` | `` | 13 |
| HasNextPage | `bool` | `` | 15 |
| HasPreviousPage | `bool` | `` | 17 |
| Message | `string` | `` | 19 |
| IsSuccess | `bool` | `` | 21 |
| Errors | `List<string>` | `` | 23 |

### RoadGuardSystem.DTOs.Commons.PaginationDto

[RoadGuardSystem.DTOs/Commons/PaginationDto.cs:5](../../../RoadGuardSystem.DTOs/Commons/PaginationDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| PageNumber | `int` | `[Range(1, int.MaxValue)]` | 7 |
| PageSize | `int` | `[Range(1, 100)]` | 10 |

### RoadGuardSystem.DTOs.Defects.CandidateClassificationDto

[RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:5](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs); `RecordDeclaration`; `(string? DefectTypeCode, string? CauseCategoryCode, string? Severity,     Guid RoadSectionVersionId, Guid? SegmentId = null)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateDecisionRequestDto

[RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:8](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs); `RecordDeclaration`; `(string? SourceKind, Guid SourceId, string? SourceVersion,     string? GeometryVersion, string? Decision, Guid? TargetDefectId, string? TargetVersion,     CandidateClassificationDto? Classification, string? Reason, Guid? SupersedesDecisionId = null, string? PreviousDecisionVersion = null,     CandidateRecurrenceDto? Recurrence = null)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateRecurrenceDto

[RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:13](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs); `RecordDeclaration`; `(Guid PreviousDefectId, Guid PriorRepairDecisionId, Guid[] EvidenceFileIds, string? ExpectedLifecycleVersion = null)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateDecisionResponseDto

[RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs:14](../../../RoadGuardSystem.DTOs/Defects/CandidateDecisionDtos.cs); `RecordDeclaration`; `(Guid Id, string SourceKind, Guid SourceId, string Decision,     Guid? DefectId, string Version, Guid? SupersedesDecisionId)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateMatchSourceDto

[RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:3](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs); `RecordDeclaration`; `(string Kind, Guid Id, string Version)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateMatchHistoryDto

[RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:4](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs); `RecordDeclaration`; `(int SourceLinkCount)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateMatchItemDto

[RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:5](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs); `RecordDeclaration`; `(Guid DefectId, string Version, Guid? SegmentId,     int PriorityGroup, double? DistanceMeters, double? AccuracyMeters,     IReadOnlyList<string> ReasonCodes, IReadOnlyList<Guid> EvidenceRefs,     CandidateMatchHistoryDto HistorySummary)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateMatchPageDto

[RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:9](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs); `RecordDeclaration`; `(CandidateMatchSourceDto Source, string GeometryVersion,     string AlgorithmVersion, IReadOnlyList<CandidateMatchItemDto> Items, string? NextCursor)`; ``

### RoadGuardSystem.DTOs.Defects.CandidateMatchResult

[RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs:11](../../../RoadGuardSystem.DTOs/Defects/CandidateMatchDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, CandidateMatchPageDto? Page = null)`; ``

### RoadGuardSystem.DTOs.Defects.DefectAssessmentRequestDto

[RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:5](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs); `RecordDeclaration`; `(string? DefectTypeCode, string? CauseCategoryCode,     string? Severity, string? Reason, Guid[]? EvidenceIds)`; ``

### RoadGuardSystem.DTOs.Defects.DefectVerificationRequestDto

[RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:9](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs); `RecordDeclaration`; `(string? Decision, string? VerificationMethod,     Guid[]? EvidenceIds, string? Reason, Guid? FieldTaskId = null, Guid? FieldSubmissionId = null, string? FieldContentHash = null)`; ``

### RoadGuardSystem.DTOs.Defects.DefectGeometryDto

[RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:13](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs); `RecordDeclaration`; `(int Srid, string Wkt)`; ``

### RoadGuardSystem.DTOs.Defects.DefectViewDto

[RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:22](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid? RoadSectionVersionId,     Guid? SegmentId, string DefectTypeCode, string? CauseCategoryCode, string Severity,     string Status, DefectGeometryDto? Geometry, string Version)`; ``

### RoadGuardSystem.DTOs.Defects.DefectPageDto

[RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:33](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs); `RecordDeclaration`; `(IReadOnlyList<DefectViewDto> Items, string? NextCursor)`; ``

### RoadGuardSystem.DTOs.Defects.DefectWorkflowResult

[RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs:42](../../../RoadGuardSystem.DTOs/Defects/DefectWorkflowDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, DefectViewDto? Defect = null,     DefectPageDto? Page = null, IReadOnlyDictionary<string, string[]>? Errors = null)`; ``

### RoadGuardSystem.DTOs.Exports.CreateExportRequestDto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:6](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(string Kind, string Format, Guid[]? SegmentIds = null, Guid[]? DefectIds = null, DateTimeOffset? From = null, DateTimeOffset? To = null, bool IncludeOriginalFiles = false)`; ``

### RoadGuardSystem.DTOs.Exports.ExportJobViewDto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:16](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Kind, string Format, string Status, Guid SnapshotId, string SnapshotHash, Guid? ArtifactId, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? ExpiresAt, string? ErrorCode, string Completeness, string[] MissingSections, string Version)`; ``

### RoadGuardSystem.DTOs.Exports.ExportSourceRevisionDto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:17](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(string Kind, Guid Id, string Version)`; ``

### RoadGuardSystem.DTOs.Exports.ExportFileDto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:26](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string? ArchivePath, bool Included, string? ReasonCode)`; ``

### RoadGuardSystem.DTOs.Exports.ExportSectionDto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:35](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(string Name, string Availability, string[] ReasonCodes)`; ``

### RoadGuardSystem.DTOs.Exports.ExportLabelDto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:44](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(Guid LabelId, int Revision, Guid RevisionId, Guid ProjectId, string TypeCode, decimal X, decimal Y, decimal Width, decimal Height, Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string SourceKind, Guid SourceId, string SourceVersion, Guid ApprovalId, Guid ApprovedBy, DateTimeOffset ApprovedAt, Guid? ProcessingJobId, Guid? ModelVersionId, Guid? DatasetVersionId, string Mode, Guid? SegmentId)`; ``

### RoadGuardSystem.DTOs.Exports.ExportManifestV1Dto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:53](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(string SchemaVersion, Guid SnapshotId, Guid ProjectId, string Kind, string Format, Guid RequestedBy, DateTimeOffset CreatedAt, DateTimeOffset SnapshotAt, string[] DefinitionVersions, CreateExportRequestDto Filters, ExportSourceRevisionDto[] SourceRevisions, ExportFileDto[] Files, ExportSectionDto[] Sections, ExportLabelDto[]? Labels, string SnapshotHash)`; ``

### RoadGuardSystem.DTOs.Exports.ExportSnapshotPayloadDto

[RoadGuardSystem.DTOs/Exports/ExportDtos.cs:62](../../../RoadGuardSystem.DTOs/Exports/ExportDtos.cs); `RecordDeclaration`; `(ExportManifestV1Dto Manifest, ReportingCaptureDto? Dossier)`; ``

### RoadGuardSystem.DTOs.Files.CompletedUploadPartDto

[RoadGuardSystem.DTOs/Files/CompletedUploadPartDto.cs:5](../../../RoadGuardSystem.DTOs/Files/CompletedUploadPartDto.cs); `RecordDeclaration`; `(     [Range(1, int.MaxValue)] int PartNumber,     [Required, MaxLength(512)] string ETag)`; ``

### RoadGuardSystem.DTOs.Files.FileMetadataResponseDto

[RoadGuardSystem.DTOs/Files/FileMetadataResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/FileMetadataResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     string Status,     string ChecksumSha256,     string MediaType,     long SizeBytes,     string Version)`; ``

### RoadGuardSystem.DTOs.Files.OfflineUploadCreateRequestDto

[RoadGuardSystem.DTOs/Files/OfflineUploadCreateRequestDto.cs:4](../../../RoadGuardSystem.DTOs/Files/OfflineUploadCreateRequestDto.cs); `RecordDeclaration`; `(Guid AdmissionId, Guid CaptureOriginId, UploadCreateRequestDto Upload)`; ``

### RoadGuardSystem.DTOs.Files.ReporterEvidenceCreateRequestDto

[RoadGuardSystem.DTOs/Files/ReporterEvidenceCreateRequestDto.cs:4](../../../RoadGuardSystem.DTOs/Files/ReporterEvidenceCreateRequestDto.cs); `RecordDeclaration`; `(string FileName, string MediaType, long SizeBytes, string ChecksumSha256)`; ``

### RoadGuardSystem.DTOs.Files.UploadCompleteRequestDto

[RoadGuardSystem.DTOs/Files/UploadCompleteRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Files/UploadCompleteRequestDto.cs); `RecordDeclaration`; `(     [Required, MinLength(1)] IReadOnlyList<CompletedUploadPartDto> Parts,     [Required, RegularExpression("^[a-f0-9]{64}$")] string ChecksumSha256)`; ``

### RoadGuardSystem.DTOs.Files.UploadCreateRequestDto

[RoadGuardSystem.DTOs/Files/UploadCreateRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Files/UploadCreateRequestDto.cs); `RecordDeclaration`; `(     [Required, MaxLength(40)] string Purpose,     Guid? ProjectId,     Guid? TargetId,     [Required, MaxLength(255)] string FileName,     [Required, MaxLength(120)] string MediaType,     long SizeBytes,     [Required, RegularExpression("^[a-f0-9]{64}$")] string ChecksumSha256)`; ``

### RoadGuardSystem.DTOs.Files.UploadPartUrlResponseDto

[RoadGuardSystem.DTOs/Files/UploadPartUrlResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/UploadPartUrlResponseDto.cs); `RecordDeclaration`; `(int PartNumber, string Url, DateTimeOffset ExpiresAt)`; ``

### RoadGuardSystem.DTOs.Files.UploadPartUrlsRequestDto

[RoadGuardSystem.DTOs/Files/UploadPartUrlsRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Files/UploadPartUrlsRequestDto.cs); `RecordDeclaration`; `(     [Required, MinLength(1)] IReadOnlyList<int> PartNumbers)`; ``

### RoadGuardSystem.DTOs.Files.UploadPartUrlsResponseDto

[RoadGuardSystem.DTOs/Files/UploadPartUrlsResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/UploadPartUrlsResponseDto.cs); `RecordDeclaration`; `(IReadOnlyList<UploadPartUrlResponseDto> Parts)`; ``

### RoadGuardSystem.DTOs.Files.UploadSessionResponseDto

[RoadGuardSystem.DTOs/Files/UploadSessionResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Files/UploadSessionResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     Guid FileId,     string Status,     int PartSizeBytes,     DateTimeOffset ExpiresAt,     string Version)`; ``

### RoadGuardSystem.DTOs.Identity.AccountUpdateRequestDto

[RoadGuardSystem.DTOs/Identity/AccountUpdateRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Identity/AccountUpdateRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Status | `string?` | `[Required, MinLength(1)]` | 9 |
| Role | `string?` | `[Required, MinLength(1)]` | 12 |
| Reason | `string?` | `[Required, MinLength(1)]` | 15 |

### RoadGuardSystem.DTOs.Identity.ActorResponseDto

[RoadGuardSystem.DTOs/Identity/ActorResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Identity/ActorResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     string DisplayName,     string Role,     string Version)`; ``

### RoadGuardSystem.DTOs.Identity.AdminPasswordResetRequestDto

[RoadGuardSystem.DTOs/Identity/AdminPasswordResetRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Identity/AdminPasswordResetRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| ExpectedTargetRowVersion | `string?` | `[Required]` | 7 |
| OperationId | `Guid` | `` | 10 |

### RoadGuardSystem.DTOs.Identity.AdminPasswordResetResponseDto

[RoadGuardSystem.DTOs/Identity/AdminPasswordResetResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Identity/AdminPasswordResetResponseDto.cs); `RecordDeclaration`; `(     Guid UserId,     bool MustChangePassword,     string? TemporaryPassword)`; ``

### RoadGuardSystem.DTOs.Identity.AdminResetPasswordV2RequestDto

[RoadGuardSystem.DTOs/Identity/AdminResetPasswordV2RequestDto.cs:6](../../../RoadGuardSystem.DTOs/Identity/AdminResetPasswordV2RequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| TemporaryPassword | `string?` | `[Required, MinLength(1)]` | 9 |
| Reason | `string?` | `[Required, MinLength(1)]` | 12 |

### RoadGuardSystem.DTOs.Identity.ProfileResponseDto

[RoadGuardSystem.DTOs/Identity/ProfileResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Identity/ProfileResponseDto.cs); `RecordDeclaration`; `(     Guid UserId,     string Username,     string DisplayName,     string? Email,     string RoleCode,     string Status,     string RowVersion)`; ``

### RoadGuardSystem.DTOs.Identity.ProfileUpdateRequestDto

[RoadGuardSystem.DTOs/Identity/ProfileUpdateRequestDto.cs:7](../../../RoadGuardSystem.DTOs/Identity/ProfileUpdateRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| DisplayName | `string?` | `[Required] [StringLength(200, MinimumLength = 1)]` | 9 |
| Email | `string?` | `[StringLength(254)]` | 13 |
| ExpectedRowVersion | `string?` | `[Required]` | 16 |
| OperationId | `Guid` | `` | 19 |
| ExtraFields | `Dictionary<string, JsonElement>?` | `[JsonExtensionData]` | 21 |

### RoadGuardSystem.DTOs.Identity.UpdateMeRequestDto

[RoadGuardSystem.DTOs/Identity/UpdateMeRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Identity/UpdateMeRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| DisplayName | `string?` | `[Required, StringLength(200, MinimumLength = 1)]` | 9 |

### RoadGuardSystem.DTOs.Inspections.FieldTaskCreateInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:5](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid DefectId, string DefectVersion, Guid? SurveyId, string SourceKind,     Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,     byte RequiredMeasurementType, string MeasurementScope, string? Instructions, Guid AssignedToUserId,     DateTimeOffset DueAt, Guid? MapPublicationId = null, Guid? CrsProfileRevisionId = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldTaskActionInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:18](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(string Reason, Guid? AssignedToUserId = null, FieldHandoverInput? Handover = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldHandoverInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:28](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(string PerformedPortionState, string Summary, Guid StartOriginId, Guid[] SubmissionIds, Guid? RecipientUserId)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldLocationImpactActionInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:38](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid ImpactId, string Action, string Reason, Guid? AssignedToUserId = null, FieldHandoverInput? Handover = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldStartInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:48](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, DateTimeOffset ClaimedAt, Guid? DeviceId = null,     long? MonotonicMilliseconds = null, string? BootId = null, string? OfflineProof = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldMeasurementInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:59](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(string SampleId, string Type, decimal? Value, string State,     string? UnknownReason, string Dimension, string Unit, double? Longitude, double? Latitude,     string? LocationReason, string Instrument, string Method, string? Notes = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldEvidenceDeclaration

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:71](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid CaptureOriginId, Guid? FileId, string Purpose,     string ChecksumSha256, string MediaType, DateTimeOffset? CapturedAt, string? AttemptChecklist)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldPositionProofInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:82](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(string Kind, string? Checklist, double? Longitude, double? Latitude, string? ObservedSlabId = null, Guid? ObservedSegmentId = null, Guid? ObservedRouteVersionId = null, double? ObservedChainageMeters = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldSubmissionInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:92](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, Guid StartOriginId, Guid? ParentSubmissionId,     FieldMeasurementInput[]? Measurements, FieldEvidenceDeclaration[]? Evidence, FieldPositionProofInput? LocationProof,     string CaptureType, bool? Repaired, string? UnrepairedReason, Guid? DeviceId = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldReviewInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:104](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid SubmissionId, string Decision, string Reason)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldEvidenceReuseInput

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:114](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid FileId, Guid SourceEvidenceId, string SourceKind, string ExpectedChecksum, string Reason)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldTaskView

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:124](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid DefectId, Guid? SurveyId, string SourceKind,     Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,     string Mode, string Status, Guid? AssignmentId, Guid? AssignedToUserId, string Version,     Guid? FirstStartOriginId, Guid? LatestSubmissionId, Guid? MapPublicationId, Guid? CrsProfileRevisionId,     byte RequiredMeasurementType = 0, string? MeasurementScope = null, string? Instructions = null, DateTimeOffset? DueAt = null)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldSubmissionView

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:137](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid TaskId, Guid RootId, Guid? ParentId, int Revision,     DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalReviewDueAt, string Readiness,     string[] MissingReasons, string ContentHash, FieldMeasurementInput[] Measurements,     FieldEvidenceDeclaration[] Evidence, string CaptureType, bool? Repaired, string? UnrepairedReason)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldReviewView

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:149](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid TaskId, Guid SubmissionId, string Decision, string ReceiptActivation)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldWorkflowResult

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:158](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, object? Value = null, string? Version = null, bool Replayed = false)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldVerificationSourceQuery

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:167](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid DefectId, Guid SubmissionId, string ExpectedContentHash)`; ``

### RoadGuardSystem.DTOs.Inspections.FieldVerificationSourceFacts

[RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs:176](../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs); `RecordDeclaration`; `(Guid TaskId, Guid DefectId, Guid SubmissionId, string ContentHash,     string Decision, Guid[] EvidenceIds, Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId)`; ``

### RoadGuardSystem.DTOs.Inspections.InspectionTaskPageResponseDto

[RoadGuardSystem.DTOs/Inspections/InspectionTaskPageResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Inspections/InspectionTaskPageResponseDto.cs); `RecordDeclaration`; `(     IReadOnlyList<InspectionTaskResponseDto> Items,     string? NextCursor,     DateTimeOffset AsOf)`; ``

### RoadGuardSystem.DTOs.Inspections.InspectionTaskResponseDto

[RoadGuardSystem.DTOs/Inspections/InspectionTaskResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Inspections/InspectionTaskResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     Guid ProjectId,     IReadOnlyList<Guid> DefectIds,     string Mode,     Guid CrewId,     Guid? PolicyVersionId,     string Status,     string Version)`; ``

### RoadGuardSystem.DTOs.Invitations.AcceptInvitationRequestDto

[RoadGuardSystem.DTOs/Invitations/AcceptInvitationRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Invitations/AcceptInvitationRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| InvitationToken | `string?` | `[Required, MinLength(1)]` | 9 |
| DisplayName | `string?` | `[Required, MinLength(1)]` | 12 |
| Password | `string?` | `[Required, MinLength(1)]` | 15 |

### RoadGuardSystem.DTOs.Invitations.CreateInvitationRequestDto

[RoadGuardSystem.DTOs/Invitations/CreateInvitationRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Invitations/CreateInvitationRequestDto.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Email | `string?` | `[Required, EmailAddress]` | 9 |
| Role | `string?` | `[Required, MinLength(1)]` | 12 |
| ProjectIds | `IReadOnlyList<Guid>?` | `[Required]` | 15 |

### RoadGuardSystem.DTOs.Invitations.InvitationResponseDto

[RoadGuardSystem.DTOs/Invitations/InvitationResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Invitations/InvitationResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     string Status,     DateTimeOffset ExpiresAt,     string Version)`; ``

### RoadGuardSystem.DTOs.Labels.LabelAnnotationDto

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:5](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(string? Kind, string? CoordinateSpace, decimal X, decimal Y,     decimal Width, decimal Height)`; ``

### RoadGuardSystem.DTOs.Labels.CreateTrainingLabelDto

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:17](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(string? SourceKind, Guid SourceId, string? SourceVersion,     Guid FileId, LabelAnnotationDto? Annotation, string? DefectTypeCode, string? Reason)`; ``

### RoadGuardSystem.DTOs.Labels.ReviseTrainingLabelDto

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:21](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(Guid FileId, LabelAnnotationDto? Annotation,     string? DefectTypeCode, string? Reason)`; ``

### RoadGuardSystem.DTOs.Labels.ReviewTrainingLabelDto

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:25](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(string? Decision, string? Reason)`; ``

### RoadGuardSystem.DTOs.Labels.TrainingLabelViewDto

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:28](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, int Revision, string Status,     string SourceKind, Guid SourceId, string SourceVersion, Guid FileId, string FileVersion,     LabelAnnotationDto Annotation, string DefectTypeCode, Guid? ReviewedBy, DateTimeOffset? ReviewedAt,     string? ReviewReason, string Version)`; ``

### RoadGuardSystem.DTOs.Labels.TrainingLabelResult

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:41](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, TrainingLabelViewDto? Label = null,     IReadOnlyDictionary<string, string[]>? Errors = null)`; ``

### RoadGuardSystem.DTOs.Labels.TrainingLabelPageDto

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:43](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(IReadOnlyList<TrainingLabelViewDto> Items, string? NextCursor)`; ``

### RoadGuardSystem.DTOs.Labels.TrainingLabelPageResult

[RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs:52](../../../RoadGuardSystem.DTOs/Labels/TrainingLabelDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, TrainingLabelPageDto? Page = null)`; ``

### RoadGuardSystem.DTOs.Messaging.BusinessRequestAckInput

[RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs:3](../../../RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs); `RecordDeclaration`; `(DateTimeOffset? ClaimedDeviceAt)`; ``

### RoadGuardSystem.DTOs.Messaging.BusinessDutyAppointmentInput

[RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs:4](../../../RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs); `RecordDeclaration`; `(Guid AssigneeId, string Reason)`; ``

### RoadGuardSystem.DTOs.Messaging.ClockExtensionInput

[RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs:5](../../../RoadGuardSystem.DTOs/Messaging/BusinessDutyInputs.cs); `RecordDeclaration`; `(DateTimeOffset NewDueAt, string Reason)`; ``

### RoadGuardSystem.DTOs.Messaging.WeeklyDigestDutyView

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:7](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid ClockId, Guid TargetId, string Kind, Guid OriginEventId,     DateTimeOffset OriginAtUtc, DateTimeOffset DueAtRecoveryUtc)`; ``

### RoadGuardSystem.DTOs.Messaging.WeeklyDigestView

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:9](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid? RecipientId, DateTimeOffset ScheduledAtUtc,     DateTimeOffset RecoveredAtUtc, WeeklyDigestDutyView[] PendingAtRecovery, DateTimeOffset[] RecoveryPeriods)`; ``

### RoadGuardSystem.DTOs.Messaging.H6NotificationEventDto

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:12](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(int SchemaVersion, Guid EventId, string Kind, Guid ProjectId,     string SourceKind, Guid SourceId, Guid OriginEventId, DateTimeOffset OccurredAtUtc,     Guid? SourceRevisionId = null, Guid? ResponsibleUserId = null, DateTimeOffset? ScheduledAtUtc = null)`; ``

### RoadGuardSystem.DTOs.Messaging.H6NotificationScopeDto

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:16](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid NotificationId, Guid? ProjectId, Guid? OccurrenceId,     string Classification, string SourceKind, Guid SourceId, string EventType, string ResolverVersion)`; ``

### RoadGuardSystem.DTOs.Messaging.H6DispatchResult

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:18](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(string Status, Guid? MessageId = null, Guid? OccurrenceId = null,     int Delivered = 0, int Unresolved = 0, string? ReasonCode = null)`; ``

### RoadGuardSystem.DTOs.Messaging.H6NotificationPlan

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:20](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(H6NotificationEventDto Source, NotificationEventEnvelope? Envelope,     string MessageType, string Title, string Body, bool AuditOnly, H6RepairCorrectionAuditFacts? AuditFacts = null)`; ``

### RoadGuardSystem.DTOs.Messaging.H6NotificationSourceScope

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:22](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(string SourceKind, Guid SourceId, Guid ProjectId,     Guid? AssignedUserId = null)`; ``

### RoadGuardSystem.DTOs.Messaging.H6NotificationSourceResolution

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:28](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(string Status, string? ReasonCode = null,     Guid? TaskId = null, Guid? AssignmentId = null, Guid? ResponsibleUserId = null, bool ResponsibleIsSupervisor = false,     Guid? BindingId = null, UserRoleCode? ResponsibleRole = null)`; ``

### RoadGuardSystem.DTOs.Messaging.H6OutboxClaim

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:31](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid Id, string MessageType, DateTimeOffset OccurredAtUtc,     string PayloadJson, Guid Fence, DateTimeOffset LeaseExpiresAtUtc, int Attempt)`; ``

### RoadGuardSystem.DTOs.Messaging.H6ClockView

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:33](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Kind, Guid TargetId, Guid OriginEventId,     DateTimeOffset OriginAt, DateTimeOffset OriginalDueAt, DateTimeOffset CurrentDueAt, bool Overdue,     DateTimeOffset? CompletedAt, DateTimeOffset? AcknowledgedAt, string Version,     H6ClockExtensionView[] Extensions, H6ClockBreachView[] Breaches, string[] PendingCapabilities)`; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| AppointedActorId | `Guid?` | `` | 38 |
| AppointedRole | `string?` | `` | 39 |
| Appointments | `object[]` | `` | 40 |

### RoadGuardSystem.DTOs.Messaging.H6ClockExtensionView

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:42](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ActorId, DateTimeOffset At, DateTimeOffset PreviousDueAt,     DateTimeOffset NewDueAt, string Reason, bool PreviousDeadlineBreached)`; ``

### RoadGuardSystem.DTOs.Messaging.H6ClockBreachView

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:44](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid Id, DateTimeOffset DueAt, DateTimeOffset ObservedAt)`; ``

### RoadGuardSystem.DTOs.Messaging.H6ClockCursor

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:45](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(Guid ActorId, Guid ProjectId, string Role, DateTimeOffset AfterDueAtUtc, Guid AfterClockId)`; ``

### RoadGuardSystem.DTOs.Messaging.H6ClockPage

[RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs:47](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationDtos.cs); `RecordDeclaration`; `(string Status, H6ClockView[] Items, H6ClockCursor? Continuation = null)`; ``

### RoadGuardSystem.DTOs.Messaging.H6NotificationProtocolTypes

[RoadGuardSystem.DTOs/Messaging/H6NotificationProtocolTypes.cs:7](../../../RoadGuardSystem.DTOs/Messaging/H6NotificationProtocolTypes.cs); `ClassDeclaration`; ``; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| All | `IReadOnlyList<string>` | `` | 10 |

### RoadGuardSystem.DTOs.Messaging.H6RepairCorrectionAuditDto

[RoadGuardSystem.DTOs/Messaging/H6RepairCorrectionAuditDto.cs:6](../../../RoadGuardSystem.DTOs/Messaging/H6RepairCorrectionAuditDto.cs); `RecordDeclaration`; `(int SchemaVersion, Guid EventId, Guid OriginEventId,     Guid ProjectId, string Kind, string SourceKind, Guid SourceId, Guid SourceRevisionId,     DateTimeOffset OccurredAtUtc, Guid ObligationId, Guid SupersedesDecisionId, string Result)`; ``

### RoadGuardSystem.DTOs.Messaging.H6RepairCorrectionAuditFacts

[RoadGuardSystem.DTOs/Messaging/H6RepairCorrectionAuditDto.cs:10](../../../RoadGuardSystem.DTOs/Messaging/H6RepairCorrectionAuditDto.cs); `RecordDeclaration`; `(Guid ObligationId, Guid SupersedesDecisionId, string Result)`; ``

### RoadGuardSystem.DTOs.Messaging.NotificationDto

[RoadGuardSystem.DTOs/Messaging/NotificationDto.cs:3](../../../RoadGuardSystem.DTOs/Messaging/NotificationDto.cs); `RecordDeclaration`; `(     Guid Id,     string Message,     Guid? ResourceId,     bool Read,     DateTimeOffset OccurredAt,     string Version)`; ``

### RoadGuardSystem.DTOs.Messaging.NotificationPageDto

[RoadGuardSystem.DTOs/Messaging/NotificationPageDto.cs:3](../../../RoadGuardSystem.DTOs/Messaging/NotificationPageDto.cs); `RecordDeclaration`; `(     IReadOnlyList<NotificationDto> Items,     string? NextCursor,     DateTimeOffset AsOf)`; ``

### RoadGuardSystem.DTOs.Offline.H5PackageHeaderDto

[RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs:7](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs); `RecordDeclaration`; `(int Version, string PayloadSchema, Guid PackageId, Guid ProjectId,     Guid OriginalActorId, string SourceDeviceId, Guid[] OriginIds, string PayloadSha256)`; ``

### RoadGuardSystem.DTOs.Offline.H5RecipientKeyWrapDto

[RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs:11](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs); `RecordDeclaration`; `(Guid ActorId, string DeviceId, string RecipientKeyFingerprint,     string EphemeralPublicKey, string Nonce, string WrappedKey, string Tag)`; ``

### RoadGuardSystem.DTOs.Offline.H5EncryptedPackageDto

[RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs:15](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs); `RecordDeclaration`; `(H5PackageHeaderDto Header, string PayloadNonce, string Ciphertext,     string PayloadTag, H5RecipientKeyWrapDto[] Recipients, string SenderSigningPublicKey, string Signature)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineCaptureArtifactInput

[RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs:7](../../../RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs); `RecordDeclaration`; `(OfflineCaptureManifest Manifest, string ManifestSignature,     int ChunkIndex, H5EncryptedPackageDto Envelope)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineCaptureUploadInput

[RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs:10](../../../RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs); `RecordDeclaration`; `(Guid OfflineAdmissionId, Guid CaptureOriginId,     UploadCreateRequestDto Upload)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineResolvedCaptureView

[RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs:15](../../../RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs); `RecordDeclaration`; `(Guid CaptureOriginId, Guid FileId, string UploadState,     string Checksum, string Purpose, string MediaType, Guid ActualUploaderId)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineDeviceRegisterInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:7](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid DeviceId, string EncryptionPublicKey, string SigningPublicKey)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineDeviceRevokeInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:9](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(string Reason)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineSnapshotInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:11](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid DeviceRegistrationId, Guid TaskId)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineGrantItemScope

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:13](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, string Kind, string CorePayloadHash, string EnvelopeHash,     Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId = null)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineHandoverGrantInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:16](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid PackageId, Guid RecipientDeviceRegistrationId,     OfflineGrantItemScope[] Items, string Reason)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineGrantRevokeInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:19](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(string Reason)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineOperationDescriptor

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:21](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, Guid EffectId, string Kind, string EnvelopeHash,     string CorePayloadHash, Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineSourceManifestInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:24](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid SourceBatchId, Guid SourceDeviceRegistrationId,     OfflineOperationDescriptor[] Items, string Signature)`; ``

### RoadGuardSystem.DTOs.Offline.OfflinePackageExportInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:27](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(H5EncryptedPackageDto Package, OfflineSourceManifestInput Manifest,     Guid? SourceFileId = null)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineRepairPayload

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:32](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid ResourceId, string Action,     RepairExecutionStartInput? Start = null, RepairExecutionFinishInput? Finish = null,     RepairMeasurementAssessmentInput? Assessment = null)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineOperationInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:36](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(int SchemaVersion, Guid OriginId, Guid EffectId, string Kind,     Guid OriginalActorId, Guid SourceDeviceId, Guid SnapshotId, Guid TaskId, Guid AssignmentId,     string TaskVersion, string CorePayloadHash, Guid[] Dependencies, DateTimeOffset? ClaimedFinishedAt,     FieldStartInput? FieldStart = null, FieldSubmissionInput? FieldSubmission = null,     OfflineRepairPayload? Repair = null, FieldTaskActionInput? FieldAction = null)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineSignedBatchInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:42](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid BatchId, Guid SourceDeviceRegistrationId,     OfflineOperationInput[] Operations, string Signature)`; ``

### RoadGuardSystem.DTOs.Offline.OfflinePackageImportInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:45](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid BatchId, Guid PackageId, Guid GrantId,     Guid RecipientDeviceRegistrationId, OfflineOperationInput[] Operations, string SourceSignature,     string RecipientSignature)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineStartReconcileInput

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:49](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid BatchId, Guid OriginId)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineWorkflowResult

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:52](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, object? Value = null,     string? Version = null, bool Replayed = false)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineItemResult

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:54](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, string Kind, string State, string? Code,     Guid? EffectId, Guid? CanonicalOriginId, bool DurableAcknowledgment, DateTimeOffset ServerReceivedAt,     string TimeProvenance, string SyncLateness, DateTimeOffset? ClaimedFinishedAt,     DateTimeOffset? VerifiedFinishedAt, DateTimeOffset? VerifiedSyncDueAt, object? Outcome)`; ``

### RoadGuardSystem.DTOs.Offline.OfflineBatchResult

[RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs:58](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs); `RecordDeclaration`; `(Guid BatchId, OfflineItemResult[] Items,     string LocalQueueInstruction = "KEEP_UNACKNOWLEDGED_OR_CONFLICTED_ITEMS")`; ``

### RoadGuardSystem.DTOs.Processing.AiDetectionDto

[RoadGuardSystem.DTOs/Processing/AiDetectionDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/AiDetectionDto.cs); `RecordDeclaration`; `(     [param: Required] string DetectionId,     Guid FrameFileId,     int TimestampMs,     [param: Required, MinLength(1)] string TypeCode,     decimal Confidence,     [param: Required, MinLength(4), MaxLength(4)] IReadOnlyList<decimal> Bbox)`; ``

### RoadGuardSystem.DTOs.Processing.AiMockScopeRequest

[RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:5](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs); `RecordDeclaration`; `(Guid RouteVersionId, Guid SegmentSetId, Guid[] SegmentIds, string TargetBand)`; ``

### RoadGuardSystem.DTOs.Processing.CreateAiMockRunRequest

[RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:15](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs); `RecordDeclaration`; `(Guid DatasetId, AiMockScopeRequest Scope, Guid ModelVersionId,     string PreprocessingVersion, string ConfigVersion, string FixtureVersion, string Stage,     string ExpectedGeometryVersion, Guid? AnalysisRunId = null, Guid? CandidateSnapshotId = null)`; ``

### RoadGuardSystem.DTOs.Processing.AiMockRunView

[RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:27](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Stage, string Mode, string Status,     Guid ProcessingJobId, Guid AttemptId, string ManifestHash, Guid? ResultId, string FixtureVersion,     string? ErrorCode, string Version)`; ``

### RoadGuardSystem.DTOs.Processing.AiDetectionResultV1

[RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:38](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs); `RecordDeclaration`; `(Guid DetectionId, Guid SourceVideoFileId, Guid FrameFileId,     string FrameFileVersion, long TimestampMs, string TypeCode, decimal Confidence, decimal[] Bbox,     Guid? SegmentId, string PositionStatus)`; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| Geometry | `object?` | `` | 42 |

### RoadGuardSystem.DTOs.Processing.AiMatchResultV1

[RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:44](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs); `RecordDeclaration`; `(Guid DetectionId, Guid CandidateDefectId, string CandidateVersion,     int Rank, decimal? Score, string[] ReasonCodes)`; ``

### RoadGuardSystem.DTOs.Processing.AiResultV1

[RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs:46](../../../RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs); `RecordDeclaration`; `(string SchemaVersion, Guid RunId, string Stage, string Mode, Guid JobId,     Guid AttemptId, string ManifestHash, Guid ModelVersionId, string FixtureVersion, string ResultHash,     DateTimeOffset CompletedAt, IReadOnlyList<AiDetectionResultV1> Detections, IReadOnlyList<AiMatchResultV1> Matches)`; ``

### RoadGuardSystem.DTOs.Processing.CreateProcessingJobRequestDto

[RoadGuardSystem.DTOs/Processing/CreateProcessingJobRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Processing/CreateProcessingJobRequestDto.cs); `RecordDeclaration`; `(     Guid DatasetId,     BandScopeDto Scope,     Guid ModelVersionId,     [param: Required, MinLength(1)] string PreprocessingVersion,     [param: Required, MinLength(1)] string ConfigVersion,     [param: Required, RegularExpression("^(MOCK\|REAL)$")] string Mode)`; ``

### RoadGuardSystem.DTOs.Processing.CreateValidationRunRequestDto

[RoadGuardSystem.DTOs/Processing/CreateValidationRunRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/CreateValidationRunRequestDto.cs); `RecordDeclaration`; `(     [param: Required, MinLength(1)] IReadOnlyList<ValidationPairDto> Pairs,     Guid ModelVersionId,     Guid DatasetSplitId,     [param: Required, MinLength(1)] string MeasurementType,     [param: Required, MinLength(1)] string Unit)`; ``

### RoadGuardSystem.DTOs.Processing.ProcessingJobResponseDto

[RoadGuardSystem.DTOs/Processing/ProcessingJobResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Processing/ProcessingJobResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     string Status,     Guid? ResultId,     int AttemptNumber,     string Version,     string JobType,     string? Error)`; ``

### RoadGuardSystem.DTOs.Processing.ReceiveAiResultRequestDto

[RoadGuardSystem.DTOs/Processing/ReceiveAiResultRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/ReceiveAiResultRequestDto.cs); `RecordDeclaration`; `(     Guid JobAttemptId,     [param: Required, RegularExpression("^[a-f0-9]{64}$")] string ManifestHash,     Guid ModelVersionId,     [param: Required, RegularExpression("^(MOCK\|REAL)$")] string Mode,     Guid RawResultFileId,     [param: Required, RegularExpression("^[a-f0-9]{64}$")] string ChecksumSha256,     [param: Required] IReadOnlyList<AiDetectionDto> Detections)`; ``

### RoadGuardSystem.DTOs.Processing.RetryProcessingJobRequestDto

[RoadGuardSystem.DTOs/Processing/RetryProcessingJobRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Processing/RetryProcessingJobRequestDto.cs); `RecordDeclaration`; `([param: Required, MinLength(1), MaxLength(1000)] string Reason)`; ``

### RoadGuardSystem.DTOs.Processing.ValidationPairDto

[RoadGuardSystem.DTOs/Processing/ValidationPairDto.cs:3](../../../RoadGuardSystem.DTOs/Processing/ValidationPairDto.cs); `RecordDeclaration`; `(Guid GroundTruthId, Guid DerivedMeasurementId)`; ``

### RoadGuardSystem.DTOs.Processing.ValidationResultDto

[RoadGuardSystem.DTOs/Processing/ValidationResultDto.cs:3](../../../RoadGuardSystem.DTOs/Processing/ValidationResultDto.cs); `RecordDeclaration`; `(     Guid Id,     int UsedCount,     int ExcludedCount,     decimal? Bias,     decimal? Mae,     decimal? Rmse,     string Unit,     IReadOnlyList<string> ExclusionReasons)`; ``

### RoadGuardSystem.DTOs.Projects.CreateHandoverRequestDto

[RoadGuardSystem.DTOs/Projects/CreateHandoverRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateHandoverRequestDto.cs); `RecordDeclaration`; `(     [Required, MaxLength(80)] string DocumentNo,     [Required] DateOnly? HandoverDate,     Guid? FileId,     string? Notes)`; ``

### RoadGuardSystem.DTOs.Projects.CreateProjectRequestDto

[RoadGuardSystem.DTOs/Projects/CreateProjectRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateProjectRequestDto.cs); `RecordDeclaration`; `(     [MaxLength(50)] string? Code,     [Required, MaxLength(255)] string Name,     Guid PrimaryPmId,     DateOnly? HandoverDate,     DateOnly? WarrantyEndDate,     IReadOnlyList<Guid>? HandoverFileIds,     string? ProjectCode = null,     string? Description = null,     int? EngineeringUtmSrid = null,     DateOnly? StartDate = null,     DateOnly? EndDate = null,     Guid PrimaryProjectManagerUserId = default,     CreateHandoverRequestDto? Handover = null,     Guid OperationId = default)`; ``

### RoadGuardSystem.DTOs.Projects.CreateProjectResponseDto

[RoadGuardSystem.DTOs/Projects/CreateProjectResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/CreateProjectResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     string Code,     string Name,     Guid PrimaryPmId,     DateOnly HandoverDate,     DateOnly WarrantyEndDate,     string Status,     string Version,     Guid ProjectId,     string ProjectCode,     Guid PrimaryProjectManagerUserId,     Guid HandoverDocumentId,     string RowVersion)`; ``

### RoadGuardSystem.DTOs.Projects.CreateRoadSectionRequestDto

[RoadGuardSystem.DTOs/Projects/CreateRoadSectionRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateRoadSectionRequestDto.cs); `RecordDeclaration`; `(     [Required, StringLength(80)] string Code,     [StringLength(255)] string? Name,     [Range(32648, 32649)] int Srid,     [Required, MinLength(2)] IReadOnlyList<RoadSectionCoordinateDto> Coordinates,     DateTimeOffset EffectiveFrom,     [Required] string ChangeReason,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Projects.CreateRoadSectionVersionRequestDto

[RoadGuardSystem.DTOs/Projects/CreateRoadSectionVersionRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/CreateRoadSectionVersionRequestDto.cs); `RecordDeclaration`; `(     [Range(32648, 32649)] int Srid,     [Required, MinLength(2)] IReadOnlyList<RoadSectionCoordinateDto> Coordinates,     DateTimeOffset EffectiveFrom,     [Required] string ChangeReason,     Guid ExpectedCurrentVersionId,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Projects.CrsControlPoint

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(GeometryPoint Native, GeometryPoint Wgs84, string IndependentSource)`; ``

### RoadGuardSystem.DTOs.Projects.CrsEllipsoidDefinition

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:12](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(double SemiMajorAxisMeters, double InverseFlattening, string SourceReference)`; ``

### RoadGuardSystem.DTOs.Projects.CrsProjectionDefinition

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:21](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(IReadOnlyDictionary<string, double> Parameters, string SourceReference)`; ``

### RoadGuardSystem.DTOs.Projects.CrsTransformOperation

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:30](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(string Method, string Convention, string Direction, double[] Parameters,     string SourceReference, string? AreaOfUse = null, double? PublishedAccuracyMeters = null, string? AccuracySource = null)`; ``

### RoadGuardSystem.DTOs.Projects.CrsProfileInput

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:40](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(string Code, int SourceSrid, string Datum, string Projection,     string AxisOrder, double MetresPerUnit, string SourceReference, string SourceChecksum,     CrsTransformOperation? Operation = null, CrsControlPoint[]? IndependentControls = null,     double? SourcedToleranceMeters = null, string? ToleranceSource = null, bool SampleOnly = true,     CrsEllipsoidDefinition? Ellipsoid = null, CrsProjectionDefinition? ProjectionDefinition = null)`; ``

### RoadGuardSystem.DTOs.Projects.CrsProfileView

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:53](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Code, int Revision, string Status,     CrsProfileInput Definition, Guid CreatedBy, DateTimeOffset CreatedAt)`; ``

### RoadGuardSystem.DTOs.Projects.RouteSystemInput

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:63](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(string Code, string Name)`; ``

### RoadGuardSystem.DTOs.Projects.RouteSystemView

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:72](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Code, string Name)`; ``

### RoadGuardSystem.DTOs.Projects.ChainageControl

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:81](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(double GeometricOffsetMeters, double StationMeters)`; ``

### RoadGuardSystem.DTOs.Projects.ChainageCalibrationInput

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:90](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(string SourceReference, string SourceChecksum,     ChainageControl[] Controls, string SelectedReason)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryDraftReadiness

[RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs:100](../../../RoadGuardSystem.DTOs/Projects/CrsProfileDtos.cs); `RecordDeclaration`; `(bool Ready, string[] MissingFields, string[] Errors,     string CrsStatus, bool SampleOnly, string[] Warnings)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryMapLayer

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(string Key, string Status, string FeatureKind, int Count, string[] ReasonCodes,     int? SpatialSrid = null, Guid? CrsProfileRevisionId = null, bool SampleOnly = false)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryMapManifest

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:13](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(Guid PublicationId, Guid ProjectId, Guid RouteVersionId, Guid SegmentSetId,     Guid LayoutRevisionId, Guid? CrsProfileRevisionId, int NativeSrid, string ProfileStatus, bool SampleOnly,     double DisplayToleranceMeters, string ContentHash, GeometryMapLayer[] Layers,     Guid? RouteSystemId = null, Guid? RoadSectionId = null, double? CanonicalLengthMeters = null,     double? DeclaredLengthMeters = null, string? ChainageStatus = null)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryMapFeature

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:26](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(string Id, string Layer, GeometryShape Geometry, double[] Bbox, object Properties)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryMapSnapshot

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:35](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(GeometryMapManifest Manifest, GeometryMapFeature[] Features)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryMapPage

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:44](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(Guid PublicationId, string ContentHash, string Layer,     GeometryMapFeature[] Features, string? NextCursor)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryMapPublishInput

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:54](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(Guid LayoutRevisionId, string PublicationMode, string Reason)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryImpactInput

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:63](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(Guid PreviousRouteVersionId, Guid NewRouteVersionId, string Reason)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryImpactDecisionInput

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:72](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(Guid TaskId, string Action, string Reason)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryAffectedReference

[RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs:81](../../../RoadGuardSystem.DTOs/Projects/GeometryMapDtos.cs); `RecordDeclaration`; `(string Kind, Guid Id, Guid RouteVersionId, Guid? SegmentSetId,     string Readiness, string[] MissingReasonCodes)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryPoint

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(double X, double Y)`; ``

### RoadGuardSystem.DTOs.Projects.WidthProfileItem

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:12](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(double FromOffsetMeters, double ToOffsetMeters, double WidthMeters)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryDraftInput

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:21](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(string SourceKind, int SourceCrs, double StationOriginMeters,     string ChangeReason, GeometryPoint[]? Coordinates, Guid? SourceFileId, int? TrackIndex,     int? TrackSegmentIndex, WidthProfileItem[] WidthProfile, double SurveyWidthMeters,     string? BranchCode = null, string? RoadCode = null, string? RoadName = null,     NativeAlignmentInput? NativeAlignment = null, Guid? RouteSystemId = null, string? RouteKind = null,     Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,     ChainageCalibrationInput? ChainageCalibration = null, double? TessellationToleranceMeters = null,     CrsProfileInput? ResolvedCrsProfile = null)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryDraftView

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:37](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid? RoadSectionId, string RoadCode,     string? RoadName, string Status, string SourceKind, int SourceCrs, Guid? SourceFileId,     string? SourceChecksum, int? TrackIndex, int? TrackSegmentIndex, GeometryPoint[] OriginalCoordinates,     GeometryPoint[] EditedCoordinates, double StationOriginMeters, WidthProfileItem[] WidthProfile,     double SurveyWidthMeters, Guid CreatedBy, Guid UpdatedBy, string Version,     NativeAlignmentInput? NativeAlignment = null, GeometryDraftReadiness? Readiness = null)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryShape

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:51](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(string Type, object Coordinates)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryPreview

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:60](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(int SourceCrs, int EngineeringSrid, double LengthMeters,     GeometryShape MetricCenterline, GeometryShape? Wgs84Centerline, GeometryShape RoadSurface,     GeometryShape SurveyArea, string[] Warnings, Guid? CrsProfileRevisionId = null, bool SampleOnly = false)`; ``

### RoadGuardSystem.DTOs.Projects.RoadGeometryVersionView

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:71](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid ProjectId, Guid RoadSectionId, Guid RouteVersionId,     int VersionNo, bool IsCurrent, DateTimeOffset EffectiveFrom, Guid? SourceDraftId, int? SourceCrs,     int EngineeringSrid, double? StationOriginMeters, WidthProfileItem[]? WidthProfile,     double? SurveyWidthMeters, GeometryShape MetricCenterline, GeometryShape? Wgs84Centerline,     double LengthMeters, string MetadataStatus, Guid? ApprovedBy, DateTimeOffset? ApprovedAt, string GeometryHash,     Guid? CrsProfileRevisionId = null, Guid? RouteSystemId = null, string? RouteKind = null,     Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,     ChainageCalibrationInput? ChainageCalibration = null, string? CrsStatus = null, bool SampleOnly = false)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryConfirmInput

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:87](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid? ExpectedCurrentVersionId, DateTimeOffset EffectiveFrom, string Reason)`; ``

### RoadGuardSystem.DTOs.Projects.SegmentDefinition

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:96](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(double TargetLengthMeters = 100, string RemainderMode = "KEEP", double[]? BoundariesMeters = null,     NativeAlignmentInput? NativeAlignment = null, double? TessellationToleranceMeters = null,     ChainageCalibrationInput? ChainageCalibration = null)`; ``

### RoadGuardSystem.DTOs.Projects.SegmentGeometry

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:107](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid? Id, int Sequence, double FromOffsetMeters, double ToOffsetMeters,     double StartStationMeters, double EndStationMeters, double LengthMeters, GeometryShape MetricGeometry, GeometryShape? Wgs84Geometry)`; ``

### RoadGuardSystem.DTOs.Projects.SegmentPreview

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:117](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid RouteVersionId, double TotalLengthMeters, double[] BoundariesMeters,     SegmentGeometry[] Segments, string DefinitionHash)`; ``

### RoadGuardSystem.DTOs.Projects.SegmentGeometryView

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:127](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, int Sequence, double? FromOffsetMeters, double? ToOffsetMeters,     double? StartStationMeters, double? EndStationMeters, double? LengthMeters, GeometryShape? MetricGeometry,     GeometryShape? Wgs84Geometry, string[] MissingMetadata)`; ``

### RoadGuardSystem.DTOs.Projects.SegmentSetView

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:138](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid RouteVersionId, string Status, SegmentDefinition? Definition,     string? GeometryHash, SegmentGeometryView[] Segments, Guid? PublishedBy, DateTimeOffset? PublishedAt, string Version,     string MetadataStatus, string[] MissingMetadata)`; ``

### RoadGuardSystem.DTOs.Projects.SegmentPublishInput

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:149](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(Guid? ExpectedPublishedSetId, string Reason)`; ``

### RoadGuardSystem.DTOs.Projects.GeometryPackageView

[RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs:158](../../../RoadGuardSystem.DTOs/Projects/GeometryWorkflowDtos.cs); `RecordDeclaration`; `(string SchemaVersion, Guid ProjectId, RoadGeometryVersionView Route, SegmentSetView SegmentSet)`; ``

### RoadGuardSystem.DTOs.Projects.NativeAlignmentPrimitive

[RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs); `RecordDeclaration`; `(string Kind, GeometryPoint Start, GeometryPoint End,     GeometryPoint? Center = null, double? Radius = null, double? StartAngleRadians = null, double? SweepRadians = null)`; ``

### RoadGuardSystem.DTOs.Projects.NativeAlignmentInput

[RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs:13](../../../RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs); `RecordDeclaration`; `(Guid CrsProfileRevisionId, NativeAlignmentPrimitive[] Primitives, int SpatialSrid = 0)`; ``

### RoadGuardSystem.DTOs.Projects.NativeArcRange

[RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs:22](../../../RoadGuardSystem.DTOs/Projects/NativeAlignmentDtos.cs); `RecordDeclaration`; `(double FromOffsetMeters, double ToOffsetMeters, double Radius)`; ``

### RoadGuardSystem.DTOs.Projects.PavementWidthInterval

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(decimal FromOffsetMeters, decimal ToOffsetMeters, decimal WidthMeters)`; ``

### RoadGuardSystem.DTOs.Projects.PavementLayoutInput

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:12](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(decimal StripWidthMeters, decimal SlabLengthMeters, PavementWidthInterval[] WidthProfile)`; ``

### RoadGuardSystem.DTOs.Projects.PlannedSlabCell

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:21](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(int Sequence, int LongitudinalRow, int Strip, decimal FromOffsetMeters,     decimal ToOffsetMeters, decimal FromLateralMeters, decimal ToLateralMeters, bool IsTerminalResidual,     bool IsWidthTransitionFragment)`; ``

### RoadGuardSystem.DTOs.Projects.PavementLayoutPreview

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:32](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(decimal AnalyticLengthMeters, decimal StripWidthMeters,     decimal SlabLengthMeters, PlannedSlabCell[] Cells, string[] Warnings)`; ``

### RoadGuardSystem.DTOs.Projects.PavementPlanCreateInput

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:42](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(PavementLayoutInput Layout, double DisplayToleranceMeters)`; ``

### RoadGuardSystem.DTOs.Projects.SlabGeometrySnapshot

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:51](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(string Key, int? PlannedSequence, GeometryPoint[] Footprint,     double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,     Guid? SourceFileId = null, string ProvenanceStatus = "CLAIMED_CUSTOM")`; ``

### RoadGuardSystem.DTOs.Projects.AsBuiltSlabInput

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:62](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(string Key, int? PlannedSequence, GeometryPoint[] Footprint,     double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,     Guid? SourceFileId = null)`; ``

### RoadGuardSystem.DTOs.Projects.AsBuiltLayoutInput

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:73](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(Guid SourcePlanId, AsBuiltSlabInput[] Slabs, string Reason)`; ``

### RoadGuardSystem.DTOs.Projects.PavementGeometryPreview

[RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs:82](../../../RoadGuardSystem.DTOs/Projects/PavementLayoutDtos.cs); `RecordDeclaration`; `(PavementLayoutPreview? Plan, SlabGeometrySnapshot[] Slabs,     string[] Warnings, double DisplayToleranceMeters, double RenderedGapAreaSquareMeters = 0,     double RenderedOverlapAreaSquareMeters = 0)`; ``

### RoadGuardSystem.DTOs.Projects.ProjectLifecycleHistoryDto

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:3](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(Guid Id, string Kind, Guid ActorId, DateTimeOffset RecordedAtUtc,     Guid? ObligationId, Guid? GrantId, Guid? ReceiverId, Guid? OperationalClosureId, string Reason, string BasisReference,     string AuthoritySourceReference, string SourceDisposition, Guid? DefectId, Guid? LinkedDefectId, string? HandlingScope = null)`; ``

### RoadGuardSystem.DTOs.Projects.ProjectLifecycleViewDto

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:6](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(Guid ProjectId, string ConstructionCompletion, string OperationalClosure,     bool WarrantyRecorded, bool AcceptsNewReports, string ObligationInventory, string OperationalClosureEligibility,     Guid[] OutstandingMandatoryObligationIds, string[] MissingReasons, ProjectLifecycleHistoryDto[] History, string Version,     LD06ActionDto[]? Actions = null, ObligationResponsibilityDto[]? Responsibilities = null,     LD06TransferableObligationDto[]? TransferableObligations = null)`; ``

### RoadGuardSystem.DTOs.Projects.LD06TransferableObligationDto

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:11](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(Guid Id, Guid DefectId, string Kind, bool Mandatory, Guid ScopeId,     Guid PhysicalRoadId, string LocationVersion, string RouteLabel, decimal From, decimal To, decimal OffsetFrom,     decimal OffsetTo, string ScopeHash, string Version)`; ``

### RoadGuardSystem.DTOs.Projects.LD06ActionDto

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:14](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Kind, Guid ActorId, DateTimeOffset At, Guid? SourceActionId,     Guid? DefectId, Guid? LinkedDefectId, Guid? ObligationId, Guid? ReceivingProjectId, Guid? PriorRepairDecisionId, string ScopeHash, string FactsJson)`; ``

### RoadGuardSystem.DTOs.Projects.ObligationResponsibilityDto

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:16](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(Guid ObligationId, Guid OriginProjectId, Guid CurrentProjectId, Guid AcceptanceActionId)`; ``

### RoadGuardSystem.DTOs.Projects.RenewedHandlingScopeInput

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:17](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(Guid OperationalClosureId, string Reason, string HandlingScope, string Basis)`; ``

### RoadGuardSystem.DTOs.Projects.ProjectLifecycleMutationResult

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:19](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, ProjectLifecycleViewDto? Value = null)`; ``

### RoadGuardSystem.DTOs.Projects.LD06LifecycleInputDto

[RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs:20](../../../RoadGuardSystem.DTOs/Projects/ProjectLifecycleDtos.cs); `RecordDeclaration`; `(string Reason, Guid[] EvidenceFileIds, Guid? SourceActionId = null,     Guid? DefectId = null, Guid? LinkedDefectId = null, Guid? ObligationId = null, Guid? ReceivingProjectId = null,     Guid? PriorRepairDecisionId = null, string? ScopeHash = null,     RoadGuardSystem.DTOs.Defects.CandidateDecisionRequestDto? NewDefect = null)`; ``

### RoadGuardSystem.DTOs.Projects.ProjectListItemResponseDto

[RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs); `RecordDeclaration`; `(     Guid ProjectId,     string ProjectCode,     string Name,     string Status,     Guid? PrimaryProjectManagerUserId,     string? PrimaryProjectManagerName)`; ``

### RoadGuardSystem.DTOs.Projects.ProjectListResponseDto

[RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs:11](../../../RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs); `RecordDeclaration`; `(     IReadOnlyList<ProjectListItemResponseDto> Items,     string? NextCursor,     DateTimeOffset AsOf)`; ``

### RoadGuardSystem.DTOs.Projects.ProjectWorkPackageResponseDto

[RoadGuardSystem.DTOs/Projects/ProjectWorkPackageResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/ProjectWorkPackageResponseDto.cs); `RecordDeclaration`; `(     Guid ProjectId,     string ProjectCode,     string Name,     string? Description,     int? EngineeringUtmSrid,     string Status,     DateOnly? StartDate,     DateOnly? EndDate,     string AccessRole,     string RowVersion,     IReadOnlyList<RoadSectionWorkPackageDto> RoadSections,     IReadOnlyList<WarrantyWorkPackageDto> Warranties)`; ``

### RoadGuardSystem.DTOs.Projects.ReassignPrimaryProjectManagerRequestDto

[RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerRequestDto.cs); `RecordDeclaration`; `(     Guid PrimaryProjectManagerUserId,     [Required] DateOnly? EffectiveFrom,     [Required, MaxLength(1000)] string Reason,     [Required] string ExpectedCurrentMembershipRowVersion,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Projects.ReassignPrimaryProjectManagerResponseDto

[RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/ReassignPrimaryProjectManagerResponseDto.cs); `RecordDeclaration`; `(     Guid PreviousMembershipId,     Guid CurrentMembershipId,     string CurrentMembershipRowVersion)`; ``

### RoadGuardSystem.DTOs.Projects.RoadSectionCoordinateDto

[RoadGuardSystem.DTOs/Projects/RoadSectionCoordinateDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/RoadSectionCoordinateDto.cs); `RecordDeclaration`; `(double X, double Y)`; ``

### RoadGuardSystem.DTOs.Projects.RoadSectionVersionResponseDto

[RoadGuardSystem.DTOs/Projects/RoadSectionVersionResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/RoadSectionVersionResponseDto.cs); `RecordDeclaration`; `(     Guid ProjectId,     Guid RoadSectionId,     Guid RoadSectionVersionId,     string Code,     int VersionNo,     bool IsCurrent,     DateTimeOffset EffectiveFrom,     string ChangeReason)`; ``

### RoadGuardSystem.DTOs.Projects.RoadSectionWorkPackageDto

[RoadGuardSystem.DTOs/Projects/RoadSectionWorkPackageDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/RoadSectionWorkPackageDto.cs); `RecordDeclaration`; `(     Guid RoadSectionId,     string Code,     string? Name,     Guid CurrentVersionId,     int VersionNo,     string GeometryWkt,     int Srid,     DateTimeOffset EffectiveFrom,     string ChangeReason)`; ``

### RoadGuardSystem.DTOs.Projects.UpdateProjectRequestDto

[RoadGuardSystem.DTOs/Projects/UpdateProjectRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Projects/UpdateProjectRequestDto.cs); `RecordDeclaration`; `(     [Required, MaxLength(255)] string Name,     string? Description,     int? EngineeringUtmSrid,     DateOnly? StartDate,     DateOnly? EndDate,     [Required] string ExpectedRowVersion,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Projects.UpdateProjectResponseDto

[RoadGuardSystem.DTOs/Projects/UpdateProjectResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/UpdateProjectResponseDto.cs); `RecordDeclaration`; `(     Guid ProjectId,     string ProjectCode,     string Name,     string? Description,     int? EngineeringUtmSrid,     DateOnly? StartDate,     DateOnly? EndDate,     string Status,     string RowVersion)`; ``

### RoadGuardSystem.DTOs.Projects.WarrantyWorkPackageDto

[RoadGuardSystem.DTOs/Projects/WarrantyWorkPackageDto.cs:3](../../../RoadGuardSystem.DTOs/Projects/WarrantyWorkPackageDto.cs); `RecordDeclaration`; `(     Guid WarrantyId,     Guid? RoadSectionId,     Guid? HandoverDocumentId,     DateOnly HandoverDate,     DateOnly WarrantyStartDate,     DateOnly WarrantyEndDate,     decimal? RetainedValue,     string Scope,     string? Terms,     Guid? SourceDocumentId,     string Status)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairCoreHash

[RoadGuardSystem.DTOs/Repairs/RepairCoreHash.cs:8](../../../RoadGuardSystem.DTOs/Repairs/RepairCoreHash.cs); `ClassDeclaration`; ``; ``

### RoadGuardSystem.DTOs.Repairs.RepairCorrectionBasisInput

[RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:5](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs); `RecordDeclaration`; `(string Text, Guid[] EvidenceReferenceIds)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairCorrectionInput

[RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:8](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs); `RecordDeclaration`; `(Guid SupersedesDecisionId, string Result, string Reason, RepairCorrectionBasisInput Basis)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairReviewRequestInput

[RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:11](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs); `RecordDeclaration`; `(string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairReviewRequestView

[RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:14](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ItemId, Guid DecisionId, Guid ActorId,     string Role, string Reason, DateTimeOffset At, string Version)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairCorrectionView

[RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs:17](../../../RoadGuardSystem.DTOs/Repairs/RepairCorrectionDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ItemId, Guid ObligationId, Guid SupersedesDecisionId,     string Result, string Reason, string Basis, Guid[] EvidenceReferenceIds, Guid ActorId, DateTimeOffset At,     bool ObligationResolved, string ItemState, string Version)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairEligibilityReadView

[RoadGuardSystem.DTOs/Repairs/RepairEligibilityReadDtos.cs:4](../../../RoadGuardSystem.DTOs/Repairs/RepairEligibilityReadDtos.cs); `RecordDeclaration`; `(bool Eligible, string Activation, Guid ItemId, Guid BindingId,     Guid? PolicyRevisionId, string? PolicyContentHash, string PolicyState, RepairMeasurementRule[] Rules,     Guid? AssessmentId, Guid[] MeasurementIds, DateTimeOffset? OriginalVerifiedStart, DateTimeOffset? ExecutionExpiresAt,     string SourceMapping, RepairWarrantySource[] WarrantySources, RepairHandoverSource[] HandoverSources,     string[] MissingReasons, string Version, RoadCoverageMapping? CoverageMapping = null)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairEligibilityServiceResult

[RoadGuardSystem.DTOs/Repairs/RepairEligibilityReadDtos.cs:9](../../../RoadGuardSystem.DTOs/Repairs/RepairEligibilityReadDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, RepairEligibilityReadView? Value = null)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairFirstStartV1

[RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs:8](../../../RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs); `RecordDeclaration`; `(string SchemaVersion, Guid OriginEventId, string PayloadHash,     Guid ProjectId, Guid DefectId, Guid TaskId, Guid AssignmentId, Guid? RepairItemId, Guid? AuthorizationId,     Guid OriginalActorId, string SourceDeviceId, string TaskVersion, string AssignmentVersion,     string LocationVersion, Guid? PolicyRevisionId, DateTimeOffset ClaimedStartedAt,     DateTimeOffset ServerReceivedAt, RepairTimeProvenance TimeProvenance)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSubmissionFactsV1

[RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs:16](../../../RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs); `RecordDeclaration`; `(string SchemaVersion, Guid OriginEventId, string PayloadHash,     Guid SubmissionId, Guid RevisionId, Guid? ParentRevisionId, Guid OriginalIntakeId,     Guid ProjectId, Guid DefectId, Guid TaskId, Guid AssignmentId, Guid? RepairItemId, Guid? AuthorizationId,     Guid OriginalActorId, string SourceDeviceId, string TaskVersion, string AssignmentVersion,     string LocationVersion, Guid? PolicyRevisionId, string ChecklistVersion, bool Performed,     string? UnperformedReason, DateTimeOffset? ClaimedStartedAt, DateTimeOffset? ClaimedFinishedAt,     DateTimeOffset CapturedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalServerReceivedAt,     RepairTimeProvenance TimeProvenance, IReadOnlyList<RepairMeasurementFactV1> Measurements,     IReadOnlyList<RepairEvidenceFactsV1> Evidence, IReadOnlyList<string> MissingReasons)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairMeasurementFactV1

[RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs:26](../../../RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs); `RecordDeclaration`; `(Guid MeasurementId, string Code, decimal? Value, string Unit,     string ValueStatus, decimal? Longitude, decimal? Latitude, string LocationStatus, string? VerifiedLocationReference)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairEvidenceFactsV1

[RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs:29](../../../RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs); `RecordDeclaration`; `(Guid ReferenceId, Guid FileId, string? FileVersion, string? Sha256,     string Purpose, string Readiness, string SourceKind, Guid SourceId, string SourceVersion,     Guid OriginalActorId, DateTimeOffset? CapturedAt, Guid? ReuseDecisionId)`; ``

### RoadGuardSystem.DTOs.Repairs.EffectiveRepairFactsV1

[RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs:35](../../../RoadGuardSystem.DTOs/Repairs/RepairFieldSeams.cs); `RecordDeclaration`; `(Guid ProjectId, Guid DefectId, Guid ObligationId, Guid ItemId,     RepairMode Mode, bool Mandatory, bool ObligationResolved, Guid? OriginalPerformedAttemptId,     Guid? OriginalDecisionId, Guid? EffectiveDecisionId, Guid? SupersededDecisionId,     RepairPresentationState EffectiveResult, bool EffectivelyAccepted, string HeadVersion, IReadOnlyList<Guid> DecisionHistoryIds,     IReadOnlyList<Guid> LocationReferenceIds, IReadOnlyList<string> MissingReasons)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairHistoryView

[RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:3](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs); `RecordDeclaration`; `(RepairItemView Item, RepairDecisionHistoryView[] Decisions,     RepairAttemptHistoryView[] Attempts, RepairReviewRequestHistoryView[] ReviewRequests)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairDecisionHistoryView

[RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:6](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ItemId, Guid ObligationId, Guid DefectId,     string Mode, Guid ActorId, string Role, string Reason, DateTimeOffset At, Guid? SupersedesDecisionId,     string Result, RepairCorrectionBasisHistoryView? Basis)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairCorrectionBasisHistoryView

[RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:10](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs); `RecordDeclaration`; `(string Text, RepairEvidenceHistoryView[] Evidence)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairEvidenceHistoryView

[RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:12](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs); `RecordDeclaration`; `(Guid FileId, string? FileVersion, string? Sha256, string Purpose,     bool Verified, string SourceKind, Guid SourceId, DateTimeOffset? CapturedAt, Guid? ReuseDecisionId, bool ReusedSource)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairAttemptHistoryView

[RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:15](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs); `RecordDeclaration`; `(Guid Id, Guid OriginId, string ContentHash, Guid ItemId,     Guid ObligationId, Guid ProjectId, Guid DefectId, Guid CrewId, Guid TaskId, Guid AssignmentId,     Guid? AuthorizationId, string LocationVersion, Guid? PolicyRevisionId, bool Performed, string? UnperformedReason,     DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, DateTimeOffset ServerReceivedAt,     string TimeProvenance, RepairEvidenceHistoryView[] Evidence)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairReviewRequestHistoryView

[RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs:21](../../../RoadGuardSystem.DTOs/Repairs/RepairHistoryDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ItemId, Guid DecisionId, Guid ActorId,     string Role, string Reason, DateTimeOffset At)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairLifecycleHandoverInput

[RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:5](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs); `RecordDeclaration`; `(Guid FirstStartId, Guid RecipientUserId,     string PerformedPortion, string SafetyState)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairCancellationInput

[RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:8](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs); `RecordDeclaration`; `(string Reason, RepairLifecycleHandoverInput? Handover)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairNormalContinuationInput

[RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:10](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs); `RecordDeclaration`; `(string RepairPlan, string ChecklistVersion, string Reason,     RepairLifecycleHandoverInput? Handover)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairLifecycleView

[RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs:13](../../../RoadGuardSystem.DTOs/Repairs/RepairLifecycleDtos.cs); `RecordDeclaration`; `(Guid SourceItemId, Guid? SuccessorItemId, Guid ObligationId,     Guid? CancellationEventId, Guid? HandoverEventId, Guid? ContinuationId, string SourceState,     string? SuccessorState, string SourceVersion, string? SuccessorVersion)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPolicyDefinitionInput

[RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs:6](../../../RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs); `RecordDeclaration`; `(string DefectTypeCode, string ChecklistVersion,     RepairMeasurementRule[] Measurements, string[] StopConditions, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPolicyReasonInput

[RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs:9](../../../RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs); `RecordDeclaration`; `(string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPolicyView

[RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs:11](../../../RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid? CurrentChangeId, Guid? PublishedRevisionId,     int? Revision, string DefectTypeCode, string ChecklistVersion, RepairMeasurementRule[] Measurements,     string[] StopConditions, string State, Guid ActorId, DateTimeOffset At, string Reason, string Version)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPolicyServiceResult

[RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs:14](../../../RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, RepairPolicyView? Value = null, bool Replayed = false)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyInstallInput

[RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:5](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs); `RecordDeclaration`; `(DateTimeOffset FirstCheckDueAt, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyAcknowledgementInput

[RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:7](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs); `RecordDeclaration`; `(string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyView

[RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:9](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid DefectId, Guid FormalObligationId,     Guid SafetyObligationId, Guid ResponsibleActorId, DateTimeOffset? InstalledAt, DateTimeOffset? FirstCheckDueAt,     Guid? CurrentCheckId, RepairSafetyCheckView[] Checks, RepairSafetyWarningView[] Warnings,     RepairSafetyAcknowledgementView[] Acknowledgements, string Version)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyCheckView

[RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:13](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ActorId, DateTimeOffset At, string Result, string Findings,     Guid[] EvidenceLinkIds)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyWarningView

[RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:15](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs); `RecordDeclaration`; `(Guid Id, Guid SourceCheckId, Guid IntendedActorId,     DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalDueAt, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyAcknowledgementView

[RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs:17](../../../RoadGuardSystem.DTOs/Repairs/RepairSafetyDtos.cs); `RecordDeclaration`; `(Guid Id, Guid WarningId, Guid ActorId, DateTimeOffset At,     bool AfterOriginalDue)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairActualScopeInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:7](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid PhysicalRoadId, Guid RouteVersionId, Guid? SegmentSetId,     Guid? LayoutRevisionId, string? SlabId, decimal FromMeters, decimal ToMeters, decimal OffsetFromMeters, decimal OffsetToMeters)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairObligationInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:10](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(string Kind, bool Mandatory, RepairActualScopeInput Scope, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPackageCreateInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:12](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid DefectId, string DefectVersion, RepairObligationInput[] Obligations, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairItemProposeInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:14](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid ObligationId, string Mode, string RepairPlan, string ChecklistVersion, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairDecisionInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:16](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairItemAssignInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:18](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(FieldTaskCreateInput Task, Guid? PolicyRevisionId, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairMeasurementAssessmentInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:20](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, Guid FieldFirstStartId,     FieldMeasurementInput[]? Measurements, FieldEvidenceDeclaration[]? Evidence,     FieldPositionProofInput? LocationProof, Guid? DeviceId = null,     [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, bool?>? StopConditions = null)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairExecutionStartInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:25](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, Guid FieldFirstStartId, DateTimeOffset ClaimedAt, Guid AssessmentId,     Guid? DeviceId = null, long? MonotonicMilliseconds = null, string? BootId = null)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairExecutionFinishInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:28](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid OriginId, Guid ExecutionStartId, DateTimeOffset ClaimedAt,     Guid? DeviceId = null, long? MonotonicMilliseconds = null, string? BootId = null)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairAttemptSubmitInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:31](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid FieldSubmissionId, string ExpectedContentHash, Guid? ExecutionFinishId)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairAttemptSupplementInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:33](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid FieldSubmissionId, string ExpectedContentHash)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairAttemptReviewInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:35](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid FieldSubmissionId, string Decision, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairItemCancelInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:37](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(string Reason, FieldHandoverInput? Handover)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPolicyRuleInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:39](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(string Code, string Unit, decimal Minimum, decimal Maximum)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPolicyDraftInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:41](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(string DefectTypeCode, string ChecklistVersion,     RepairPolicyRuleInput[] Measurements, string[] StopConditions, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyCreateInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:44](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid FormalRepairObligationId, Guid SafetyObligationId, Guid ResponsibleActorId,     string CheckSchedule, string ReplacementCondition, string RemovalCondition, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyCheckInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:47](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(string Result, string Findings, Guid[] EvidenceIds)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairSafetyResponsibilityInput

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:49](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid NextResponsibleActorId, string PerformedPortion,     string Handover, string Reason)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairTaskBindingView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:53](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid ItemId, Guid ObligationId, Guid TaskId, Guid AssignmentId,     string Mode, Guid? AuthorizationId, Guid? PolicyRevisionId, Guid RouteVersionId, Guid? SegmentSetId,     Guid? LayoutRevisionId, Guid? MapPublicationId, Guid? CrsProfileRevisionId, string? SlabId, Guid? FirstStartId)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairEligibilityView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:56](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(bool Eligible, string Reason, string SourceMapping,     string[] UnknownSources, DateTimeOffset? OriginalVerifiedStart, DateTimeOffset? ExecutionExpiresAt)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairAssessmentView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:58](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ItemId, Guid TaskId, Guid FirstStartId, Guid SessionId,     string Stage, string Readiness, string[] MissingReasons, string LocationState, string ContentHash)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairExecutionStartView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:60](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ItemId, Guid TaskId, Guid AssessmentId, Guid FirstStartId,     DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset? VerifiedOriginalAt, string TimeProvenance)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairExecutionFinishView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:62](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ItemId, Guid TaskId, Guid ExecutionStartId,     DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset? VerifiedOriginalAt, string TimeProvenance,     Guid? SyncClockId, DateTimeOffset? OriginalSyncDueAt)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairAttemptIntakeView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:65](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid AttemptId, Guid LinkId, Guid ItemId, Guid SubmissionId,     Guid ReviewClockId, DateTimeOffset OriginalReviewDueAt, string IntakeReadiness)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairAttemptReviewView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:67](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid ReviewId, Guid ItemId, Guid SubmissionId, string Decision,     string ReceiptActivation)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairItemView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:69](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid DefectId, Guid ObligationId, string Mode,     string State, string Result, Guid? EffectiveDecisionId, Guid[] AttemptIds, Guid[] DecisionIds, string Version)`; ``

### RoadGuardSystem.DTOs.Repairs.RepairPackageView

[RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs:71](../../../RoadGuardSystem.DTOs/Repairs/RepairWorkflowDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, Guid DefectId, bool AllMandatoryResolved,     Guid[] UnresolvedMandatoryObligationIds, RepairItemView[] Items, string Version)`; ``

### RoadGuardSystem.DTOs.Repairs.RoadCoverageInputDto

[RoadGuardSystem.DTOs/Repairs/RoadCoverageDtos.cs:6](../../../RoadGuardSystem.DTOs/Repairs/RoadCoverageDtos.cs); `RecordDeclaration`; `(Guid ScopeObligationId, string ExpectedScopeHash,     Guid HandoverDocumentId, string HandoverVersion, string SourceKind, Guid SourceId, string SourceVersion,     DateTimeOffset ApplicableFromUtc, DateTimeOffset ApplicableToUtc, string Provenance, string Reason,     Guid? SupersedesId = null)`; ``

### RoadGuardSystem.DTOs.Repairs.RoadCoverageServiceResult

[RoadGuardSystem.DTOs/Repairs/RoadCoverageDtos.cs:15](../../../RoadGuardSystem.DTOs/Repairs/RoadCoverageDtos.cs); `RecordDeclaration`; `(int Status, string? Code = null, RoadCoverageReadFact? Value = null)`; ``

### RoadGuardSystem.DTOs.Reporting.CaseReportRefV1

[RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs:3](../../../RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs); `RecordDeclaration`; `(Guid ReportId, string Version, DateTimeOffset ReceivedAt)`; ``

### RoadGuardSystem.DTOs.Reporting.CaseEvidenceRefV1

[RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs:12](../../../RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs); `RecordDeclaration`; `(Guid CaseId, Guid SourceReportId, Guid EvidenceId,     Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType)`; ``

### RoadGuardSystem.DTOs.Reporting.CasePublicationRefV1

[RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs:22](../../../RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs); `RecordDeclaration`; `(Guid PublicationId, string Version, Guid[] RecipientReportIds, Guid[] EvidenceIds)`; ``

### RoadGuardSystem.DTOs.Reporting.CaseReadFactV1

[RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs:31](../../../RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs); `RecordDeclaration`; `(Guid CaseId, Guid ProjectId, string Version, string Status,     CaseReportRefV1[] CurrentReports, ReportingSourceRefDto[] Conclusions, CasePublicationRefV1[] Publications)`; ``

### RoadGuardSystem.DTOs.Reporting.DefectReadFactV1

[RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs:41](../../../RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs); `RecordDeclaration`; `(Guid DefectId, Guid ProjectId, string Version, string Status,     string SourceKind, Guid SourceId, string SourceVersion, Guid? RouteVersionId, Guid? SegmentSetId,     Guid? SegmentId, string? GeometryVersion, string PositionStatus)`; ``

### RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1

[RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs:52](../../../RoadGuardSystem.DTOs/Reporting/CaseDefectReadDtos.cs); `RecordDeclaration`; `(string SchemaVersion, Guid SnapshotId, Guid ProjectId,     DateTimeOffset CapturedAt, string Hash, CaseReadFactV1[] Cases, DefectReadFactV1[] Defects,     CaseEvidenceRefV1[] AuthorizedEvidence, string[] MissingReasons)`; ``

### RoadGuardSystem.DTOs.Reporting.StatisticsQuantityInputDto

[RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs:5](../../../RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs); `RecordDeclaration`; `(Guid MeasurementId, string SourceVersion, Guid? SegmentId)`; ``

### RoadGuardSystem.DTOs.Reporting.DefectStatisticsInputDto

[RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs:7](../../../RoadGuardSystem.DTOs/Reporting/DefectStatisticsInputDto.cs); `RecordDeclaration`; `(Guid ObligationId, string ScopeHash, string DefectVersion, Guid SharedPartId,     Guid[] SegmentIds, StatisticsQuantityInputDto[] Quantities, string Provenance, string Reason, Guid? SupersedesId = null)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:3](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(DateTimeOffset? From = null, DateTimeOffset? To = null,     Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid[]? SegmentIds = null)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingSourceRefDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:13](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(string Type, Guid Id, string Version)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingMetricDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:22](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(string Code, ReportingDimensionsDto Dimensions, decimal? Value, string Unit,     long? Numerator, long? Denominator, bool PeriodApplicable, string Availability, string[] ReasonCodes, ReportingSourceRefDto[] SourceRefs)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingDimensionsDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:32](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(string? Status = null, string? Band = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingCaptureDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:41](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(ProjectSummaryV1 Summary, ReportingItemDto[] Items, ReportingFileDto[] Files,     ReportingTimelineItemDto[] Timeline, ReportingAvailabilityDto TimelineAvailability,     CaseDefectSnapshotV1? CaseDefectFacts = null,     [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] global::RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSnapshot? DefectStatistics = null)`; ``

### RoadGuardSystem.DTOs.Reporting.ProjectSummaryV1

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:53](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(string SchemaVersion, Guid ProjectId, DateTimeOffset ReadAt, string DefinitionVersion,     ReportingFiltersDto Filters, ReportingMetricDto[] Metrics, string[] Warnings, string Isolation)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingItemDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:63](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(Guid Id, string Metric, string Type, string Version, string? Status = null,     Guid? ParentTaskId = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid? SegmentId = null, string? Band = null,     Guid? DatasetId = null, Guid? AssessmentId = null, long? SizeBytes = null, string? Unit = null, decimal? Bias = null,     decimal? Mae = null, decimal? Rmse = null, int? Used = null, int? Excluded = null, Guid? ModelVersionId = null, string? SplitId = null, string? MeasurementType = null)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingFileDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:75](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(Guid FileId, string Version, string Sha256, long SizeBytes, string MediaType)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingTimelineItemDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:84](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(Guid EventId, DateTimeOffset OccurredAt, string? ActorDisplay, string Action,     ReportingSourceRefDto Source, string Summary)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingAvailabilityDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:94](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(string Availability, string[] ReasonCodes)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingItemsPageDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:103](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(string SchemaVersion, string Metric, DateTimeOffset ReadAt, ReportingItemDto[] Items,     string? NextCursor, ReportingAvailabilityDto Availability)`; ``

### RoadGuardSystem.DTOs.Reporting.ReportingTimelinePageDto

[RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs:105](../../../RoadGuardSystem.DTOs/Reporting/ReportingDtos.cs); `RecordDeclaration`; `(string SchemaVersion, DateTimeOffset ReadAt, ReportingTimelineItemDto[] Items,     string? NextCursor, ReportingAvailabilityDto Availability)`; ``

### RoadGuardSystem.DTOs.Reports.OwnReportEvidenceDto

[RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:3](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs); `RecordDeclaration`; `(Guid Id, Guid FileId, string FileVersion, Guid? SupplementId,     DateTimeOffset? CapturedAt, string LocationSource, ReportEvidenceLocationDto? Location)`; ``

### RoadGuardSystem.DTOs.Reports.OwnReportSupplementDto

[RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:13](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs); `RecordDeclaration`; `(Guid Id, string Description, DateTimeOffset CreatedAt)`; ``

### RoadGuardSystem.DTOs.Reports.OwnReportUpdateDto

[RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:22](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs); `RecordDeclaration`; `(Guid Id, string Summary, DateTimeOffset PublishedAt, IReadOnlyList<Guid> EvidenceIds)`; ``

### RoadGuardSystem.DTOs.Reports.OwnReportDto

[RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:31](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs); `RecordDeclaration`; `(Guid Id, string Description, DateTimeOffset CreatedAt, string Version,     IReadOnlyList<OwnReportEvidenceDto> Evidence, string RoutingStatus,     IReadOnlyList<OwnReportUpdateDto> PublicUpdates, IReadOnlyList<OwnReportSupplementDto> Supplements)`; ``

### RoadGuardSystem.DTOs.Reports.OwnReportPageDto

[RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs:42](../../../RoadGuardSystem.DTOs/Reports/OwnReportDtos.cs); `RecordDeclaration`; `(IReadOnlyList<OwnReportDto> Items, string? NextCursor)`; ``

### RoadGuardSystem.DTOs.Reports.ReportEvidenceLocationDto

[RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:5](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs); `RecordDeclaration`; `(decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters)`; ``

### RoadGuardSystem.DTOs.Reports.ReportEvidenceInputDto

[RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:16](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs); `RecordDeclaration`; `(Guid FileId, string? FileVersion, string? LocationSource,     DateTimeOffset? CapturedAt = null, ReportEvidenceLocationDto? Location = null)`; ``

### RoadGuardSystem.DTOs.Reports.CreateReporterReportRequestDto

[RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:20](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs); `RecordDeclaration`; `(string? Description, IReadOnlyList<ReportEvidenceInputDto?>? Evidence)`; ``

### RoadGuardSystem.DTOs.Reports.ReporterReportResponseDto

[RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs:23](../../../RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs); `RecordDeclaration`; `(Guid Id, string Description, DateTimeOffset CreatedAt, string Version, IReadOnlyList<Guid> EvidenceIds)`; ``

### RoadGuardSystem.DTOs.Retention.ConfirmRetentionBasisRequest

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:4](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid[] WarrantyIds, string ExpectedReferenceInventoryVersion, string Reason)`; ``

### RoadGuardSystem.DTOs.Retention.CreateRetentionHoldRequest

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:14](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(string ScopeType, Guid ScopeId, string Reason)`; ``

### RoadGuardSystem.DTOs.Retention.ReleaseRetentionHoldRequest

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:24](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(string Reason)`; ``

### RoadGuardSystem.DTOs.Retention.CreateRetentionEvaluationRequest

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:34](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid[]? FileIds)`; ``

### RoadGuardSystem.DTOs.Retention.RetentionReferenceView

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:44](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(string Kind, Guid Id, Guid? ProjectId, string SourceVersion)`; ``

### RoadGuardSystem.DTOs.Retention.RetentionWarrantyView

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:53](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Scope, DateOnly WarrantyEndDate, string SourceVersion)`; ``

### RoadGuardSystem.DTOs.Retention.RetentionHoldView

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:62](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid Id, string ScopeType, Guid ScopeId, string State, Guid CreatedBy,     DateTimeOffset CreatedAt, string Reason, Guid? ReleasedBy, DateTimeOffset? ReleasedAt, string Version)`; ``

### RoadGuardSystem.DTOs.Retention.RetentionItemView

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:72](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid FileId, string Eligibility, IReadOnlyList<string> ReasonCodes,     DateTimeOffset? EligibleAfter, string BasisVersion, string InventoryVersion, string HoldVersion, bool IsCurrent)`; ``

### RoadGuardSystem.DTOs.Retention.RetentionFileView

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:82](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid FileId, Guid? BasisHeadId, string BasisVersion,     string InventoryVersion, bool InventoryComplete, string Classification,     IReadOnlyList<RetentionReferenceView> References, IReadOnlyList<RetentionWarrantyView> ApplicableObligations,     IReadOnlyList<string> InventoryReasonCodes, int ActiveHoldCount, RetentionItemView Evaluation)`; ``

### RoadGuardSystem.DTOs.Retention.RetentionBasisView

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:94](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid Id, Guid FileId, int Revision, string PolicyVersion,     string Classification, string ReferenceInventoryVersion, bool InventoryComplete,     IReadOnlyList<RetentionWarrantyView> WarrantyRefs, Guid ConfirmedBy, DateTimeOffset ConfirmedAt,     string Reason, Guid? SupersedesId, string Version)`; ``

### RoadGuardSystem.DTOs.Retention.RetentionEvaluationView

[RoadGuardSystem.DTOs/Retention/RetentionDtos.cs:106](../../../RoadGuardSystem.DTOs/Retention/RetentionDtos.cs); `RecordDeclaration`; `(Guid Id, Guid ProjectId, string Status, DateTimeOffset CreatedAt,     DateTimeOffset? EvaluatedAt, string PolicyVersion, IReadOnlyList<RetentionItemView> Items, string? NextCursor, string Version)`; ``

### RoadGuardSystem.DTOs.Staff.StaffPageResponseDto

[RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:3](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs); `RecordDeclaration`; `(IReadOnlyList<T> Items, string? NextCursor, DateTimeOffset AsOf)`; ``

### RoadGuardSystem.DTOs.Staff.StaffUserResponseDto

[RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:5](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs); `RecordDeclaration`; `(Guid UserId, string DisplayName, string? Email, string? PhoneNumber, string Role, string Status)`; ``

### RoadGuardSystem.DTOs.Staff.StaffAccountResponseDto

[RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:8](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs); `RecordDeclaration`; `(Guid Id, string DisplayName, string Role, string Version, string? Email, string? PhoneNumber)`; ``

### RoadGuardSystem.DTOs.Staff.ProjectMemberResponseDto

[RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:10](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs); `RecordDeclaration`; `(Guid MembershipId, Guid UserId, string DisplayName, string Role, bool IsPrimary,     string Status, DateOnly ValidFrom, DateOnly? ValidTo, string MembershipVersion)`; ``

### RoadGuardSystem.DTOs.Staff.PrimaryProjectManagerResponseDto

[RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:13](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs); `RecordDeclaration`; `(PrimaryProjectManagerReadDto? PrimaryProjectManager)`; ``

### RoadGuardSystem.DTOs.Staff.PrimaryProjectManagerReadDto

[RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs:15](../../../RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs); `RecordDeclaration`; `(Guid MembershipId, Guid UserId, string DisplayName,     DateOnly ValidFrom, DateOnly? ValidTo, string MembershipVersion)`; ``

### RoadGuardSystem.DTOs.Surveys.AssessmentEvidenceDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:3](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid FileId, long? FromMilliseconds = null, long? ToMilliseconds = null)`; ``

### RoadGuardSystem.DTOs.Surveys.AssessmentItemDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:4](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, string TargetBand,     string PositionStatus, string QualityStatus, string CoverageStatus, string Reason, IReadOnlyList<AssessmentEvidenceDto> Evidence)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateAssessmentDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:6](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(string MethodVersion, IReadOnlyList<AssessmentItemDto> Items)`; ``

### RoadGuardSystem.DTOs.Surveys.AssessmentView

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:7](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid Id, Guid DatasetId, string MethodVersion, Guid ReviewedBy, DateTimeOffset ReviewedAt,     IReadOnlyList<AssessmentItemDto> Items, string Version)`; ``

### RoadGuardSystem.DTOs.Surveys.BaselineItemDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:9](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, string TargetBand,     Guid DatasetId, Guid AssessmentId, Guid? ExpectedBaselineSelectionId)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateBaselineDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:11](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(IReadOnlyList<BaselineItemDto> Items, string Reason)`; ``

### RoadGuardSystem.DTOs.Surveys.BaselineSelectedItemDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:12](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid SelectionId, Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId,     string TargetBand, Guid DatasetId, Guid AssessmentId)`; ``

### RoadGuardSystem.DTOs.Surveys.BaselineView

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:14](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid Id, IReadOnlyList<BaselineSelectedItemDto> Items, Guid SelectedBy, DateTimeOffset SelectedAt, string Version)`; ``

### RoadGuardSystem.DTOs.Surveys.DatasetSourceFileDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:15](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid FileId, string ChecksumSha256, long SizeBytes, string MediaType, string Purpose)`; ``

### RoadGuardSystem.DTOs.Surveys.DatasetDetailView

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:16](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid Id, Guid SurveyTaskId, Guid ProjectId, Guid DataVersionId, Guid? SubmittedBy,     DateTimeOffset? SubmittedAt, DateTimeOffset? RecordedAt, Guid? DeviceId, IReadOnlyList<BandScopeDto> Scope,     string IntegrityStatus, string TelemetryStatus, IReadOnlyList<DatasetSourceFileDto> SourceFiles, IReadOnlyList<DatasetPairDto> Pairs, string Version)`; ``

### RoadGuardSystem.DTOs.Surveys.SurveyTaskWorkPackage

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:19](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(string SchemaVersion, SurveyTaskV2ResponseDto Task, DateTimeOffset? DueAt,     PositionDto? AccessPoint, IReadOnlyList<SurveyGeometryRefDto> GeometryRefs, string ScopeVersion)`; ``

### RoadGuardSystem.DTOs.Surveys.SurveyGeometryRefDto

[RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs:21](../../../RoadGuardSystem.DTOs/Surveys/AssessmentDtos.cs); `RecordDeclaration`; `(Guid RouteVersionId, Guid SegmentSetId, IReadOnlyList<Guid> SegmentIds)`; ``

### RoadGuardSystem.DTOs.Surveys.BandScopeDto

[RoadGuardSystem.DTOs/Surveys/BandScopeDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/BandScopeDto.cs); `RecordDeclaration`; `(     Guid RouteVersionId,     Guid SegmentSetId,     [MinLength(1)] IReadOnlyList<Guid> SegmentIds,     [Required, MinLength(1)] string TargetBand)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateSurveyPlanRequestDto

[RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanRequestDto.cs); `RecordDeclaration`; `(     DateTimeOffset PlannedStartAt,     DateTimeOffset PlannedEndAt,     Guid RoadSectionVersionId,     SurveyType SurveyType,     [Required] string OutputRequirements,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateSurveyPlanResponseDto

[RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanResponseDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanResponseDto.cs); `RecordDeclaration`; `(     Guid PlanId,     Guid ProjectId,     Guid RoadSectionId,     Guid? RoadSectionVersionId,     DateTimeOffset PlannedStartAt,     DateTimeOffset PlannedEndAt,     SurveyType SurveyType,     SurveyPlanStatus Status,     string OutputRequirements)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateSurveyPlanV2RequestDto

[RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanV2RequestDto.cs); `RecordDeclaration`; `(     [Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope,     DateTimeOffset PlannedAt,     [Required] string SurveyType)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateSurveyRequestRequestDto

[RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestRequestDto.cs:6](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestRequestDto.cs); `RecordDeclaration`; `(     Guid? SurveyPlanId,     Guid RoadSectionVersionId,     SurveyType SurveyType,     DateTimeOffset? DueAt,     [Required] string OutputRequirements,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateSurveyRequestResponseDto

[RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestResponseDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestResponseDto.cs); `RecordDeclaration`; `(     Guid RequestId,     Guid ProjectId,     Guid RoadSectionId,     Guid? RoadSectionVersionId,     Guid? SurveyPlanId,     Guid RequestedByUserId,     SurveyType SurveyType,     SurveyRequestStatus Status,     DateTimeOffset RequestedAt,     DateTimeOffset DueAt,     string OutputRequirements)`; ``

### RoadGuardSystem.DTOs.Surveys.CreateSurveyTaskV2RequestDto

[RoadGuardSystem.DTOs/Surveys/CreateSurveyTaskV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/CreateSurveyTaskV2RequestDto.cs); `RecordDeclaration`; `(     [Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope,     [Required] string SurveyType,     Guid OperatorId,     DateTimeOffset? DueAt,     PositionDto? AccessPoint,     Guid? PlanId = null)`; ``

### RoadGuardSystem.DTOs.Surveys.DatasetCoverageItemDto

[RoadGuardSystem.DTOs/Surveys/DatasetCoverageItemDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/DatasetCoverageItemDto.cs); `RecordDeclaration`; `(     BandScopeDto Scope,     string PositionCoverage,     string QualityCoverage,     string OverallCoverage,     IReadOnlyList<string> Reasons)`; ``

| Property | CLR type | Attributes | Source line |
|---|---|---|---|
| CoverageStatus | `string?` | `` | 10 |

### RoadGuardSystem.DTOs.Surveys.DatasetCoverageResponseDto

[RoadGuardSystem.DTOs/Surveys/DatasetCoverageResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/DatasetCoverageResponseDto.cs); `RecordDeclaration`; `(     Guid DatasetId,     IReadOnlyList<DatasetCoverageItemDto> Items,     string MethodVersion,     Guid? AssessmentId = null,     string? Version = null)`; ``

### RoadGuardSystem.DTOs.Surveys.DatasetResponseDto

[RoadGuardSystem.DTOs/Surveys/DatasetResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/DatasetResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     Guid SurveyTaskId,     Guid DataVersionId,     string IntegrityStatus,     string TelemetryStatus,     string Version)`; ``

### RoadGuardSystem.DTOs.Surveys.PointDto

[RoadGuardSystem.DTOs/Surveys/PointDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PointDto.cs); `RecordDeclaration`; `(     [Range(-180, 180)] double Longitude,     [Range(-90, 90)] double Latitude)`; ``

### RoadGuardSystem.DTOs.Surveys.PositionDto

[RoadGuardSystem.DTOs/Surveys/PositionDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PositionDto.cs); `RecordDeclaration`; `(     [Required] PointDto Point,     [Required] string Source,     double? AccuracyMeters = null,     DateTimeOffset? CapturedAt = null)`; ``

### RoadGuardSystem.DTOs.Surveys.PostponeSurveyPlanRequestDto

[RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanRequestDto.cs); `RecordDeclaration`; `(     DateTimeOffset? NewPlannedStartAt,     [Required, MinLength(1)] string Reason,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Surveys.PostponeSurveyPlanResponseDto

[RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanResponseDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanResponseDto.cs); `RecordDeclaration`; `(     Guid PlanId,     SurveyPlanStatus Status,     DateTimeOffset? NewPlannedStartAt,     Guid PostponementId)`; ``

### RoadGuardSystem.DTOs.Surveys.PostponeSurveyPlanV2RequestDto

[RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanV2RequestDto.cs); `RecordDeclaration`; `(     [Required, MinLength(1)] string Reason)`; ``

### RoadGuardSystem.DTOs.Surveys.ReassignSurveyTaskV2RequestDto

[RoadGuardSystem.DTOs/Surveys/ReassignSurveyTaskV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/ReassignSurveyTaskV2RequestDto.cs); `RecordDeclaration`; `(     Guid OperatorId,     [Required, MinLength(1)] string Reason,     DateTimeOffset? DueAt = null)`; ``

### RoadGuardSystem.DTOs.Surveys.SubmitDatasetRequestDto

[RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs); `RecordDeclaration`; `(     [param: Required, MinLength(1)] IReadOnlyList<Guid> VideoFileIds,     [param: Required] IReadOnlyList<Guid> TelemetryFileIds,     [param: Required] DateTimeOffset RecordedAt,     Guid? DeviceId,     [param: Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope,     IReadOnlyList<DatasetPairDto>? Pairs = null)`; ``

### RoadGuardSystem.DTOs.Surveys.DatasetPairDto

[RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs:13](../../../RoadGuardSystem.DTOs/Surveys/SubmitDatasetRequestDto.cs); `RecordDeclaration`; `(Guid VideoFileId, Guid? TelemetryFileId, long TimeOffsetMilliseconds)`; ``

### RoadGuardSystem.DTOs.Surveys.SupplementSurveyTaskV2RequestDto

[RoadGuardSystem.DTOs/Surveys/SupplementSurveyTaskV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/SupplementSurveyTaskV2RequestDto.cs); `RecordDeclaration`; `(     [Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope,     [Required, MinLength(1)] string Reason,     Guid OperatorId)`; ``

### RoadGuardSystem.DTOs.Surveys.SurveyPlanV2ResponseDto

[RoadGuardSystem.DTOs/Surveys/SurveyPlanV2ResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/SurveyPlanV2ResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     Guid ProjectId,     IReadOnlyList<BandScopeDto> Scope,     DateTimeOffset PlannedAt,     string Status,     string Version)`; ``

### RoadGuardSystem.DTOs.Surveys.SurveyTaskPageV2ResponseDto

[RoadGuardSystem.DTOs/Surveys/SurveyTaskPageV2ResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/SurveyTaskPageV2ResponseDto.cs); `RecordDeclaration`; `(     IReadOnlyList<SurveyTaskV2ResponseDto> Items,     string? NextCursor,     DateTimeOffset AsOf)`; ``

### RoadGuardSystem.DTOs.Surveys.SurveyTaskReasonV2RequestDto

[RoadGuardSystem.DTOs/Surveys/SurveyTaskReasonV2RequestDto.cs:5](../../../RoadGuardSystem.DTOs/Surveys/SurveyTaskReasonV2RequestDto.cs); `RecordDeclaration`; `(     [Required, MinLength(1)] string Reason)`; ``

### RoadGuardSystem.DTOs.Surveys.SurveyTaskV2ResponseDto

[RoadGuardSystem.DTOs/Surveys/SurveyTaskV2ResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Surveys/SurveyTaskV2ResponseDto.cs); `RecordDeclaration`; `(     Guid Id,     Guid ProjectId,     IReadOnlyList<BandScopeDto> Scope,     Guid OperatorId,     string Status,     string Version,     Guid? SupplementTaskId = null)`; ``

### RoadGuardSystem.DTOs.Warranties.CreateWarrantyRequestDto

[RoadGuardSystem.DTOs/Warranties/CreateWarrantyRequestDto.cs:3](../../../RoadGuardSystem.DTOs/Warranties/CreateWarrantyRequestDto.cs); `RecordDeclaration`; `(     Guid? RoadSectionId,     Guid? HandoverDocumentId,     DateOnly HandoverDate,     DateOnly WarrantyStartDate,     DateOnly WarrantyEndDate,     decimal? RetainedValue,     string Scope,     string? Terms,     Guid? SourceDocumentId,     string Status,     Guid OperationId)`; ``

### RoadGuardSystem.DTOs.Warranties.CreateWarrantyResponseDto

[RoadGuardSystem.DTOs/Warranties/CreateWarrantyResponseDto.cs:3](../../../RoadGuardSystem.DTOs/Warranties/CreateWarrantyResponseDto.cs); `RecordDeclaration`; `(     Guid WarrantyId,     Guid ProjectId,     Guid? RoadSectionId,     Guid? HandoverDocumentId,     DateOnly HandoverDate,     DateOnly WarrantyStartDate,     DateOnly WarrantyEndDate,     decimal? RetainedValue,     string Scope,     string? Terms,     Guid? SourceDocumentId,     string Status)`; ``
