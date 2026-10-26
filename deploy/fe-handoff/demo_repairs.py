"""Owned local Normal/FastTrack proposals, exclusively through existing production APIs.

Does not fabricate Verified/approved/executed states or Supervisor appointments.
The independent acceptance tests cover later transitions separately.
"""
def initialize(j, tokens, request, suffix='a'):
    project = '72f84201-c001-4000-8000-00000000000' + ('1' if suffix == 'a' else '2')
    root = '/api/v1/projects/' + project
    pm, supervisor = tokens['pm-' + suffix], tokens['supervisor']
    prefix = '' if suffix == 'a' else 'b-'
    item_key = 'normal-item' if suffix == 'a' else 'fast-track-item'
    mode = 'NORMAL' if suffix == 'a' else 'FAST_TRACK'
    def etag(reply):
        return next(v for k, v in reply[2].items() if k.lower() == 'etag')
    def command(key, path, body, actor, version=None):
        key = prefix + key
        headers = {'Idempotency-Key': 'handoff-repair-v1-' + key}
        if version:
            headers['If-Match'] = version
        saved = j.once('input-repair-' + key, lambda: {'body': body, 'headers': headers})
        return j.once('repair-' + key, lambda: list(request(
            path, 'POST', saved['body'], actor, saved['headers'], metadata=True)))
    if 'repair-' + prefix + item_key not in j.data['steps']:
        if suffix == 'a':
            report = j.data['steps']['incident']['id']
        else:
            file_id = j.data['steps']['upload']['fileId']
            file = request('/api/v1/reporter-evidence/files/' + file_id, token=tokens['reporter-a'])[1]
            report = command('report', '/api/v1/reports', {
                'description': 'Synthetic FastTrack proposal source; no approval or execution fabricated.',
                'evidence': [{'fileId': file_id, 'fileVersion': file['version'], 'locationSource': 'UNKNOWN'}]
            }, tokens['reporter-a'])[1]['id']
        road = j.data['steps']['ai-confirm-' + suffix][1]
        route = road['routeVersionId']
        segment_set = j.data['steps']['ai-set-' + suffix][1]['id']
        geometry = request(root + '/geometry-package?routeVersionId=' + route +
                           '&segmentSetId=' + segment_set, token=pm, metadata=True)
        # Discover the actual retained report's Case; never select an unrelated Case.
        cursor, case = None, None
        while True:
            path = '/api/v1/cases?pageSize=100'
            if cursor:
                import urllib.parse
                path += '&cursor=' + urllib.parse.quote(cursor, safe='')
            page = request(path, token=supervisor)[1]
            case = next((x for x in page['items'] if report in x['reportIds']), None)
            cursor = page.get('nextCursor')
            if case or not cursor:
                break
        if case is None:
            raise ValueError('Owned synthetic report Case not discoverable')
        current_case = request('/api/v1/cases/' + case['id'], token=supervisor, metadata=True)
        command('triage', '/api/v1/cases/' + case['id'] + '/triage', {
            'projectId': project, 'verificationMethod': 'EXISTING_EVIDENCE',
            'reason': 'Synthetic local repair navigation demo', 'routeVersionId': route,
            'segmentSetId': segment_set, 'geometryVersion': etag(geometry).strip('"')
        }, supervisor, etag(current_case))
        source = request(root + '/candidates?sourceKind=REPORT&sourceId=' + report, token=pm)[1]
        decision = command('candidate', root + '/candidate-decisions', {
            'sourceKind': 'REPORT', 'sourceId': report, 'sourceVersion': source['source']['version'],
            'geometryVersion': source['geometryVersion'], 'decision': 'KEEP_NEW',
            'reason': 'Synthetic development source; not a verified diagnosis',
            'classification': {'defectTypeCode': 'POTHOLE', 'severity': 'LOW', 'roadSectionVersionId': route}
        }, pm)
        defect = decision[1]['defectId']
        detail = request(root + '/defects/' + defect, token=pm, metadata=True)
        package = command('package', root + '/repair-packages', {
            'defectId': defect, 'defectVersion': etag(detail).strip('"'),
            'obligations': [{'kind': 'FORMAL_REPAIR', 'mandatory': True,
                'scope': {'physicalRoadId': road['roadSectionId'], 'routeVersionId': route,
                          'segmentSetId': segment_set, 'fromMeters': 1, 'toMeters': 2,
                          'offsetFromMeters': 0, 'offsetToMeters': 1},
                'reason': 'Synthetic local actual scope'}], 'reason': 'Synthetic local navigation'
        }, pm, etag(detail))
        command(item_key, root + '/repair-packages/' + package[1]['id'] + '/items', {
            'obligationId': package[1]['unresolvedMandatoryObligationIds'][0], 'mode': mode,
            'repairPlan': 'Synthetic proposal; approval and execution remain real user actions',
            'checklistVersion': 'v1', 'reason': 'Development navigation demo'
        }, pm, etag(package))
    package = j.data['steps']['repair-' + prefix + 'package'][1]['id']
    item = j.data['steps']['repair-' + prefix + item_key][1]['id']
    packages = request(root + '/repair-packages', token=pm)[1]['items']
    items = request(root + '/repair-packages/' + package + '/items?mode=' + mode, token=pm)[1]['items']
    if not any(x['id'] == package for x in packages) or not any(x['id'] == item for x in items):
        raise ValueError('Owned actual repair proposal not present in approved collections')
    if suffix == 'b':
        # Owner-approved notification-only assignment; no approval clock or executed state.
        recipient = request('/api/v1/me', token=supervisor)[1]['id']
        assignment_path = root + '/repair-packages/' + package + '/items/' + item + '/notification-supervisor'
        j.once('repair-b-notification-supervisor', lambda: list(request(
            assignment_path, 'PUT', {'supervisorId': recipient}, pm,
            {'Idempotency-Key': 'handoff-b2-assignment-v1', 'If-None-Match': '*'}, metadata=True)))
    if suffix == 'a':
        initialize(j, tokens, request, 'b')
    j.data['repairDemoVersion'] = 2
    j.save()
