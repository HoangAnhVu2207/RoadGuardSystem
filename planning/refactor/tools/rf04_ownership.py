"""Explicit, reviewable RF-04 draft ownership; these are proposals, not assignments."""

TASK_OWNER = {
    "RF-10-01": "A", "RF-10-02": "A", "RF-10-03": "B",
    "RF-10-04": "B", "RF-10-05": "B", "RF-10-06": "A",
    "RF-10-07": "A", "RF-10-08": "B", "RF-10-09-A": "A",
    "RF-10-09-B": "B", "RF-05": "A", "RF-06": "A", "RF-08": "A", "RF-09": "A", "RF-11": "A",
}

CURRENT_CONTROLLER_TASK = {
    "AuthController": "RF-10-01", "ReporterRegistrationsController": "RF-10-01",
    "InvitationsController": "RF-10-01", "ProfileController": "RF-10-01",
    "MeController": "RF-10-01", "AdminUsersController": "RF-10-01",
    "UsersController": "RF-10-01", "ProjectsController": "RF-10-02",
    "ProjectRoadSectionsController": "RF-10-02",
    "ProjectWarrantiesController": "RF-10-02",
    "ProjectWorkPackagesController": "RF-10-02",
    "InspectionTasksController": "RF-10-07",
    "SurveyPlanningController": "RF-10-03", "SurveyV2Controller": "RF-10-03",
    "UploadsController": "RF-10-04", "ProcessingV2Controller": "RF-10-05",
    "NotificationsController": "RF-10-09-A",
}

TASK_REQUIREMENTS = {
    "RF-10-01": "R01-R03", "RF-10-02": "R04-R05",
    "RF-10-03": "R06-R07", "RF-10-04": "R13",
    "RF-10-05": "R14", "RF-10-06": "R08-R09",
    "RF-10-07": "R10-R12", "RF-10-08": "R15-R16",
    "RF-10-09-A": "R17", "RF-10-09-B": "R18", "RF-06": "R18",
    "RF-05": "R01-R18", "RF-08": "R01-R18", "RF-09": "R01-R18", "RF-11": "R01-R18",
}

# IDs are enumerated by product meaning. Never infer ownership from sequence.
SOURCE_TASKS = {
    "RF-10-01": "FR-01 FR-02 FR-03 BR-02 US-01 US-17 US-27 US-28",
    "RF-10-02": "FR-04 FR-05 FR-06 FR-07 FR-08 FR-09 FR-10 FR-32 FR-33 BR-01 BR-32 BR-33 BR-34 BR-35 BR-36 BR-37 BR-38 US-03 US-24 US-30 US-31 US-32 US-36 US-38 US-40 PF-01",
    "RF-10-03": "FR-26 FR-28 FR-30 BR-40 BR-41 BR-43 US-04 US-05 US-07 US-25 US-39 PF-07",
    "RF-10-04": "FR-27 BR-20 US-06",
    "RF-10-05": "FR-29 FR-31 FR-36 BR-39 BR-42 BR-44 US-18 US-26",
    "RF-10-06": "FR-11 FR-12 FR-13 FR-14 BR-04 BR-07 BR-29 BR-30 BR-31 BR-47 BR-48 US-08 US-09 US-10 US-21 US-22 US-23 PF-02",
    "RF-10-07": "FR-15 FR-16 FR-17 FR-18 FR-19 FR-20 FR-21 FR-23 FR-24 FR-25 FR-37 BR-03 BR-05 BR-06 BR-08 BR-09 BR-10 BR-11 BR-12 BR-13 BR-14 BR-17 BR-18 BR-21 BR-22 BR-23 BR-24 BR-25 BR-26 BR-27 BR-28 BR-46 US-11 US-12 US-13 US-14 US-20 US-33 US-34 US-35 US-37 US-41 PF-03 PF-04 PF-05 PF-06",
    "RF-10-08": "FR-22 BR-15 BR-16 BR-19 US-02 PF-08",
    "RF-10-09-A": "BR-45 US-19 US-29 NFR-09",
    "RF-10-09-B": "FR-34 FR-35 US-15 US-16",
}

SOURCE_COORDINATION = {
    "BR-46": ["RF-10-09-A"],  # Notification is an effect, not the Emergency policy owner.
    "FR-37": ["RF-10-09-A"],
    "BR-10": ["RF-10-09-A"],
    "FR-27": ["RF-10-03", "RF-10-05"],
    "FR-29": ["RF-10-03"],
    "US-26": ["RF-10-03"],
    "FR-22": ["RF-10-07"],
    "PF-08": ["RF-10-03", "RF-10-07"],
    "FR-25": ["RF-10-06"],
    "FR-36": ["RF-10-09-B"],
    "BR-02": ["RF-10-04", "RF-10-06"],
    "NFR-03": ["RF-10-07", "RF-10-03"],
    "NFR-11": ["RF-10-03", "RF-10-04"],
}

NFR_TASK = {
    "NFR-01": "RF-10-01", "NFR-02": "RF-10-08", "NFR-03": "RF-08",
    "NFR-04": "RF-10-08", "NFR-05": "RF-10-04", "NFR-06": "RF-10-09-B",
    "NFR-07": "RF-10-02", "NFR-08": "RF-10-09-A", "NFR-09": "RF-10-09-A",
    "NFR-10": "RF-10-05", "NFR-11": "RF-09", "NFR-12": "RF-10-08",
    "NFR-13": "RF-10-08", "NFR-14": "RF-10-05",
}

R_TASK = dict(zip((f"R{n:02}" for n in range(1, 19)),
    "RF-10-01 RF-10-01 RF-10-01 RF-10-02 RF-10-02 RF-10-03 RF-10-03 RF-10-06 RF-10-06 RF-10-07 RF-10-07 RF-10-07 RF-10-04 RF-10-05 RF-10-08 RF-08 RF-10-09-A RF-10-09-B".split()))
CG_TASK = dict(zip((f"CG{n:02}" for n in range(1, 18)),
    "RF-08 RF-10-01 RF-10-01 RF-10-03 RF-10-02 RF-10-03 RF-10-06 RF-10-06 RF-10-07 RF-10-04 RF-10-05 RF-10-08 RF-10-09-A RF-10-09-B RF-06 RF-09 RF-10-04".split()))
L_TASK = dict(zip((f"L{n:02}" for n in range(1, 17)),
    "RF-10-01 RF-10-01 RF-10-01 RF-10-01 RF-10-03 RF-10-03 RF-10-03 RF-10-01 RF-11 RF-11 RF-10-03 RF-10-09-A RF-11 RF-10-08 RF-08 RF-08".split()))
DECISION_TASK = dict(zip("32A 33A 34A 35A 36A 37 38 39A 40 41A 42A 43A 44".split(),
    "RF-10-07 RF-10-06 RF-10-06 RF-10-07 RF-10-01 RF-10-01 RF-10-04 RF-10-02 RF-10-09-B RF-10-09-A RF-10-08 RF-10-08 RF-09".split()))

SOURCE_TASK = {}
for task, identifiers in SOURCE_TASKS.items():
    for identifier in identifiers.split():
        if identifier in SOURCE_TASK:
            raise ValueError(f"Duplicate primary owner: {identifier}")
        SOURCE_TASK[identifier] = task
SOURCE_TASK.update(NFR_TASK)

OPERATION_TASK_OVERRIDES = {
    "reviewTrainingLabel": "RF-10-06",
    "listAuditEvents": "RF-10-09-A",
    "createModelVersion": "RF-10-05", "activateModelVersion": "RF-10-05",
    "retireModelVersion": "RF-10-05", "getModelVersion": "RF-10-05",
    "createDevice": "RF-10-03", "listDefectTypes": "RF-10-06",
    "updateDefectType": "RF-10-06", "getReminderConfig": "RF-10-09-A",
    "setReminderConfig": "RF-10-09-A", "exportApprovedLabels": "RF-10-06",
    "listAdminJobs": "RF-10-05",
}

# x-fr in the historical OpenAPI is broad trace context, not sole ownership.
# Each listed operation crosses a requirement's primary module boundary.
OPERATION_CONTEXT_REASONS = {}
for names, reason in [
    ("adminResetPassword updateAccount getAccount", "Account administration is identity-owned; FR-36 also covers unrelated model/catalog configuration."),
    ("listNotifications readNotification getNotification", "Notification delivery/read state is messaging-owned; FR-34 also describes dashboards."),
    ("listProjects setMembership", "Project membership and listing use identity scope and project records; the other FR supplies a cross-module guard."),
    ("listCrews createCrew", "Crew directory is project/identity context for repair assignment, not the repair item write itself."),
    ("closeMixedCase assessRecurrence", "Case/defect decision is Reporter-owned; repair acceptance history is an input."),
    ("listDefects setDefectSlabLinks", "Defect projection/link is defect-owned; road slab geometry is a referenced source."),
    ("setWorkOrder getWorkOrder", "Repair ordering uses the PM's defect priority as input; work order state belongs to repair."),
    ("setSurveyAccessPoint exportMission", "Survey task/mission owns these operations while road/navigation facts are inputs."),
    ("createUploadSession getUploadPartUrls completeUpload getUploadSession getFileMetadata downloadFile", "File admission/download owns this operation; repair evidence, offline retry and export are consumers or guards."),
    ("requestDeletion decideDeletion setLegalHold getDeletionRequest", "Retention policy and deletion workflow own the action; FR-35 also covers export."),
    ("getAsyncJob", "Asynchronous job read belongs to processing; route/export/report FRs are related consumers."),
    ("getReminderConfig setReminderConfig", "Notification configuration belongs to messaging; model/catalog administration in FR-36 is broader."),
    ("createDevice", "Survey device registration is assigned to survey; FR-36 also describes platform administration."),
    ("listAuditEvents", "Audit query belongs to durable audit; FR-36 also describes general administration."),
    ("exportApprovedLabels", "PM approved-label decision governs export; AI training is a downstream consumer."),
    ("getOwnReport publishCase", "Reporter-facing case projection is Reporter-owned; repair completion evidence is an input."),
    ("reviewTrainingLabel", "PM label approval is Reporter/defect-owned; FR-36 also covers model administration."),
    ("getInspectionTask getInspectionSnapshot listMyInspectionTasks", "Inspection task query belongs to measurement/repair; FR-22 supplies offline read context."),
    ("submitDataset", "Survey dataset submission owns the record; FR-27 file admission is a verified source dependency."),
    ("listDefectTypes updateDefectType", "Defect taxonomy is defect-owned; FR-36 covers general catalog administration."),
]:
    for name in names.split():
        OPERATION_CONTEXT_REASONS[name] = reason


def ownership(task):
    return {"rf_task": task, "proposed_owner": TASK_OWNER[task],
            "module_requirements": TASK_REQUIREMENTS[task]}
