import copy
import json
import unittest
import yaml
from verify_current_docs import audit
from sync_current_docs import BE


class CurrentDocsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.mapping=json.loads((BE/'10_Traceability/api-source-map.json').read_text(encoding='utf-8'))
        cls.spec=yaml.safe_load((BE/'03_API_Contracts/openapi.backend.yaml').read_text(encoding='utf-8'))

    def check_change(self,field,value,expected):
        mapping=copy.deepcopy(self.mapping)
        row=mapping['operations'][0]; row[field]=value
        errors=audit(mapping,self.spec,{'files':{}})
        self.assertIn(expected+' '+row['id'],errors)
        self.assertNotIn('method/path coverage mismatch',errors)

    def test_exact_snapshot(self):
        self.assertEqual(audit(self.mapping,self.spec,{'files':{}}),[])

    def test_auth_tampering(self): self.check_change('security',[{'ForgedGrant':[]}],'security mismatch')
    def test_dto_tampering(self): self.check_change('request',{'fake':'body'},'request mismatch')
    def test_headers_or_status_tampering(self): self.check_change('responses',{'200':{}},'responses mismatch')
    def test_source_tampering(self): self.check_change('sha256','0'*64,'source pin')
    def test_action_tampering(self): self.check_change('action','MissingAction','source locator')
    def test_missing_operation(self): self.assertTrue(audit({'operations':[]},self.spec,{'files':{}}))

    def test_dependency_tampering(self):
        mapping=copy.deepcopy(self.mapping)
        row=next(r for r in mapping['operations'] if r['sourceEvidence'])
        row['sourceEvidence'][0]['sha256']='0'*64
        self.assertIn('dependency source pin '+row['id'],audit(mapping,self.spec,{'files':{}}))

    def test_manifest_drift(self):
        path=self.mapping['operations'][0]['source']
        self.assertIn('source drift '+path,audit(self.mapping,self.spec,{'files':{path:'0'*64}}))


if __name__=='__main__': unittest.main()
