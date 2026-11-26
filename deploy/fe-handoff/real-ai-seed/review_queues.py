"""Leave actionable PM measurement and Supervisor Normal approval queues in both projects."""
import argparse
import json
from pathlib import Path

from demo_flow import PROJECTS, Project, frames
from demo_flow_api import Api, require, save
from demo_flow_repairs import seed_repair


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work", required=True, type=Path)
    parser.add_argument("--media", required=True, type=Path)
    parser.add_argument("--api", default="http://127.0.0.1:28081")
    parser.add_argument("--storage-port", default=29001, type=int)
    args = parser.parse_args()
    private = args.work / "private"
    actors = json.loads((private / "demo-accounts.json").read_text(encoding="utf-8-sig"))
    api = Api(args.api, args.storage_port, actors, private / "review-queue-journal")
    for actor in actors:
        api.login(actor["key"])
    foundations = json.loads((args.work / "real-flow.json").read_text())["projects"]
    receipt = {"projects": {}, "provenance": "DEMO"}
    for code, (_, _, clip) in PROJECTS.items():
        p = Project(api, code, foundations[code]["foundation"], frames(args.media, clip, args.work / "demo-images"))
        rows = []
        for index in range(5):
            key = "awaiting-review-" + str(index + 1)
            crew = p.crews[index % 2]
            row = p.report(key, p.reporters[index % 2])
            defect = p.open_defect(row)
            p.verify_defect(row, defect)
            field_source = p.report(key + "-field", p.reporters[index % 2])
            field_defect = p.open_defect(field_source)
            measurement = p.measurement(key, field_defect, crew, None)
            require(measurement["status"] == "Submitted", "Wrong PM queue state")
            repair = seed_repair(p, key, defect, crew, "NORMAL", False, None, await_approval=True)
            require(repair["state"] == "AwaitingApproval", "Wrong Supervisor proposal state")
            rows.append(dict(repairDefectId=defect, fieldDefectId=field_defect,
                             pmMeasurement=measurement, supervisorProposal=repair))
            receipt["projects"][code] = rows
            save(args.work / "review-queues.json", receipt)
        print(code, len(rows), "PM submissions and Supervisor proposals verified", flush=True)
        # Additional independent Normal examples keep ten completion decisions actionable across both roads.
        completion = []
        for index in range(4):
            key = "completion-queue-" + str(index + 1)
            crew = p.crews[(index + 1) % 2]
            row = p.report(key, p.reporters[index % 2])
            defect = p.open_defect(row)
            p.verify_defect(row, defect)
            final = index == 3
            repair = seed_repair(p, key, defect, crew, "NORMAL", final, None)
            require(repair["state"] == ("Confirmed" if final else "Reviewed"), "Wrong repair completion state")
            completion.append(dict(defectId=defect, **repair))
            receipt.setdefault("additionalNormalRepairs", {})[code] = completion
            save(args.work / "review-queues.json", receipt)


if __name__ == "__main__":
    main()
