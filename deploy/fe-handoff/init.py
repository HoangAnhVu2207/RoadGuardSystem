import base64,json,os,secrets,uuid,pathlib
p=pathlib.Path('/state'); p.mkdir(exist_ok=True)
token_volume=pathlib.Path('/ai-token'); token_volume.mkdir(exist_ok=True)
# One init container per project; reuses completed state. Never regenerates partial state.
if (p/'config.json').exists():
    required=['sql-password','storage.env','seed-credentials.json','demo-accounts.json','ai-token']
    if not all((p/f).is_file() for f in required): raise SystemExit('Incomplete private state; recovery required, no regeneration')
    if not (token_volume/'ai-token').is_file():
        (token_volume/'ai-token').write_text((p/'ai-token').read_text())
    if (token_volume/'ai-token').read_text() != (p/'ai-token').read_text():
        raise SystemExit('Worker credential does not match this handoff')
    (token_volume/'ai-token').chmod(0o444)
    raise SystemExit(0)
if any(p.iterdir()): raise SystemExit('Partial private state refused')
owner=uuid.uuid4().hex
key=lambda:secrets.token_urlsafe(48)
signing_key=base64.b64encode(secrets.token_bytes(48)).decode('ascii')
sql='Rg1!'+key(); storage=key()
actors=json.loads(pathlib.Path('/catalog/accounts.json').read_text())['Actors']
passwords=[dict(ActorId=a['Id'],Password='Rg1!'+key()) for a in actors]
files={'sql-password':sql,'storage.env':'MINIO_ROOT_USER=sandbox\nMINIO_ROOT_PASSWORD='+storage+'\n','seed-credentials.json':json.dumps(dict(BundleId=str(uuid.uuid4()),Passwords=passwords)),'demo-accounts.json':json.dumps([dict(email=a['Email'],role=a['Role'],key=a['Key'],password=pw['Password']) for a,pw in zip(actors,passwords)],indent=2)}
ai_token='Rg1!'+key()
files['ai-token']=ai_token
config={
    'Handoff': {'Enabled':True, 'Owner':owner, 'AccountCatalog':'real-accounts.json', 'ScenarioVersion':2, 'DataLabel':'REAL_AI_DEMO_DATA', 'AllowedOrigins':os.environ.get('RG_FE_ORIGINS','http://localhost:5173,http://localhost:8081,http://127.0.0.1:5173,http://127.0.0.1:8081').split(',')},
    'Anh02': {'WorkersEnabled':True, 'MockEnabled':False, 'Export':{'UnicodeFontPath':'/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'}},
    'Sandbox': {'Enabled':True, 'MailHost':'mailpit', 'KeyPath':'/keys'},
    'RoadGuardDatabase': {'ConnectionString':f'Server=sql;Database=RoadGuard_FE_{owner};User ID=sa;Password={sql};Encrypt=true;TrustServerCertificate=true', 'InitializeOnStartup':False, 'SeedDevelopmentUsers':False},
    'AiRealWorker': {'Enabled':True, 'WorkerEndpoint':'http://ai:8999/', 'ServiceTokenFile':'/run/secrets/ai-token', 'ReleasedModelVersionId':'72f84201-d033-4000-8000-000000000002', 'CheckpointSha256':'bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0', 'PreprocessingVersion':'ffmpeg-pts-v1', 'ConfigVersion':'ft95-cpu-v1', 'SampleFps':1, 'MaxFrames':180, 'ClassMap':{'alligator crack':'ALLIGATOR_CRACK','longitudinal crack':'LONGITUDINAL_CRACK','oblique crack':'OBLIQUE_CRACK','pothole':'POTHOLE','repair':'REPAIR_PATCH','transverse crack':'TRANSVERSE_CRACK'}, 'TempRoot':'/ai-be-spool', 'FfprobePath':'/usr/bin/ffprobe'},
    'Jwt': {'Issuer':'roadguard-handoff', 'Audience':'roadguard-handoff', 'ActiveKeyId':'handoff', 'SigningKeys':{'handoff':signing_key}, 'AccessTokenLifetimeMinutes':15, 'SessionLifetimeHours':24, 'RefreshTokenLifetimeDays':7},
    'PasswordChangeFingerprint': {'Key':base64.b64encode(secrets.token_bytes(48)).decode('ascii')},
    'IdentityOnboarding': {'Secret':base64.b64encode(secrets.token_bytes(48)).decode('ascii'), 'FrontendBaseUrl':'http://localhost:5173'},
    'MinioStorage': {'Endpoint':'http://minio:9000', 'PublicEndpoint':'http://localhost:'+os.environ.get('RG_FE_OBJECT_PORT','19001'), 'AccessKey':'sandbox', 'SecretKey':storage, 'BucketName':'roadguard-private', 'UseSsl':False},
    'Logging': {'LogLevel':{'Default':'Warning','Microsoft':'Warning'}}
}
for name,data in files.items():
    f=p/name; f.write_text(data); f.chmod(0o444)
token_file=token_volume/'ai-token'; token_file.write_text(ai_token); token_file.chmod(0o444)
f=p/'config.json'; f.write_text(json.dumps(config)); f.chmod(0o444)
print('Private handoff state initialized; values omitted.')
