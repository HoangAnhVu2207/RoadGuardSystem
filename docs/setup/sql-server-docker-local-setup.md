# Huong dan cau hinh SQL Server va Docker tren may Huy

Tai lieu nay giup Huy tao moi truong SQL Server doc lap tuong duong moi truong da dung cho `P2-00` den `P2-03`. Hai may khong dung chung database, tai khoan `sa` hay mat khau. Schema va migration duoc dong bo qua Git.

## 1. Ba che do SQL can phan biet

| Che do | Khi nao dung | Server trong connection string | Docker can chay |
|---|---|---|---|
| Testcontainers tu dong | Cach khuyen nghi de chay integration test | Tu dong cap phat | Co |
| Docker Compose co dinh | Migration, seed, debug du lieu local | `localhost,<MSSQL_PORT>` | Co |
| SQL Server native tren Windows | Khi Huy da cai SQL Server Developer/Express | `.\<TEN_INSTANCE_CUA_HUY>` | Khong |

Khong dat bien `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` khi muon dung Testcontainers. Neu bien nay ton tai nhung rong, sai cu phap hoac khong ket noi duoc, test se fail-fast va khong fallback sang Docker.

## 2. Yeu cau ban dau

Mo PowerShell tai thu muc goc repository va kiem tra:

```powershell
git branch --show-current
dotnet --version
docker --version
docker compose version
docker version
```

Ket qua mong doi:

- Nhanh lam viec cua Huy la `huy`.
- .NET SDK khop voi `global.json` cua checkout hien tai.
- `docker version` hien ca `Client` va `Server`. Neu chi co Client hoac bao loi pipe/daemon, mo Docker Desktop va doi den khi engine san sang.
- Docker Desktop dung Linux containers. Repository dang pin image `mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04`.

Neu chi dung SQL Server native, hai lenh Docker cuoi co the bo qua. Tuy nhien Docker van can thiet de chay duong Testcontainers va tai hien gate day du cua repository.

## 3. Cach khuyen nghi: Testcontainers tu dong

Test fixture cua repository tu khoi dong mot SQL Server container tam thoi, tao database dang `RoadGuard_Test_<guid>`, chay test, sau do xoa database va container.

Xoa bien ket noi khoi PowerShell hien tai:

```powershell
Remove-Item Env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING -ErrorAction SilentlyContinue
```

Neu truoc day Huy da luu bien o User scope va muon bo cau hinh do:

```powershell
[Environment]::SetEnvironmentVariable(
    'ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING',
    $null,
    'User')
```

Dong PowerShell cu, mo cua so moi, sau do chay:

```powershell
dotnet restore RoadGuardSystem.slnx
dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=P2-00"
```

Gate dat khi lenh test tra exit code `0`, khong co failed/skipped test va khong con database `RoadGuard_Test_*` sau teardown. Khong can chay `docker compose up` cho che do nay.

## 4. Docker Compose co dinh cho migration, seed va debug

### 4.1 Tao file cau hinh local

Tai thu muc goc repository:

```powershell
Copy-Item .env.example .env
notepad .env
```

Thay placeholder trong `.env` bang gia tri rieng cua Huy:

```dotenv
MSSQL_PORT=1433
MSSQL_SA_PASSWORD=<THAY_BANG_MAT_KHAU_MANH_RIENG_CUA_HUY>
```

Khong de nguyen ky tu `<` va `>`. Mat khau phai co it nhat 8 ky tu, gom chu hoa, chu thuong, chu so va ky tu dac biet. Khong dung lai mat khau cua may khac.

File `.env` da duoc `.gitignore` bao ve. Van phai kiem tra truoc moi commit:

```powershell
git check-ignore -v .env
git ls-files .env
```

Lenh thu nhat phai hien rule ignore; lenh thu hai phai khong tra ve duong dan nao.

### 4.2 Xu ly xung dot cong

Kiem tra cong `1433`:

```powershell
Get-NetTCPConnection -LocalPort 1433 -ErrorAction SilentlyContinue
```

Neu cong da bi SQL Server native hoac dich vu khac su dung, sua `.env` thanh mot cong con trong, vi du `14330`. Moi connection string Docker sau do phai dung cung cong nay.

### 4.3 Kiem tra va khoi dong container

```powershell
pwsh -NoProfile -File tests/Operations/Verify-DockerCompose.ps1
docker compose config --quiet
docker compose up -d sqlserver
docker compose ps
docker inspect --format '{{.State.Health.Status}}' roadguard-sqlserver
```

Ket qua cuoi phai la `healthy`. Neu van la `starting`, doi healthcheck chay lai va thuc hien lai hai lenh kiem tra cuoi; khong coi `Up` la bang chung database da san sang.

### 4.4 Dat connection string ma khong ghi mat khau vao lich su lenh

Chon dung cong da dat trong `.env`:

```powershell
$sqlPort = 1433
$sqlCredential = Get-Credential -UserName 'sa' -Message 'Nhap mat khau sa cua SQL Server Docker tren may Huy'
$sqlPassword = $sqlCredential.GetNetworkCredential().Password
$dockerMasterConnection = "Server=localhost,$sqlPort;Database=master;User Id=sa;Password=$sqlPassword;Encrypt=True;TrustServerCertificate=True"
$dockerDevConnection = "Server=localhost,$sqlPort;Database=RoadGuardDev;User Id=sa;Password=$sqlPassword;Encrypt=True;TrustServerCertificate=True"

$env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING = $dockerMasterConnection
$env:ROADGUARD_MIGRATION_CONNECTION_STRING = $dockerDevConnection
$env:ROADGUARD_CONNECTION_STRING = $dockerDevConnection

Remove-Variable dockerMasterConnection, dockerDevConnection
Remove-Variable sqlPassword, sqlCredential
```

Ba bien co muc dich khac nhau:

- `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING`: integration fixture ket noi vao `master`, tao va xoa database test biet lap.
- `ROADGUARD_MIGRATION_CONNECTION_STRING`: EF Core design-time factory dung cho migration.
- `ROADGUARD_CONNECTION_STRING`: Seeder CLI dung de ket noi database dich.

`.env` khong tu dong nap cac bien .NET nay. Can dat chung trong chinh PowerShell se chay `dotnet`.

### 4.5 Xac minh Docker SQL bang test cua repository

```powershell
dotnet restore RoadGuardSystem.slnx
dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=P2-00"
```

Gate dat khi test tra exit code `0`, khong co failed/skipped test. Tai khoan SQL dung cho integration test phai co quyen tao, doi trang thai va xoa database tam.

## 5. SQL Server native tren Windows

Phuong an nay danh cho may da cai SQL Server Developer hoac Express. Ten may va ten instance cua Huy khong duoc sao chep tu may nguoi khac.

Trong SQL Server Setup:

1. Cai `Database Engine Services`.
2. Chon named instance rieng, vi du `<TEN_INSTANCE_CUA_HUY>`.
3. Chon `Mixed Mode (SQL Server authentication and Windows authentication)`.
4. Dat mat khau manh rieng cho `sa` va them tai khoan Windows cua Huy lam SQL administrator.
5. Trong SQL Server Configuration Manager, bat TCP/IP neu can, sau do restart dich vu instance.

Kiem tra dich vu:

```powershell
Get-Service -Name 'MSSQL*','SQLBrowser' -ErrorAction SilentlyContinue |
    Select-Object Name, Status, StartType
```

Dich vu `MSSQL$<TEN_INSTANCE_CUA_HUY>` phai o trang thai `Running`. SQL Browser khong can bat khi chi ket noi local bang `.\<TEN_INSTANCE_CUA_HUY>`; khong mo SQL Server ra mang noi bo hoac Internet cho cong viec nay.

Dat connection string trong PowerShell hien tai:

```powershell
$sqlInstance = '.\<TEN_INSTANCE_CUA_HUY>'
$sqlCredential = Get-Credential -UserName 'sa' -Message 'Nhap mat khau sa cua SQL Server native tren may Huy'
$sqlPassword = $sqlCredential.GetNetworkCredential().Password
$nativeMasterConnection = "Server=$sqlInstance;Database=master;User Id=sa;Password=$sqlPassword;Encrypt=True;TrustServerCertificate=True"
$nativeDevConnection = "Server=$sqlInstance;Database=RoadGuardDev;User Id=sa;Password=$sqlPassword;Encrypt=True;TrustServerCertificate=True"

$env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING = $nativeMasterConnection
$env:ROADGUARD_MIGRATION_CONNECTION_STRING = $nativeDevConnection
$env:ROADGUARD_CONNECTION_STRING = $nativeDevConnection

Remove-Variable nativeMasterConnection, nativeDevConnection
Remove-Variable sqlPassword, sqlCredential
```

Thay toan bo `<TEN_INSTANCE_CUA_HUY>` bang ten instance thuc te. Trong chuoi PowerShell dung dau nhay don, dau `\` khong can escape them.

Xac minh:

```powershell
dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=P2-00"
```

Neu bien ket noi da duoc dat, fixture bat buoc dung instance nay. No se fail thay vi am tham chuyen sang Testcontainers khi connection string sai hoac server khong truy cap duoc.

## 6. Ap dung migration va chay seeder

Chi thuc hien sau khi SQL Server dich da `healthy`/`Running` va `ROADGUARD_MIGRATION_CONNECTION_STRING`, `ROADGUARD_CONNECTION_STRING` da duoc dat cho database `RoadGuardDev`.

```powershell
dotnet ef database update `
    --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj `
    --startup-project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj

dotnet run --project tools/RoadGuardSystem.Seeder/RoadGuardSystem.Seeder.csproj --
```

Khong truyen connection string qua tham so dong lenh. Seeder co chu y chi doc `ROADGUARD_CONNECTION_STRING`.

API hien tai chua dang ky persistence trong composition root. Khong them credential vao `appsettings.json` hoac `launchSettings.json`; khi API duoc noi database o task sau, host se nhan cau hinh runtime qua secret store hoac bien moi truong duoc phe duyet.

## 7. Dung moi truong dung cach

Testcontainers tu dong don container va database khi test ket thuc. Voi Docker Compose:

```powershell
docker compose stop
```

Lenh nay dung container va giu volume `mssql_data`. Khi can khoi dong lai:

```powershell
docker compose start
```

Khong dung `docker compose down -v` trong quy trinh thong thuong, vi `-v` xoa volume va du lieu local.

Xoa cac bien khoi PowerShell khi khong con dung:

```powershell
Remove-Item Env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING -ErrorAction SilentlyContinue
Remove-Item Env:ROADGUARD_MIGRATION_CONNECTION_STRING -ErrorAction SilentlyContinue
Remove-Item Env:ROADGUARD_CONNECTION_STRING -ErrorAction SilentlyContinue
```

## 8. Xu ly loi thuong gap

| Hien tuong | Nguyen nhan can kiem tra | Cach xac minh/xu ly |
|---|---|---|
| `Docker daemon is not running` | Docker Desktop/WSL engine chua san sang | Mo Docker Desktop; `docker version` phai hien Server. |
| Container `unhealthy` | Mat khau khong dat policy, cong xung dot, SQL chua khoi dong | `docker compose logs sqlserver`; sua `.env`, sau do khoi dong lai container. Khong dua log co secret vao worklog. |
| `port is already allocated` | Cong `MSSQL_PORT` dang duoc dich vu khac dung | Kiem tra `Get-NetTCPConnection`; chon cong khac trong `.env` va connection string. |
| Test bao bien ket noi rong/sai | Bien ton tai nhung gia tri khong hop le | Xoa bien de dung Testcontainers, hoac dat lai connection string dung trong cung PowerShell. |
| `Login failed for user 'sa'` | Sai mat khau, `sa` bi disable hoac native SQL chua bat Mixed Mode | Kiem tra SQL Server configuration va dat lai credential local. |
| Khong tao duoc `RoadGuard_Test_*` | Login khong co quyen tao/xoa database | Dung tai khoan local duoc cap dung quyen; khong bo qua integration test. |
| Named instance khong ket noi duoc | Sai ten instance hoac dich vu chua chay | `Get-Service -Name 'MSSQL*'`; dung `.\<TEN_INSTANCE_CUA_HUY>`. |
| Test mo them container du da chay Compose | Bien test dang unset | Day la hanh vi dung cua Testcontainers. Dat `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` neu muon test dung Compose SQL. |

## 9. Checklist ban giao cua Huy

- [ ] Checkout dang o nhanh `huy` va khong ghi de thay doi cua nguoi khac.
- [ ] Docker Desktop engine san sang, neu dung Docker/Testcontainers.
- [ ] `.env` ton tai local, bi Git ignore va khong chua mat khau dung chung.
- [ ] Docker Compose SQL dat `healthy`, hoac native instance dat `Running`.
- [ ] Chi mot trong hai duong test duoc chon ro rang: bien ket noi hop le, hoac bien hoan toan unset de dung Testcontainers.
- [ ] `P2-00` integration filter tra exit code `0`, zero failed va zero skipped.
- [ ] Khong con database `RoadGuard_Test_*` sau khi test ket thuc.
- [ ] Completion log ghi ro che do SQL, ten instance/host khong nhay cam, command, exit code, test count va timestamp; khong ghi password hay connection string day du.

## 10. Nguon cau hinh trong repository

- Docker Compose: [`../../docker-compose.yml`](../../docker-compose.yml)
- Mau bien Docker local: [`../../.env.example`](../../.env.example)
- Integration fixture: [`../../tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs`](../../tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs)
- EF migration factory: [`../../RoadGuardSystem.Repositories/Migrations/RoadGuardDbContextDesignTimeFactory.cs`](../../RoadGuardSystem.Repositories/Migrations/RoadGuardDbContextDesignTimeFactory.cs)
- Seeder CLI: [`../../tools/RoadGuardSystem.Seeder/Program.cs`](../../tools/RoadGuardSystem.Seeder/Program.cs)
- Docker verifier: [`../../tests/Operations/Verify-DockerCompose.ps1`](../../tests/Operations/Verify-DockerCompose.ps1)

Neu repository thay doi image, ten bien, port, migration factory hoac test fixture, cap nhat tai lieu nay trong cung task thay doi; khong dung ban copy ben ngoai repository lam nguon chinh.
