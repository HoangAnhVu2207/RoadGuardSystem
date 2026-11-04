import unittest
from route_inventory import discover


class RouteInventoryTests(unittest.TestCase):
    def parse(self, header='', attrs='[HttpGet("items/{id:guid}")]'):
        return discover(header + '\npublic sealed class SampleController {\n' + attrs + '\npublic Task<IActionResult> Read() => null;\n}')

    def test_action_only_c1(self):
        self.assertEqual(self.parse(attrs='[Authorize, HttpPost("api/v{version:apiVersion}/publications")]')[0]['path'], '/api/v1/publications')

    def test_relative(self):
        self.assertEqual(self.parse('[Route("api/v{version:apiVersion}/projects")]')[0]['path'], '/api/v1/projects/items/{id}')

    def test_absolute_overrides(self):
        for template in ('/public/items', '~/public/items'):
            self.assertEqual(self.parse('[Route("ignored")]', '[HttpGet("' + template + '")]')[0]['path'], '/public/items')

    def test_empty_suffix(self):
        self.assertEqual(self.parse('[Route("api/items")]', '[HttpGet]')[0]['path'], '/api/items')

    def test_route_tokens_and_multiple_bases(self):
        rows = self.parse('[Route("api/[controller]/[action]"), Route("legacy")]', '[HttpGet]')
        self.assertEqual([x['path'] for x in rows], ['/api/Sample/Read', '/legacy'])

    def test_missing_template_rejected(self):
        with self.assertRaises(ValueError): self.parse(attrs='[HttpGet]')

    def test_nonliteral_rejected(self):
        with self.assertRaises(ValueError): self.parse(attrs='[HttpGet(Routes.Value)]')

    def test_comments_not_operations(self):
        self.assertEqual(len(self.parse(attrs='// [HttpDelete("fake")]\n[HttpGet("real")]')), 1)


if __name__ == '__main__': unittest.main()
