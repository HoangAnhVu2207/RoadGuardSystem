# RoadGuard - quy trinh Codex gon theo tung lat cat

Quy tac hien hanh nam trong [AGENTS.md](../../AGENTS.md). Cac task `Done` va worklog cu la lich su bat bien; workflow nay chi ap dung cho task moi hoac task duoc tiep tuc sau 20/09/2026.

## A. Prompt giao scope - bat buoc truoc khi sua

```text
Chuan bi scope cho RoadGuard <TASK-ID>, owner <anh|huy>, chua viet code.

Doc git status, task row, dependency thuc te va file lien quan. Tra dung mau:
- Muc tieu
- In scope
- Out of scope
- File se sua / shared hotspot
- Dependency da co trong checkout
- Bac kiem tra va lenh se chay
- Package, migration, du lieu hoac tac dong ngoai
- Neu task lon: chia 3-5 lat cat

Ket thuc bang: "Tra loi Dong y <TASK-ID> de bat dau trong phien nay."
Khong sua file cho den khi nguoi dung dong y ro rang.
```

## B. Prompt lap ke hoach task lon

```text
Doc task <TASK-ID> va spec lien quan. Chia thanh 3-5 lat cat co the build/chay doc lap; moi lat co endpoint hoac output, file doc quyen, dependency, bac kiem tra va dieu kien xong. Chi lap ke hoach, chua viet code. Bao toan task Done va thay doi chua commit cua nguoi khac.
```

## C. Prompt lam mot endpoint

```text
Lam lat cat <TASK-ID>/<SLICE> da duoc dong y.

Truoc code, viet spec 5-8 dong:
1. method + route;
2. actor/quyen;
3. input + validation;
4. success status + response;
5. ProblemDetails/error code;
6. quy tac nghiep vu;
7. persistence/idempotency/concurrency neu co;
8. audit/du lieu nhay cam neu co.

Chi sua file trong scope. Endpoint moi phai them Http/<feature>.http.
Build project dang sua voi -nologo -v q -clp:ErrorsOnly.
Sau do chay dotnet watch va goi request .http, bao status/body/state that.
Chi viet 1-3 integration test sau smoke neu co tien/tinh toan, phan quyen, du lieu nhay cam, validation quan trong, concurrency/idempotency, SQL dac thu hoac bug cu.
Khong them package hay migration neu scope khong noi ro.
```

## D. Prompt them test quan trong

```text
Endpoint <ROUTE> da smoke thanh cong. Viet toi da 3 integration test cho <RISK>. Dung WebApplicationFactory; dung SQL Server/Testcontainers neu claim lien quan spatial, constraint, rowversion hoac migration. Tranh mock; khong test DTO/DI/CRUD don gian. Chi chay filter <FEATURE> voi output gon.
```

## E. Prompt sua bug

```text
Bug: <MO-TA>. File/slice: <PATH>. Tai hien bang request .http hoac test gon phu hop rui ro, sua trong scope, build project va chay lai dung ca tai hien. Neu loi con ton tai sau hai lan sua, dung va bao ten check + chan doan ngan; khong mo rong scope.
```

## Thang kiem tra

```powershell
# Bac 1: moi lan sua
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly

# Bac 2: response that
dotnet watch --project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
# Chay request tu Http/*.http va kiem tra status/body/state.

# Bac 3: chi khi rui ro can test
dotnet test tests/RoadGuardSystem.ApiTests --no-build --filter "FullyQualifiedName~<Feature>" -v q

# Bac 4: truoc commit/merge hoac thay doi dung chung
dotnet test RoadGuardSystem.slnx --no-build -v q
```

Sau build thanh cong moi dung `--no-build`. Truoc commit, xem `git status --short`, `git diff --check`, diff cac path cu the va staged diff; stage tung path. Khong merge/push neu chua duoc phep.
