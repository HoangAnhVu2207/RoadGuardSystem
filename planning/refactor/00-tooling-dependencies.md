# RF-00: Tooling va dependency khi thay docs/agent

- Branch `anh`; surveyed local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; working tree dirty. Danh sach nay la `CURRENT_VERIFIED` tu source/script va `PROPOSAL` cho buoc chuyen doi, khong kich hoat bo agent moi.

| Nguon / cong cu | Duong dan phu thuoc hien tai | Neu doi/xoa som | Cach xu ly de xuat |
|---|---|---|---|
| CI `verify` job, `.github/workflows/ci.yml` | `tests/Documentation/Verify-P102Docs.ps1`, `tests/Operations/Verify-DockerCompose.ps1`, `tests/CI/Verify-CiWorkflow.ps1`, security verifier, `dotnet format` | CI fail hoac mat guard | Cap nhat validator va CI trong mot task rieng sau khi docs moi duoc duyet. |
| `Verify-P102Docs.ps1` | `AGENTS.md`, `docs/README.md`, `docs/diagram/V2/**` hoac `docs/design/**`, ADR 001-006, hai P1/P2 plan, `.agents/skills/**`; dem chinh xac so skill directories | Xoa/di chuyen docs hoac them skill se fail | Chuyen required-path/ownership/link rules, bo hard-coded layout sau khi co thay the. |
| `Verify-AgentSetup.ps1` | `AGENTS.md`, `.agents/mcp_config.json`, `.agents/rules/roadguard.md`, nam skill cu, Postman JSON, `docs/prompts/RoadGuard_Task_Workflow.md`, plan cu; token va gioi han dong | Gan nhu moi thay doi agent/docs lam fail | Luu nhu historical guard; viet guard moi theo manifest truoc khi retire. Script nay da duoc doc, khong chay trong RF-00. |
| `docs/diagram/V2/ci/check_alignment.py` | `planning/V2/task_manifest.json`, 133 operation cards, decision register, lifecycle, canonical `docs/diagram/V2/05_Technical/openapi.yaml` SHA | Doi path/contract lam hash mismatch | Chuyen manifest va hash theo version; giu old snapshot read-only. |
| FE contract guards: `check_contracts.py`, `test_contract_guard.py`, `validate_package.py` | Canonical OpenAPI, FE snapshot, `contract.lock.json`, generated schemas/types/fixtures | FE lock/fixture drift, guard fail | Review contract mot lan, cap nhat snapshot/lock/generated artifacts atomically; khong auto-relock. Hien `check_contracts.py` da fail. |
| `docs/diagram/V2/ci/check-docs.cmd` | Python guards trong `docs/diagram/V2/ci` | Duong dan bat mat | Doc day du script truoc khi chay; cap nhat invocation khi doi layout. |
| Postman | `docs/postman/RoadGuardSystem-V2.postman_collection.json`, environment JSON/YAML, collection folder, docs README va seeded fixture IDs | Collection request/assertion va fixture diverge | Mapping operationId/route/fixture truoc khi di chuyen; khong chay destructive requests trong RF-00. |
| API `.http` | `RoadGuardSystem.API/RoadGuardSystem.API.http` | Link va example route drift | Doi cung public contract sau khi duyet. |
| ADR 001-006 va `docs/README.md` | Cross-links den AGENTS, layer rule, V2 design, scope | Mat nguon quyet dinh lich su | Giu ban cu co trang thai historical/superseded va redirect map; khong xoa trong giai doan khao sat. |
| V2 planning/worklogs | 133 cards, manifest, governance, execution, link den docs/agent/ADR/OpenAPI | Link hong va claim trang thai bi sai | Giữ immutable history, lap crosswalk sang plan moi, khong viet lai Done cu. |
| Agent discovery | `AGENTS.md`, `.agents/rules/roadguard.md`, `.agents/skills/**`, `.agents/mcp_config.json` | Agent/tooling tu dong nap rule cu hoac khong tim thay skill | Chi kich hoat huong dan moi sau khi nguoi dung chap nhan va co precedence ro rang. |

## Ban do tham chieu

`rg -l` tren docs/planning/tests/.github/tools/AGENTS (cac dinh dang md, ps1, py, yml, yaml, json) tim thay: 183 file nhac `AGENTS.md`, 156 file nhac `.agents/`, 159 file nhac `docs/diagram/V2`, 24 file nhac `docs/adr/`, 6 file nhac `docs/postman/`, 197 file nhac `planning/V2`, 155 file nhac `openapi.yaml`. Day la so file co text, co the overlap va chua la graph semantic day du. `.agent/` khong duoc thay trong tree goc; `.agents/` ton tai.

## Lenh va an toan

- Da doc `tools/Test/Invoke-RoadGuardTests.ps1`, `tests/Documentation/Verify-P102Docs.ps1`, `tests/Tooling/Verify-AgentSetup.ps1`, `tests/CI/Verify-CiWorkflow.ps1`, `tests/Operations/Verify-DockerCompose.ps1` va phan CI lien quan truoc khi quyet dinh chay.
- Khong chay wrapper `Invoke-RoadGuardTests.ps1` vi `All` chay full SQL/API; khong chay seeder, EF database update, Docker compose up, contract generator `build_contracts.py`, Postman collection hoac script co kha nang ghi source/DB.
- `Verify-P102Docs.ps1`, `Verify-CiWorkflow.ps1`, `Verify-DockerCompose.ps1` duoc chay va exit 0. Python `check_alignment.py` exit 0; `check_contracts.py` exit 1 `CONTRACT_LOCK_MISMATCH`.
- Chi sau khi replacement docs/agent duoc chap nhan moi sua cac hard-coded path, run guard moi/cu va retire path cu theo tung buoc.
