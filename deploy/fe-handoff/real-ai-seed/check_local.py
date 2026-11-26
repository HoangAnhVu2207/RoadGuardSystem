"""Verify the real handoff through loopback and export the local account guide privately."""
import argparse
import json
import time
from collections import Counter
from pathlib import Path
from urllib.parse import urlencode, urlsplit
from urllib.request import ProxyHandler, Request, build_opener

ROLES = {1: "Supervisor", 2: "PM", 3: "Drone Operator", 4: "Repair Crew", 5: "Reporter"}
HTTP = build_opener(ProxyHandler({}))


def loopback(origin):
    url = urlsplit(origin)
    if url.hostname not in {"localhost", "127.0.0.1", "::1"} or url.scheme != "http" or url.path not in {"", "/"}:
        raise ValueError("Expected local handoff HTTP origin")
    return origin.rstrip("/")


def request(url, data=None):
    payload = json.dumps(data).encode() if data is not None else None
    req = Request(url, data=payload, headers={"Content-Type": "application/json"})
    with HTTP.open(req, timeout=12) as response:
        raw = response.read()
        return response.status, json.loads(raw) if raw else {}


def mail_ids(mailpit, email):
    _, page = request(mailpit + "/api/v1/search?" + urlencode({"query": "to:" + email}))
    return {item["ID"] for item in page["messages"] if any(
        target["Address"].casefold() == email.casefold() for target in item["To"])}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api", required=True, type=loopback)
    parser.add_argument("--mailpit", required=True, type=loopback)
    parser.add_argument("--credentials", required=True, type=Path)
    parser.add_argument("--receipt", required=True, type=Path)
    args = parser.parse_args()
    users = json.loads(args.credentials.read_text(encoding="utf-8-sig"))
    catalog = json.loads(Path(__file__).with_name("accounts.json").read_text())["Actors"]
    if {(a["key"], a["email"], a["role"]) for a in users} != {(a["Key"], a["Email"], a["Role"]) for a in catalog} or len(users) != 14:
        raise ValueError("Generated accounts differ from the approved catalog")
    ready_status, ready = request(args.api + "/ready")
    if ready_status != 200 or ready.get("status") != "ready":
        raise RuntimeError("BE readiness failed")
    for account in users:
        _, result = request(args.api + "/api/v1/auth/android/login",
                            {key: account[key] for key in ("email", "password")})
        if not result.get("accessToken") or not result.get("user", {}).get("id"):
            raise RuntimeError("Login contract failed for " + account["key"])
    reporter = next(u for u in users if u["role"] == 5)
    email = reporter["email"]
    before = mail_ids(args.mailpit, email)
    status, _ = request(args.api + "/api/v1/auth/reporter/password-reset/request", {"email": email})
    if status != 202:
        raise RuntimeError("Reporter reset request did not return 202")
    for _ in range(30):
        delivered = mail_ids(args.mailpit, email) - before
        if delivered:
            break
        time.sleep(1)
    else:
        raise RuntimeError("Mailpit did not receive a new message for the selected Reporter")
    guide = args.credentials.with_name("TAI-KHOAN-DOCKER-LOCAL.md")
    lines = ["# Tài khoản RoadGuard dành cho FE", "",
             f"BE trên máy chạy Docker ở {args.api}. Nhận email thử ở {args.mailpit}.",
             "Mật khẩu bên dưới thuộc bộ dữ liệu local trên máy này.", "",
             "| Vai trò | Tài khoản | Email | Mật khẩu local |", "|---|---|---|---|"]
    for user in users:
        lines.append(f"| {ROLES[user['role']]} | {user['key']} | {user['email']} | `{user['password']}` |")
    guide.write_text("\n".join(lines) + "\n", encoding="utf-8")
    report = {"status": "PASS", "ready": ready_status, "loginCount": len(users),
              "loginsByRole": dict(Counter(ROLES[u["role"]] for u in users)),
              "resetRequest": status, "newMailpitDelivery": True}
    args.receipt.parent.mkdir(parents=True, exist_ok=True)
    args.receipt.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("14 local logins and new Mailpit delivery verified")


if __name__ == "__main__":
    main()
