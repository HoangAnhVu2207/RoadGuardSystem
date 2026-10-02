import copy
import unittest

from rf09_transition_guard import compare_openapi, compare_sql


class TransitionGuardTests(unittest.TestCase):
    def setUp(self):
        self.openapi = {
            "paths": {"/files": {"post": {"responses": {"201": {"content": {"application/json": {"schema": {"$ref": "#/components/schemas/File"}}}}}}}},
            "components": {"schemas": {"File": {"type": "object", "required": ["sizeBytes"],
                "properties": {"sizeBytes": {"type": "integer", "minimum": 1, "maximum": 2147483647}}}}},
        }
        self.inventory = {"sql": {group: [] for group in
            ("tables", "columns", "keys", "foreignKeys", "indexes", "checks", "triggers")}}

    def test_unchanged_is_empty(self):
        changes = []
        compare_openapi(self.openapi, copy.deepcopy(self.openapi), changes)
        compare_sql(self.inventory, copy.deepcopy(self.inventory), {"Files"}, changes)
        self.assertEqual([], changes)

    def test_required_enum_range_status_and_media_changes_detected(self):
        new = copy.deepcopy(self.openapi)
        schema = new["components"]["schemas"]["File"]
        schema["required"].append("kind")
        schema["properties"]["sizeBytes"]["maximum"] = 100
        schema["properties"]["kind"] = {"enum": ["VIDEO"]}
        new["paths"]["/files"]["post"]["responses"]["201"]["content"] = {"application/problem+json": {}}
        changes = []
        compare_openapi(self.openapi, new, changes)
        labels = {item["path"]: item["classification"] for item in changes}
        self.assertEqual("POTENTIALLY_BREAKING", labels["schemas.File.required"])
        self.assertEqual("POTENTIALLY_BREAKING", labels["schemas.File.properties.sizeBytes.maximum"])
        self.assertEqual("POTENTIALLY_BREAKING", labels["operations.POST /files.responses.201.content.application/json"])
        self.assertEqual("ADDITIVE", labels["schemas.File.properties.kind"])

    def test_nullable_column_and_operation_are_additive(self):
        new_api = copy.deepcopy(self.openapi)
        new_api["paths"]["/health"] = {"get": {"responses": {"200": {}}}}
        new_sql = copy.deepcopy(self.inventory)
        new_sql["sql"]["columns"].append({"schema": "dbo", "table": "Files", "name": "SizeBytes64",
                                             "type": "bigint", "nullable": True})
        changes = []
        compare_openapi(self.openapi, new_api, changes)
        compare_sql(self.inventory, new_sql, {"Files"}, changes)
        self.assertEqual(2, len(changes))
        self.assertEqual({"ADDITIVE"}, {item["classification"] for item in changes})

    def test_sql_type_change_requires_review(self):
        before = copy.deepcopy(self.inventory)
        before["sql"]["columns"].append({"schema": "dbo", "table": "Files", "name": "SizeBytes",
                                            "type": "int", "nullable": False})
        after = copy.deepcopy(before)
        after["sql"]["columns"][0]["type"] = "bigint"
        changes = []
        compare_sql(before, after, {"Files"}, changes)
        self.assertEqual("POTENTIALLY_BREAKING", changes[0]["classification"])

    def test_nullable_and_enum_narrowing_require_review(self):
        before = copy.deepcopy(self.openapi)
        before["components"]["schemas"]["File"]["properties"]["sizeBytes"].update(
            {"nullable": True, "enum": [1, 2]})
        after = copy.deepcopy(before)
        after["components"]["schemas"]["File"]["properties"]["sizeBytes"].update(
            {"nullable": False, "enum": [1]})
        changes = []
        compare_openapi(before, after, changes)
        self.assertEqual(2, len(changes))
        self.assertEqual({"POTENTIALLY_BREAKING"}, {item["classification"] for item in changes})

    def test_malformed_inventory_fails_closed(self):
        with self.assertRaises(ValueError):
            compare_sql(self.inventory, {"sql": {"columns": "bad"}}, {"Files"}, [])


if __name__ == "__main__":
    unittest.main()
