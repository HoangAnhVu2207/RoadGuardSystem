"""Generate safe read collections and add the folder to the owner's manual collection.

No HTTP calls, environment edits or embedded credentials. Existing requests are preserved.
"""
import argparse
import copy
import json
from pathlib import Path


def event(kind, lines):
    return {"listen": kind, "script": {"type": "text/javascript", "exec": lines}}


def request(name, path, token="supervisorToken", expected=200, scripts=(), cookie=False):
    auth = {"type": "noauth"} if token is None or cookie else {
        "type": "bearer", "bearer": [{"key": "token", "value": "{{" + token + "}}", "type": "string"}]}
    item = {"name": name, "request": {"method": "GET", "auth": auth,
        "header": [{"key": "Accept", "value": "application/json"}], "url": "{{baseUrl}}" + path},
        "protocolProfileBehavior": {"disableCookies": not cookie},
        "event": [event("test", [f"pm.test('HTTP {expected}', () => pm.expect(pm.response.code).to.eql({expected}));"] + list(scripts))]}
    if token is None or cookie:
        item["event"].insert(0, event("prerequest", ["pm.request.headers.remove('Authorization');"]))
    return item


def folder():
    select_project = [
        "if (pm.response.code === 200) { const p=pm.response.json(); pm.test('Project page',()=>pm.expect(p.items).to.be.an('array'));",
        "if (p.items.length === 1) pm.environment.set('projectId',p.items[0].projectId);",
        "else if (!pm.environment.get('projectId') && p.items.length) pm.environment.set('projectId',p.items[0].projectId); }"]
    select_user = [
        "pm.environment.set('selectedStaffUserId',''); pm.environment.set('selectedPmUserId',''); pm.environment.set('staffPmCandidateIds','[]');",
        "if (pm.response.code === 200) { const p=pm.response.json(); pm.environment.set('staffPmCandidateIds',JSON.stringify(p.items.map(x=>x.userId))); const u=p.items.find(x=>x.userId!==pm.environment.get('currentPrimaryPmUserId'));",
        "pm.environment.set('selectedPmUserId',u ? u.userId : ''); if (u) pm.environment.set('selectedStaffUserId',u.userId); }"]
    primary = [
        "pm.environment.set('currentPrimaryMembershipVersion',''); pm.environment.set('currentPrimaryPmUserId','');",
        "if (pm.response.code === 200) { const m=pm.response.json().primaryProjectManager;",
        "if (m) { pm.environment.set('currentPrimaryMembershipVersion',m.membershipVersion); pm.environment.set('currentPrimaryPmUserId',m.userId);",
        "if (pm.environment.get('selectedPmUserId')===m.userId) { const ids=JSON.parse(pm.environment.get('staffPmCandidateIds')||'[]'); const replacement=ids.find(id=>id!==m.userId); pm.environment.set('selectedPmUserId',replacement||''); if(replacement) pm.environment.set('selectedStaffUserId',replacement); } } }"]
    items = [
        request("01 — Chọn dự án (Supervisor)", "/api/v1/projects?search={{projectSearch}}", scripts=select_project),
        request("02 — Nhân sự nội bộ (Supervisor)", "/api/v1/users?limit=20"),
        request("03 — Chọn PM ACTIVE theo tên/email", "/api/v1/users?role=PM&status=ACTIVE&search={{staffSearch}}&limit=20", scripts=select_user),
        request("04 — Reporter (lọc rõ role)", "/api/v1/users?role=REPORTER&search={{staffSearch}}&limit=20"),
        request("05 — Thành viên hiện hành (Supervisor)", "/api/v1/projects/{{projectId}}/members?limit=20"),
        request("06 — Thành viên hiện hành (PM)", "/api/v1/projects/{{projectId}}/members?limit=20", token="pmToken"),
        request("07 — Lịch sử membership (Supervisor)", "/api/v1/projects/{{projectId}}/members?includeHistory=true&limit=20"),
        request("08 — PM chính + version cho PUT", "/api/v1/projects/{{projectId}}/primary-project-manager", scripts=primary),
        request("09 — Chi tiết tài khoản đã chọn (Supervisor)", "/api/v1/users/{{selectedStaffUserId}}"),
        request("10 — Đọc lại PM chính sau PUT thủ công", "/api/v1/projects/{{projectId}}/primary-project-manager", scripts=primary),
        request("11 — Không đăng nhập (401)", "/api/v1/users", token=None, expected=401),
        request("12 — PM không đọc directory (403)", "/api/v1/users", token="pmToken", expected=403),
        request("13 — PM không đọc lịch sử (403)", "/api/v1/projects/{{projectId}}/members?includeHistory=true", token="pmToken", expected=403),
        request("14 — PM đọc PM chính", "/api/v1/projects/{{projectId}}/primary-project-manager", token="pmToken"),
        request("15 — Query sai (400)", "/api/v1/users?limit=101", expected=400),
    ]
    for label, path in [
        ("Directory", "/api/v1/users?limit=20"),
        ("Members", "/api/v1/projects/{{projectId}}/members?limit=20"),
        ("Primary PM", "/api/v1/projects/{{projectId}}/primary-project-manager"),
        ("Account detail", "/api/v1/users/{{selectedStaffUserId}}")]:
        items.append(request("Cookie — " + label + " (không bearer)", path, cookie=True))
    return {"name": "Nhân sự và phân công", "description":
        "Chọn environment → login H00 Supervisor/PM → 01 chọn dự án → 02/03 lấy userId → 05/08 lấy membershipVersion. "
        "projectSearch/staffSearch tùy chọn, không body GET. 03 tự chọn PM khác currentPrimaryPmUserId nếu đã chạy 08. "
        "Nếu danh sách nhiều kết quả, thu hẹp search rồi chạy lại. Không đổi actorUserId/targetUserId. "
        "Cookie: dùng web csrf/login hiện có trên cùng HTTPS origin, bật cookie jar; không Authorization. "
        "Folder này chỉ GET; chạy cả folder không đổi PM. PUT có trong collection riêng CHỈ chạy khi chủ động chọn. "
        "Import lại collection sau cập nhật; Postman không tự reload file.", "item": items}


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--manual", type=Path)
    parser.add_argument("--output", type=Path, default=Path("docs/postman/RoadGuard-Staff-Membership-Read.postman_collection.json"))
    args = parser.parse_args()
    group = folder()
    variables = ["baseUrl", "supervisorToken", "pmToken", "projectId", "projectSearch", "staffSearch",
        "selectedStaffUserId", "selectedPmUserId", "staffPmCandidateIds", "currentPrimaryPmUserId", "currentPrimaryMembershipVersion"]
    canonical = {"info": {"name": "RoadGuard — Nhân sự và phân công (chỉ đọc)",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"},
        "variable": [{"key": key, "value": ""} for key in variables], "item": [group]}
    write(args.output, canonical)
    if args.manual:
        original = json.loads(args.manual.read_text(encoding="utf-8-sig"))
        updated = copy.deepcopy(original)
        if any(i["name"] == group["name"] for i in updated["item"]):
            raise ValueError("Folder already exists; refusing to replace user-edited requests.")
        updated["item"].append(group)
        present = {v["key"] for v in updated.get("variable", [])}
        updated.setdefault("variable", []).extend({"key": key, "value": ""} for key in variables if key not in present)
        assert updated["item"][:-1] == original["item"]
        write(args.manual, updated)


if __name__ == "__main__":
    main()
