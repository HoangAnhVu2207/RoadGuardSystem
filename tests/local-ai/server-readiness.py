"""Read-only Ubuntu census for the BE/AI deployment; outputs no credentials.

Only mutation is a new private JSON report in the root-owned task directory.
Requires interactive sudo; never changes services, SQL or server configuration.
"""
import json
import os
from pathlib import Path
import pwd
import re
import socket
import stat
import subprocess
import sys

EXPECTED = '7e21430886049a98c0df33a6989acbb4fdc42479'

def run(args, timeout=60):
    result = subprocess.run(args, capture_output=True, text=True, timeout=timeout)
    if result.returncode:
        raise RuntimeError('COMMAND_FAILED_' + Path(args[0]).name.upper())
    return result.stdout

def sql(query):
    # Password is expanded only inside the existing SQL container, never emitted.
    command = ('export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; '
               'exec /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -N -C '
               '-d RoadGuard_Server -b -y 0 -w 65535 -Q "$1"')
    raw = run(['docker', 'exec', 'roadguard-server-sql-1', '/bin/bash', '-c', command,
               '--', 'SET NOCOUNT ON; SET LOCK_TIMEOUT 5000; ' + query])
    # FOR JSON may be split into multiple sqlcmd output rows at 2033 chars.
    return json.loads(''.join(raw.splitlines()).strip())

def main():
    if os.geteuid() != 0 or socket.gethostname() != 'roadguardsystem':
        raise RuntimeError('WRONG_HOST_OR_USER')
    script = Path(__file__).absolute()
    for path in (script, script.parent):
        mode = path.lstat()
        if path.is_symlink() or mode.st_uid != 0 or mode.st_mode & 0o022:
            raise RuntimeError('UNTRUSTED_TASK_PATH')
    active = Path('/etc/roadguard/active-release').read_text().strip()
    if active != EXPECTED or Path('/opt/roadguard/current').resolve().name != active:
        raise RuntimeError('ACTIVE_RELEASE_CHANGED')
    app = json.loads(Path('/etc/roadguard/application.json').read_text())
    report = {'host': socket.gethostname(), 'activeRelease': active, 'sqlReadOnly': True}
    report['migrations'] = sql('SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId FOR JSON PATH;')
    report['tableCounts'] = sql('''SELECT s.name AS schemaName,t.name AS tableName,SUM(p.rows) AS approximateRows
        FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id
        JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1)
        GROUP BY s.name,t.name ORDER BY s.name,t.name FOR JSON PATH;''')
    report['columns'] = sql('''SELECT s.name AS schemaName,t.name AS tableName,c.name AS columnName,
        ty.name AS dataType,c.is_nullable AS nullable
        FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
        JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
        ORDER BY s.name,t.name,c.column_id FOR JSON PATH;''')
    report['databaseOwnerMarkerPresent'] = sql("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RoadGuardServerOwner') THEN 1 ELSE 0 END AS bit) AS present FOR JSON PATH;")[0]['present']
    report['settings'] = {
        'serverDeploymentEnabled': app.get('ServerDeployment', {}).get('Enabled', False),
        'allowedOrigins': app.get('ServerDeployment', {}).get('AllowedOrigins', []),
        'aiWorkerEnabled': app.get('AiRealWorker', {}).get('Enabled', False),
        'minioConfigured': bool(app.get('MinioStorage', {}).get('Endpoint')),
        'smtpUsernamePresent': bool(app.get('IdentityOnboarding', {}).get('SmtpUsername')),
        'smtpPasswordPresent': bool(app.get('IdentityOnboarding', {}).get('SmtpPassword')),
    }
    report['containers'] = []
    for name in ['roadguard-server-api-1', 'roadguard-server-minio-1', 'roadguard-server-sql-1']:
        value = json.loads(run(['docker', 'inspect', '--format', '{{json .}}', name]))
        state, host = value['State'], value['HostConfig']
        report['containers'].append({'name': name, 'image': value['Config']['Image'],
            'running': state['Running'], 'health': state.get('Health', {}).get('Status'),
            'restartCount': value['RestartCount'], 'oom': state['OOMKilled'],
            'memoryLimit': host['Memory'], 'nanoCpus': host['NanoCpus'],
            'portBindings': host.get('PortBindings'),
            'mounts': [{'source': m['Source'], 'destination': m['Destination'], 'rw': m['RW']} for m in value['Mounts']]})
    report['filesystems'] = []
    for directory in ['/srv/roadguard', '/srv/roadguard/backups', '/var/lib/docker']:
        stats = os.statvfs(directory)
        report['filesystems'].append({'path': directory, 'freeBytes': stats.f_bavail * stats.f_frsize})
    report['versions'] = {'docker': run(['docker', '--version']).strip(),
                          'compose': run(['docker', 'compose', 'version', '--short']).strip()}
    destination = script.parent / 'readiness.json'
    if destination.exists() or destination.is_symlink():
        raise RuntimeError('REPORT_EXISTS_READ_IT_BEFORE_RERUN')
    descriptor = os.open(destination, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o640)
    os.fchown(descriptor, 0, pwd.getpwnam('dev').pw_gid)
    with os.fdopen(descriptor, 'w') as stream:
        json.dump(report, stream, indent=2)
    print('READINESS_SAVED: ' + str(destination))

if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        reason = str(error) if isinstance(error, RuntimeError) and re.fullmatch(r'[A-Z0-9_]+', str(error)) else type(error).__name__
        print('READINESS_STOPPED: ' + reason, file=sys.stderr)
        sys.exit(1)
