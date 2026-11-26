"""Exercise signed offline handover on the accepted measurement tasks in the local seed."""
import argparse
import json
import subprocess
from pathlib import Path


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work", required=True, type=Path)
    parser.add_argument("--dll", required=True, type=Path)
    parser.add_argument("--api", default="http://127.0.0.1:28081")
    args = parser.parse_args()
    private = args.work / "private"
    actors = {u["key"]: u for u in load(private / "demo-accounts.json")}
    projects = load(args.work / "demo-flow.json")["projects"]
    if set(projects) != {"DKD", "DS2"}:
        raise SystemExit("Demo flow must complete both projects before offline verification")
    output = args.work / "offline-flow.json"
    receipt = load(output) if output.exists() else {"projects": {}}
    for code in ("DKD", "DS2"):
        row = projects[code]
        data = {"api": args.api, "project": row["projectId"], "task": row["offlineTaskId"]}
        for name, field in (("crew", "offlineCrew"), ("pm", "pm"), ("supervisor", "supervisor")):
            data[name] = {k: actors[row[field]][k] for k in ("email", "password")}
        if code in receipt["projects"]:
            data["previous"] = receipt["projects"][code]
        result = subprocess.run(["dotnet", str(args.dll)], input=json.dumps(data),
                                capture_output=True, text=True, timeout=180)
        if result.returncode:
            raise SystemExit(f"{code} offline failed: " + result.stderr.strip()[:250])
        receipt["projects"][code] = json.loads(result.stdout)
        temp = output.with_suffix(".tmp")
        temp.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        temp.replace(output)
        print(code, "offline import and durable readback verified")


if __name__ == "__main__":
    main()
