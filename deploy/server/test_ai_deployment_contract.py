from pathlib import Path
import unittest

ROOT = Path(__file__).parent

class AiReleaseContractTests(unittest.TestCase):
    def test_release_extraction_pins_and_materializes_ai_submodule(self):
        source = (ROOT / 'common.sh').read_text()
        self.assertIn('ls-tree', source)
        self.assertIn('git clone', source)
        self.assertIn('checkout --detach', source)
        self.assertIn('AI/roadguard-ai-engine', source)

    def test_reviewed_updater_builds_ai_and_prepares_assets_before_downtime(self):
        source = (ROOT / 'update-reviewed.sh').read_text()
        self.assertIn('prepare_ai_assets', source)
        self.assertIn('compose build api minio ai', source)

    def test_bootstrap_handoff_runs_target_updater_after_legacy_extraction(self):
        source = (ROOT / 'update-reviewed.sh').read_text()
        self.assertIn('RG_BOOTSTRAP_REENTRY', source)
        self.assertIn('RG_BOOTSTRAP_CANDIDATE', source)
        self.assertIn('exec env RG_BOOTSTRAP_REENTRY=1', source)

    def test_runtime_composition_enables_private_ai_only_when_assets_are_ready(self):
        compose = (ROOT / 'compose.yaml').read_text()
        common = (ROOT / 'common.sh').read_text()
        manage = (ROOT / 'manage.sh').read_text()
        self.assertIn('AiRealWorker__Enabled', compose)
        self.assertIn('--profile ai', common)
        self.assertIn('compose up -d --wait --wait-timeout 300 ai', manage)
        self.assertIn('ft95.pt', common)
        self.assertIn('bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0', common)
        self.assertIn('$RG_AI_SUBMODULE/.git', common)

if __name__ == '__main__':
    unittest.main()
