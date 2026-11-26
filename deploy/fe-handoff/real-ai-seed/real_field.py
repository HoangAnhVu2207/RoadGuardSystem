"""Exercise field measurement on five selected real detections; field evidence is DEMO."""
import argparse
import json
from pathlib import Path

from demo_flow import PROJECTS, Project, frames
from demo_flow_api import Api, require, save
from demo_flow_repairs import seed_repair


class SurveyProject(Project):
    def task_input(self, defect, crew, purpose):
        body = super().task_input(defect, crew, purpose)
        body.update(sourceKind="SURVEY", surveyId=self.survey, crsProfileRevisionId=self.crs)
        return body


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work", required=True, type=Path)
    parser.add_argument("--media", required=True, type=Path)
    parser.add_argument("--api", default="http://127.0.0.1:28081")
    parser.add_argument("--storage-port", default=29001, type=int)
    args = parser.parse_args()
    private = args.work / "private"
    actors = json.loads((private / "demo-accounts.json").read_text(encoding="utf-8-sig"))
    api = Api(args.api, args.storage_port, actors, private / "real-field-journal")
    data = json.loads((args.work / "real-flow.json").read_text())["projects"]
    receipt = {"projects": {}, "source": "REAL_AI", "fieldEvidence": "DEMO"}
    repairs = {"projects": {}, "source": "REAL_AI", "repairEvidence": "DEMO"}
    for code, count in (("DKD", 3), ("DS2", 2)):
        source = data[code]
        _, suffix, clip = PROJECTS[code]
        supervisor = "supervisor-a" if code == "DKD" else "supervisor-b"
        for role in ["pm-kenh-dong", "pm-duong-so-2", "crew-" + suffix + "-1", "crew-" + suffix + "-2", supervisor]:
            if role not in api.tokens:
                api.login(role)
        p = SurveyProject(api, code, source["foundation"], frames(args.media, clip, args.work / "demo-images"))
        dataset = api.read(p.pm, "/datasets/" + source["datasetId"])
        require(dataset["projectId"] == p.pid and dataset["surveyId"], "Missing source survey")
        p.survey = dataset["surveyId"]
        publication = api.read(p.pm, p.root + "/geometry-map-publications/" + p.f["sampleMapPublicationId"])
        p.crs = publication["crsProfileRevisionId"]
        review = source["pmReview"]
        rows = [(review["sourceDetectionId"], review["defectId"])]
        excluded = {review["sourceDetectionId"], source["pmReject"].get("sourceDetectionId")}
        for candidate in reversed(source["inference"]["candidates"]):
            if candidate["typeCode"] == "REPAIR_PATCH" or candidate["detectionId"] in excluded:
                continue
            ident = candidate["detectionId"]
            item = api.read(p.pm, p.root + "/ai-candidates/" + ident)
            decision = p.cmd("real-field-keep-" + ident, p.pm, p.root + "/candidate-decisions", {
                "sourceKind": "AI_DETECTION", "sourceId": ident, "sourceVersion": item["sourceVersion"],
                "geometryVersion": item["geometryVersion"], "decision": "KEEP_NEW",
                "classification": {"defectTypeCode": item["typeCode"], "severity": "LOW",
                                   "roadSectionVersionId": p.route},
                "reason": "DEMO PM review for field inspection of a real FT95 candidate"})
            rows.append((ident, decision["defectId"]))
            if len(rows) == count:
                break
        require(len(rows) == count, "Not enough eligible candidates")
        measurements = []
        for i, (detection, defect) in enumerate(rows):
            measurement = p.measurement("real-survey-field-" + detection, defect, p.crews[i % 2], "CONFIRM")
            require(measurement["status"].upper() == "COMPLETED", "PM review did not complete inspection")
            measurements.append(dict(detectionId=detection, defectId=defect, **measurement))
        receipt["projects"][code] = measurements
        save(args.work / "real-field.json", receipt)
        print(code, len(measurements), "measurements completed", flush=True)
        detection, defect = rows[0]
        repair = seed_repair(p, "real-normal-" + detection, defect, p.crews[0], "NORMAL", True, None)
        require(repair["state"] == "Confirmed" and repair["finalActor"] == supervisor,
                "Supervisor confirmation missing on the real AI defect")
        repairs["projects"][code] = dict(repair, detectionId=detection, defectId=defect)
        save(args.work / "real-normal-repair.json", repairs)
        print(code, "real AI defect normal repair confirmed", flush=True)


if __name__ == "__main__":
    main()
