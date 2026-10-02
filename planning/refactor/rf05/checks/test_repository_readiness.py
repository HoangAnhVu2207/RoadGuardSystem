"""Coverage invariants for the read-only repository readiness check."""

import unittest

from verify_repository_readiness import compare_requirement_ids


def rows(*ids):
    return [{"id": ident} for ident in ids]


class RequirementCoverageTests(unittest.TestCase):
    def test_new_mapped_requirement_is_valid_delta(self):
        self.assertEqual(compare_requirement_ids(rows("FR-01", "FR-38"),
                                                 rows("FR-01", "FR-38"),
                                                 {"FR-01": "RF-01", "FR-38": "RF-10"}), -146)

    def test_missing_mapping_rejected(self):
        with self.assertRaisesRegex(ValueError, "unmapped"):
            compare_requirement_ids(rows("FR-01", "FR-38"), rows("FR-01", "FR-38"), {"FR-01": "RF-01"})

    def test_stale_generated_requirement_rejected(self):
        with self.assertRaisesRegex(ValueError, "mapping drift"):
            compare_requirement_ids(rows("FR-01"), rows("FR-01", "FR-38"),
                                    {"FR-01": "RF-01", "FR-38": "RF-10"})

    def test_duplicate_id_rejected(self):
        with self.assertRaisesRegex(ValueError, "duplicate requirement ID"):
            compare_requirement_ids(rows("FR-01", "FR-01"), rows("FR-01"), {"FR-01": "RF-01"})


if __name__ == "__main__":
    unittest.main()
