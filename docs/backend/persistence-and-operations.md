# Persistence và operations hiện hành

**ACTIVE · IMPLEMENTED_SOURCE_VERIFIED · DOC-AUDIT-01**, baseline `f0c8762`; [matrix](../audit/DOC-AUDIT-01-MATRIX.md) gắn test execution riêng. [Fresh data mapping](data/audited-data-model.md) mô tả EF/owned SQL; không chứng minh deployed/shared database.

Controller→IService→IRepository là boundary định hướng. Repository sở hữu EF/storage/transaction/receipts; Services sở hữu orchestration/authorization mapping. Legacy exceptions không được xóa bởi audit. Actual source boundary tests có trong focused unit run.

Report/case/candidate commands dùng guarded `IdempotencyOperationService`; repair/offline có serializable caller-owned transactions, exact resource/version/origin checks, immutable facts, source proof và durable effect/receipt. Replay cần recheck current permission; same key không cho đổi payload hoặc target. External bytes/AI effects không nằm trong SQL transaction; xử lý qua upload verification, lease/receipt/recovery.

`StoredFile.SizeBytes`/`UploadSession.ExpectedSizeBytes` là long/bigint. Multipart recovery và checksum đã có; adapter hiện có MinIO hoặc local fallback theo options. Chưa xác minh live MinIO/proxy/large transfer trong audit. Reporter download có narrow post-storage-open authority recheck risk (G03); FIELD flow có recheck riêng, không suy toàn hệ thống an toàn từ một path.

H6 finite notification dispatcher/source adapters đã hiện hữu; unknown/unadopted source remains pending/failed. Recipient own inbox và current source permission khác project-global Supervisor command authority. FT final-confirmation event registered nhưng producer/source adoption còn thiếu (G04). Read receipt/delivery không thay business ACK. Worker ownership/config và crash/restart thực cần deployed test.

Processing compatibility callback kiểm attempt membership nhưng chưa active/latest fence và idempotency fingerprint còn hẹp (G02). ANH02 synthetic lease/result/provenance path tách biệt; không chứng nhận external FastAPI receipt/retry protocol.

Offline device/signature/snapshot/package/handover/import/artifact/shared core và batch/origin/reconcile reads có source. ND01 giữ original 24h và uncertain claimed time; không reset để nhận execution. Local receipt không chứng minh Android đã giữ durable queue, xử lý kill/reboot/decrypt đúng.

Retention unknown basis/hold fail-closed, exports immutable source/hash và lifecycle clocks/obligations có implementation. Không physical-delete, apply migration, seed, benchmark, shared SQL mutation hoặc deployment trong audit. Runbook [release](huy-final-release-runbook.md) là procedure cần environment-specific approval/evidence, không là release certificate.
