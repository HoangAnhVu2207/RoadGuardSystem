"""Local authenticated readiness probe: never print credentials or response bodies."""
import json
import os
from pathlib import Path
import sys
import urllib.request


def ready():
    try:
        with Path(os.environ['ROADGUARD_WORKER_TOKEN_FILE']).open('rb') as source:
            raw = source.read(1025)
        token = raw.decode('ascii').rstrip('\r\n')
        if len(raw) > 1024 or len(token) < 32 or any(ord(c) <= 32 or ord(c) >= 127 for c in token):
            return False
        request = urllib.request.Request('http://127.0.0.1:8999/internal/v1/health',
                                         headers={'Authorization': 'Bearer ' + token})
        # No proxy lookup or redirect forwarding of the private bearer credential.
        class NoRedirect(urllib.request.HTTPRedirectHandler):
            def redirect_request(self, *args, **kwargs):
                return None
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
        with opener.open(request, timeout=3) as response:
            return response.status == 200 and json.loads(response.read(1024)) == {'status': 'ok'}
    except Exception:
        return False


if __name__ == '__main__':
    ok = ready()
    print('ready' if ok else 'not-ready')
    sys.exit(0 if ok else 1)
