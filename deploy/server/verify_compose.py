"""Validate Compose without a daemon or real server secrets."""
import json
import os
from pathlib import Path
import subprocess
import tempfile


def main():
    folder = Path(__file__).resolve().parent
    root = folder.parent.parent
    with tempfile.TemporaryDirectory(prefix='roadguard-compose-') as temporary:
        temp = Path(temporary)
        dummy = temp / 'dummy.env'
        dummy.write_text('MSSQL_SA_PASSWORD=validation-only\nMINIO_ROOT_USER=validation-only\nMINIO_ROOT_PASSWORD=validation-only\n')
        content = (folder / 'compose.yaml').read_text()
        for filename in ('sql.env', 'storage.env'):
            content = content.replace(f'/etc/roadguard/{filename}', dummy.as_posix())
        config = temp / 'compose.yaml'
        config.write_text(content)
        result = subprocess.run(['docker', 'compose', '--project-directory', str(folder), '-f', str(config),
                                 '--profile', 'tools', '--profile', 'ai', 'config', '--format', 'json'],
                                env={**os.environ, 'RG_RELEASE': '0' * 40, 'RG_BIND_IP': '127.0.0.1'},
                                capture_output=True, text=True, check=True)
        services = json.loads(result.stdout)['services']
        assert Path(services['api']['build']['context']).resolve() == root
        assert Path(services['minio']['build']['context']).resolve() == folder
        assert not services['sql'].get('ports'), 'SQL must stay private'
        for service in services.values():
            assert service.get('environment', {}).get('ASPNETCORE_ENVIRONMENT', 'Production') == 'Production'
            for volume in service.get('volumes', []):
                assert not volume.get('source', '').endswith('ca.key'), 'CA private key must stay outside containers'
        assert services['api']['ports'][0]['host_ip'] == '127.0.0.1'
        assert services['minio']['ports'][0]['published'] == '9443'
        assert services['minio']['ports'][0]['target'] == 9000
        ai = services['ai']
        assert ai.get('profiles') == ['ai']
        assert not ai.get('ports'), 'AI must not publish a host port'
        assert set(ai.get('networks', {})) == {'backend'}, 'AI must stay on the private backend network'
        api_env = services['api'].get('environment', {})
        assert api_env['AiRealWorker__WorkerEndpoint'] == 'http://ai:8999/'
        assert api_env['AiRealWorker__ServiceTokenFile'] == '/run/secrets/ai-token'
        assert api_env['AiRealWorker__TempRoot'] == '/ai-be-spool'
        assert api_env['AiRealWorker__FfprobePath'] == '/usr/bin/ffprobe'
        api_sources = {volume.get('source', '') for volume in services['api'].get('volumes', [])}
        assert '/etc/roadguard/ai-token' in api_sources
        assert '/srv/roadguard/ai/be-spool' in api_sources
    print('Compose validated: source contexts, private SQL, loopback ports and CA isolation. No daemon or real credentials used.')


if __name__ == '__main__':
    main()
