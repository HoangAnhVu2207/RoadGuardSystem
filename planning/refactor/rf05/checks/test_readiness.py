"""Regression checks for decision provenance, scope and regeneration."""

import copy
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
MODULE = ROOT / "planning/refactor/tools/rf05_readiness.py"
if not MODULE.exists():
    MODULE = ROOT / "planning/refactor/rf05/drafts/rf05_readiness.py.draft"
namespace = {}
exec(compile(MODULE.read_text(encoding="utf-8"), str(MODULE), "exec"), namespace)
validate = namespace["validate_readiness"]
apply = namespace["apply_readiness"]
digest = namespace["digest"]


class ReadinessTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        plan = self.root / "planning/refactor"
        plan.mkdir(parents=True)
        self.wording = "Confirmed exact scope for both excerpts."
        self.source = "planning/refactor/decisions.md"
        self.anchor = "new-owner-decision-2026-10-01"
        self.status_line = "Status: Accepted by owner on 2026-10-01."
        (self.root / self.source).write_text(
            f"## New owner decision 2026-10-01\n{self.status_line}\n\n"
            f"| ID | Meaning | Scope |\n|---|---|---|\n| D-NEW | {self.wording} | Both excerpts for FR-27 |\n", encoding="utf-8")
        self.row = {"id": "FR-27", "source_authority": "HISTORICAL_SOURCE_ONLY",
                    "authority": "HISTORICAL_SOURCE_ONLY", "implementation_readiness": "BLOCKED",
                    "claims": [{"id": "FR-27.C01", "content": "first historical claim", "authority": "HISTORICAL_SOURCE_ONLY",
                                "current_authority": "HISTORICAL_SOURCE_ONLY"},
                               {"id": "FR-27.C02", "content": "second historical claim", "authority": "HISTORICAL_SOURCE_ONLY",
                                "current_authority": "HISTORICAL_SOURCE_ONLY"}],
                    "missing_decision": "review", "blocked_checkpoint": "RF-10 START"}
        self.scope = {"decision_id": "D-NEW", "decision_source": f"{self.source}#{self.anchor}",
                      "decision_claim": self.wording, "scope": "Both excerpts for FR-27",
                      "claim_ids": ["FR-27.C01", "FR-27.C02"]}
        self.decision = {"status": "Accepted", "source": self.source, "anchor": self.anchor,
                         "status_line": self.status_line, "content": self.wording,
                         "confirmed_by": "owner", "confirmed_at": "2026-10-01",
                         "requirements": {"FR-27": {"scope": self.scope["scope"],
                                                         "claim_sha256": {c["id"]: digest(c["content"]) for c in self.row["claims"]}}}}
        self.index_path = plan / "04-decision-index.json"
        self.save_index()
        self.ready = {"state": "READY", "approved_scopes": [self.scope],
                      "required_evidence": ["Planned isolated HTTP and SQL checks"]}

    def save_index(self):
        self.index_path.write_text(json.dumps({"schema_version": 1, "decisions": {"D-NEW": self.decision}}), encoding="utf-8")

    def generated(self, readiness=None):
        registry = self.root / "registry.json"
        registry.write_text(json.dumps({"schema_version": 1, "decisions": {"FR-27": readiness or self.ready}}), encoding="utf-8")
        return apply([copy.deepcopy({k: v for k, v in self.row.items() if k != "source_authority"})],
                     self.root, registry, self.root / "absent.json")[0]

    def tearDown(self):
        self.tmp.cleanup()

    def test_accepted_decision_generates_ready_without_mutating_source_origin(self):
        row = self.generated()
        self.assertEqual(row["source_authority"], "HISTORICAL_SOURCE_ONLY")
        self.assertEqual(row["authority"], "TARGET_CONFIRMED_BY_DECISION")
        self.assertEqual(row["implementation_readiness"], "READY")
        self.assertIs(validate(row, self.root), row)

    def test_partial_scope_stays_blocked(self):
        self.scope["claim_ids"] = ["FR-27.C01"]
        self.decision["requirements"]["FR-27"]["claim_sha256"].pop("FR-27.C02")
        self.save_index()
        partial = {"state": "BLOCKED", "approved_scopes": [self.scope],
                   "reason": "second claim unknown", "checkpoint": "RF-10 START"}
        row = self.generated(partial)
        self.assertEqual(row["authority"], "PARTIALLY_CONFIRMED_BY_DECISION")
        self.assertEqual(row["claims"][1]["current_authority"], "HISTORICAL_SOURCE_ONLY")
        with self.assertRaisesRegex(ValueError, "partial decision"):
            self.generated({**self.ready, "approved_scopes": [self.scope]})

    def test_historical_path_does_not_grant_authority(self):
        invalid = copy.deepcopy(self.ready)
        invalid["approved_scopes"][0]["decision_source"] = "docs/product/historical-fr-br.md#fr-27"
        with self.assertRaisesRegex(ValueError, "source/anchor mismatch"):
            self.generated(invalid)

    def test_proposed_or_unknown_decision_rejected(self):
        for status in ("Proposed", "Unknown"):
            self.decision["status"] = status
            self.save_index()
            with self.subTest(status=status), self.assertRaisesRegex(ValueError, "not Accepted"):
                self.generated()

    def test_unrelated_decision_or_claim_rejected(self):
        for change, error in (({"decision_id": "D-OTHER"}, "unrelated"),
                              ({"claim_ids": ["FR-01.C01"]}, "unrelated")):
            invalid = copy.deepcopy(self.ready)
            invalid["approved_scopes"][0].update(change)
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, error):
                self.generated(invalid)

    def test_missing_source_anchor_or_wording_rejected(self):
        for key, value, error in (("decision_source", "wrong.md#anchor", "source/anchor"),
                                  ("decision_claim", "not owner wording", "does not match")):
            invalid = copy.deepcopy(self.ready)
            invalid["approved_scopes"][0][key] = value
            with self.subTest(key=key), self.assertRaisesRegex(ValueError, error):
                self.generated(invalid)
        (self.root / self.source).unlink()
        with self.assertRaisesRegex(ValueError, "missing decision source"):
            self.generated()

    def test_changed_claim_rejected(self):
        self.row["claims"][0]["content"] += " changed"
        with self.assertRaisesRegex(ValueError, "confirmed claim content changed"):
            self.generated()

    def test_scope_must_be_in_decision_source(self):
        self.decision["requirements"]["FR-27"]["scope"] = "Scope absent from owner record"
        self.scope["scope"] = "Scope absent from owner record"
        self.save_index()
        with self.assertRaisesRegex(ValueError, "scope unrelated"):
            self.generated()

    def test_decision_source_must_name_requirement(self):
        source = self.root / self.source
        source.write_text(source.read_text(encoding="utf-8").replace("Both excerpts for FR-27", "Both excerpts for another module"),
                          encoding="utf-8")
        self.scope["scope"] = "Both excerpts for another module"
        self.decision["requirements"]["FR-27"]["scope"] = self.scope["scope"]
        self.save_index()
        with self.assertRaisesRegex(ValueError, "scope unrelated"):
            self.generated()

    def test_forged_generated_output_rejected(self):
        row = self.generated()
        row["authority"] = "HISTORICAL_SOURCE_ONLY"
        with self.assertRaisesRegex(ValueError, "generated authority/readiness mismatch"):
            validate(row, self.root)

    def test_regeneration_requires_registry_record(self):
        first = self.generated()
        previous = self.root / "previous.json"
        previous.write_text(json.dumps({"fr": [first], **{k: [] for k in ("br", "us", "pf", "nfr")}}), encoding="utf-8")
        registry = self.root / "registry.json"
        regenerated = apply([copy.deepcopy({k: v for k, v in self.row.items() if k != "source_authority"})],
                            self.root, registry, previous)
        self.assertEqual(regenerated[0]["readiness"], first["readiness"])
        registry.write_text(json.dumps({"schema_version": 1, "decisions": {}}), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "missing from registry"):
            apply([copy.deepcopy(self.row)], self.root, registry, previous)


if __name__ == "__main__":
    unittest.main()
