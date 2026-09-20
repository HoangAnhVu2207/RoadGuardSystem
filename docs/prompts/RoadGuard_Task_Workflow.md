# RoadGuard - 3 prompt dung chung cho moi task

Quy tac hien hanh nam trong [AGENTS.md](../../AGENTS.md). Cac task `Done` va worklog cu la lich su bat bien. Luon bat dau bang Prompt 1; chi dung Prompt 2 sau khi owner dong y; dung Prompt 3 de kiem tra va ban giao.

## Prompt 1 - nhan task, phan loai va chot scope

```text
Chuan bi RoadGuard task <TASK-ID hoac MO-TA>, owner <anh|huy>, chua sua file.

Doc git status, task row trong planning neu co, dependency thuc te, file lien quan va quy tac repository. Tu phan loai:
- Task nho: mot muc tieu ro rang, mot owner, scope va dependency da ro, co the hoan thanh va kiem tra trong mot lat cat, khong co migration/package/xoa du lieu/tac dong ngoai chua duoc phep.
- Task lon: co nhieu output doc lap, nhieu owner/shared hotspot, can thay doi schema/migration, anh huong nhieu project hoac phai lam theo thu tu.

Tra scope card:
- Task / owner / branch
- Muc tieu
- In scope
- Out of scope
- File se sua / shared hotspot
- Dependency da co va con thieu trong checkout
- Bac kiem tra va lenh se chay
- Package, migration, du lieu hoac tac dong ngoai

Neu task nho, ket thuc bang:
"Task nho. Ban co muon lam luon khong? Tra loi Dong y <TASK-ID> de bat dau trong phien nay."

Neu task lon, tu chia ngay thanh 3-5 lat cat nho theo thu tu dependency. Moi lat cat phai co output, owner/file doc quyen, dependency, cach kiem tra va dieu kien xong. Chi ro lat cat dau tien da san sang, sau do yeu cau owner dong y ke hoach va lat cat se lam truoc.

Khong sua file cho den khi owner dong y ro rang trong phien nay. Khong hoi nguoi dung co muon chia task lon hay khong.
```

## Prompt 2 - thuc thi task hoac mot lat cat da duyet

```text
Thuc thi RoadGuard <TASK-ID>/<SLICE> dung scope da duoc owner dong y.

Truoc khi sua, doc lai git status, task row, dependency va cac file se cham. Neu checkout hoac scope da thay doi, dung va bao xung dot; khong tu mo rong scope.

Viet delivery contract 5-8 dong phu hop voi task, gom: tac nhan/trigger, input va validation, output thanh cong, loi on dinh, quy tac nghiep vu, persistence/idempotency/concurrency neu co, quyen/audit/du lieu nhay cam neu co, va bang chung de coi la xong. Neu la endpoint, dong dau phai ghi method + route va actor/quyen.

Thuc hien theo kien truc hien tai va chi sua file da duyet. Bao toan thay doi chua commit cua nguoi khac. Khong them package, migration, thay schema, xoa du lieu hoac tao tac dong ngoai neu scope khong noi ro. Neu la endpoint, them/cap nhat Http/*.http, tra response record thay vi EF entity va giu ProblemDetails/error code on dinh. Neu la read endpoint/query, dung AsNoTracking va projection. Neu la persistence SQL dac thu, dung SQL Server/Testcontainers; SQLite khong phai bang chung cho constraint, spatial, migration hoac rowversion.

Kiem tra theo rui ro: neu sua project code, build project thay doi truoc; chay output/smoke that neu task co the chay; sau smoke moi them 1-3 test tap trung cho authorization, du lieu nhay cam, validation quan trong, tien/tinh toan, concurrency/idempotency, SQL dac thu hoac bug da tai hien. Task tai lieu/tooling dung verifier va kiem tra link/diff phu hop thay cho runtime test.

Neu cung mot loi van con sau hai lan sua, dung, bao ten check va chan doan ngan; khong thu lan ba va khong mo rong scope.
```

## Prompt 3 - kiem tra, sua trong scope va ban giao

```text
Kiem tra va ban giao RoadGuard <TASK-ID>/<SLICE> theo scope da duyet.

Doc git status va diff cua dung cac path trong scope. Doi chieu delivery contract, acceptance criteria, ownership, dependency, gioi han 500 dong va thay doi ngoai scope. Chay bac kiem tra re nhat du de chung minh output; truoc commit/merge hoac khi sua hanh vi dung chung, chay full relevant tests. Khong coi test bi skip, khong discover test hoac dung sai moi truong SQL la pass.

Neu co loi trong scope, sua va chay lai check lien quan; toi da hai lan sua cho cung mot loi. Neu loi can package, migration, schema, owner/file khac hoac tac dong ngoai chua duyet, dung va xin scope moi.

Tra ket qua gon:
- File da doi va hanh vi/output dat duoc
- Lenh kiem tra, ket qua that va smoke response/output neu co
- Test tap trung/full test, hoac ly do N/A
- Package, migration, du lieu va tac dong ngoai thuc te
- Rui ro con lai / blocker
- Lat cat tiep theo neu task lon

Chi cap nhat task row/worklog neu scope cho phep. Khong sua lich su `Done`; khong commit, merge, rebase, pull, push, tag, stash hoac doi branch/worktree khi chua duoc owner dong y.
```

## Thang kiem tra tham chieu

```powershell
# Build project thay doi truoc
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly

# Smoke API that khi task co endpoint
dotnet watch --project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
# Chay request tu Http/*.http va kiem tra status/body/state.

# Test tap trung khi rui ro yeu cau
dotnet test tests/RoadGuardSystem.ApiTests --no-build --filter "FullyQualifiedName~<Feature>" -v q

# Truoc commit/merge hoac khi thay doi hanh vi dung chung
dotnet test RoadGuardSystem.slnx --no-build -v q
```

Chi dung `--no-build` sau khi build thanh cong. Voi project khac API, thay project/test/filter theo file va rui ro thuc te. Truoc khi xin commit, chay `git diff --check`, xem status, diff tung path va staged diff; chi stage path da duyet.
