"""Guidance discovery regression fixtures; no repository files are modified."""

import tempfile
import unittest
from pathlib import Path

from verify_guidance import verify_inventory


class GuidanceInventoryTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        (self.root / "AGENTS.md").write_text("new guidance", encoding="utf-8")
        self.manifest = {"status": "PROPOSED_INACTIVE", "acceptance_source": None,
                         "accepted_at": None, "guidance_paths": ["AGENTS.md"],
                         "retired_guidance": [".agents/skills/roadguard-old/SKILL.md"], "skills": []}

    def tearDown(self):
        self.tmp.cleanup()

    def test_valid_inventory(self):
        self.assertEqual(verify_inventory(self.root, self.manifest, True), ["AGENTS.md"])

    def test_retired_skill_reappears(self):
        path = self.root / ".agents/skills/roadguard-old/SKILL.md"
        path.parent.mkdir(parents=True)
        path.write_text("old", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "old active guidance remains"):
            verify_inventory(self.root, self.manifest, True)

    def test_new_declared_skill_allowed(self):
        path = self.root / ".agents/skills/new-review/SKILL.md"
        path.parent.mkdir(parents=True)
        path.write_text("new", encoding="utf-8")
        self.manifest["guidance_paths"].append(".agents/skills/new-review/SKILL.md")
        self.manifest["skills"].append({"path": ".agents/skills/new-review/SKILL.md",
                                        "status": "PROPOSED_INACTIVE"})
        self.assertEqual(len(verify_inventory(self.root, self.manifest, True)), 2)

    def test_undeclared_skill_rejected(self):
        path = self.root / ".agents/skills/new-review/SKILL.md"
        path.parent.mkdir(parents=True)
        path.write_text("new", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "undeclared discoverable guidance"):
            verify_inventory(self.root, self.manifest, True)

    def test_nested_agent_guide_outside_manifest_rejected(self):
        path = self.root / "RoadGuardSystem.API/AGENTS.md"
        path.parent.mkdir(parents=True)
        path.write_text("undisclosed", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "undeclared discoverable guidance"):
            verify_inventory(self.root, self.manifest, True)

    def test_skill_status_must_match_manifest_mode(self):
        path = self.root / ".agents/skills/new-review/SKILL.md"
        path.parent.mkdir(parents=True)
        path.write_text("new", encoding="utf-8")
        self.manifest["guidance_paths"].append(".agents/skills/new-review/SKILL.md")
        self.manifest["skills"].append({"path": ".agents/skills/new-review/SKILL.md",
                                        "status": "ACTIVE_AFTER_OWNER_ACCEPTANCE"})
        with self.assertRaisesRegex(ValueError, "skill status mismatch"):
            verify_inventory(self.root, self.manifest, True)

    def test_active_without_acceptance_rejected(self):
        self.manifest["status"] = "ACTIVE_AFTER_OWNER_ACCEPTANCE"
        with self.assertRaisesRegex(ValueError, "missing owner acceptance metadata"):
            verify_inventory(self.root, self.manifest, False)


if __name__ == "__main__":
    unittest.main()
