# RF-00: Baseline RoadGuardSystem

## Pham vi va Git

- Thoi diem kiem tra: 2026-09-30, Asia/Bangkok.
- Nhanh muc tieu va checkout hien tai: `anh`.
- Local `HEAD`: `2efc8a5775f834c7f0fe37cc0ce703011649e1f1` (`style: format identity onboarding tests`, 2026-09-30 12:54:07 +0700).
- `origin/anh` local tracking ref: cung SHA, divergence `0 0`. `git ls-remote --heads origin anh` tra ve cung SHA tai thoi diem kiem tra; khong suy rong sang thay doi remote sau do.
- Khao sat **working tree local dirty tren commit tren**, khong phai clean checkout cua remote. Khong checkout, fetch, reset, clean, commit, push, hay sua file dang doi.
- Thay doi co san tu truoc: `SurveyV2PersistenceService.cs`, `P2V2SurveyScopeConcurrencyTests.cs`, `planning/CROSS_OWNER_HANDOFFS.md`, `planning/V2/Execution/ANH-02-project-survey.md`, `planning/V2/Governance/README.md`; untracked `SurveyV2PersistenceService.Dataset.cs`, `COV-BASELINE-Q11-CONTRACT.md`, va `planning/refactor/` tu lan khao sat truoc. `git status --short --branch` la danh sach chuan tai thoi diem nay.

## Solution va runtime (`CURRENT_VERIFIED`)

- `RoadGuardSystem.slnx` khai bao 9 project: API, BusinessObjects, DTOs, Repositories, Services, UnitTests, ApiTests, IntegrationTests, Seeder.
- Moi `.csproj` target `net8.0`; `global.json` chon SDK `10.0.401` (`dotnet --version` = `10.0.401`). `Directory.Build.props` bat nullable, analyzers, deterministic build va warnings-as-errors voi danh sach ngoai le.
- Tham chieu chinh: API -> Services -> Repositories -> BusinessObjects/DTOs; DTOs -> BusinessObjects; Seeder -> Repositories. Xem cac `ProjectReference` trong tung `.csproj`.
- Dependency chinh: ASP.NET Core MVC/JWT/Swagger/API Versioning (`RoadGuardSystem.eAPI.csproj`); EF Core SQL Server + NetTopologySuite + AWS S3 (`RoadGuardSystem.cRepositories.csproj`); Quartz (`RoadGuardSystem.dServices.csproj`); xUnit, WebApplicationFactory, Testcontainers.MsSql (`tests/*/*.csproj`).
- `RoadGuardSystem.API/Program.cs`: `AddControllers`, DI platform, seeding, optional upload worker trong Development, optional `DbInitializer` neu `RoadGuardDatabase:InitializeOnStartup`, middleware correlation/problem-details/versioning, Swagger Development, auth, `/health`, controller routes.
- Cau hinh: `appsettings.json`, `appsettings.Development.json`, optional `appsettings.Development.local.json`, environment variables va options/DI extension. File local chua config nhay cam duoc nhin **chi ten khoa**, khong sao chep gia tri. `.env.example` + `docker-compose.yml` mo ta SQL local; CI tao credential ephemeral.
- Persistence: `RoadGuardSystem.Repositories/RoadGuardDbContext.cs` khai bao cac DbSet cho identity, project/road/warranty, survey, file, inspection/defect, processing, audit/outbox/idempotency. Co 38 file migration non-Designer tinh theo `Get-ChildItem`; day la so luong file, khong phai bang chung schema da apply.

## Kiem chung thuc thi

| Lenh | Ket qua | Gioi han |
|---|---|---|
| `dotnet restore RoadGuardSystem.slnx --nologo` | exit 0, all projects up-to-date | Khong kiem tra package moi hoac clean NuGet cache. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental -nologo -v q -clp:ErrorsOnly` | exit 0, 594 warnings, 0 errors | Build tu working tree dirty, khong quy ket cho remote commit sach. |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --nologo -v q` | 170 pass, 0 fail, 0 skip | Test binaries vua duoc build cung solution; khong chung minh dung nghiep vu. |
| Child process xoa `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING`, roi `dotnet test ...IntegrationTests.csproj --no-build --nologo -v q --filter 'FullyQualifiedName~SqlServerSpatialRoundTripTests'` | 7 pass, 0 fail, 0 skip | `SqlServerTestFixture` tao Testcontainers SQL va database `RoadGuard_Test_<GUID>`; chi nhom spatial duoc chay. |
| Child process xoa bien SQL test, roi `dotnet test ...ApiTests.csproj --no-build --nologo -v q --filter 'FullyQualifiedName~ProblemDetailsNegativeTests'` | 19 fail, 0 pass | Startup/seeder fail truoc HTTP assertions; xem muc loi nen. |
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | exit 0; 16 docs/ownership/link entries pass | Validator cu, khong bao phu docs refactor moi. |
| `pwsh -NoProfile -File tests/CI/Verify-CiWorkflow.ps1` | exit 0 | Static CI check. |
| `pwsh -NoProfile -File tests/Operations/Verify-DockerCompose.ps1` | exit 0 | Static/isolated compose config check, khong khoi dong stack. |
| `python docs/diagram/V2/ci/check_alignment.py` | exit 0, 133 tasks, hash `ada7f48f...`, runtime `NOT_RUN` | Static alignment only. |
| `python docs/diagram/V2/09_Frontend/contracts/check_contracts.py` | exit 1, `CONTRACT_LOCK_MISMATCH` | Existing canonical/snapshot/lock drift; khong sua trong RF-00. |

`docker info --format '{{.ServerVersion}}'` tra ve `29.6.1`. Bien `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` ton tai trong shell goc; gia tri duoc giu kin. SQL integration test chi chay sau khi xoa bien trong child process de buoc Testcontainers.

## Loi nen va chua xac minh

- API platform test: `ProblemDetailsNegativeTests` tao `CustomWebApplicationFactory("Development")`; `Program.cs:36` goi `DbInitializer`, `PostmanScenarioSeedStep.ValidateExistingFixtureAsync` nem `InvalidOperationException: Postman fixture collision: survey ... has incompatible ownership or dependencies` (`PostmanScenarioSeedStep.cs:440`, `:506`). Mot test verbose tai hien cung stack. Test HTTP chua bat dau; 19 fail khong chung minh 19 loi API rieng le. Factory co `RoadGuardDatabase:ConnectionString` rieng trong `tests/RoadGuardSystem.ApiTests/Infrastructure/CustomWebApplicationFactory.cs`; ket hop local config/initializer can duoc phan tich truoc khi chay lai. Log co truy van DB, nhung khong du bang chung khang dinh khong co ghi du lieu; khong chay tiep API suite.
- `CONTRACT_LOCK_MISMATCH` la static doc/contract drift duoc xac nhan bang lenh tren; khong duoc chinh sua lock khi chua review contract.
- 594 warning build can phan loai theo ID/module o RF-01; RF-00 chi co tong so.
- Chua chay full IntegrationTests, full ApiTests, smoke API that, migration apply/rollback, seeder entry point, storage/AI provider, deployment, hay so sanh DB schema live. Khong co bang chung clean remote checkout pass.

## RF-01 co the tiep tuc

Co the doc source, lap module inventory, mapping caller/dependency va contract gap tren branch `anh` voi nhan `local dirty`; cac phan can API smoke/SQL upgrade phai cho fixture co lap va baseline seeder duoc lam ro. Khong thay doi public contract, schema, migration, CI hoac production code tu RF-00.
