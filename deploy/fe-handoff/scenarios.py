"""Local handoff scenario setup. Calls real API; never fabricates approved states."""
import base64,hashlib,http.client,json,pathlib,re,time,urllib.request,urllib.error,urllib.parse,uuid
VERSION=1
BASE='http://api:8080'
VIDEO_HASH='152c804d61a49960bb3307501c68da4baccb68b264416d657b2533df0476da15'
def geometry_input(suffix):
 return {'sourceKind':'COORDINATES','sourceCrs':32648,'stationOriginMeters':0,'changeReason':'MOCK_AI_DEVELOPMENT_DATA','coordinates':[{'x':500000,'y':1100000},{'x':500200,'y':1100000}],'widthProfile':[{'fromOffsetMeters':0,'toOffsetMeters':200,'widthMeters':8}],'surveyWidthMeters':12,'roadCode':'MOCK-'+suffix.upper()}


def validate(config):
 h=config.get('Handoff',{});owner=h.get('Owner','')
 c=config.get('RoadGuardDatabase',{}).get('ConnectionString','')
 if not (h.get('Enabled') is True and h.get('ScenarioVersion')==VERSION and re.fullmatch('[0-9a-f]{32}',owner) and config.get('Sandbox',{}).get('Enabled') is True and c.startswith(f'Server=sql;Database=RoadGuard_FE_{owner};')):
  raise ValueError('Scenario setup requires an explicitly opted-in owned local handoff profile')
 return owner

class Journal:
 def __init__(self,path,owner):
  self.path=path;self.data=json.loads(path.read_text()) if path.exists() else {'version':VERSION,'owner':owner,'steps':{}}
  if self.data.get('version')!=VERSION or self.data.get('owner')!=owner:raise ValueError('Scenario journal owner/version mismatch; no automatic upgrade')
 def save(self):
  self.path.parent.mkdir(parents=True,exist_ok=True);temp=self.path.with_suffix('.tmp');temp.write_text(json.dumps(self.data));temp.chmod(0o600);temp.replace(self.path)
 def once(self,key,action):
  if key not in self.data['steps']:
   self.data['steps'][key]=action();self.save()
  return self.data['steps'][key]

def request(path,method='GET',body=None,token=None,headers=None,metadata=False):
 h=dict(headers or {})
 if token:h['Authorization']='Bearer '+token
 if body is not None:h['Content-Type']='application/json'
 r=urllib.request.Request(BASE+path,data=None if body is None else json.dumps(body).encode(),method=method,headers=h)
 try:
  with urllib.request.urlopen(r,timeout=20) as response:
   value=json.loads(response.read() or b'null')
   return (response.status,value,dict(response.headers)) if metadata else (response.status,value)
 except urllib.error.HTTPError as error:
  # Do not include bodies, tokens or signed URLs in startup logs.
  raise RuntimeError(f'Scenario API rejected {method} {path.split("?")[0]}: HTTP {error.code}') from None

def upload(url,payload):
 u=urllib.parse.urlsplit(url)
 if u.scheme!='http' or u.hostname!='localhost' or u.username or u.password:raise ValueError('Only local handoff signed storage URLs are accepted')
 c=http.client.HTTPConnection('minio',9000,timeout=20)
 try:
  c.request('PUT',u.path+'?'+u.query,body=payload,headers={'Host':u.netloc});r=c.getresponse();r.read()
  if r.status!=200:raise RuntimeError(f'Local synthetic object PUT failed: HTTP {r.status}')
  return r.getheader('ETag')
 finally:c.close()

def main():
 config=json.loads(pathlib.Path('/handoff/config.json').read_text());owner=validate(config)
 # Named progress volume, not host paths. Serial Compose singleton; no Docker socket.
 import fcntl
 lock=pathlib.Path('/progress/setup.lock').open('w');fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
 j=Journal(pathlib.Path('/progress/scenarios.json'),owner)
 accounts=json.loads(pathlib.Path('/handoff/demo-accounts.json').read_text())
 def login(key):
  a=next(x for x in accounts if x['key']==key)
  return request('/api/v1/auth/android/login','POST',{'email':a['email'],'password':a['password']})[1]['accessToken']
 tokens={key:login(key) for key in ['supervisor','pm-a','pm-b','reporter-a','operator-a','operator-b']}
 if j.data.get('complete'):
  if 'ai-confirm-a' in j.data['steps']:
   from demo_repairs import initialize as initialize_repairs
   initialize_repairs(j,tokens,request)
  from demo_collections import initialize as initialize_collections
  initialize_collections(j,tokens,request)
  print('Existing scenarios preserved; approved collection demo reconciled.');return
 def mutate(key,path,body,actor,method='POST',headers=None):
  h={'Idempotency-Key':'handoff-v1-'+key,**(headers or {})}
  # Persist inputs before request, so lost acknowledgements replay the identical operation.
  saved=j.once('input-'+key,lambda:{'body':body,'headers':h})
  return j.once(key,lambda:request(path,method,saved['body'],tokens[actor],saved['headers'])[1])
 for suffix,pid in [('a','72f84201-c001-4000-8000-000000000001'),('b','72f84201-c001-4000-8000-000000000002')]:
  root='/api/v1/projects/'+pid
  p=request(root+'/work-package',token=tokens['supervisor'])[1]
  if not p.get('engineeringUtmSrid'):
   mutate('project-'+suffix,root,{'name':p['name'],'description':p.get('description'),'startDate':p.get('startDate'),'endDate':p.get('endDate'),'engineeringUtmSrid':32648,'operationId':str(uuid.uuid5(uuid.NAMESPACE_URL,'handoff-project-'+suffix)),'expectedRowVersion':p['rowVersion']},'supervisor','PUT')
  mutate('road-'+suffix,root+'/road-sections',{'code':'DEMO-'+suffix.upper(),'name':'Synthetic demo road '+suffix.upper(),'srid':32648,'coordinates':[{'x':500000,'y':1200000},{'x':500250,'y':1200250}],'effectiveFrom':'2026-01-01T00:00:00Z','changeReason':'Development synthetic scenario','operationId':str(uuid.uuid5(uuid.NAMESPACE_URL,'handoff-road-'+suffix))},'supervisor')
 content=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=');digest=hashlib.sha256(content).hexdigest()
 u=mutate('upload','/api/v1/reporter-evidence/uploads',{'fileName':'synthetic-demo.png','mediaType':'image/png','sizeBytes':len(content),'checksumSha256':digest},'reporter-a');uid=u['id'];fid=u['fileId'];root='/api/v1/reporter-evidence/uploads/'+uid
 if 'input-complete-upload' in j.data['steps'] and 'complete-upload' not in j.data['steps']:
  mutate('complete-upload',root+'/complete',None,'reporter-a')
 if 'complete-upload' not in j.data['steps']:
  # Regenerate the short-lived part URL on an interrupted setup; no capability saved to journal.
  part=request(root+'/part-urls','POST',{'partNumbers':[1]},tokens['reporter-a'],{'Idempotency-Key':'handoff-part-'+uuid.uuid4().hex})[1]['parts'][0]
  etag=upload(part['url'],content)
  version=request(root,token=tokens['reporter-a'])[1]['version']
  mutate('complete-upload',root+'/complete',{'parts':[{'partNumber':1,'eTag':etag}],'checksumSha256':digest},'reporter-a',headers={'If-Match':'"'+version+'"'})
 for _ in range(45):
  f=request('/api/v1/reporter-evidence/files/'+fid,token=tokens['reporter-a'])[1]
  if f['status']=='VERIFIED':break
  time.sleep(2)
 else:raise RuntimeError('Synthetic evidence verification did not complete within 90 seconds')
 mutate('incident','/api/v1/reports',{'description':'Synthetic FE demo incident; no approved diagnosis or repair state.','evidence':[{'fileId':fid,'fileVersion':f['version'],'locationSource':'UNKNOWN'}]},'reporter-a')
 for suffix,pid in [('a','72f84201-c001-4000-8000-000000000001'),('b','72f84201-c001-4000-8000-000000000002')]:
  e=mutate('export-'+suffix,'/api/v1/projects/'+pid+'/exports',{'kind':'DOSSIER','format':'PDF','includeOriginalFiles':False},'pm-'+suffix)
  for _ in range(60):
   state=request('/api/v1/projects/'+pid+'/exports/'+e['id'],token=tokens['pm-'+suffix])[1]['status']
   if state=='SUCCEEDED':break
   if state=='FAILED':raise RuntimeError('Synthetic export worker failed; inspect job status')
   time.sleep(2)
  else:raise RuntimeError('Synthetic export worker did not complete within 120 seconds')
 if config.get('Anh02',{}).get('MockEnabled'):
  from demo_ai import initialize
  initialize(j,tokens,request,upload,geometry_input,VIDEO_HASH)
  from demo_repairs import initialize as initialize_repairs
  initialize_repairs(j,tokens,request)
 from demo_collections import initialize as initialize_collections
 initialize_collections(j,tokens,request)
 j.data['complete']=True;j.save();print('Synthetic roads A/B, verified private evidence, reporter incident and PDF exports ready. Advanced approved workflow states are not fabricated.')

if __name__=='__main__':
 try:main()
 except Exception as error:
  print('Scenario setup FAILED: '+type(error).__name__+'; inspect API/worker health. No credentials or capabilities logged.',flush=True);raise SystemExit(1)
