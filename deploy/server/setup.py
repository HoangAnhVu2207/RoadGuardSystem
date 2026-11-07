"""One-time root-run setup. Secrets are generated on the server, never printed."""
import argparse
import base64
import copy
import getpass
import ipaddress
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import uuid

CONFIG = Path('/etc/roadguard')
DATA = Path('/srv/roadguard')
PUBLIC_CA = Path('/opt/roadguard/roadguard-lan-ca.crt')


def validate_host(value):
    address = ipaddress.IPv4Address(value)
    if not address.is_private or address.is_loopback or address.is_unspecified or address.is_multicast:
        raise ValueError('Use the dedicated server LAN IPv4 address.')
    return str(address)


def configuration(host, email, display_name, password):
    host = validate_host(host)
    if len(email) > 256 or not re.fullmatch(r'[^\s@]+@[^\s@]+\.[^\s@]+', email) or not display_name.strip() or len(display_name) > 100:
        raise ValueError('A valid Supervisor email and display name are required.')
    if not 16 <= len(password) <= 128 or not all((any(c.isupper() for c in password), any(c.islower() for c in password),
                                    any(c.isdigit() for c in password), any(not c.isalnum() for c in password))):
        raise ValueError('Password needs 16+ characters including upper/lower/digit/symbol.')
    token = lambda: 'Rg1!' + secrets.token_urlsafe(40)
    signing = lambda: base64.b64encode(secrets.token_bytes(48)).decode('ascii')
    sa, runtime, storage, pfx = (token() for _ in range(4))
    origin = f'https://{host}:8443'
    app = {
        'AllowedHosts': f'{host};localhost',
        'ServerDeployment': {'Enabled': True, 'Owner': uuid.uuid4().hex, 'KeyPath': '/keys', 'AllowedOrigins': [origin]},
        'RoadGuardDatabase': {'ConnectionString': f'Server=sql;Database=RoadGuard_Server;User ID=roadguard_app;Password={runtime};Encrypt=true;TrustServerCertificate=false',
                              'InitializeOnStartup': False, 'SeedDevelopmentUsers': False, 'MaxRetryCount': 0,
                              'EnableSensitiveDataLogging': False, 'EnableDetailedErrors': False},
        'Jwt': {'Issuer': origin, 'Audience': 'roadguard-clients', 'ActiveKeyId': 'server-v1',
                'SigningKeys': {'server-v1': signing()}, 'AccessTokenLifetimeMinutes': 15, 'SessionLifetimeHours': 24, 'RefreshTokenLifetimeDays': 7},
        'PasswordChangeFingerprint': {'Key': signing()},
        'IdentityOnboarding': {'Secret': signing(), 'FrontendBaseUrl': origin, 'SmtpUsername': '', 'SmtpPassword': ''},
        'MinioStorage': {'Endpoint': 'https://minio:9000', 'PublicEndpoint': f'https://{host}:9443', 'AccessKey': 'roadguard',
                         'SecretKey': storage, 'BucketName': 'roadguard-private', 'UseSsl': True},
        'FileStorage': {'RootPath': '/files', 'MaximumSizeBytes': 8589934592},
        'UploadSession': {'RecoveryEnabled': True},
        'Anh02': {'WorkersEnabled': True, 'MockEnabled': False, 'Export': {'UnicodeFontPath': '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'}},
        'Sandbox': {'Enabled': False}, 'Handoff': {'Enabled': False},
        'AiServiceJwt': {'Enabled': False},
        'Kestrel': {'Endpoints': {'Https': {'Url': 'https://+:8443', 'Certificate': {'Path': '/run/tls/server.pfx', 'Password': pfx}}}},
        'Logging': {'LogLevel': {'Default': 'Warning', 'Microsoft': 'Warning'}}
    }
    maintenance = copy.deepcopy(app)
    maintenance['RoadGuardDatabase']['ConnectionString'] = f'Server=sql;Database=RoadGuard_Server;User ID=sa;Password={sa};Encrypt=true;TrustServerCertificate=false'
    maintenance['ServerDeployment'].update(RuntimePassword=runtime, InitialSupervisor={'Email': email, 'DisplayName': display_name, 'Password': password})
    return app, maintenance, sa, storage, pfx


def require_empty_target(config=CONFIG, data=DATA):
    if config.exists() or config.is_symlink() or data.exists() or data.is_symlink():
        raise ValueError('Existing /etc/roadguard or /srv/roadguard: setup refuses overwrite. Use lifecycle update instead.')


def write(path, value, uid=0, gid=10001, mode=0o640):
    path.write_text(value, encoding='utf-8')
    path.chmod(mode)
    os.chown(path, uid, gid)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--host', required=True, type=validate_host)
    args = parser.parse_args()
    if not hasattr(os, 'geteuid') or os.geteuid() != 0:
        raise SystemExit('Run this setup with sudo on the Ubuntu server.')
    require_empty_target(CONFIG, DATA)
    email = input('First Supervisor email: ').strip()
    name = input('Supervisor display name: ').strip()
    password = getpass.getpass('Initial password (16+ chars, upper/lower/digit/symbol): ')
    if password != getpass.getpass('Repeat password: '):
        raise SystemExit('Passwords do not match. Nothing was created.')
    app, maintenance, sa, storage, pfx = configuration(args.host, email, name, password)
    os.umask(0o077)
    CONFIG.mkdir(mode=0o700)
    DATA.mkdir(mode=0o700)
    tls = CONFIG / 'tls'
    tls.mkdir(mode=0o755)
    os.chmod(tls, 0o755)
    for folder, uid in [('sql', 10001), ('objects', 10001), ('keys', 1654), ('files', 1654), ('backups', 0)]:
        path = DATA / folder
        path.mkdir(mode=0o700)
        os.chown(path, uid, 10001)
    write(CONFIG / 'application.json', json.dumps(app, indent=2, ensure_ascii=False), uid=1654)
    write(CONFIG / 'maintenance.json', json.dumps(maintenance, indent=2, ensure_ascii=False), uid=1654)
    write(CONFIG / 'sql.env', f'MSSQL_SA_PASSWORD={sa}\n', mode=0o600)
    write(CONFIG / 'storage.env', f'MINIO_ROOT_USER=roadguard\nMINIO_ROOT_PASSWORD={storage}\nMINIO_API_CORS_ALLOW_ORIGIN=https://{args.host}:8443\n', mode=0o600)
    write(CONFIG / 'server.env', f'RG_BIND_IP={args.host}\n', mode=0o600)
    write(CONFIG / 'mssql.conf', '[network]\ntlscert = /run/tls/server.crt\ntlskey = /run/tls/server.key\ntlsprotocols = 1.2\nforceencryption = 1\n', mode=0o644)
    write(tls / 'server.ext', f'subjectAltName=DNS:sql,DNS:minio,DNS:localhost,IP:{args.host},IP:127.0.0.1\nextendedKeyUsage=serverAuth\nkeyUsage=critical,digitalSignature,keyEncipherment\nbasicConstraints=critical,CA:FALSE\n', mode=0o600)
    commands = [
        ['req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '3650', '-subj', '/CN=RoadGuard LAN CA', '-addext', 'basicConstraints=critical,CA:TRUE', '-addext', 'keyUsage=critical,keyCertSign,cRLSign', '-keyout', 'ca.key', '-out', 'ca.crt'],
        ['req', '-newkey', 'rsa:2048', '-nodes', '-subj', '/CN=sql', '-keyout', 'server.key', '-out', 'server.csr'],
        ['x509', '-req', '-in', 'server.csr', '-CA', 'ca.crt', '-CAkey', 'ca.key', '-CAcreateserial', '-days', '365', '-extfile', 'server.ext', '-out', 'server.crt'],
        ['pkcs12', '-export', '-out', 'server.pfx', '-inkey', 'server.key', '-in', 'server.crt', '-certfile', 'ca.crt', '-passout', 'env:RG_PFX_PASSWORD'],
        ['verify', '-CAfile', 'ca.crt', '-verify_hostname', 'sql', 'server.crt'],
        ['verify', '-CAfile', 'ca.crt', '-verify_ip', args.host, 'server.crt']
    ]
    for command in commands:
        subprocess.run(['openssl', *command], cwd=tls, env={**os.environ, 'RG_PFX_PASSWORD': pfx},
                       check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    for path in tls.iterdir():
        os.chown(path, 0, 10001)
        path.chmod(0o600 if path.name == 'ca.key' else 0o640)
    (tls / 'ca.crt').chmod(0o644)
    # Only the public CA is exported for clients; its private key stays root-only.
    write(PUBLIC_CA, (tls / 'ca.crt').read_text(), gid=0, mode=0o644)
    print('Private configuration and LAN certificates created. No demo data. Initial Supervisor must change password after login.')
    print('SMTP is not configured: invitations/OTP/password recovery must be configured and tested before use.')


if __name__ == '__main__':
    main()
