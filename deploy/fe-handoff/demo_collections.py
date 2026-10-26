"""Additive approved collection demo. API-only, owner-validated by scenarios.main; no deletes."""
PROJECTS = [('a', '72f84201-c001-4000-8000-000000000001'), ('b', '72f84201-c001-4000-8000-000000000002')]
COLLECTIONS = ['repair-packages', 'exports', 'survey-plans', 'survey-tasks', 'processing-jobs', 'ai-mock-runs', 'offline/devices', 'offline/packages', 'offline/grants', 'retention/evaluations']
def initialize(j, tokens, request):
 for suffix, project in PROJECTS:
  root = '/api/v1/projects/' + project
  token = tokens['pm-' + suffix]
  # Evaluate only the already uploaded synthetic project video. Never select the entire inventory.
  file_id = j.data['steps']['ai-upload-' + suffix][1]['fileId']
  key = 'collections-v1-evaluation-' + suffix
  j.once(key, lambda: request(root + '/retention/evaluations', 'POST', {'fileIds': [file_id]}, token,
      {'Idempotency-Key': 'handoff-' + key})[1])
  for route in COLLECTIONS:
   status, page = request(root + '/' + route, token=token)
   if status != 200 or set(page) != {'items', 'nextCursor'} or not isinstance(page['items'], list):
    raise ValueError('Approved collection profile mismatch')
 j.data['collectionsVersion'] = 1
 j.save()
