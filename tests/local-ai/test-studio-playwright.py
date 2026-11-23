"""Real local PM/Crew acceptance. Writes only the opted-in disposable fixture.

Run with the D: test venv and PLAYWRIGHT_BROWSERS_PATH documented in README.
No passwords/tokens are read by this test or placed in the browser.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import shutil
from urllib.parse import urlsplit
from uuid import uuid4

from playwright.sync_api import sync_playwright, expect

ORIGIN = 'http://127.0.0.1:8765'
ROOT = Path('D:/RoadGuard-AI-LocalTest/pm-crew/playwright')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--headless', action='store_true', help='Default shows Chromium for the owner.')
    args = parser.parse_args()
    if shutil.disk_usage('D:/').free < 30 * 1024**3:
        raise RuntimeError('D: must retain at least 30 GiB free.')
    out = ROOT / datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    out.mkdir(parents=True)
    report = {'scope': 'REAL local BE/SQL/MinIO; synthetic route and test measurements',
              'origin': ORIGIN, 'steps': [], 'passed': False}

    def passed(name, **facts):
        report['steps'].append({'name': name, 'status': 'PASS', **facts})
        print('PASS ' + name, flush=True)

    with sync_playwright() as pw:
        # Playwright assertions have their own 5s default, independent of action
        # timeouts. Normal multipart verification can take up to 20s here.
        expect.set_options(timeout=60000)
        browser = pw.chromium.launch(headless=args.headless, slow_mo=350 if not args.headless else 0)
        context = browser.new_context(viewport={'width': 1440, 'height': 1000},
                                      record_video_dir=str(out / 'video'),
                                      record_video_size={'width': 1440, 'height': 1000})
        context.set_default_timeout(60000)
        context.tracing.start(screenshots=True, snapshots=True, sources=False)
        page = context.new_page()
        errors = []
        page.on('pageerror', lambda error: errors.append(str(error)))
        page.on('console', lambda msg: errors.append(msg.text) if msg.type == 'error' else None)
        # Browser navigation/subresources stay on the test UI; storage transfers are
        # performed server-side through the fixture's guarded normal upload facade.
        context.route('**/*', lambda route: route.continue_() if
                      urlsplit(route.request.url).hostname in ('127.0.0.1', 'localhost', '::1')
                      else route.abort())

        def api(role, method, path, body=None):
            response = context.request.post(ORIGIN + '/studio/workflow/be',
                headers={'X-Studio-Request': '1'}, data={
                    'role': role, 'method': method, 'path': path, 'body': body,
                    'idempotencyKey': str(uuid4())})
            assert response.status == 200, f'Local facade status {response.status}'
            return response.json()

        def click(name):
            page.get_by_role('button', name=name, exact=True).click()
            expect(page.locator('#wf-message')).to_have_text('Đã lưu/đọc lại từ BE.')

        try:
            page.goto(ORIGIN)
            page.get_by_role('button', name='03 PM → Crew đo đạc', exact=True).click()
            expect(page.locator('#wf-message')).to_have_text('Đã lưu/đọc lại từ BE.')
            metadata_response = context.request.get(ORIGIN + '/studio/workflow/context')
            assert metadata_response.status == 200
            fixture = metadata_response.json()
            assert fixture['isolatedLocal'] is True and fixture['detectionIds']
            assert not {'accounts', 'password', 'accessToken', 'storageOrigin'} & fixture.keys()
            root = '/api/v1/projects/' + fixture['projectId']
            passed('UI loads isolated REAL fixture without credentials', proposals=len(fixture['detectionIds']))

            facts = None
            for detection_id in fixture['detectionIds']:
                candidate = api('PM', 'GET', root + '/ai-candidates/' + detection_id)
                assert candidate['status'] == 200
                candidate = candidate['body']
                if candidate['typeCode'] != fixture['classMap']['repair'] and not (candidate.get('disposition') or {}).get('decisionId'):
                    facts = candidate
                    break
            assert facts, 'No undecided non-repair proposal remains; restart the disposable host.'
            page.locator('#wf-candidate').select_option(facts['detectionId'])
            expect(page.locator('#wf-message')).to_have_text('Đã lưu/đọc lại từ BE.')
            expect(page.locator('#wf-frame')).to_be_visible()
            expect(page.locator('#wf-drone')).to_contain_text('EMBEDDED_SRT')
            expect(page.locator('#wf-drone')).to_contain_text('Đây không phải vị trí hư hỏng mặt đất.')
            page.wait_for_function("document.querySelector('#wf-frame').complete && document.querySelector('#wf-frame').naturalWidth > 0")
            passed('Actual AI frame and time-aligned embedded SRT are visible', mode=facts['mode'])
            page.screenshot(path=str(out / '01-ai-proposal.jpg'), full_page=True)

            forbidden = api('CREW', 'POST', root + '/candidate-decisions', {
                'sourceKind': 'AI_DETECTION', 'sourceId': facts['detectionId'],
                'sourceVersion': facts['sourceVersion'], 'geometryVersion': facts['geometryVersion'],
                'decision': 'REJECT', 'classification': None, 'reason': 'Playwright negative role check'})
            assert forbidden['status'] == 403 and forbidden['body']['code'] == 'access_forbidden'
            passed('Real BE denies Crew permission to decide AI proposal', http=403)

            # Invalid PM input must not make a candidate decision request.
            mutations = []
            def decision_request(request):
                if request.url.endswith('/studio/workflow/be') and '/candidate-decisions' in (request.post_data or ''):
                    mutations.append(request.url)
            page.on('request', decision_request)
            page.locator('#wf-decision-reason').fill('')
            page.get_by_role('button', name='PM giữ lỗi mới', exact=True).click()
            expect(page.locator('#wf-message')).to_contain_text('Nhập lý do PM.')
            assert mutations == []
            page.remove_listener('request', decision_request)
            passed('Missing PM reason is rejected before mutation')
            page.locator('#wf-decision-reason').fill('Playwright local test: Crew must measure this AI proposal.')
            click('PM giữ lỗi mới')
            expect(page.locator('#wf-defect')).to_contain_text('Defect đã lưu:')
            passed('PM keeps actual AI proposal as Defect')

            click('Giao nhiệm vụ đo')
            expect(page.locator('#wf-task')).to_contain_text('NewAssigned')
            task_id = page.locator('#wf-tasks').input_value()
            report['taskId'] = task_id
            passed('PM assigns a real field inspection task')
            click('Nhận nhiệm vụ')
            expect(page.locator('#wf-task')).to_contain_text('Accepted')
            passed('Assigned Crew accepts task')
            click('Bắt đầu đo')
            expect(page.locator('#wf-task')).to_contain_text('InProgress')
            passed('Crew starts measurement with real task state')

            page.get_by_role('button', name='Gửi kết quả đo', exact=True).click()
            expect(page.locator('#wf-message')).to_contain_text('Nhập số đo không âm.')
            page.locator('#wf-value').fill('1.25')
            page.locator('#wf-location-reason').fill('Local Playwright test: segment markings, no ground GPS.')
            page.locator('#wf-chainage').fill('25')
            page.locator('#wf-markings').check()
            page.get_by_role('button', name='Gửi kết quả đo', exact=True).click()
            expect(page.locator('#wf-message')).to_contain_text('Tải ảnh bằng Crew trước khi gửi bài đo.')
            passed('UI blocks missing measurement and missing photo')

            # Use an actual AI JPEG as explicitly labelled local test evidence.
            frame = context.request.get(ORIGIN + '/studio/workflow/frames/' + facts['detectionId'])
            assert frame.status == 200 and frame.headers['content-type'].startswith('image/jpeg')
            sample = out / 'test-photo-not-field-evidence.jpg'
            sample.write_bytes(frame.body())
            page.locator('#wf-photo').set_input_files(str(sample))
            click('Tải ảnh bằng Crew')
            expect(page.locator('#wf-upload-state')).to_contain_text('MinIO đã xác minh ảnh:')
            passed('Crew photo uses real multipart MinIO upload and verification', uploadStatus='VERIFIED')
            click('Gửi kết quả đo')
            expect(page.locator('#wf-task')).to_contain_text('Submitted')
            expect(page.locator('#wf-readiness')).to_have_text('READY · []')
            passed('Crew submits measurements with READY and no missing reasons')
            page.screenshot(path=str(out / '02-ready-submission.jpg'), full_page=True)

            click('PM lưu quyết định')
            expect(page.locator('#wf-task')).to_contain_text('Completed')
            expect(page.locator('#wf-review-result')).to_contain_text('CONFIRM')
            passed('PM confirms submission and task becomes Completed')
            page.locator('#wf-review-result').scroll_into_view_if_needed()
            page.screenshot(path=str(out / '03-pm-confirmed.jpg'))
            page.screenshot(path=str(out / '03-pm-confirmed-full.jpg'), full_page=True)

            page.reload()
            page.get_by_role('button', name='03 PM → Crew đo đạc', exact=True).click()
            expect(page.locator('#wf-message')).to_have_text('Đã lưu/đọc lại từ BE.')
            page.locator('#wf-tasks').select_option(task_id)
            expect(page.locator('#wf-task')).to_contain_text('Completed')
            expect(page.locator('#wf-readiness')).to_have_text('READY · []')
            readback = api('PM', 'GET', root + '/field-inspection-tasks/' + task_id)
            assert readback['status'] == 200 and readback['body']['status'] == 'Completed'
            passed('Completed task and READY submission persist after reload')

            page.set_viewport_size({'width': 390, 'height': 844})
            assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), 'Mobile horizontal overflow'
            page.screenshot(path=str(out / '04-mobile.jpg'), full_page=True)
            passed('Mobile 390px view has no horizontal overflow')
            assert errors == [], 'Browser error/console entries observed: ' + repr(errors)
            passed('Browser has no JavaScript or console errors')
            report['passed'] = True
        except Exception as error:
            report['error'] = str(error)
            page.screenshot(path=str(out / 'failure.jpg'), full_page=True)
            raise
        finally:
            report['browserErrors'] = errors
            context.tracing.stop(path=str(out / 'trace.zip'))
            video = page.video
            context.close()
            if video:
                video.save_as(str(out / 'pm-crew-playwright.webm'))
            browser.close()
            (out / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
            print('PLAYWRIGHT_REPORT ' + str(out / 'report.json'), flush=True)
            print('PLAYWRIGHT_VIDEO ' + str(out / 'pm-crew-playwright.webm'), flush=True)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
