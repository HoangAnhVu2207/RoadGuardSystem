"""Owned SQL/MinIO + real production API subprocess fault acceptance; no secrets in evidence.

Prerequisites: build API and MultipartHarness, existing owner-approved local MinIO
at 127.0.0.1:19000; RGM_MINIO_CONFIG points to ignored JSON containing minioUser/
minioPassword. Never run against deployed/shared targets. Creates one isolated
SQL container/database and unique bucket; keeps completed test objects as evidence.
"""
import base64, concurrent.futures, hashlib, http.client, http.server, json, os
import pathlib, secrets, socket, struct, subprocess, threading, time, urllib.error
import urllib.parse, urllib.request, uuid, zlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'artifacts' / 'multipart-recovery'
OUT.mkdir(parents=True, exist_ok=True)
RUN = uuid.uuid4().hex
NAME = 'roadguard-multipart-' + RUN
BUCKET = NAME
DB = 'RoadGuard_Multipart_' + RUN
LABEL = 'multipart-' + RUN
MINIO = 'roadguard-anh02-live-minio'
config = json.loads(pathlib.Path(os.environ['RGM_MINIO_CONFIG']).read_text())
inspected = json.loads(subprocess.check_output(['docker', 'inspect', MINIO]))[0]
assert inspected['Config']['Labels'].get('roadguard.task') == 'anh02-runtime-d2414844'
assert '19000/tcp' in inspected['NetworkSettings']['Ports']
env = os.environ.copy()
env['MSSQL_SA_PASSWORD'] = secrets.token_urlsafe(32) + 'aA1!'
env['RGM_PASSWORD'] = secrets.token_urlsafe(24) + 'aA1!'
env['RGM_RUN'] = RUN
env['RGM_STORAGE'] = 'http://127.0.0.1:19000'
env['RGM_ACCESS'] = config['minioUser']
env['RGM_SECRET'] = config['minioPassword']
HARNESS = ROOT / 'tools/RoadGuardSystem.MultipartHarness/bin/Debug/net8.0/RoadGuardSystem.MultipartHarness.dll'
API = ROOT / 'RoadGuardSystem.API/bin/Debug/net8.0/RoadGuardSystem.eAPI.dll'
proof = {'run': RUN, 'base': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT).decode().strip(), 'injected': True, 'cases': [], 'storageEndpoint': env['RGM_STORAGE'], 'bucket': BUCKET, 'database': DB}
proof['sourceHashes'] = {name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in (
    'RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs',
    'RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.Recovery.cs',
    'RoadGuardSystem.Repositories/Implementations/Storage/MinioUploadObjectStorage.cs',
    'RoadGuardSystem.BusinessObjects/Files/UploadSession.cs',
    'RoadGuardSystem.API/Program.cs', 'RoadGuardSystem.API/Workers/MultipartRecoveryWorker.cs')}
proof['apiAssemblyHash']=hashlib.sha256(API.read_bytes()).hexdigest()

active_api = None
proxy = None

def free_port():
    with socket.socket() as s: s.bind(('127.0.0.1', 0)); return s.getsockname()[1]

def sql(*args):
    r = subprocess.run(['dotnet', str(HARNESS), *args], cwd=ROOT, env=env, capture_output=True, text=True)
    if r.returncode: raise RuntimeError('Isolated SQL/storage harness failed; diagnostics retained locally')
    return json.loads(r.stdout.strip().splitlines()[-1]) if r.stdout.strip() else None

def request_http(path, body=None, token=None, key=None, match=None, url=None, method=None):
    headers = {}
    if token: headers['Authorization'] = 'Bearer ' + token
    if key: headers['Idempotency-Key'] = key
    if match: headers['If-Match'] = match
    data = json.dumps(body).encode() if body is not None else None
    if data: headers['Content-Type'] = 'application/json'
    try:
        with urllib.request.urlopen(urllib.request.Request(url or BASE + path, data=data, headers=headers, method=method), timeout=45) as r:
            b = r.read(); return r.status, json.loads(b) if r.headers.get_content_type() == 'application/json' or r.headers.get_content_type() == 'application/problem+json' else b, dict(r.headers)
    except urllib.error.HTTPError as e:
        b = e.read(); return e.code, json.loads(b) if b else None, dict(e.headers)

def wait_for(fn, seconds=50):
    until = time.monotonic() + seconds
    while time.monotonic() < until:
        result = fn()
        if result: return result
        time.sleep(.5)
    raise AssertionError('Acceptance deadline elapsed')

class FaultProxy(http.server.BaseHTTPRequestHandler):
    mode = 'none'
    initiated = threading.Event()
    release = threading.Event()
    remote_ids = []
    def log_message(self, *args): pass
    def do_GET(self): self.forward()
    def do_POST(self): self.forward()
    def do_PUT(self): self.forward()
    def do_DELETE(self): self.forward()
    def forward(self):
        assert self.path.split('?')[0].startswith('/' + BUCKET + '/')
        data = self.rfile.read(int(self.headers.get('Content-Length', 0)))
        upstream = http.client.HTTPConnection('127.0.0.1', 19000, timeout=30)
        upstream.request(self.command, self.path, body=data, headers=dict(self.headers))
        r = upstream.getresponse(); b = r.read()
        initiating = self.command == 'POST' and urllib.parse.urlsplit(self.path).query.startswith('uploads')
        if initiating and r.status == 200 and FaultProxy.mode != 'none':
            import xml.etree.ElementTree as ET
            upload_id = next(n.text for n in ET.fromstring(b).iter() if n.tag.endswith('UploadId'))
            FaultProxy.remote_ids.append(hashlib.sha256(upload_id.encode()).hexdigest())
            selected = FaultProxy.mode; FaultProxy.mode = 'none'; FaultProxy.initiated.set()
            if selected == 'block': FaultProxy.release.wait(60)
            if selected == 'drop':
                error=b'<Error><Code>ServiceUnavailable</Code><Message>Synthetic gateway lost acknowledgement</Message></Error>'
                self.send_response(503); self.send_header('Content-Length',str(len(error))); self.end_headers(); self.wfile.write(error); upstream.close(); return
        try:
            self.send_response(r.status)
            for k, v in r.getheaders():
                if k.lower() not in ('transfer-encoding', 'connection'): self.send_header(k, v)
            self.end_headers(); self.wfile.write(b)
        except (BrokenPipeError, ConnectionResetError): pass
        upstream.close()

def start_api(storage_endpoint, recovery=True):
    global active_api, BASE
    assert active_api is None
    port = free_port(); BASE = 'http://127.0.0.1:' + str(port)
    settings = {'ASPNETCORE_ENVIRONMENT':'Development', 'ASPNETCORE_URLS':BASE,
        'RoadGuardDatabase__ConnectionString':env['RGM_SQL'], 'RoadGuardDatabase__InitializeOnStartup':'false', 'RoadGuardDatabase__SeedDevelopmentUsers':'false',
        'MinioStorage__Endpoint':storage_endpoint, 'MinioStorage__BucketName':BUCKET, 'MinioStorage__AccessKey':env['RGM_ACCESS'], 'MinioStorage__SecretKey':env['RGM_SECRET'], 'MinioStorage__UseSsl':'false',
        'Jwt__Issuer':'multipart-isolated', 'Jwt__Audience':'multipart-isolated', 'Jwt__ActiveKeyId':'run', 'Jwt__SigningKeys__run':base64.b64encode(secrets.token_bytes(32)).decode(),
        'Jwt__AccessTokenLifetimeMinutes':'120', 'Jwt__SessionLifetimeHours':'8', 'Jwt__RefreshTokenLifetimeDays':'30',
        'PasswordChangeFingerprint__Key':base64.b64encode(secrets.token_bytes(32)).decode(), 'IdentityOnboarding__Secret':base64.b64encode(secrets.token_bytes(32)).decode(),
        'Anh02__WorkersEnabled':'false', 'Anh02__MockEnabled':'false', 'Huy01__EnableLifecycleAndCase':'false',
        'UploadSession__RecoveryEnabled':str(recovery).lower(), 'UploadSession__RecoveryRetrySeconds':'1', 'UploadSession__RecoveryDeadlineSeconds':'20', 'Logging__LogLevel__Default':'Warning'}
    # Development auto-enables recovery for configured storage; grace prevents the fault window race.
    if storage_endpoint != env['RGM_STORAGE']: settings['UploadSession__RecoveryRetrySeconds'] = '10'
    api_env = env.copy(); api_env.update(settings)
    log = open(OUT / ('api-' + RUN + '-' + str(port) + '.log'), 'w')
    active_api = subprocess.Popen(['dotnet', str(API)], cwd=API.parent, env=api_env, stdout=log, stderr=log, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    def ready():
        if active_api.poll() is not None: raise RuntimeError('Owned API exited before readiness')
        try: return request_http('/health')[0] == 200
        except (OSError, urllib.error.URLError): return False
    wait_for(ready, 40)
    return active_api.pid

def stop_api(crash=False):
    global active_api
    assert active_api is not None
    pid = active_api.pid
    if crash: active_api.kill()
    else: active_api.terminate()
    active_api.wait(timeout=15); active_api = None
    return pid

def login(who='owner'):
    status, body, _ = request_http('/api/v1/auth/login', {'email':who + '-' + RUN + '@multipart.example.test', 'password':env['RGM_PASSWORD']})
    assert status == 200, ('login', status)
    return body['accessToken']

def chunk(kind, b): return struct.pack('>I', len(b)) + kind + b + struct.pack('>I', zlib.crc32(kind+b))
# Real synthetic PNG, not a MIME header stub; >8MiB exercises two multipart PUTs.
pixels = b''.join(b'\0' + hashlib.shake_256(('row-'+str(i)).encode()).digest(7500) for i in range(1200))
BYTES = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB',2500,1200,8,2,0,0,0)) + chunk(b'IDAT',zlib.compress(pixels)) + chunk(b'IEND',b'')
HASH = hashlib.sha256(BYTES).hexdigest()
assert 8388608 < len(BYTES) < 20971520

def create(token):
    status, body, _ = request_http('/api/v1/reporter-evidence/uploads', {'fileName':'synthetic-recovery.png','mediaType':'image/png','sizeBytes':len(BYTES),'checksumSha256':HASH}, token, uuid.uuid4().hex)
    assert status == 201, ('create', status)
    return body['id'], body['fileId']

def parts(token, upload, key, numbers=(1,2)):
    return request_http('/api/v1/reporter-evidence/uploads/'+upload+'/part-urls', {'partNumbers':list(numbers)}, token, key)

def finish(token, upload, file, urls):
    completed=[]
    for part in urls['parts']:
        n=part['partNumber']; data=BYTES[(n-1)*8388608:n*8388608]
        with urllib.request.urlopen(urllib.request.Request(part['url'], data=data, method='PUT'), timeout=30) as r:
            assert r.status == 200; completed.append({'partNumber':n, 'eTag':r.headers['ETag'].strip('"')})
    status, _, headers = request_http('/api/v1/reporter-evidence/uploads/'+upload, token=token); assert status == 200
    complete_key=uuid.uuid4().hex; body={'checksumSha256':HASH, 'parts':completed}
    assert request_http('/api/v1/reporter-evidence/uploads/'+upload+'/complete',body,token,complete_key,headers['ETag'])[0] == 202
    assert request_http('/api/v1/reporter-evidence/uploads/'+upload+'/complete',body,token,complete_key,headers['ETag'])[0] == 202
    def verified():
        status, b, _ = request_http('/api/v1/reporter-evidence/files/'+file, token=token)
        return status == 200 and b['status'] == 'VERIFIED'
    wait_for(verified, 40)
    status, download, _ = request_http('/api/v1/reporter-evidence/files/'+file+'/content', token=token)
    assert status == 200 and hashlib.sha256(download).hexdigest() == HASH

try:
    subprocess.run(['docker','run','-d','--name',NAME,'--label','roadguard.task='+LABEL,'-e','ACCEPT_EULA=Y','-e','MSSQL_SA_PASSWORD','-p','127.0.0.1::1433','mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04'], env=env, capture_output=True, check=True)
    info = json.loads(subprocess.check_output(['docker','inspect',NAME]))[0]
    assert info['Config']['Labels']['roadguard.task'] == LABEL
    env['RGM_SERVER']=info['Config']['Hostname']
    sql_port=info['NetworkSettings']['Ports']['1433/tcp'][0]['HostPort']
    env['RGM_SQL']='Server=127.0.0.1,'+sql_port+';Database='+DB+';User Id=sa;Password='+env['MSSQL_SA_PASSWORD']+';TrustServerCertificate=True;'
    initialized=None
    for _ in range(40):
        try: initialized=sql('init'); break
        except RuntimeError: time.sleep(1)
    assert initialized and not initialized['pendingModelChanges']; proof['targetVerified']=initialized
    proxy=http.server.ThreadingHTTPServer(('127.0.0.1',0),FaultProxy)
    threading.Thread(target=proxy.serve_forever,daemon=True).start()
    proxy_url='http://127.0.0.1:'+str(proxy.server_port)
    for mode in ('drop','block'):
        print('Live case starting:',mode,flush=True)
        initial_pid=start_api(proxy_url); token=login(); upload,file=create(token); key=uuid.uuid4().hex
        FaultProxy.mode=mode; FaultProxy.initiated.clear(); FaultProxy.release.clear()
        with concurrent.futures.ThreadPoolExecutor() as pool:
            request=pool.submit(parts,token,upload,key)
            assert FaultProxy.initiated.wait(25), 'Remote initiation must succeed before fault'
            if mode == 'block':
                stop_api(crash=True); FaultProxy.release.set()
                try: request.result(timeout=10)
                except (OSError, urllib.error.URLError): pass
            else:
                result=request.result(timeout=30); assert result[0] == 503 and result[1]['code']=='upload_storage_unavailable', ('lost ack',result[0]); stop_api()
        before=sql('status',upload); assert before['candidates'] == 1 and before['StorageUploadId'] is None
        new_pid=start_api(env['RGM_STORAGE']); assert new_pid != initial_pid
        token=login(); other=login('other')
        assert parts(other,upload,key)[0] == 404
        # SQL deadline has not expired; one candidate is adopted, even after real process termination.
        def recover():
            r=parts(token,upload,key)
            assert r[0] in (200,503), ('recovery',r[0])
            return r if r[0] == 200 else None
        result=wait_for(recover,25)
        assert parts(token,upload,key)[0] == 200
        assert parts(token,upload,key,(2,))[0] == 409
        with concurrent.futures.ThreadPoolExecutor() as pool:
            concurrent_statuses=list(pool.map(lambda k:parts(token,upload,k)[0],[key,key,uuid.uuid4().hex]))
        assert concurrent_statuses == [200,200,200]
        accepted=sql('status',upload); assert accepted['candidates'] == 1 and accepted['MultipartPhase'] == 'DURABLE'
        for part in result[1]['parts']:
            assert urllib.parse.parse_qs(urllib.parse.urlsplit(part['url']).query)['uploadId'][0] == accepted['StorageUploadId']
        version_before=request_http('/api/v1/reporter-evidence/uploads/'+upload,token=token)[2]['ETag']
        sql('duplicate',upload) # provider/HTTP transport duplicate, authoritative binding remains unchanged
        cleaned_active=wait_for(lambda: (s if (s:=sql('status',upload))['candidates']==1 else None),20)
        assert cleaned_active['StorageUploadId'] == accepted['StorageUploadId']
        assert request_http('/api/v1/reporter-evidence/uploads/'+upload,token=token)[2]['ETag'] == version_before
        finish(token,upload,file,result[1]); final=sql('status',upload); assert final['status'] == 'Verified' and final['candidates'] == 0
        proof['cases'].append({'mode':mode,'initialPid':initial_pid,'newPid':new_pid,'processKilled':mode=='block','before':before,'after':final,'concurrentStatuses':concurrent_statuses,'sha256':HASH,'bytes':len(BYTES),'remoteAcknowledgementsCaptured':len(FaultProxy.remote_ids)})
        stop_api(); print('Live case PASS:',mode,flush=True)
    # Ambiguity terminal/restart and late orphan cleanup via actual hosted worker, without a retry client.
    initial_pid=start_api(proxy_url); token=login(); upload,file=create(token)
    FaultProxy.mode='drop'; FaultProxy.initiated.clear()
    assert parts(token,upload,uuid.uuid4().hex)[0] == 503
    sql('duplicate',upload); stop_api()
    start_api(env['RGM_STORAGE']); token=login()
    terminal=wait_for(lambda: (s if (s:=sql('status',upload))['status']=='Failed' and s['candidates']==0 else None),25)
    assert parts(token,upload,uuid.uuid4().hex)[0] == 409
    sql('duplicate',upload) # late old side effect at proven task-owned terminal key
    cleaned=wait_for(lambda: (s if (s:=sql('status',upload))['candidates']==0 else None),20)
    new_upload,new_file=create(token); status,urls,_=parts(token,new_upload,uuid.uuid4().hex); assert status==200
    finish(token,new_upload,new_file,urls)
    proof['cases'].append({'mode':'ambiguity-terminal-late-cleanup-restart','terminal':terminal,'lateCleaned':cleaned,'restart':sql('status',new_upload),'sha256':HASH})
    proof['result']='PASS'; print('Live cases: 3 PASS / 0 FAIL; actual HTTP+SQL+MinIO, real owned process kill.',flush=True)
finally:
    if active_api is not None: stop_api()
    if proxy is not None: proxy.shutdown()
    proof.setdefault('result','FAIL')
    (OUT/('live-'+RUN+'.json')).write_text(json.dumps(proof,indent=2))
    current=json.loads(subprocess.check_output(['docker','inspect',NAME]))[0]
    assert current['Config']['Labels']['roadguard.task']==LABEL
    subprocess.run(['docker','rm','-f',NAME],capture_output=True,check=True)
    print('Evidence:',str(OUT/('live-'+RUN+'.json')),flush=True)
