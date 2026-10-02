# Current SQL data dictionary

CURRENT_VERIFIED in isolated SQL after 37 migrations ending 20260929125522_P2ValidationMeasurementProvenance. Source: [inventory](current-schema.inventory.json), RoadGuardDbContext, EF configurations/entities and migration chain. This is not a production-schema claim.

`dbo.__EFMigrationsHistory` is EF infrastructure and excluded. Column purpose defaults to UNKNOWN unless the linked entity/configuration and product source are inspected; a column name alone is not an accepted business definition. SQL rows below are catalog observations. Model columns carry CLR/property/converter metadata. Enum values show converter output or underlying numeric value; check constraints may reject some of them. `-` means absent or not applicable, not a guessed value.

## identity

### dbo.Users

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.ApplicationUser. Purpose (source-interpreted from entity/configuration, not accepted business policy): User identity, credential and account state. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ApplicationUserConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| DisplayName | DisplayName : String | nvarchar | False | 400 | 0/0 | - | Never | - |
| RoleCode | RoleCode : RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode | varchar | False | 40 | 0/0 | - | Never | System.String; Unknown=REJECTED_BY_CONVERTER, Supervisor=SUPERVISOR, ProjectManager=PM, DroneOperator=DRONE_OPERATOR, RepairCrew=REPAIR_CREW, Reporter=REPORTER |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.UserStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Active=1, Suspended=2, Pending=3 |
| MustChangePassword | MustChangePassword : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| LastLoginAt | LastLoginAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| SuspendedAt | SuspendedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| CreatedAt | CreatedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |
| UserName | UserName : String | nvarchar | False | 200 | 0/0 | - | Never | - |
| NormalizedUserName | NormalizedUserName : String | nvarchar | False | 200 | 0/0 | - | Never | - |
| Email | Email : String | nvarchar | True | 508 | 0/0 | - | Never | - |
| NormalizedEmail | NormalizedEmail : String | nvarchar | True | 508 | 0/0 | - | Never | - |
| EmailConfirmed | EmailConfirmed : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| PasswordHash | PasswordHash : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| SecurityStamp | SecurityStamp : String | nvarchar | True | 72 | 0/0 | - | Never | - |
| ConcurrencyStamp | ConcurrencyStamp : String | nvarchar | True | 72 | 0/0 | - | Never | - |
| PhoneNumber | PhoneNumber : String | nvarchar | True | 100 | 0/0 | - | Never | - |
| PhoneNumberConfirmed | PhoneNumberConfirmed : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| TwoFactorEnabled | TwoFactorEnabled : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| LockoutEnd | LockoutEnd : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| LockoutEnabled | LockoutEnabled : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| AccessFailedCount | AccessFailedCount : Int32 | int | False | 4 | 10/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Users | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Users_Roles_RoleCode | RoleCode -> dbo.Roles.Code | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Users_RoleCode | RoleCode | False | - | NONCLUSTERED |
| UX_Users_Email | Email | True | ([Email] IS NOT NULL) | NONCLUSTERED |
| UX_Users_NormalizedUserName | NormalizedUserName | True | - | NONCLUSTERED |
| UX_Users_UserName | UserName | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Users_Status | ([Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

### dbo.Roles

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.ApplicationRole. Purpose (source-interpreted from entity/configuration, not accepted business policy): Role codes and labels. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/ApplicationRole.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ApplicationRoleConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Code | Code : RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode | varchar | False | 40 | 0/0 | - | Never | System.String; Unknown=REJECTED_BY_CONVERTER, Supervisor=SUPERVISOR, ProjectManager=PM, DroneOperator=DRONE_OPERATOR, RepairCrew=REPAIR_CREW, Reporter=REPORTER |
| Name | Name : String | nvarchar | False | 200 | 0/0 | - | Never | - |
| NormalizedName | NormalizedName : String | nvarchar | True | 200 | 0/0 | - | Never | - |
| ConcurrencyStamp | ConcurrencyStamp : String | nvarchar | True | 72 | 0/0 | - | Never | - |
| IsActive | IsActive : Boolean | bit | False | 1 | 1/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Roles | PRIMARY_KEY_CONSTRAINT | Code |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.Sessions

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.UserSession. Purpose (source-interpreted from entity/configuration, not accepted business policy): Issued user sessions and device metadata. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/UserSession.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/UserSessionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| UserId | UserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| IssuedAt | IssuedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| DeviceMetadataJson | DeviceMetadataJson : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| ExpiresAt | ExpiresAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| RevokedAt | RevokedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Sessions | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Sessions_Users_UserId | UserId -> dbo.Users.Id | CASCADE | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Sessions_UserId | UserId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Sessions_DeviceMetadataJson_Json | ([DeviceMetadataJson] IS NULL OR isjson([DeviceMetadataJson])=(1)); disabled=False |
| CK_Sessions_ExpiresAt | ([ExpiresAt]>[IssuedAt]); disabled=False |
| CK_Sessions_RevokedAt | ([RevokedAt] IS NULL OR [RevokedAt]>=[IssuedAt]); disabled=False |

### dbo.RefreshTokens

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.RefreshToken. Purpose (source-interpreted from entity/configuration, not accepted business policy): Refresh-token family and rotation records. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/RefreshToken.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/RefreshTokenConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| SessionId | SessionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| TokenHash | TokenHash : String | varchar | False | 128 | 0/0 | - | Never | - |
| ExpiresAt | ExpiresAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| RevokedAt | RevokedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_RefreshTokens | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_RefreshTokens_Sessions_SessionId | SessionId -> dbo.Sessions.Id | CASCADE | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_RefreshTokens_SessionId | SessionId | False | - | NONCLUSTERED |
| UX_RefreshTokens_TokenHash | TokenHash | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_RefreshTokens_TokenHash_NotEmpty | (len(ltrim(rtrim([TokenHash])))>=(32)); disabled=False |

### dbo.PasswordResetLogs

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog. Purpose (source-interpreted from entity/configuration, not accepted business policy): Password reset audit events. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/PasswordResetLog.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/PasswordResetLogConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| TargetUserId | TargetUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| PerformedByUserId | PerformedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| OccurredAt | OccurredAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| Reason | Reason : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| Result | Result : RoadGuardSystem.aBusinessObjects.Commons.PasswordResetResult | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Success=1, Failed=2, Rejected=3 |
| Source | Source : String | varchar | False | 80 | 0/0 | - | Never | - |
| CorrelationId | CorrelationId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_PasswordResetLogs | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_PasswordResetLogs_Users_PerformedByUserId | PerformedByUserId -> dbo.Users.Id | NO_ACTION | False |
| FK_PasswordResetLogs_Users_TargetUserId | TargetUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_PasswordResetLogs_OccurredAt | OccurredAt | False | - | NONCLUSTERED |
| IX_PasswordResetLogs_PerformedByUserId | PerformedByUserId | False | - | NONCLUSTERED |
| IX_PasswordResetLogs_TargetUserId | TargetUserId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_PasswordResetLogs_Reason_SafeCode | ([Reason] IS NULL OR ([Reason]='ACCOUNT_REACTIVATED' OR [Reason]='SECURITY_INCIDENT' OR [Reason]='ADMINISTRATIVE_LOCK' OR [Reason]='NO_STATUS_CHANGE' OR [Reason]='SAFETY_POLICY_VIOLATION' OR [Reason]='REGISTRATION_APPROVED' OR [Reason]='SELF_SERVICE_ACCOUNT_RECOVERY' OR [Reason]='ADMINISTRATOR_INITIATED')); disabled=False |
| CK_PasswordResetLogs_Result | ([Result]=(3) OR [Result]=(2) OR [Result]=(1)); disabled=False |
| CK_PasswordResetLogs_Source_SafeCode | ([Source]='SYSTEM' OR [Source]='COMPLIANCE_REVIEW' OR [Source]='IDENTITY_SERVICE' OR [Source]='SELF_SERVICE' OR [Source]='ADMIN_API'); disabled=False |
| [TR_PasswordResetLogs_AppendOnly](current-triggers.md#dbotr_passwordresetlogs_appendonly) | Observed rejection text: PasswordResetLogs are append-only; updates and deletions are forbidden. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L261); OUTER_WHITESPACE_ONLY |

### dbo.AccountStatusChangeLogs

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog. Purpose (source-interpreted from entity/configuration, not accepted business policy): Account status transition events. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/AccountStatusChangeLog.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/AccountStatusChangeLogConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| TargetUserId | TargetUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ChangedByUserId | ChangedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| OccurredAt | OccurredAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| FromStatus | FromStatus : RoadGuardSystem.aBusinessObjects.Commons.UserStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Active=1, Suspended=2, Pending=3 |
| ToStatus | ToStatus : RoadGuardSystem.aBusinessObjects.Commons.UserStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Active=1, Suspended=2, Pending=3 |
| Reason | Reason : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| Source | Source : String | varchar | False | 80 | 0/0 | - | Never | - |
| CorrelationId | CorrelationId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| HandoverReference | HandoverReference : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_AccountStatusChangeLogs | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_AccountStatusChangeLogs_Users_ChangedByUserId | ChangedByUserId -> dbo.Users.Id | NO_ACTION | False |
| FK_AccountStatusChangeLogs_Users_TargetUserId | TargetUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_AccountStatusChangeLogs_ChangedByUserId | ChangedByUserId | False | - | NONCLUSTERED |
| IX_AccountStatusChangeLogs_OccurredAt | OccurredAt | False | - | NONCLUSTERED |
| IX_AccountStatusChangeLogs_TargetUserId | TargetUserId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_AccountStatusChangeLogs_FromStatus | ([FromStatus]=(3) OR [FromStatus]=(2) OR [FromStatus]=(1)); disabled=False |
| CK_AccountStatusChangeLogs_FromToStatus_Diff | ([FromStatus]<>[ToStatus]); disabled=False |
| CK_AccountStatusChangeLogs_Reason_SafeCode | ([Reason]='ACCOUNT_REACTIVATED' OR [Reason]='SECURITY_INCIDENT' OR [Reason]='ADMINISTRATIVE_LOCK' OR [Reason]='NO_STATUS_CHANGE' OR [Reason]='SAFETY_POLICY_VIOLATION' OR [Reason]='REGISTRATION_APPROVED' OR [Reason]='SELF_SERVICE_ACCOUNT_RECOVERY' OR [Reason]='ADMINISTRATOR_INITIATED'); disabled=False |
| CK_AccountStatusChangeLogs_Source_SafeCode | ([Source]='SYSTEM' OR [Source]='COMPLIANCE_REVIEW' OR [Source]='IDENTITY_SERVICE' OR [Source]='SELF_SERVICE' OR [Source]='ADMIN_API'); disabled=False |
| CK_AccountStatusChangeLogs_ToStatus | ([ToStatus]=(3) OR [ToStatus]=(2) OR [ToStatus]=(1)); disabled=False |
| [TR_AccountStatusChangeLogs_AppendOnly](current-triggers.md#dbotr_accountstatuschangelogs_appendonly) | Observed rejection text: AccountStatusChangeLogs are append-only; updates and deletions are forbidden. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L273); OUTER_WHITESPACE_ONLY |

### dbo.PasswordRecoveryRequests

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.PasswordRecoveryRequest. Purpose (source-interpreted from entity/configuration, not accepted business policy): Password recovery requests. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/PasswordRecoveryRequest.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/PasswordRecoveryRequestConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| TargetUserId | TargetUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| RequestedAtUtc | RequestedAtUtc : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| CorrelationId | CorrelationId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_PasswordRecoveryRequests | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_PasswordRecoveryRequests_Users_TargetUserId | TargetUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_PasswordRecoveryRequests_TargetUserId_RequestedAtUtc | TargetUserId, RequestedAtUtc | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.ReporterRegistrationIntents

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.ReporterRegistrationIntent. Purpose (source-interpreted from entity/configuration, not accepted business policy): Reporter onboarding intent. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/ReporterRegistrationIntent.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ReporterRegistrationIntentConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| UserId | UserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| NormalizedEmail | NormalizedEmail : String | nvarchar | False | 508 | 0/0 | - | Never | - |
| ReporterType | ReporterType : RoadGuardSystem.aBusinessObjects.Commons.ReporterType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Citizen=1, InvestorRepresentative=2 |
| OtpHash | OtpHash : String | char | False | 64 | 0/0 | - | Never | - |
| OtpGeneration | OtpGeneration : Int32 | int | False | 4 | 10/0 | - | Never | - |
| FailedAttempts | FailedAttempts : Int32 | int | False | 4 | 10/0 | - | Never | - |
| ExpiresAt | ExpiresAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| ResendAvailableAt | ResendAvailableAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| ConsumedAt | ConsumedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| EmailConfirmedAt | EmailConfirmedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| CreatedAt | CreatedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_ReporterRegistrationIntents | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_ReporterRegistrationIntents_Users_UserId | UserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_ReporterRegistrationIntents_Email | NormalizedEmail | False | - | NONCLUSTERED |
| IX_ReporterRegistrationIntents_UserId | UserId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_ReporterRegistrationIntents_ReporterType | ([ReporterType]=(2) OR [ReporterType]=(1)); disabled=False |

### dbo.StaffInvitations

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.StaffInvitation. Purpose (source-interpreted from entity/configuration, not accepted business policy): Staff invitation lifecycle. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/StaffInvitation.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/StaffInvitationConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| DisplayName | DisplayName : String | nvarchar | False | 400 | 0/0 | - | Never | - |
| Email | Email : String | nvarchar | False | 508 | 0/0 | - | Never | - |
| NormalizedEmail | NormalizedEmail : String | nvarchar | False | 508 | 0/0 | - | Never | - |
| RoleCode | RoleCode : RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode | varchar | False | 40 | 0/0 | - | Never | System.String; Unknown=REJECTED_BY_CONVERTER, Supervisor=SUPERVISOR, ProjectManager=PM, DroneOperator=DRONE_OPERATOR, RepairCrew=REPAIR_CREW, Reporter=REPORTER |
| TokenHash | TokenHash : String | char | False | 64 | 0/0 | - | Never | - |
| CreatedByUserId | CreatedByUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| CreatedAt | CreatedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| ExpiresAt | ExpiresAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| AcceptedAt | AcceptedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RevokedAt | RevokedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_StaffInvitations | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_StaffInvitations_Users_CreatedByUserId | CreatedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_StaffInvitations_CreatedByUserId | CreatedByUserId | False | - | NONCLUSTERED |
| IX_StaffInvitations_Email | NormalizedEmail | False | - | NONCLUSTERED |
| UX_StaffInvitations_TokenHash | TokenHash | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.StaffInvitationProjects

- Module: identity; CLR mapping: RoadGuardSystem.BusinessObjects.Identity.StaffInvitationProject. Purpose (source-interpreted from entity/configuration, not accepted business policy): Projects attached to an invitation. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Identity/StaffInvitationProject.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/StaffInvitationProjectConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| InvitationId | InvitationId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_StaffInvitationProjects | PRIMARY_KEY_CONSTRAINT | InvitationId, ProjectId |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_StaffInvitationProjects_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_StaffInvitationProjects_StaffInvitations_InvitationId | InvitationId -> dbo.StaffInvitations.Id | CASCADE | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_StaffInvitationProjects_ProjectId | ProjectId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

## project-road

### dbo.Projects

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Projects.Project. Purpose (source-interpreted from entity/configuration, not accepted business policy): Project identity, status and spatial reference. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Projects/Project.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ProjectConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectCode | ProjectCode : String | varchar | False | 50 | 0/0 | - | Never | - |
| Name | Name : String | nvarchar | False | 510 | 0/0 | - | Never | - |
| Description | Description : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.ProjectStatus | tinyint | False | 1 | 3/0 | - | Never | Planning=1, Active=2, Closed=3 |
| StartDate | StartDate : DateOnly? | date | True | 3 | 10/0 | - | Never | - |
| EndDate | EndDate : DateOnly? | date | True | 3 | 10/0 | - | Never | - |
| CreatedAt | CreatedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |
| EngineeringUtmSrid | EngineeringUtmSrid : Int32? | int | True | 4 | 10/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Projects | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_Projects_ProjectCode | ProjectCode | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Projects_DateRange | ([EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate]>=[StartDate]); disabled=False |
| CK_Projects_EngineeringUtmSrid | ([EngineeringUtmSrid] IS NULL OR ([EngineeringUtmSrid]=(32649) OR [EngineeringUtmSrid]=(32648))); disabled=False |
| CK_Projects_Status | ([Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

### dbo.ProjectMembers

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Projects.ProjectMember. Purpose (source-interpreted from entity/configuration, not accepted business policy): User membership within a project. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Projects/ProjectMember.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ProjectMemberConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| UserId | UserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoleCode | RoleCode : RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode | varchar | False | 40 | 0/0 | - | Never | System.String; Unknown=REJECTED_BY_CONVERTER, Supervisor=SUPERVISOR, ProjectManager=PM, DroneOperator=DRONE_OPERATOR, RepairCrew=REPAIR_CREW, Reporter=REPORTER |
| IsPrimary | IsPrimary : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| ValidFrom | ValidFrom : DateOnly | date | False | 3 | 10/0 | - | Never | - |
| ValidTo | ValidTo : DateOnly? | date | True | 3 | 10/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.ProjectMemberStatus | tinyint | False | 1 | 3/0 | - | Never | Active=1, Ended=2 |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_ProjectMembers | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_ProjectMembers_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_ProjectMembers_Roles_RoleCode | RoleCode -> dbo.Roles.Code | NO_ACTION | False |
| FK_ProjectMembers_Users_UserId | UserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_ProjectMembers_ProjectId_UserId | ProjectId, UserId | False | - | NONCLUSTERED |
| IX_ProjectMembers_RoleCode | RoleCode | False | - | NONCLUSTERED |
| IX_ProjectMembers_UserId | UserId | False | - | NONCLUSTERED |
| UX_ProjectMembers_ActivePrimaryProjectManager | ProjectId | True | ([IsPrimary]=(1) AND [Status]=(1)) | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_ProjectMembers_EffectiveDateRange | ([ValidTo] IS NULL OR [ValidTo]>=[ValidFrom]); disabled=False |
| CK_ProjectMembers_PrimaryRole | ([IsPrimary]=(0) OR [RoleCode]='PM'); disabled=False |
| CK_ProjectMembers_Status | ([Status]=(2) OR [Status]=(1)); disabled=False |

### dbo.RoadSections

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Projects.RoadSection. Purpose (source-interpreted from entity/configuration, not accepted business policy): Named road sections in a project. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSection.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/RoadSectionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Code | Code : String | varchar | False | 80 | 0/0 | - | Never | - |
| Name | Name : String | nvarchar | True | 510 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_RoadSections | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_RoadSections_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_RoadSections_ProjectId_Code | ProjectId, Code | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.RoadSectionVersions

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion. Purpose (source-interpreted from entity/configuration, not accepted business policy): Versioned road geometry. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSectionVersion.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/RoadSectionVersionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionId | RoadSectionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| VersionNo | VersionNo : Int32 | int | False | 4 | 10/0 | - | Never | - |
| IsCurrent | IsCurrent : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| Geometry | Geometry : LineString | geometry | False | -1 | 0/0 | - | Never | - |
| EffectiveFrom | EffectiveFrom : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| ChangeReason | ChangeReason : String | nvarchar | False | -1 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_RoadSectionVersions | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_RoadSectionVersions_RoadSections_RoadSectionId | RoadSectionId -> dbo.RoadSections.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_RoadSectionVersions_Current | RoadSectionId | True | ([IsCurrent]=(1)) | NONCLUSTERED |
| UX_RoadSectionVersions_RoadSectionId_VersionNo | RoadSectionId, VersionNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_RoadSectionVersions_Geometry_AllowedSrid | ([Geometry].[STSrid]=(32649) OR [Geometry].[STSrid]=(32648)); disabled=False |
| CK_RoadSectionVersions_Geometry_LineString | ([Geometry].[STGeometryType]()='LineString'); disabled=False |
| CK_RoadSectionVersions_VersionNo_Positive | ([VersionNo]>(0)); disabled=False |
| [TR_RoadSectionVersions_Immutable](current-triggers.md#dbotr_roadsectionversions_immutable) | Observed rejection text: RoadSectionVersion records cannot be deleted.; RoadSectionVersion history is immutable; only IsCurrent may change. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920154542_AddRoadSectionVersionAndWarrantySchema.cs#L164); OUTER_WHITESPACE_ONLY |

### dbo.RoadSegmentSets

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Projects.RoadSegmentSet. Purpose (source-interpreted from entity/configuration, not accepted business policy): Segment-set publication state. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSegmentSet.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/RoadSegmentSetConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Status | Status : String | varchar | False | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_RoadSegmentSets | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_RoadSegmentSets_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_RoadSegmentSets_RouteVersion_Status | RoadSectionVersionId, Status | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_RoadSegmentSets_Status | ([Status]='SUPERSEDED' OR [Status]='PUBLISHED' OR [Status]='DRAFT'); disabled=False |

### dbo.RoadSegments

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Projects.RoadSegment. Purpose (source-interpreted from entity/configuration, not accepted business policy): Ordered segments within a set. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSegment.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/RoadSegmentConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SegmentSetId | SegmentSetId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Sequence | Sequence : Int32 | int | False | 4 | 10/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_RoadSegments | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_RoadSegments_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_RoadSegments_RoadSegmentSets_SegmentSetId | SegmentSetId -> dbo.RoadSegmentSets.Id | CASCADE | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_RoadSegments_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |
| UX_RoadSegments_Set_Sequence | SegmentSetId, Sequence | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_RoadSegments_Sequence_Positive | ([Sequence]>(0)); disabled=False |

### dbo.Warranties

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Warranties.Warranty. Purpose (source-interpreted from entity/configuration, not accepted business policy): Warranty terms linked to project scope. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Warranties/Warranty.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/WarrantyConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionId | RoadSectionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| HandoverDocumentId | HandoverDocumentId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| HandoverDate | HandoverDate : DateOnly | date | False | 3 | 10/0 | - | Never | - |
| WarrantyStartDate | WarrantyStartDate : DateOnly | date | False | 3 | 10/0 | - | Never | - |
| WarrantyEndDate | WarrantyEndDate : DateOnly | date | False | 3 | 10/0 | - | Never | - |
| RetainedValue | RetainedValue : Decimal? | decimal | True | 9 | 19/2 | - | Never | - |
| Scope | Scope : RoadGuardSystem.aBusinessObjects.Commons.WarrantyScope | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Project=1, RoadSection=2, ContractItem=3, Other=4 |
| Terms | Terms : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| SourceDocumentId | SourceDocumentId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.WarrantyStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Planned=1, Active=2, Expired=3, Suspended=4 |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Warranties | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Warranties_Files_SourceDocumentId | SourceDocumentId -> dbo.Files.Id | NO_ACTION | False |
| FK_Warranties_HandoverDocuments_HandoverDocumentId | HandoverDocumentId -> dbo.HandoverDocuments.Id | NO_ACTION | False |
| FK_Warranties_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_Warranties_RoadSections_RoadSectionId | RoadSectionId -> dbo.RoadSections.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Warranties_HandoverDocumentId | HandoverDocumentId | False | - | NONCLUSTERED |
| IX_Warranties_ProjectId | ProjectId | False | - | NONCLUSTERED |
| IX_Warranties_RoadSectionId | RoadSectionId | False | - | NONCLUSTERED |
| IX_Warranties_SourceDocumentId | SourceDocumentId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Warranties_DateRange | ([WarrantyEndDate]>=[WarrantyStartDate]); disabled=False |
| CK_Warranties_RetainedValue_NonNegative | ([RetainedValue] IS NULL OR [RetainedValue]>=(0)); disabled=False |
| CK_Warranties_Scope | ([Scope]=(4) OR [Scope]=(3) OR [Scope]=(2) OR [Scope]=(1)); disabled=False |
| CK_Warranties_ScopeRoadSection | (([Scope]<>(1) OR [RoadSectionId] IS NULL) AND ([Scope]<>(2) OR [RoadSectionId] IS NOT NULL)); disabled=False |
| CK_Warranties_Status | ([Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

### dbo.HandoverDocuments

- Module: project-road; CLR mapping: RoadGuardSystem.BusinessObjects.Projects.HandoverDocument. Purpose (source-interpreted from entity/configuration, not accepted business policy): Project handover document references. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Projects/HandoverDocument.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/HandoverDocumentConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| DocumentNo | DocumentNo : String | varchar | False | 80 | 0/0 | - | Never | - |
| HandoverDate | HandoverDate : DateOnly | date | False | 3 | 10/0 | - | Never | - |
| AcceptedByUserId | AcceptedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| FileId | FileId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Notes | Notes : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_HandoverDocuments | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_HandoverDocuments_Files_FileId | FileId -> dbo.Files.Id | NO_ACTION | False |
| FK_HandoverDocuments_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_HandoverDocuments_Users_AcceptedByUserId | AcceptedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_HandoverDocuments_AcceptedByUserId | AcceptedByUserId | False | - | NONCLUSTERED |
| IX_HandoverDocuments_FileId | FileId | False | - | NONCLUSTERED |
| UX_HandoverDocuments_ProjectId_DocumentNo | ProjectId, DocumentNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

## survey

### dbo.SurveyPlans

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan. Purpose (source-interpreted from entity/configuration, not accepted business policy): Planned survey schedule and state. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyPlanConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionId | RoadSectionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| PlannedStartAt | PlannedStartAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| PlannedEndAt | PlannedEndAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| SurveyType | SurveyType : RoadGuardSystem.aBusinessObjects.Commons.SurveyType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Original=1, Periodic=2, Supplementary=3 |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.SurveyPlanStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Planned=1, Postponed=2, InProgress=3, Completed=4, Cancelled=5 |
| OutputRequirements | OutputRequirements : String | nvarchar | False | -1 | 0/0 | (N'{}') | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyPlans | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyPlans_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_SurveyPlans_RoadSections_RoadSectionId | RoadSectionId -> dbo.RoadSections.Id | NO_ACTION | False |
| FK_SurveyPlans_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SurveyPlans_ProjectRoadStart | ProjectId, RoadSectionId, PlannedStartAt | False | - | NONCLUSTERED |
| IX_SurveyPlans_RoadSectionId | RoadSectionId | False | - | NONCLUSTERED |
| IX_SurveyPlans_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |
| UX_SurveyPlans_ActiveScope | ProjectId, RoadSectionId, SurveyType | True | ([Status] IN ((1), (2), (3))) | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SurveyPlans_OutputRequirements_Json | (isjson([OutputRequirements])=(1)); disabled=False |
| CK_SurveyPlans_PlannedDateRange | ([PlannedEndAt]>=[PlannedStartAt]); disabled=False |
| CK_SurveyPlans_Status | ([Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_SurveyPlans_SurveyType | ([SurveyType]=(3) OR [SurveyType]=(2) OR [SurveyType]=(1)); disabled=False |
| [TR_SurveyPlans_ScopeIntegrity](current-triggers.md#dbotr_surveyplans_scopeintegrity) | Observed rejection text: SurveyPlan road section must belong to its project. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L160); OUTER_WHITESPACE_ONLY |

### dbo.SurveyPlanScopes

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanScope. Purpose (source-interpreted from entity/configuration, not accepted business policy): Road-version scope of a plan. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanScope.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyPlanScopeConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyPlanId | SurveyPlanId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RouteSectionVersionId | RouteSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SegmentSetId | SegmentSetId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SegmentIdsJson | SegmentIdsJson : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| TargetBand | TargetBand : String | nvarchar | False | 64 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyPlanScopes | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyPlanScopes_RoadSectionVersions_RouteSectionVersionId | RouteSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_SurveyPlanScopes_SurveyPlans_SurveyPlanId | SurveyPlanId -> dbo.SurveyPlans.Id | CASCADE | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SurveyPlanScopes_RouteSectionVersionId | RouteSectionVersionId | False | - | NONCLUSTERED |
| UX_SurveyPlanScopes_UniqueBand | SurveyPlanId, RouteSectionVersionId, SegmentSetId, TargetBand | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SurveyPlanScopes_SegmentIdsJson | (isjson([SegmentIdsJson])=(1)); disabled=False |

### dbo.SurveyPlanPostponements

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanPostponement. Purpose (source-interpreted from entity/configuration, not accepted business policy): Append-only plan postponement history. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanPostponement.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyPlanPostponementConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyPlanId | SurveyPlanId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| PostponedAt | PostponedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| Reason | Reason : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| NewPlannedStartAt | NewPlannedStartAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyPlanPostponements | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyPlanPostponements_SurveyPlans_SurveyPlanId | SurveyPlanId -> dbo.SurveyPlans.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SurveyPlanPostponements_PlanId_PostponedAt | SurveyPlanId, PostponedAt | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| [TR_SurveyPlanPostponements_AppendOnly](current-triggers.md#dbotr_surveyplanpostponements_appendonly) | Observed rejection text: SurveyPlanPostponements are append-only. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L220); OUTER_WHITESPACE_ONLY |

### dbo.SurveyRequests

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest. Purpose (source-interpreted from entity/configuration, not accepted business policy): Survey request and approval state. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyRequest.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyRequestConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionId | RoadSectionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyPlanId | SurveyPlanId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| RequestedByUserId | RequestedByUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyType | SurveyType : RoadGuardSystem.aBusinessObjects.Commons.SurveyType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Original=1, Periodic=2, Supplementary=3 |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.SurveyRequestStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, NewAssigned=1, Accepted=2, Rejected=3, Reassigned=4, InProgress=5, Submitted=6, SupplementRequired=7, Completed=8, Cancelled=9, Postponed=10 |
| RequestedAt | RequestedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| CancelledAt | CancelledAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| CancellationReason | CancellationReason : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| DueAt | DueAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | (sysutcdatetime()) | Never | - |
| OutputRequirements | OutputRequirements : String | nvarchar | False | -1 | 0/0 | (N'{}') | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyRequests | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyRequests_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_SurveyRequests_RoadSections_RoadSectionId | RoadSectionId -> dbo.RoadSections.Id | NO_ACTION | False |
| FK_SurveyRequests_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_SurveyRequests_SurveyPlans_SurveyPlanId | SurveyPlanId -> dbo.SurveyPlans.Id | NO_ACTION | False |
| FK_SurveyRequests_Users_RequestedByUserId | RequestedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SurveyRequests_ProjectRoadStatus | ProjectId, RoadSectionId, Status | False | - | NONCLUSTERED |
| IX_SurveyRequests_RequestedByUserId | RequestedByUserId | False | - | NONCLUSTERED |
| IX_SurveyRequests_RoadSectionId | RoadSectionId | False | - | NONCLUSTERED |
| IX_SurveyRequests_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |
| UX_SurveyRequests_ActivePlan | SurveyPlanId | True | ([SurveyPlanId] IS NOT NULL AND ([Status] IN ((1), (2), (4), (5), (6), (7), (10)))) | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SurveyRequests_Cancellation | ([Status]=(9) AND [CancelledAt] IS NOT NULL AND len(ltrim(rtrim([CancellationReason])))>(0) OR [Status]<>(9) AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL); disabled=False |
| CK_SurveyRequests_OutputRequirements_Json | (isjson([OutputRequirements])=(1)); disabled=False |
| CK_SurveyRequests_Status | ([Status]=(10) OR [Status]=(9) OR [Status]=(8) OR [Status]=(7) OR [Status]=(6) OR [Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_SurveyRequests_SurveyType | ([SurveyType]=(3) OR [SurveyType]=(2) OR [SurveyType]=(1)); disabled=False |
| [TR_SurveyRequests_ScopeIntegrity](current-triggers.md#dbotr_surveyrequests_scopeintegrity) | Observed rejection text: SurveyRequest road section must belong to its project.; SurveyRequest source plan must match project, road section, and survey type. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L183); OUTER_WHITESPACE_ONLY |

### dbo.SurveyRequestScopes

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyRequestScope. Purpose (source-interpreted from entity/configuration, not accepted business policy): Road-version scope of a request. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyRequestScope.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyRequestScopeConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyRequestId | SurveyRequestId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RouteSectionVersionId | RouteSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SegmentSetId | SegmentSetId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SegmentIdsJson | SegmentIdsJson : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| TargetBand | TargetBand : String | nvarchar | False | 64 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyRequestScopes | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyRequestScopes_RoadSectionVersions_RouteSectionVersionId | RouteSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_SurveyRequestScopes_SurveyRequests_SurveyRequestId | SurveyRequestId -> dbo.SurveyRequests.Id | CASCADE | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SurveyRequestScopes_RouteSectionVersionId | RouteSectionVersionId | False | - | NONCLUSTERED |
| UX_SurveyRequestScopes_UniqueBand | SurveyRequestId, RouteSectionVersionId, SegmentSetId, TargetBand | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SurveyRequestScopes_SegmentIdsJson | (isjson([SegmentIdsJson])=(1)); disabled=False |

### dbo.Surveys

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.Survey. Purpose (source-interpreted from entity/configuration, not accepted business policy): Survey execution record. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/Survey.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyRequestId | SurveyRequestId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyType | SurveyType : RoadGuardSystem.aBusinessObjects.Commons.SurveyType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Original=1, Periodic=2, Supplementary=3 |
| IsBaselineConfirmed | IsBaselineConfirmed : Boolean | bit | False | 1 | 1/0 | - | Never | - |
| BaselineConfirmedByUserId | BaselineConfirmedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| BaselineConfirmedAt | BaselineConfirmedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.SurveyStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Draft=1, InProgress=2, Submitted=3, Completed=4, Cancelled=5 |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Surveys | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Surveys_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_Surveys_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_Surveys_SurveyRequests_SurveyRequestId | SurveyRequestId -> dbo.SurveyRequests.Id | NO_ACTION | False |
| FK_Surveys_Users_BaselineConfirmedByUserId | BaselineConfirmedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Surveys_BaselineConfirmedByUserId | BaselineConfirmedByUserId | False | - | NONCLUSTERED |
| IX_Surveys_ProjectStatus | ProjectId, Status | False | - | NONCLUSTERED |
| IX_Surveys_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |
| IX_Surveys_SurveyRequestId | SurveyRequestId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Surveys_BaselineConfirmation | ([IsBaselineConfirmed]=(0) AND [BaselineConfirmedByUserId] IS NULL AND [BaselineConfirmedAt] IS NULL OR [IsBaselineConfirmed]=(1) AND [BaselineConfirmedByUserId] IS NOT NULL AND [BaselineConfirmedAt] IS NOT NULL); disabled=False |
| CK_Surveys_Status | ([Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_Surveys_SurveyType | ([SurveyType]=(3) OR [SurveyType]=(2) OR [SurveyType]=(1)); disabled=False |
| [TR_Surveys_ScopeIntegrity](current-triggers.md#dbotr_surveys_scopeintegrity) | Observed rejection text: Survey road section version must belong to its project.; Survey request must match project, road section version, and survey type. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920182623_AddSurveyAssignmentSchema.cs#L147); OUTER_WHITESPACE_ONLY |

### dbo.SurveyAssignments

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment. Purpose (source-interpreted from entity/configuration, not accepted business policy): Survey operator assignment. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyAssignment.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyAssignmentConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyRequestId | SurveyRequestId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| OperatorUserId | OperatorUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| AssignedByUserId | AssignedByUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| AssignedAt | AssignedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| AcceptedAt | AcceptedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RejectedAt | RejectedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RejectionReason | RejectionReason : String | nvarchar | True | 2000 | 0/0 | - | Never | - |
| ReassignmentReason | ReassignmentReason : String | nvarchar | True | 2000 | 0/0 | - | Never | - |
| EndedAt | EndedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyAssignments | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyAssignments_SurveyRequests_SurveyRequestId | SurveyRequestId -> dbo.SurveyRequests.Id | NO_ACTION | False |
| FK_SurveyAssignments_Users_AssignedByUserId | AssignedByUserId -> dbo.Users.Id | NO_ACTION | False |
| FK_SurveyAssignments_Users_OperatorUserId | OperatorUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SurveyAssignments_AssignedByUserId | AssignedByUserId | False | - | NONCLUSTERED |
| IX_SurveyAssignments_OperatorUserId | OperatorUserId | False | - | NONCLUSTERED |
| IX_SurveyAssignments_RequestEndedAt | SurveyRequestId, EndedAt | False | - | NONCLUSTERED |
| UX_SurveyAssignments_ActiveRequest | SurveyRequestId | True | ([EndedAt] IS NULL) | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SurveyAssignments_AcceptanceRejection | ([AcceptedAt] IS NULL OR [RejectedAt] IS NULL); disabled=False |
| CK_SurveyAssignments_ActiveReassignmentReason | ([EndedAt] IS NOT NULL OR [ReassignmentReason] IS NULL); disabled=False |
| CK_SurveyAssignments_ReassignmentNotRejected | ([ReassignmentReason] IS NULL OR [RejectedAt] IS NULL); disabled=False |
| CK_SurveyAssignments_Rejection | ([RejectedAt] IS NULL AND [RejectionReason] IS NULL OR [RejectedAt] IS NOT NULL AND len(ltrim(rtrim([RejectionReason])))>(0) AND [EndedAt] IS NOT NULL); disabled=False |
| CK_SurveyAssignments_TimestampOrder | (([AcceptedAt] IS NULL OR [AcceptedAt]>=[AssignedAt]) AND ([RejectedAt] IS NULL OR [RejectedAt]>=[AssignedAt]) AND ([EndedAt] IS NULL OR [EndedAt]>=[AssignedAt])); disabled=False |

### dbo.Flights

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.Flight. Purpose (source-interpreted from entity/configuration, not accepted business policy): Flight execution records. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/Flight.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/FlightConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyId | SurveyId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| DroneDeviceId | DroneDeviceId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| OperatorUserId | OperatorUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| StartedAt | StartedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| EndedAt | EndedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| FlightNo | FlightNo : String | varchar | False | 80 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Flights | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Flights_DroneDevices_DroneDeviceId | DroneDeviceId -> dbo.DroneDevices.Id | NO_ACTION | False |
| FK_Flights_Surveys_SurveyId | SurveyId -> dbo.Surveys.Id | NO_ACTION | False |
| FK_Flights_Users_OperatorUserId | OperatorUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Flights_DroneDeviceId | DroneDeviceId | False | - | NONCLUSTERED |
| IX_Flights_OperatorUserId | OperatorUserId | False | - | NONCLUSTERED |
| UX_Flights_SurveyFlightNo | SurveyId, FlightNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Flights_TimestampOrder | ([EndedAt] IS NULL OR [EndedAt]>=[StartedAt]); disabled=False |
| [TR_Flights_ImmutableSurvey](current-triggers.md#dbotr_flights_immutablesurvey) | Observed rejection text: Flight survey identity is immutable. UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921134719_AddP230FlightSurveyIdentityImmutability.cs#L15); OUTER_WHITESPACE_ONLY |

### dbo.SurveyFiles

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyFile. Purpose (source-interpreted from entity/configuration, not accepted business policy): Files attached to survey/flight. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyFile.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyFileConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyId | SurveyId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| FlightId | FlightId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| FileId | FileId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| FileType | FileType : RoadGuardSystem.aBusinessObjects.Commons.SurveyFileType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Video=1, Srt=2, Photo=3, Other=4 |
| CaptureStartedAt | CaptureStartedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| CaptureEndedAt | CaptureEndedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| SyncStatus | SyncStatus : RoadGuardSystem.aBusinessObjects.Commons.SurveyFileSyncStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Local=1, Queued=2, Uploading=3, ServerConfirmed=4, Invalid=5 |
| Checksum | Checksum : String | char | False | 64 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyFiles | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyFiles_Files_FileId | FileId -> dbo.Files.Id | NO_ACTION | False |
| FK_SurveyFiles_Flights_FlightId | FlightId -> dbo.Flights.Id | NO_ACTION | False |
| FK_SurveyFiles_Surveys_SurveyId | SurveyId -> dbo.Surveys.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SurveyFiles_FileId | FileId | False | - | NONCLUSTERED |
| IX_SurveyFiles_FlightId | FlightId | False | - | NONCLUSTERED |
| IX_SurveyFiles_SurveyId | SurveyId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SurveyFiles_Checksum_Sha256Lowercase | (len([Checksum])=(64) AND NOT ([Checksum]) collate Latin1_General_100_BIN2 like '%[^0-9a-f]%'); disabled=False |
| CK_SurveyFiles_FileType | ([FileType]=(4) OR [FileType]=(3) OR [FileType]=(2) OR [FileType]=(1)); disabled=False |
| CK_SurveyFiles_SyncStatus | ([SyncStatus]=(5) OR [SyncStatus]=(4) OR [SyncStatus]=(3) OR [SyncStatus]=(2) OR [SyncStatus]=(1)); disabled=False |
| CK_SurveyFiles_TimestampOrder | ([CaptureEndedAt] IS NULL OR [CaptureStartedAt] IS NULL OR [CaptureEndedAt]>=[CaptureStartedAt]); disabled=False |
| [TR_SurveyFiles_ScopeIntegrity](current-triggers.md#dbotr_surveyfiles_scopeintegrity) | Observed rejection text: Survey file flight must belong to the same survey.; Survey file checksum must match the immutable stored file checksum. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921125553_AddP230FlightSurveyFileSchema.cs#L124); OUTER_WHITESPACE_ONLY |

### dbo.SurveyDataVersions

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion. Purpose (source-interpreted from entity/configuration, not accepted business policy): Versioned survey dataset. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyDataVersion.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SurveyDataVersionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyId | SurveyId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| VersionNo | VersionNo : Int32 | int | False | 4 | 10/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.SurveyDataVersionStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Draft=1, Uploading=2, ServerConfirmed=3, Invalid=4, Superseded=5 |
| IntegrityStatus | IntegrityStatus : RoadGuardSystem.aBusinessObjects.Commons.SurveyDataIntegrityStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Pending=1, Passed=2, Failed=3 |
| ConfirmedAt | ConfirmedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| ConfirmedBy | ConfirmedBy : RoadGuardSystem.aBusinessObjects.Commons.SurveyDataConfirmationActor? | tinyint | True | 1 | 3/0 | - | Never | Unknown=0, Backend=1 |
| SourceManifest | SourceManifest : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| DeviceId | DeviceId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| RecordedAt | RecordedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |
| ScopeManifest | ScopeManifest : String | nvarchar | True | -1 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SurveyDataVersions | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SurveyDataVersions_Surveys_SurveyId | SurveyId -> dbo.Surveys.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_SurveyDataVersions_SurveyVersion | SurveyId, VersionNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SurveyDataVersions_IntegrityStatus | ([IntegrityStatus]=(3) OR [IntegrityStatus]=(2) OR [IntegrityStatus]=(1)); disabled=False |
| CK_SurveyDataVersions_ServerConfirmation | ([Status]=(3) AND [IntegrityStatus]=(2) AND [ConfirmedAt] IS NOT NULL AND [ConfirmedBy]=(1) OR [Status]<>(3) AND [ConfirmedAt] IS NULL AND [ConfirmedBy] IS NULL); disabled=False |
| CK_SurveyDataVersions_SourceManifest_JsonArray | (isjson([SourceManifest])=(1) AND left(ltrim([SourceManifest]),(1))='['); disabled=False |
| CK_SurveyDataVersions_Status | ([Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_SurveyDataVersions_VersionNo | ([VersionNo]>(0)); disabled=False |
| [TR_SurveyDataVersions_Immutable](current-triggers.md#dbotr_surveydataversions_immutable) | Observed rejection text: Survey dataset identity is immutable.; Confirmed or superseded survey dataset cannot regress status.; Confirmed or superseded survey dataset manifest is immutable. UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921132735_AddP230ConfirmedDatasetImmutability.cs#L15); OUTER_WHITESPACE_ONLY |

### dbo.QualityChecks

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.QualityCheck. Purpose (source-interpreted from entity/configuration, not accepted business policy): Dataset quality-check outcomes. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/QualityCheck.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/QualityCheckConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Scope | Scope : RoadGuardSystem.aBusinessObjects.Commons.QualityCheckScope | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, SurveyFile=1, SurveyDataset=2 |
| ExecutionStage | ExecutionStage : RoadGuardSystem.aBusinessObjects.Commons.QualityCheckExecutionStage | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, ClientPrecheck=1, ServerValidation=2 |
| SurveyFileId | SurveyFileId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| SurveyDataVersionId | SurveyDataVersionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| CheckType | CheckType : RoadGuardSystem.aBusinessObjects.Commons.QualityCheckType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Format=1, Geolocation=2, TimeSync=3, Clarity=4, Lighting=5, Coverage=6, Overlap=7, Completeness=8, Other=9 |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.QualityCheckStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Pending=1, Passed=2, Failed=3, Warning=4 |
| MeasuredValue | MeasuredValue : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| Threshold | Threshold : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| Message | Message : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| CheckedAt | CheckedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| CheckedBy | CheckedBy : RoadGuardSystem.aBusinessObjects.Commons.QualityCheckActor | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, DroneApp=1, Backend=2 |
| InitiatedByUserId | InitiatedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_QualityChecks | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_QualityChecks_SurveyDataVersions_SurveyDataVersionId | SurveyDataVersionId -> dbo.SurveyDataVersions.Id | NO_ACTION | False |
| FK_QualityChecks_SurveyFiles_SurveyFileId | SurveyFileId -> dbo.SurveyFiles.Id | NO_ACTION | False |
| FK_QualityChecks_Users_InitiatedByUserId | InitiatedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_QualityChecks_InitiatedByUserId | InitiatedByUserId | False | - | NONCLUSTERED |
| IX_QualityChecks_SurveyDataVersionId | SurveyDataVersionId | False | - | NONCLUSTERED |
| IX_QualityChecks_SurveyFileId | SurveyFileId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_QualityChecks_CheckedBy | ([CheckedBy]=(2) OR [CheckedBy]=(1)); disabled=False |
| CK_QualityChecks_CheckType | ([CheckType]=(9) OR [CheckType]=(8) OR [CheckType]=(7) OR [CheckType]=(6) OR [CheckType]=(5) OR [CheckType]=(4) OR [CheckType]=(3) OR [CheckType]=(2) OR [CheckType]=(1)); disabled=False |
| CK_QualityChecks_ExactlyOneTarget | ([Scope]=(1) AND [SurveyFileId] IS NOT NULL AND [SurveyDataVersionId] IS NULL OR [Scope]=(2) AND [SurveyFileId] IS NULL AND [SurveyDataVersionId] IS NOT NULL); disabled=False |
| CK_QualityChecks_ExecutionStage | ([ExecutionStage]=(2) OR [ExecutionStage]=(1)); disabled=False |
| CK_QualityChecks_MeasuredValue_Json | ([MeasuredValue] IS NULL OR isjson([MeasuredValue])=(1)); disabled=False |
| CK_QualityChecks_Scope | ([Scope]=(2) OR [Scope]=(1)); disabled=False |
| CK_QualityChecks_StageActor | ([ExecutionStage]=(1) AND [CheckedBy]=(1) OR [ExecutionStage]=(2) AND [CheckedBy]=(2) AND [InitiatedByUserId] IS NULL); disabled=False |
| CK_QualityChecks_Status | ([Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_QualityChecks_Threshold_Json | ([Threshold] IS NULL OR isjson([Threshold])=(1)); disabled=False |

### dbo.SupplementarySurveyRequests

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest. Purpose (source-interpreted from entity/configuration, not accepted business policy): Supplementary survey requests. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Surveys/SupplementarySurveyRequest.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SupplementarySurveyRequestConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyId | SurveyId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyRequestId | SurveyRequestId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| RequestedByUserId | RequestedByUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Reason | Reason : String | nvarchar | False | 2000 | 0/0 | - | Never | - |
| RequestedScope | RequestedScope : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| RoundNo | RoundNo : Int32 | int | False | 4 | 10/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.SupplementarySurveyRequestStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Requested=1, Approved=2, Assigned=3, InProgress=4, Submitted=5, Rejected=6, Cancelled=7 |
| ApprovedByUserId | ApprovedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| ApprovedAt | ApprovedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| SourcePreservationNote | SourcePreservationNote : String | nvarchar | True | -1 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SupplementarySurveyRequests | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_SupplementarySurveyRequests_SurveyRequests_SurveyRequestId | SurveyRequestId -> dbo.SurveyRequests.Id | NO_ACTION | False |
| FK_SupplementarySurveyRequests_Surveys_SurveyId | SurveyId -> dbo.Surveys.Id | NO_ACTION | False |
| FK_SupplementarySurveyRequests_Users_ApprovedByUserId | ApprovedByUserId -> dbo.Users.Id | NO_ACTION | False |
| FK_SupplementarySurveyRequests_Users_RequestedByUserId | RequestedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_SupplementarySurveyRequests_ApprovedByUserId | ApprovedByUserId | False | - | NONCLUSTERED |
| IX_SupplementarySurveyRequests_RequestedByUserId | RequestedByUserId | False | - | NONCLUSTERED |
| IX_SupplementarySurveyRequests_SurveyRequestId | SurveyRequestId | False | - | NONCLUSTERED |
| UX_SupplementarySurveyRequests_SurveyRound | SurveyId, RoundNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SupplementarySurveyRequests_Approval | ([ApprovedByUserId] IS NULL AND [ApprovedAt] IS NULL OR [ApprovedByUserId] IS NOT NULL AND [ApprovedAt] IS NOT NULL); disabled=False |
| CK_SupplementarySurveyRequests_RequestedScope_JsonObject | (isjson([RequestedScope])=(1) AND left(ltrim([RequestedScope]),(1))='{'); disabled=False |
| CK_SupplementarySurveyRequests_RoundNo | ([RoundNo]>(0)); disabled=False |
| CK_SupplementarySurveyRequests_Status | ([Status]=(7) OR [Status]=(6) OR [Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

### dbo.DroneDevices

- Module: survey; CLR mapping: RoadGuardSystem.BusinessObjects.Devices.DroneDevice. Purpose (source-interpreted from entity/configuration, not accepted business policy): Drone inventory and status. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Devices/DroneDevice.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/DroneDeviceConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SerialNo | SerialNo : String | varchar | False | 120 | 0/0 | - | Never | - |
| Model | Model : String | nvarchar | True | 240 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.DroneDeviceStatus | tinyint | False | 1 | 3/0 | - | Never | Active=1, Maintenance=2, Retired=3 |
| ChecklistVersion | ChecklistVersion : String | varchar | True | 50 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_DroneDevices | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_DroneDevices_SerialNo | SerialNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_DroneDevices_Status | ([Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

## files

### dbo.Files

- Module: files; CLR mapping: RoadGuardSystem.BusinessObjects.Files.StoredFile. Purpose (source-interpreted from entity/configuration, not accepted business policy): Stored object identity, size and checksum. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Files/StoredFile.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/StoredFileConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| StorageUri | StorageUri : String | nvarchar | False | 4096 | 0/0 | - | Never | - |
| OriginalName | OriginalName : String | varchar | False | 255 | 0/0 | - | Never | - |
| MimeType | MimeType : String | varchar | False | 120 | 0/0 | - | Never | - |
| SizeBytes | SizeBytes : Int32 | int | False | 4 | 10/0 | - | Never | - |
| Checksum | Checksum : String | char | False | 64 | 0/0 | - | Never | - |
| UploadedByUserId | UploadedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| UploadedAt | UploadedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| RetentionUntil | RetentionUntil : DateOnly? | date | True | 3 | 10/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Files | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Files_Users_UploadedByUserId | UploadedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Files_UploadedByUserId | UploadedByUserId | False | - | NONCLUSTERED |
| UX_Files_StorageUri | StorageUri | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Files_Checksum_Sha256Lowercase | (len([Checksum])=(64) AND NOT ([Checksum]) collate Latin1_General_100_BIN2 like '%[^0-9a-f]%'); disabled=False |
| CK_Files_SizeBytes_NonNegative | ([SizeBytes]>=(0)); disabled=False |
| [TR_Files_Immutable](current-triggers.md#dbotr_files_immutable) | Observed rejection text: Files are immutable; create a new file identity and use the retention workflow for deletion. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260919085118_AddImmutableFileStorageBoundary.cs#L54); OUTER_WHITESPACE_ONLY |

### dbo.FileScopes

- Module: files; CLR mapping: RoadGuardSystem.BusinessObjects.Files.FileScope. Purpose (source-interpreted from entity/configuration, not accepted business policy): Project/purpose/target association of file. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Files/FileScope.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/FileScopeConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| FileId | FileId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| TargetId | TargetId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| OwnerUserId | OwnerUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Purpose | Purpose : String | varchar | False | 40 | 0/0 | - | Never | - |
| CreatedAt | CreatedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_FileScopes | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_FileScopes_Files_FileId | FileId -> dbo.Files.Id | NO_ACTION | False |
| FK_FileScopes_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_FileScopes_Users_OwnerUserId | OwnerUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_FileScopes_OwnerUserId | OwnerUserId | False | - | NONCLUSTERED |
| IX_FileScopes_ProjectId_OwnerUserId | ProjectId, OwnerUserId | False | - | NONCLUSTERED |
| UX_FileScopes_FileId | FileId | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.UploadSessions

- Module: files; CLR mapping: RoadGuardSystem.BusinessObjects.Files.UploadSession. Purpose (source-interpreted from entity/configuration, not accepted business policy): Multipart upload session state. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Files/UploadSession.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/UploadSessionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| FileId | FileId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| OwnerUserId | OwnerUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ObjectKey | ObjectKey : String | varchar | False | 512 | 0/0 | - | Never | - |
| Purpose | Purpose : String | varchar | False | 40 | 0/0 | - | Never | - |
| MediaType | MediaType : String | varchar | False | 120 | 0/0 | - | Never | - |
| ExpectedSizeBytes | ExpectedSizeBytes : Int64 | bigint | False | 8 | 19/0 | - | Never | - |
| ExpectedChecksumSha256 | ExpectedChecksumSha256 : String | char | False | 64 | 0/0 | - | Never | - |
| PartSizeBytes | PartSizeBytes : Int32 | int | False | 4 | 10/0 | - | Never | - |
| ExpiresAt | ExpiresAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.UploadSessionStatus | tinyint | False | 1 | 3/0 | - | Never | Pending=1, Uploading=2, Verifying=3, Verified=4, Failed=5 |
| StorageUploadId | StorageUploadId : String | varchar | True | 1024 | 0/0 | - | Never | - |
| FailureCode | FailureCode : String | varchar | True | 80 | 0/0 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_UploadSessions | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_UploadSessions_Files_FileId | FileId -> dbo.Files.Id | NO_ACTION | False |
| FK_UploadSessions_Users_OwnerUserId | OwnerUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_UploadSessions_FileId | FileId | False | - | NONCLUSTERED |
| IX_UploadSessions_OwnerUserId | OwnerUserId | False | - | NONCLUSTERED |
| IX_UploadSessions_Status_ExpiresAt | Status, ExpiresAt | False | - | NONCLUSTERED |
| UX_UploadSessions_ObjectKey | ObjectKey | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_UploadSessions_ExpectedSizeBytes_Positive | ([ExpectedSizeBytes]>(0)); disabled=False |
| CK_UploadSessions_PartSizeBytes_Positive | ([PartSizeBytes]>(0)); disabled=False |

### dbo.UploadParts

- Module: files; CLR mapping: RoadGuardSystem.BusinessObjects.Files.UploadPart. Purpose (source-interpreted from entity/configuration, not accepted business policy): Upload part numbers and object-store receipt. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Files/UploadPart.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/UploadPartConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| UploadSessionId | UploadSessionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| PartNumber | PartNumber : Int32 | int | False | 4 | 10/0 | - | Never | - |
| ETag | ETag : String | varchar | True | 512 | 0/0 | - | Never | - |
| UrlIssuedAt | UrlIssuedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| UrlExpiresAt | UrlExpiresAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_UploadParts | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_UploadParts_UploadSessions_UploadSessionId | UploadSessionId -> dbo.UploadSessions.Id | CASCADE | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_UploadParts_Session_PartNumber | UploadSessionId, PartNumber | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

## processing

### dbo.ProcessingBlocks

- Module: processing; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.ProcessingBlock. Purpose (source-interpreted from entity/configuration, not accepted business policy): Immutable processing input scope. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/ProcessingBlock.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ProcessingBlockConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyDataVersionId | SurveyDataVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| BlockNo | BlockNo : Int32 | int | False | 4 | 10/0 | - | Never | - |
| RangeMetadata | RangeMetadata : String | nvarchar | False | -1 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_ProcessingBlocks | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_ProcessingBlocks_SurveyDataVersions_SurveyDataVersionId | SurveyDataVersionId -> dbo.SurveyDataVersions.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_ProcessingBlocks_DataVersionBlockNo | SurveyDataVersionId, BlockNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_ProcessingBlocks_BlockNo | ([BlockNo]>(0)); disabled=False |
| CK_ProcessingBlocks_RangeMetadata_JsonObject | (isjson([RangeMetadata])=(1) AND left(ltrim([RangeMetadata]),(1))='{'); disabled=False |
| [TR_ProcessingBlocks_Immutable](current-triggers.md#dbotr_processingblocks_immutable) | Observed rejection text: ProcessingBlocks are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L241); OUTER_WHITESPACE_ONLY |

### dbo.ProcessingJobs

- Module: processing; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.ProcessingJob. Purpose (source-interpreted from entity/configuration, not accepted business policy): Processing job state. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ProcessingJobConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProcessingBlockId | ProcessingBlockId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ModelVersionId | ModelVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.ProcessingJobStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Queued=1, Running=2, RetryableFailure=3, DataFailure=4, Completed=5, Cancelled=6 |
| StartedAt | StartedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| CompletedAt | CompletedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| ErrorCode | ErrorCode : String | varchar | True | 80 | 0/0 | - | Never | - |
| ErrorMessage | ErrorMessage : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| ManifestHash | ManifestHash : String | char | False | 64 | 0/0 | ('') | OnAdd | - |
| Mode | Mode : String | varchar | False | 8 | 0/0 | ('') | OnAdd | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | ('00000000-0000-0000-0000-000000000000') | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |
| ManifestJson | ManifestJson : String | nvarchar | False | -1 | 0/0 | (N'{}') | OnAdd | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_ProcessingJobs | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_ProcessingJobs_AIModelVersions_ModelVersionId | ModelVersionId -> dbo.AIModelVersions.Id | NO_ACTION | False |
| FK_ProcessingJobs_ProcessingBlocks_ProcessingBlockId | ProcessingBlockId -> dbo.ProcessingBlocks.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_ProcessingJobs_Block | ProcessingBlockId | False | - | NONCLUSTERED |
| IX_ProcessingJobs_ModelVersionId | ModelVersionId | False | - | NONCLUSTERED |
| IX_ProcessingJobs_ProjectStatus | ProjectId, Status | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_ProcessingJobs_Status | ([Status]=(6) OR [Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_ProcessingJobs_TimestampOrder | ([CompletedAt] IS NULL OR [StartedAt] IS NULL OR [CompletedAt]>=[StartedAt]); disabled=False |

### dbo.ProcessingAttempts

- Module: processing; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt. Purpose (source-interpreted from entity/configuration, not accepted business policy): Processing retry/attempt history. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/ProcessingAttempt.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ProcessingAttemptConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProcessingJobId | ProcessingJobId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| AttemptNo | AttemptNo : Int32 | int | False | 4 | 10/0 | - | Never | - |
| StartedAt | StartedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| EndedAt | EndedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| ErrorType | ErrorType : RoadGuardSystem.aBusinessObjects.Commons.ProcessingAttemptErrorType? | tinyint | True | 1 | 3/0 | - | Never | Unknown=0, Infrastructure=1, Data=2, None=3 |
| WorkerReference | WorkerReference : String | varchar | True | 120 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_ProcessingAttempts | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_ProcessingAttempts_ProcessingJobs_ProcessingJobId | ProcessingJobId -> dbo.ProcessingJobs.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_ProcessingAttempts_JobId | ProcessingJobId | False | - | NONCLUSTERED |
| UX_ProcessingAttempts_JobAttemptNo | ProcessingJobId, AttemptNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_ProcessingAttempts_AttemptNo | ([AttemptNo]>(0)); disabled=False |
| CK_ProcessingAttempts_ErrorType | ([ErrorType] IS NULL OR ([ErrorType]=(3) OR [ErrorType]=(2) OR [ErrorType]=(1))); disabled=False |
| CK_ProcessingAttempts_TimestampOrder | ([EndedAt] IS NULL OR [EndedAt]>=[StartedAt]); disabled=False |
| [TR_ProcessingAttempts_AppendOnly](current-triggers.md#dbotr_processingattempts_appendonly) | Observed rejection text: ProcessingAttempts are append-only. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L253); OUTER_WHITESPACE_ONLY |

### dbo.AIModelVersions

- Module: processing; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.AIModelVersion. Purpose (source-interpreted from entity/configuration, not accepted business policy): AI model version metadata. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/AIModelVersion.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/AIModelVersionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ModelName | ModelName : String | varchar | False | 120 | 0/0 | - | Never | - |
| VersionLabel | VersionLabel : String | varchar | False | 80 | 0/0 | - | Never | - |
| ArtifactUri | ArtifactUri : String | nvarchar | False | 4096 | 0/0 | - | Never | - |
| Metrics | Metrics : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| OperatingThresholds | OperatingThresholds : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.AIModelVersionStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Draft=1, Released=2, Retired=3 |
| ReleasedAt | ReleasedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| ReleasedByUserId | ReleasedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_AIModelVersions | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_AIModelVersions_Users_ReleasedByUserId | ReleasedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_AIModelVersions_ModelName | ModelName | False | - | NONCLUSTERED |
| IX_AIModelVersions_ReleasedByUserId | ReleasedByUserId | False | - | NONCLUSTERED |
| UX_AIModelVersions_VersionLabel | VersionLabel | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_AIModelVersions_Metrics_Json | ([Metrics] IS NULL OR isjson([Metrics])=(1)); disabled=False |
| CK_AIModelVersions_OperatingThresholds_Json | ([OperatingThresholds] IS NULL OR isjson([OperatingThresholds])=(1)); disabled=False |
| CK_AIModelVersions_ReleaseMetadata | ([ReleasedAt] IS NULL AND [ReleasedByUserId] IS NULL OR [ReleasedAt] IS NOT NULL AND [ReleasedByUserId] IS NOT NULL); disabled=False |
| CK_AIModelVersions_Status | ([Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

### dbo.ValidationRuns

- Module: processing; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.ValidationRun. Purpose (source-interpreted from entity/configuration, not accepted business policy): Validation execution linked to processing. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/ValidationRun.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ValidationRunConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ModelVersionId | ModelVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| DatasetSplitId | DatasetSplitId : String | varchar | False | 120 | 0/0 | - | Never | - |
| MeasurementType | MeasurementType : String | varchar | False | 80 | 0/0 | - | Never | - |
| Unit | Unit : String | varchar | False | 32 | 0/0 | - | Never | - |
| PairsJson | PairsJson : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.ValidationRunStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Queued=1, Completed=2, Failed=3 |
| UsedCount | UsedCount : Int32 | int | False | 4 | 10/0 | - | Never | - |
| ExcludedCount | ExcludedCount : Int32 | int | False | 4 | 10/0 | - | Never | - |
| Bias | Bias : Decimal? | decimal | True | 9 | 18/6 | - | Never | - |
| Mae | Mae : Decimal? | decimal | True | 9 | 18/6 | - | Never | - |
| Rmse | Rmse : Decimal? | decimal | True | 9 | 18/6 | - | Never | - |
| ExclusionReasonsJson | ExclusionReasonsJson : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_ValidationRuns | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_ValidationRuns_AIModelVersions_ModelVersionId | ModelVersionId -> dbo.AIModelVersions.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_ValidationRuns_ModelVersionId | ModelVersionId | False | - | NONCLUSTERED |
| IX_ValidationRuns_ProjectStatus | ProjectId, Status | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_ValidationRuns_ExclusionReasonsJson | (isjson([ExclusionReasonsJson])=(1) AND left(ltrim([ExclusionReasonsJson]),(1))='['); disabled=False |
| CK_ValidationRuns_PairsJson | (isjson([PairsJson])=(1) AND left(ltrim([PairsJson]),(1))='['); disabled=False |
| CK_ValidationRuns_Status | ([Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

## defect-inspection

### dbo.DefectTypes

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Catalogs.DefectType. Purpose (source-interpreted from entity/configuration, not accepted business policy): Defect taxonomy. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Catalogs/DefectType.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/DefectTypeConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Code | Code : String | varchar | False | 80 | 0/0 | - | Never | - |
| Name | Name : String | nvarchar | False | 300 | 0/0 | - | Never | - |
| Description | Description : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| IsActive | IsActive : Boolean | bit | False | 1 | 1/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_DefectTypes | PRIMARY_KEY_CONSTRAINT | Code |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.CauseCategories

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Catalogs.CauseCategory. Purpose (source-interpreted from entity/configuration, not accepted business policy): Cause taxonomy. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Catalogs/CauseCategory.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/CauseCategoryConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Code | Code : String | varchar | False | 80 | 0/0 | - | Never | - |
| Name | Name : String | nvarchar | False | 300 | 0/0 | - | Never | - |
| IsActive | IsActive : Boolean | bit | False | 1 | 1/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_CauseCategories | PRIMARY_KEY_CONSTRAINT | Code |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.SeverityRuleVersions

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Catalogs.SeverityRuleVersion. Purpose (source-interpreted from entity/configuration, not accepted business policy): Severity rule version metadata. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Catalogs/SeverityRuleVersion.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/SeverityRuleVersionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| StandardCode | StandardCode : String | varchar | False | 80 | 0/0 | - | Never | - |
| RoadTypeCode | RoadTypeCode : String | varchar | False | 80 | 0/0 | - | Never | - |
| VersionNo | VersionNo : Int32 | int | False | 4 | 10/0 | - | Never | - |
| RuleDefinition | RuleDefinition : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| EffectiveFrom | EffectiveFrom : DateOnly | date | False | 3 | 10/0 | - | Never | - |
| EffectiveTo | EffectiveTo : DateOnly? | date | True | 3 | 10/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_SeverityRuleVersions | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_SeverityRuleVersions_ScopeVersion | StandardCode, RoadTypeCode, VersionNo | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_SeverityRuleVersions_EffectiveDateRange | ([EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]); disabled=False |
| CK_SeverityRuleVersions_RuleDefinition_JsonObject | (isjson([RuleDefinition])=(1) AND left(ltrim([RuleDefinition]),(1))='{'); disabled=False |
| CK_SeverityRuleVersions_VersionNo_Positive | ([VersionNo]>(0)); disabled=False |

### dbo.Defects

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Defects.Defect. Purpose (source-interpreted from entity/configuration, not accepted business policy): Verified or tracked defect records. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Defects/Defect.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/DefectConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| DefectTypeCode | DefectTypeCode : String | varchar | False | 80 | 0/0 | - | Never | - |
| CauseCategoryCode | CauseCategoryCode : String | varchar | True | 80 | 0/0 | - | Never | - |
| Geometry | Geometry : Geometry | geometry | True | -1 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| ReportedAt | ReportedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Severity | Severity : RoadGuardSystem.aBusinessObjects.Commons.DefectSeverity | tinyint | False | 1 | 3/0 | (CONVERT([tinyint],(0))) | Never | Unknown=0, Low=1, Medium=2, High=3, Critical=4 |
| SourceAIDetectionId | SourceAIDetectionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.DefectStatus | tinyint | False | 1 | 3/0 | (CONVERT([tinyint],(0))) | Never | Unknown=0, Open=1, Verified=2, Rejected=3, Resolved=4 |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Defects | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Defects_AIDetections_SourceAIDetectionId | SourceAIDetectionId -> dbo.AIDetections.Id | NO_ACTION | False |
| FK_Defects_CauseCategories_CauseCategoryCode | CauseCategoryCode -> dbo.CauseCategories.Code | NO_ACTION | False |
| FK_Defects_DefectTypes_DefectTypeCode | DefectTypeCode -> dbo.DefectTypes.Code | NO_ACTION | False |
| FK_Defects_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_Defects_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Defects_CauseCategoryCode | CauseCategoryCode | False | - | NONCLUSTERED |
| IX_Defects_DefectTypeCode | DefectTypeCode | False | - | NONCLUSTERED |
| IX_Defects_ProjectId | ProjectId | False | - | NONCLUSTERED |
| IX_Defects_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |
| UX_Defects_SourceAIDetectionId | SourceAIDetectionId | True | ([SourceAIDetectionId] IS NOT NULL) | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_Defects_Severity | ([Severity] IS NULL OR ([Severity]=(4) OR [Severity]=(3) OR [Severity]=(2) OR [Severity]=(1) OR [Severity]=(0))); disabled=False |
| CK_Defects_Status | ([Status] IS NULL OR ([Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1) OR [Status]=(0))); disabled=False |

### dbo.AIDetections

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.AIDetection. Purpose (source-interpreted from entity/configuration, not accepted business policy): AI detection candidates. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/AIDetection.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/AIDetectionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProcessingJobId | ProcessingJobId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ModelVersionId | ModelVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Geometry | Geometry : Geometry | geometry | True | -1 | 0/0 | - | Never | - |
| DefectTypeCode | DefectTypeCode : String | varchar | True | 80 | 0/0 | - | Never | - |
| Confidence | Confidence : Decimal | decimal | False | 5 | 6/5 | - | Never | - |
| EstimatedWidth | EstimatedWidth : Decimal? | decimal | True | 9 | 12/3 | - | Never | - |
| EstimatedLength | EstimatedLength : Decimal? | decimal | True | 9 | 12/3 | - | Never | - |
| RawPayload | RawPayload : String | nvarchar | False | -1 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_AIDetections | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_AIDetections_AIModelVersions_ModelVersionId | ModelVersionId -> dbo.AIModelVersions.Id | NO_ACTION | False |
| FK_AIDetections_DefectTypes_DefectTypeCode | DefectTypeCode -> dbo.DefectTypes.Code | NO_ACTION | False |
| FK_AIDetections_ProcessingJobs_ProcessingJobId | ProcessingJobId -> dbo.ProcessingJobs.Id | NO_ACTION | False |
| FK_AIDetections_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_AIDetections_DefectTypeCode | DefectTypeCode | False | - | NONCLUSTERED |
| IX_AIDetections_ModelVersionId | ModelVersionId | False | - | NONCLUSTERED |
| IX_AIDetections_ProcessingJobId | ProcessingJobId | False | - | NONCLUSTERED |
| IX_AIDetections_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_AIDetections_Confidence | ([Confidence]>=(0) AND [Confidence]<=(1)); disabled=False |
| CK_AIDetections_EstimatedDimensions | (([EstimatedWidth] IS NULL OR [EstimatedWidth]>=(0)) AND ([EstimatedLength] IS NULL OR [EstimatedLength]>=(0))); disabled=False |
| CK_AIDetections_RawPayload_Json | (isjson([RawPayload])=(1)); disabled=False |
| [TR_AIDetections_Immutable](current-triggers.md#dbotr_aidetections_immutable) | Observed rejection text: AIDetections are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L360); OUTER_WHITESPACE_ONLY |

### dbo.DefectVerificationLogs

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog. Purpose (source-interpreted from entity/configuration, not accepted business policy): Defect/detection review history. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Defects/DefectVerificationLog.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/DefectVerificationLogConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| DefectId | DefectId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| AIDetectionId | AIDetectionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Action | Action : RoadGuardSystem.aBusinessObjects.Commons.DefectVerificationAction | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, PreliminaryKeep=1, Adjust=2, Confirm=3, Reject=4, Merge=5 |
| BeforeSnapshot | BeforeSnapshot : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| AfterSnapshot | AfterSnapshot : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| SeverityRuleVersionId | SeverityRuleVersionId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| FieldInspectionTaskId | FieldInspectionTaskId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| VerifiedByUserId | VerifiedByUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Reason | Reason : String | nvarchar | False | 2000 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_DefectVerificationLogs | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_DefectVerificationLogs_AIDetections_AIDetectionId | AIDetectionId -> dbo.AIDetections.Id | NO_ACTION | False |
| FK_DefectVerificationLogs_Defects_DefectId | DefectId -> dbo.Defects.Id | NO_ACTION | False |
| FK_DefectVerificationLogs_FieldInspectionTasks_FieldInspectionTaskId | FieldInspectionTaskId -> dbo.FieldInspectionTasks.Id | NO_ACTION | False |
| FK_DefectVerificationLogs_SeverityRuleVersions_SeverityRuleVersionId | SeverityRuleVersionId -> dbo.SeverityRuleVersions.Id | NO_ACTION | False |
| FK_DefectVerificationLogs_Users_VerifiedByUserId | VerifiedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_DefectVerificationLogs_AIDetectionId | AIDetectionId | False | - | NONCLUSTERED |
| IX_DefectVerificationLogs_DefectId | DefectId | False | - | NONCLUSTERED |
| IX_DefectVerificationLogs_FieldInspectionTaskId | FieldInspectionTaskId | False | - | NONCLUSTERED |
| IX_DefectVerificationLogs_SeverityRuleVersionId | SeverityRuleVersionId | False | - | NONCLUSTERED |
| IX_DefectVerificationLogs_VerifiedByUserId | VerifiedByUserId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_DefectVerificationLogs_Action | ([Action]=(5) OR [Action]=(4) OR [Action]=(3) OR [Action]=(2) OR [Action]=(1)); disabled=False |
| CK_DefectVerificationLogs_AfterSnapshot_Json | ([AfterSnapshot] IS NULL OR isjson([AfterSnapshot])=(1)); disabled=False |
| CK_DefectVerificationLogs_BeforeSnapshot_Json | ([BeforeSnapshot] IS NULL OR isjson([BeforeSnapshot])=(1)); disabled=False |
| CK_DefectVerificationLogs_ExactlyOneTarget | ([DefectId] IS NOT NULL AND [AIDetectionId] IS NULL OR [DefectId] IS NULL AND [AIDetectionId] IS NOT NULL); disabled=False |
| [TR_DefectVerificationLogs_AppendOnly](current-triggers.md#dbotr_defectverificationlogs_appendonly) | Observed rejection text: DefectVerificationLogs are append-only. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L372); OUTER_WHITESPACE_ONLY |

### dbo.FieldInspectionTasks

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask. Purpose (source-interpreted from entity/configuration, not accepted business policy): Field inspection work items. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/FieldInspectionTaskConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| TaskCode | TaskCode : String | nvarchar | False | 160 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| DefectId | DefectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyId | SurveyId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RequiredMeasurementType | RequiredMeasurementType : Byte | tinyint | False | 1 | 3/0 | - | Never | - |
| MeasurementScope | MeasurementScope : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| Instructions | Instructions : String | nvarchar | True | 2000 | 0/0 | - | Never | - |
| MissingInformation | MissingInformation : String | nvarchar | True | 2000 | 0/0 | - | Never | - |
| DueAt | DueAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionTaskStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, NewAssigned=1, Accepted=2, Rejected=3, InProgress=4, SupplementRequired=5, Submitted=6, Completed=7 |
| AssignedByUserId | AssignedByUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ReviewDecision | ReviewDecision : RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionReviewDecision? | tinyint | True | 1 | 3/0 | - | Never | Unknown=0, DefectConfirmed=1, NoDefect=2 |
| ReviewedByUserId | ReviewedByUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| ReviewedAt | ReviewedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| ReviewReason | ReviewReason : String | nvarchar | True | 2000 | 0/0 | - | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_FieldInspectionTasks | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_FieldInspectionTasks_Defects_DefectId | DefectId -> dbo.Defects.Id | NO_ACTION | False |
| FK_FieldInspectionTasks_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_FieldInspectionTasks_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_FieldInspectionTasks_Surveys_SurveyId | SurveyId -> dbo.Surveys.Id | NO_ACTION | False |
| FK_FieldInspectionTasks_Users_AssignedByUserId | AssignedByUserId -> dbo.Users.Id | NO_ACTION | False |
| FK_FieldInspectionTasks_Users_ReviewedByUserId | ReviewedByUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_FieldInspectionTasks_AssignedByUserId | AssignedByUserId | False | - | NONCLUSTERED |
| IX_FieldInspectionTasks_DefectId | DefectId | False | - | NONCLUSTERED |
| IX_FieldInspectionTasks_ProjectId | ProjectId | False | - | NONCLUSTERED |
| IX_FieldInspectionTasks_ReviewedByUserId | ReviewedByUserId | False | - | NONCLUSTERED |
| IX_FieldInspectionTasks_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |
| IX_FieldInspectionTasks_SurveyId | SurveyId | False | - | NONCLUSTERED |
| UX_FieldInspectionTasks_TaskCode | TaskCode | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_FieldInspectionTasks_MeasurementScope_Json | (isjson([MeasurementScope])=(1) AND left(ltrim([MeasurementScope]),(1))='{'); disabled=False |
| CK_FieldInspectionTasks_ReviewDecision | ([ReviewDecision] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewedAt] IS NULL OR [ReviewDecision] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewedAt] IS NOT NULL AND [Status]=(7)); disabled=False |
| CK_FieldInspectionTasks_Status | ([Status]=(7) OR [Status]=(6) OR [Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |

### dbo.FieldInspectionAssignments

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment. Purpose (source-interpreted from entity/configuration, not accepted business policy): Inspector assignment records. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionAssignment.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/FieldInspectionAssignmentConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| FieldInspectionTaskId | FieldInspectionTaskId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| AssignedToUserId | AssignedToUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| AssignedByUserId | AssignedByUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| AssignedAt | AssignedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| EndedAt | EndedAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionAssignmentStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Active=1, Rejected=2, Ended=3 |
| Reason | Reason : String | nvarchar | True | 2000 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_FieldInspectionAssignments | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_FieldInspectionAssignments_FieldInspectionTasks_FieldInspectionTaskId | FieldInspectionTaskId -> dbo.FieldInspectionTasks.Id | NO_ACTION | False |
| FK_FieldInspectionAssignments_Users_AssignedByUserId | AssignedByUserId -> dbo.Users.Id | NO_ACTION | False |
| FK_FieldInspectionAssignments_Users_AssignedToUserId | AssignedToUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_FieldInspectionAssignments_AssignedByUserId | AssignedByUserId | False | - | NONCLUSTERED |
| IX_FieldInspectionAssignments_AssignedToUserId | AssignedToUserId | False | - | NONCLUSTERED |
| UX_FieldInspectionAssignments_ActiveTask | FieldInspectionTaskId | True | ([Status]=(1)) | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_FieldInspectionAssignments_State | ([Status]=(1) AND [EndedAt] IS NULL AND [Reason] IS NULL OR ([Status]=(3) OR [Status]=(2)) AND [EndedAt] IS NOT NULL AND len(ltrim(rtrim([Reason])))>(0)); disabled=False |
| CK_FieldInspectionAssignments_Status | ([Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_FieldInspectionAssignments_TimestampOrder | ([EndedAt] IS NULL OR [EndedAt]>=[AssignedAt]); disabled=False |

### dbo.FieldInspectionSessions

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession. Purpose (source-interpreted from entity/configuration, not accepted business policy): Inspector field sessions. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionSession.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/FieldInspectionSessionConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| Purpose | Purpose : RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionPurpose | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, DefectVerification=1, ResearchValidation=2 |
| FieldInspectionTaskId | FieldInspectionTaskId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyId | SurveyId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| SessionCode | SessionCode : String | nvarchar | False | 160 | 0/0 | - | Never | - |
| InspectorUserId | InspectorUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| InspectorName | InspectorName : String | nvarchar | False | 400 | 0/0 | - | Never | - |
| ConductedAt | ConductedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| WeatherCondition | WeatherCondition : String | nvarchar | True | 200 | 0/0 | - | Never | - |
| Method | Method : String | nvarchar | False | 400 | 0/0 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionSessionStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Draft=1, Completed=2, Imported=3, Locked=4 |
| EvidenceFileId | EvidenceFileId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_FieldInspectionSessions | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_FieldInspectionSessions_FieldInspectionTasks_FieldInspectionTaskId | FieldInspectionTaskId -> dbo.FieldInspectionTasks.Id | NO_ACTION | False |
| FK_FieldInspectionSessions_Files_EvidenceFileId | EvidenceFileId -> dbo.Files.Id | NO_ACTION | False |
| FK_FieldInspectionSessions_Projects_ProjectId | ProjectId -> dbo.Projects.Id | NO_ACTION | False |
| FK_FieldInspectionSessions_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_FieldInspectionSessions_Surveys_SurveyId | SurveyId -> dbo.Surveys.Id | NO_ACTION | False |
| FK_FieldInspectionSessions_Users_InspectorUserId | InspectorUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_FieldInspectionSessions_EvidenceFileId | EvidenceFileId | False | - | NONCLUSTERED |
| IX_FieldInspectionSessions_FieldInspectionTaskId | FieldInspectionTaskId | False | - | NONCLUSTERED |
| IX_FieldInspectionSessions_InspectorUserId | InspectorUserId | False | - | NONCLUSTERED |
| IX_FieldInspectionSessions_ProjectVersion | ProjectId, RoadSectionVersionId | False | - | NONCLUSTERED |
| IX_FieldInspectionSessions_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |
| IX_FieldInspectionSessions_SurveyId | SurveyId | False | - | NONCLUSTERED |
| UX_FieldInspectionSessions_SessionCode | SessionCode | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_FieldInspectionSessions_Purpose | ([Purpose]=(2) OR [Purpose]=(1)); disabled=False |
| CK_FieldInspectionSessions_PurposeScope | ([Purpose]=(1) AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL OR [Purpose]=(2) AND [FieldInspectionTaskId] IS NULL); disabled=False |
| CK_FieldInspectionSessions_Status | ([Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| [TR_FieldInspectionSessions_Immutable](current-triggers.md#dbotr_fieldinspectionsessions_immutable) | Observed rejection text: Completed, imported, or locked field inspection sessions are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L295); OUTER_WHITESPACE_ONLY |
| [TR_FieldInspectionSessions_Integrity](current-triggers.md#dbotr_fieldinspectionsessions_integrity) | Observed rejection text: Field inspection session scope or active assignment is invalid. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L260); OUTER_WHITESPACE_ONLY |

### dbo.GroundTruthMeasurements

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement. Purpose (source-interpreted from entity/configuration, not accepted business policy): Field measurement evidence. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Inspections/GroundTruthMeasurement.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/GroundTruthMeasurementConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| FieldInspectionSessionId | FieldInspectionSessionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SampleId | SampleId : String | nvarchar | False | 200 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyId | SurveyId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| DefectId | DefectId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| MeasurementType | MeasurementType : RoadGuardSystem.aBusinessObjects.Commons.MeasurementType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, DepressionDepth=1, SlabFaultingHeight=2, ShoulderErosionExtent=3 |
| Value | Value : Decimal | decimal | False | 9 | 19/6 | - | Never | - |
| Unit | Unit : String | nvarchar | False | 40 | 0/0 | - | Never | - |
| Location | Location : Point | geography | False | -1 | 0/0 | - | Never | - |
| InstrumentName | InstrumentName : String | nvarchar | False | 300 | 0/0 | - | Never | - |
| InstrumentReference | InstrumentReference : String | nvarchar | True | 300 | 0/0 | - | Never | - |
| MeasurementMethod | MeasurementMethod : String | nvarchar | False | 1000 | 0/0 | - | Never | - |
| MeasuredBy | MeasuredBy : String | nvarchar | False | 400 | 0/0 | - | Never | - |
| MeasuredAt | MeasuredAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| EvidenceFileId | EvidenceFileId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Notes | Notes : String | nvarchar | True | -1 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_GroundTruthMeasurements | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_GroundTruthMeasurements_Defects_DefectId | DefectId -> dbo.Defects.Id | NO_ACTION | False |
| FK_GroundTruthMeasurements_FieldInspectionSessions_FieldInspectionSessionId | FieldInspectionSessionId -> dbo.FieldInspectionSessions.Id | NO_ACTION | False |
| FK_GroundTruthMeasurements_Files_EvidenceFileId | EvidenceFileId -> dbo.Files.Id | NO_ACTION | False |
| FK_GroundTruthMeasurements_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_GroundTruthMeasurements_Surveys_SurveyId | SurveyId -> dbo.Surveys.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_GroundTruthMeasurements_DefectId | DefectId | False | - | NONCLUSTERED |
| IX_GroundTruthMeasurements_EvidenceFileId | EvidenceFileId | False | - | NONCLUSTERED |
| IX_GroundTruthMeasurements_RoadVersionType | RoadSectionVersionId, MeasurementType | False | - | NONCLUSTERED |
| IX_GroundTruthMeasurements_SurveyId | SurveyId | False | - | NONCLUSTERED |
| UX_GroundTruthMeasurements_SessionSample | FieldInspectionSessionId, SampleId | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_GroundTruthMeasurements_EvidenceOrReason | ([EvidenceFileId] IS NOT NULL OR len(ltrim(rtrim([Notes])))>(0)); disabled=False |
| CK_GroundTruthMeasurements_Location | ([Location].[STSrid]=(4326) AND [Location].[STIsEmpty]()=(0)); disabled=False |
| CK_GroundTruthMeasurements_MeasurementType | ([MeasurementType]=(3) OR [MeasurementType]=(2) OR [MeasurementType]=(1)); disabled=False |
| CK_GroundTruthMeasurements_Unit | (lower([Unit])='m' OR lower([Unit])='cm' OR lower([Unit])='mm'); disabled=False |
| CK_GroundTruthMeasurements_Value | ([Value]>=(0)); disabled=False |
| [TR_GroundTruthMeasurements_Immutable](current-triggers.md#dbotr_groundtruthmeasurements_immutable) | Observed rejection text: Submitted ground truth measurements are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L351); OUTER_WHITESPACE_ONLY |
| [TR_GroundTruthMeasurements_Integrity](current-triggers.md#dbotr_groundtruthmeasurements_integrity) | Observed rejection text: Ground truth measurement purpose or scope is invalid. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L315); OUTER_WHITESPACE_ONLY |

### dbo.DerivedMeasurements

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement. Purpose (source-interpreted from entity/configuration, not accepted business policy): Derived measurement values. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/DerivedMeasurement.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/DerivedMeasurementConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SurveyDataVersionId | SurveyDataVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RoadSectionVersionId | RoadSectionVersionId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SampleId | SampleId : String | nvarchar | False | 200 | 0/0 | - | Never | - |
| MeasurementType | MeasurementType : RoadGuardSystem.aBusinessObjects.Commons.MeasurementType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, DepressionDepth=1, SlabFaultingHeight=2, ShoulderErosionExtent=3 |
| Value | Value : Decimal | decimal | False | 9 | 19/6 | - | Never | - |
| Unit | Unit : String | nvarchar | False | 40 | 0/0 | - | Never | - |
| UncertaintyEstimate | UncertaintyEstimate : Decimal? | decimal | True | 9 | 19/6 | - | Never | - |
| SourceType | SourceType : RoadGuardSystem.aBusinessObjects.Commons.DerivedMeasurementSourceType | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, SurfaceModel=1, Dsm=2, ManualDerived=3, Other=4 |
| AlgorithmVersion | AlgorithmVersion : String | nvarchar | True | 200 | 0/0 | - | Never | - |
| ComputedAt | ComputedAt : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| Status | Status : RoadGuardSystem.aBusinessObjects.Commons.DerivedMeasurementStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Draft=1, Published=2, Superseded=3 |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_DerivedMeasurements | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_DerivedMeasurements_RoadSectionVersions_RoadSectionVersionId | RoadSectionVersionId -> dbo.RoadSectionVersions.Id | NO_ACTION | False |
| FK_DerivedMeasurements_SurveyDataVersions_SurveyDataVersionId | SurveyDataVersionId -> dbo.SurveyDataVersions.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_DerivedMeasurements_DataSampleTypeStatus | SurveyDataVersionId, SampleId, MeasurementType, Status | False | - | NONCLUSTERED |
| IX_DerivedMeasurements_RoadSectionVersionId | RoadSectionVersionId | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_DerivedMeasurements_MeasurementType | ([MeasurementType]=(3) OR [MeasurementType]=(2) OR [MeasurementType]=(1)); disabled=False |
| CK_DerivedMeasurements_SourceType | ([SourceType]=(4) OR [SourceType]=(3) OR [SourceType]=(2) OR [SourceType]=(1)); disabled=False |
| CK_DerivedMeasurements_Status | ([Status]=(3) OR [Status]=(2) OR [Status]=(1)); disabled=False |
| CK_DerivedMeasurements_UncertaintyEstimate | ([UncertaintyEstimate] IS NULL OR [UncertaintyEstimate]>=(0)); disabled=False |
| CK_DerivedMeasurements_Unit | (lower([Unit])='m' OR lower([Unit])='cm' OR lower([Unit])='mm'); disabled=False |
| CK_DerivedMeasurements_Value | ([Value]>=(0)); disabled=False |

### dbo.MeasurementValidationSamples

- Module: defect-inspection; CLR mapping: RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample. Purpose (source-interpreted from entity/configuration, not accepted business policy): Comparison samples for measurement validation. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Processing/MeasurementValidationSample.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/MeasurementValidationSampleConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ValidationRunId | ValidationRunId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| GroundTruthMeasurementId | GroundTruthMeasurementId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| DerivedMeasurementId | DerivedMeasurementId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SignedError | SignedError : Decimal | decimal | False | 9 | 19/6 | - | Never | - |
| AbsoluteError | AbsoluteError : Decimal | decimal | False | 9 | 19/6 | - | Never | - |
| InclusionStatus | InclusionStatus : RoadGuardSystem.aBusinessObjects.Commons.ValidationSampleInclusionStatus | tinyint | False | 1 | 3/0 | - | Never | Unknown=0, Included=1, Excluded=2, Outlier=3 |
| ExclusionReason | ExclusionReason : String | nvarchar | True | 1000 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_MeasurementValidationSamples | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_MeasurementValidationSamples_DerivedMeasurements_DerivedMeasurementId | DerivedMeasurementId -> dbo.DerivedMeasurements.Id | NO_ACTION | False |
| FK_MeasurementValidationSamples_GroundTruthMeasurements_GroundTruthMeasurementId | GroundTruthMeasurementId -> dbo.GroundTruthMeasurements.Id | NO_ACTION | False |
| FK_MeasurementValidationSamples_ValidationRuns_ValidationRunId | ValidationRunId -> dbo.ValidationRuns.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_MeasurementValidationSamples_DerivedMeasurementId | DerivedMeasurementId | False | - | NONCLUSTERED |
| IX_MeasurementValidationSamples_GroundTruthMeasurementId | GroundTruthMeasurementId | False | - | NONCLUSTERED |
| UX_MeasurementValidationSamples_RunPair | ValidationRunId, GroundTruthMeasurementId, DerivedMeasurementId | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_MeasurementValidationSamples_InclusionStatus | ([InclusionStatus]=(3) OR [InclusionStatus]=(2) OR [InclusionStatus]=(1)); disabled=False |
| CK_MeasurementValidationSamples_Reason | ([InclusionStatus]=(1) OR len(ltrim(rtrim([ExclusionReason])))>(0)); disabled=False |

## messaging

### dbo.AuditLogs

- Module: messaging; CLR mapping: RoadGuardSystem.BusinessObjects.Auditing.AuditLog. Purpose (source-interpreted from entity/configuration, not accepted business policy): Audit event history. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Auditing/AuditLog.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/AuditLogConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| ActorUserId | ActorUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| OccurredAtUtc | OccurredAtUtc : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| EventType | EventType : String | varchar | False | 100 | 0/0 | - | Never | - |
| EntityType | EntityType : String | varchar | False | 100 | 0/0 | - | Never | - |
| EntityId | EntityId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| BeforeSnapshot | BeforeSnapshot : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| AfterSnapshot | AfterSnapshot : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| Reason | Reason : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| Source | Source : String | varchar | False | 80 | 0/0 | - | Never | - |
| CorrelationId | CorrelationId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_AuditLogs | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_AuditLogs_Users_ActorUserId | ActorUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_AuditLogs_ActorUserId | ActorUserId | False | - | NONCLUSTERED |
| IX_AuditLogs_CorrelationId | CorrelationId | False | - | NONCLUSTERED |
| IX_AuditLogs_Entity | EntityType, EntityId | False | - | NONCLUSTERED |
| IX_AuditLogs_OccurredAtUtc | OccurredAtUtc | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_AuditLogs_AfterSnapshot_Json | ([AfterSnapshot] IS NULL OR isjson([AfterSnapshot])=(1)); disabled=False |
| CK_AuditLogs_BeforeSnapshot_Json | ([BeforeSnapshot] IS NULL OR isjson([BeforeSnapshot])=(1)); disabled=False |
| [TR_AuditLogs_AppendOnly](current-triggers.md#dbotr_auditlogs_appendonly) | Observed rejection text: AuditLogs are append-only; corrections require a new audit event. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives.cs#L138); OUTER_WHITESPACE_ONLY |

### dbo.IdempotencyRecords

- Module: messaging; CLR mapping: RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord. Purpose (source-interpreted from entity/configuration, not accepted business policy): Durable request replay keys and outcomes. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/IdempotencyRecordConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| ActorUserId | ActorUserId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| ProjectId | ProjectId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| Operation | Operation : String | varchar | False | 100 | 0/0 | - | Never | - |
| IdempotencyKey | IdempotencyKey : String | varchar | False | 200 | 0/0 | - | Never | - |
| RequestFingerprint | RequestFingerprint : String | char | False | 64 | 0/0 | - | Never | - |
| OperationId | OperationId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| OutcomeJson | OutcomeJson : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| CreatedAtUtc | CreatedAtUtc : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_IdempotencyRecords | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_IdempotencyRecords_OperationId | OperationId | False | - | NONCLUSTERED |
| UX_IdempotencyRecords_ScopeKey | ActorUserId, ProjectId, Operation, IdempotencyKey | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_IdempotencyRecords_OutcomeJson_Json | (isjson([OutcomeJson])=(1)); disabled=False |

### dbo.OutboxMessages

- Module: messaging; CLR mapping: RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage. Purpose (source-interpreted from entity/configuration, not accepted business policy): Durable dispatch intents. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/OutboxMessageConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| MessageType | MessageType : String | varchar | False | 200 | 0/0 | - | Never | - |
| OccurredAtUtc | OccurredAtUtc : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |
| CorrelationId | CorrelationId : Guid? | uniqueidentifier | True | 16 | 0/0 | - | Never | - |
| PayloadJson | PayloadJson : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| DeliveryAttemptCount | DeliveryAttemptCount : Int32 | int | False | 4 | 10/0 | ((0)) | Never | - |
| DeliveryStatus | DeliveryStatus : RoadGuardSystem.aBusinessObjects.Commons.OutboxDeliveryStatus | tinyint | False | 1 | 3/0 | (CONVERT([tinyint],(1))) | Never | Pending=1, Leased=2, Completed=3, DeadLetter=4 |
| LastErrorCode | LastErrorCode : String | varchar | True | 80 | 0/0 | - | Never | - |
| LastErrorMessage | LastErrorMessage : String | nvarchar | True | -1 | 0/0 | - | Never | - |
| LeaseExpiresAtUtc | LeaseExpiresAtUtc : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| LeaseOwner | LeaseOwner : String | varchar | True | 120 | 0/0 | - | Never | - |
| NextAttemptAtUtc | NextAttemptAtUtc : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | (sysutcdatetime()) | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_OutboxMessages | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_OutboxMessages_CorrelationId | CorrelationId | False | - | NONCLUSTERED |
| IX_OutboxMessages_DeliveryStatus_NextAttempt | DeliveryStatus, NextAttemptAtUtc | False | - | NONCLUSTERED |
| IX_OutboxMessages_OccurredAtUtc | OccurredAtUtc | False | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |
| CK_OutboxMessages_DeliveryAttemptCount | ([DeliveryAttemptCount]>=(0)); disabled=False |
| CK_OutboxMessages_DeliveryStatus | ([DeliveryStatus]=(4) OR [DeliveryStatus]=(3) OR [DeliveryStatus]=(2) OR [DeliveryStatus]=(1)); disabled=False |
| CK_OutboxMessages_PayloadJson_Json | (isjson([PayloadJson])=(1)); disabled=False |

### dbo.ConsumerEffectReceipts

- Module: messaging; CLR mapping: RoadGuardSystem.BusinessObjects.Messaging.ConsumerEffectReceipt. Purpose (source-interpreted from entity/configuration, not accepted business policy): Consumer effect deduplication receipts. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Messaging/ConsumerEffectReceipt.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/ConsumerEffectReceiptConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | OnAdd | - |
| MessageId | MessageId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ConsumerName | ConsumerName : String | varchar | False | 100 | 0/0 | - | Never | - |
| EffectId | EffectId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| ProcessedAtUtc | ProcessedAtUtc : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | - | Never | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_ConsumerEffectReceipts | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_ConsumerEffectReceipts_OutboxMessages_MessageId | MessageId -> dbo.OutboxMessages.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| UX_ConsumerEffectReceipts_MessageConsumer | MessageId, ConsumerName | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

### dbo.Notifications

- Module: messaging; CLR mapping: RoadGuardSystem.BusinessObjects.Messaging.Notification. Purpose (source-interpreted from entity/configuration, not accepted business policy): User notification state. Detailed field semantics remain UNKNOWN unless independently sourced.
- Source: [entity](../../../RoadGuardSystem.BusinessObjects/Messaging/Notification.cs), [EF configuration](../../../RoadGuardSystem.Repositories/Configurations/NotificationConfiguration.cs), SQL catalog in inventory and migration chain. Query filter: none in EF model.

Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):

| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Id | Id : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| RecipientUserId | RecipientUserId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| SourceEntityType | SourceEntityType : String | varchar | False | 80 | 0/0 | - | Never | - |
| SourceEntityId | SourceEntityId : Guid | uniqueidentifier | False | 16 | 0/0 | - | Never | - |
| EventType | EventType : String | varchar | False | 80 | 0/0 | - | Never | - |
| Title | Title : String | nvarchar | False | 400 | 0/0 | - | Never | - |
| Body | Body : String | nvarchar | False | -1 | 0/0 | - | Never | - |
| ReadAt | ReadAt : DateTimeOffset? | datetimeoffset | True | 10 | 34/7 | - | Never | - |
| OccurredAtUtc | OccurredAtUtc : DateTimeOffset | datetimeoffset | False | 10 | 34/7 | (sysutcdatetime()) | Never | - |
| RowVersion | RowVersion : Byte[] | timestamp | False | 8 | 0/0 | - | OnAddOrUpdate; concurrency | - |

Keys (ordered columns):

| Constraint | Kind | Columns in order |
| --- | --- | --- |
| PK_Notifications | PRIMARY_KEY_CONSTRAINT | Id |

Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):

| FK | Child columns -> principal | Delete | Disabled |
| --- | --- | --- | --- |
| FK_Notifications_Users_RecipientUserId | RecipientUserId -> dbo.Users.Id | NO_ACTION | False |

Indexes (ordered key columns followed by included columns):

| Index | Key / included | Unique | Filter | Kind |
| --- | --- | --- | --- | --- |
| IX_Notifications_RecipientOccurredAtId | RecipientUserId, OccurredAtUtc, Id | False | - | NONCLUSTERED |
| IX_Notifications_RecipientUserId_ReadAt | RecipientUserId, ReadAt | False | - | NONCLUSTERED |
| UX_Notifications_RecipientSourceEvent | RecipientUserId, SourceEntityType, SourceEntityId, EventType | True | - | NONCLUSTERED |

Check constraints and migration SQL triggers:

| Object | Definition / state |
| --- | --- |

