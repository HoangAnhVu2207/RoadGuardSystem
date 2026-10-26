import base64,json,os,secrets,uuid,pathlib
p=pathlib.Path('/state'); p.mkdir(exist_ok=True)
# One init container per project; reuses completed state. Never regenerates partial state.
if (p/'config.json').exists():
    required=['sql-password','storage.env','seed-credentials.json','demo-accounts.json']
    if not all((p/f).is_file() for f in required): raise SystemExit('Incomplete private state; recovery required, no regeneration')
    raise SystemExit(0)
if any(p.iterdir()): raise SystemExit('Partial private state refused')
owner=uuid.uuid4().hex
key=lambda:secrets.token_urlsafe(48)
signing_key=base64.b64encode(secrets.token_bytes(48)).decode('ascii')
sql='Rg1!'+key(); storage=key()
actors=json.loads(pathlib.Path('/catalog/accounts.json').read_text())['Actors']
passwords=[dict(ActorId=a['Id'],Password='Rg1!'+key()) for a in actors]
files={'sql-password':sql,'storage.env':'MINIO_ROOT_USER=sandbox\nMINIO_ROOT_PASSWORD='+storage+'\n','seed-credentials.json':json.dumps(dict(BundleId=str(uuid.uuid4()),Passwords=passwords)),'demo-accounts.json':json.dumps([dict(email=a['Email'],role=a['Role'],key=a['Key'],password=pw['Password']) for a,pw in zip(actors,passwords)],indent=2)}
config={'Handoff':{'Enabled':True,'Owner':owner,'ScenarioVersion':1,'DataLabel':'MOCK_AI_DEVELOPMENT_DATA','AllowedOrigins':os.environ.get('RG_FE_ORIGINS','http://localhost:5173,http://localhost:8081,http://127.0.0.1:5173,http://127.0.0.1:8081').split(',')},'Anh02':{'WorkersEnabled':True,'MockEnabled':True,'Export':{'UnicodeFontPath':'/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'}},'Sandbox':{'Enabled':True,'MailHost':'mailpit','KeyPath':'/keys'},'RoadGuardDatabase':{'ConnectionString':f'Server=sql;Database=RoadGuard_FE_{owner};User ID=sa;Password={sql};Encrypt=true;TrustServerCertificate=true','InitializeOnStartup':False,'SeedDevelopmentUsers':False},'Jwt':{'Issuer':'roadguard-handoff','Audience':'roadguard-handoff','ActiveKeyId':'handoff','SigningKeys':{'handoff':signing_key},'AccessTokenLifetimeMinutes':15,'SessionLifetimeHours':24,'RefreshTokenLifetimeDays':7},'PasswordChangeFingerprint':{'Key':base64.b64encode(secrets.token_bytes(48)).decode('ascii')},'IdentityOnboarding':{'Secret':base64.b64encode(secrets.token_bytes(48)).decode('ascii'),'FrontendBaseUrl':'http://localhost:5173'},'MinioStorage':{'Endpoint':'http://minio:9000','PublicEndpoint':'http://localhost:'+os.environ.get('RG_FE_OBJECT_PORT','19001'),'AccessKey':'sandbox','SecretKey':storage,'BucketName':'roadguard-private','UseSsl':False},'Logging':{'LogLevel':{'Default':'Warning','Microsoft':'Warning'}}}
for name,data in files.items():
    f=p/name; f.write_text(data); f.chmod(0o444)
f=p/'config.json'; f.write_text(json.dumps(config)); f.chmod(0o444)
print('Private handoff state initialized; values omitted.')
