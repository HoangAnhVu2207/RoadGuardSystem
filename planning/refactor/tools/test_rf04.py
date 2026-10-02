"""Small mutation tests for RF-04 review regressions; reads draft artifacts only."""

import copy
import json
import unittest
from pathlib import Path

import yaml

from build_rf04_crosswalk import version
from verify_rf04 import ROOT, PLAN, verify_operation, verify_pilot, verify_source


class Rf04RegressionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.operation = json.loads((PLAN / "04-operation-crosswalk.json").read_text(encoding="utf-8"))
        cls.source = json.loads((PLAN / "04-source-crosswalk.json").read_text(encoding="utf-8"))
        cls.pilot = yaml.safe_load((ROOT / "contracts/http/work-package.proposed.yaml").read_text(encoding="utf-8"))

    def test_inspection_task_owner_mismatch_is_rejected(self):
        altered = copy.deepcopy(self.operation)
        next(r for r in altered["current"] if r["id"] == "C027")["rf_task"] = "RF-10-02"
        with self.assertRaises(AssertionError):
            verify_operation(altered)

    def test_emergency_wrong_primary_task_is_rejected(self):
        altered = copy.deepcopy(self.source)
        next(r for r in altered["br"] if r["id"] == "BR-46")["rf_task"] = "RF-10-09-A"
        with self.assertRaises(AssertionError):
            verify_source(altered)

    def test_api_version_is_not_raw_exact(self):
        self.assertNotEqual("/api/v1/me/inspection-tasks", "/api/v2/me/inspection-tasks")
        self.assertNotEqual(version("/api/v1/me/inspection-tasks"), version("/api/v2/me/inspection-tasks"))
        altered = copy.deepcopy(self.operation)
        row = next(r for r in altered["draft"] if r["operation_id"] == "listMyInspectionTasks")
        row["raw_route"] = "/api/v2/me/inspection-tasks"
        row["version"] = "2"
        row["match_kind"] = "exact_raw"
        with self.assertRaises(AssertionError):
            verify_operation(altered)

    def test_operation_count_change_reports_delta(self):
        altered = copy.deepcopy(self.operation)
        new = copy.deepcopy(altered["draft"][0])
        new.update({"id": "D-new", "operation_id": "newDraftOperation", "route": "/new-draft",
                    "raw_route": "/new-draft", "normalized_route": "/new-draft", "version": "UNSPECIFIED",
                    "current_matches": [], "match_kind": "none", "transition_owner": None})
        altered["draft"].append(new)
        altered["counts"]["draft_operations"] += 1
        altered["counts"]["unique_operation_ids"] += 1
        delta = verify_operation(altered)
        self.assertEqual(delta["draft_operations"], 1)

    def test_nested_schema_missing_or_broken_ref_is_rejected(self):
        for mutation in ("missing", "broken"):
            with self.subTest(mutation=mutation):
                altered = copy.deepcopy(self.pilot)
                road = altered["components"]["schemas"]["ProjectWorkPackageResponse"]["properties"]["roadSections"]
                if mutation == "missing":
                    road["items"] = {"type": "object"}
                else:
                    road["items"] = {"$ref": "#/components/schemas/MissingRoadSection"}
                with self.assertRaises((AssertionError, KeyError)):
                    verify_pilot(altered)


if __name__ == "__main__":
    unittest.main()
