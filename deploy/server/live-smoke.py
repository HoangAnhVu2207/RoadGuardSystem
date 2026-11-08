#!/usr/bin/env python3
"""Interactive LAN smoke: real HTTPS/cookies/SQL session, no demo seeding.

Passwords and cookies stay in process memory. Does not test FE, AI or upload flows.
"""
import getpass
import http.cookiejar
import json
import ssl
import sys
import urllib.error
import urllib.request
import uuid


def validate_target(target):
    if target != 'https://192.168.1.11:8443':
        raise ValueError('Only the reviewed LAN server target is supported.')


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def expect(label, result, status):
    actual, body = result
    if actual != status:
        raise RuntimeError(f'{label}: expected HTTP {status}, received {actual}')
    print(f'PASS {label}: HTTP {status}', flush=True)
    return body


class Client:
    def __init__(self, target):
        validate_target(target)
        self.target = target
        self.cookies = http.cookiejar.CookieJar()
        context = ssl.create_default_context(cafile='/opt/roadguard/roadguard-lan-ca.crt')
        self.opener = urllib.request.build_opener(
            urllib.request.ProxyHandler({}), NoRedirect(),
            urllib.request.HTTPSHandler(context=context),
            urllib.request.HTTPCookieProcessor(self.cookies))

    def request(self, method, path, payload=None, headers=None):
        request = urllib.request.Request(self.target + path,
                                        data=None if payload is None else json.dumps(payload).encode('utf-8'),
                                        headers={'Content-Type': 'application/json', **(headers or {})}, method=method)
        try:
            response = self.opener.open(request, timeout=20)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            raw = response.read(1024 * 1024)
            try:
                body = json.loads(raw) if raw else {}
            except ValueError:
                body = None
            return response.code, body

    def csrf(self):
        body = expect('CSRF token', self.request('GET', '/api/v1/auth/web/csrf'), 200)
        token = body.get('requestToken') if isinstance(body, dict) else None
        if not token:
            raise RuntimeError('CSRF response has no request token.')
        return {'X-CSRF-TOKEN': token}


def main():
    if not sys.stdin.isatty():
        raise RuntimeError('Run interactively; do not put passwords in command arguments or files.')
    client = Client('https://192.168.1.11:8443')
    expect('health', client.request('GET', '/health'), 200)
    expect('anonymous session denied', client.request('GET', '/api/v1/auth/web/session'), 401)
    email = input('Supervisor email: ').strip()
    password = getpass.getpass('Supervisor password (hidden): ')

    def login():
        return expect('Web login', client.request('POST', '/api/v1/auth/web/login',
                      {'email': email, 'password': password}, client.csrf()), 200)

    session = login()
    if session.get('mustChangePassword'):
        print('Initial password change is required. Enter your new password privately; it will replace the initial password.')
        new_password = getpass.getpass('New password (hidden): ')
        confirmation = getpass.getpass('Repeat new password (hidden): ')
        if new_password != confirmation:
            raise RuntimeError('Passwords differ; no password change submitted.')
        expect('required password change', client.request('POST', '/api/v1/auth/forced-password-change',
               {'username': email, 'currentPassword': password, 'newPassword': new_password,
                'confirmPassword': confirmation, 'operationId': str(uuid.uuid4())}), 204)
        password = new_password
        client.cookies.clear()
        session = login()
    password = None
    if session.get('mustChangePassword'):
        raise RuntimeError('Password-change flag is still set.')
    if session.get('user', {}).get('role') != 'SUPERVISOR':
        raise RuntimeError('The supplied account is not a Supervisor; no business tests executed.')
    try:
        expect('authenticated session', client.request('GET', '/api/v1/auth/web/session'), 200)
        expect('project list via SQL', client.request('GET', '/api/v1/projects?limit=1'), 200)
        denied = expect('logout without CSRF denied', client.request('POST', '/api/v1/auth/web/logout', {},
                        {'Idempotency-Key': str(uuid.uuid4())}), 403)
        if not isinstance(denied, dict) or denied.get('code') != 'csrf_failed':
            raise RuntimeError('Expected csrf_failed denial code.')
        expect('session survives denied write', client.request('GET', '/api/v1/auth/web/session'), 200)
    finally:
        expect('logout', client.request('POST', '/api/v1/auth/web/logout', {},
               {**client.csrf(), 'Idempotency-Key': str(uuid.uuid4())}), 204)
    expect('logged-out session denied', client.request('GET', '/api/v1/auth/web/session'), 401)
    print('LIVE AUTH/READ SMOKE PASSED. Upload, multi-role workflows, FE and AI are NOT VERIFIED by this script.')


if __name__ == '__main__':
    try:
        main()
    except (KeyboardInterrupt, EOFError):
        print('Smoke interrupted; no automatic retry or rollback.', file=sys.stderr)
        sys.exit(2)
    except (RuntimeError, ValueError) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
    except Exception:
        print('Smoke failed (transport/runtime). Response bodies and credentials withheld.', file=sys.stderr)
        sys.exit(1)
