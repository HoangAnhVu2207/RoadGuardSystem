# 05 Migrations and Schema Baseline

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Current migration assembly has exactly two migrations:

1.20261008171739_BaselineCurrentSchema.
2.20261009043010_AllowVerifiedDefectPostRepairTasks.

RoadGuardDbContextModelSnapshot is the current EF snapshot. Baseline replacement is for NEW isolated databases under the assigned baseline decision; it is not authorization to replay migration history on an existing team/production database. __EFMigrationsHistory is infrastructure, not one of191 application tables. Applied history is not rewritten in this task.

Baseline migration creates tables, constraints, indexes and source trigger definitions; additive migration changes the legitimate Reporter-source/verified Defect FIELD relationship guard. Preserve source provenance and constraints when designing a later additive change. No fake Survey/Defect row is a valid migration workaround.

Read current source files under RoadGuardSystem.Repositories/Migrations, the full snapshot, configuration assembly and SQL source inventory. Existing design-time factory and seeding tooling are inventory items, never run as task setup. No migration apply/seed/backfill/downgrade was performed in BE-DOC-02.

Historical fixture model/snapshot/SQL tests from DOC-AUDIT-01 have unchanged inputs but are not rerun/live deployment proof. New local model metadata revalidates table/property/constraint names without connecting SQL. EF warns about FieldInspectionPurpose default sentinel: current generated default can select database default for CLR Unknown; this is recorded as an observed model warning, not a reproduced wrong business transition.
