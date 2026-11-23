import json,requests,uuid
from pathlib import Path
root=Path('D:/RoadGuard-AI-LocalTest/pm-crew')
f=json.loads((root/'fixture.json').read_text())
base=f['backendUrl']; result={}
clients={}
for role in ('PM','CREW'):
    c=requests.Session(); r=c.post(base+'/api/v1/auth/login',json=f['accounts'][role],timeout=30)
    result[role+'_login']=r.status_code
    if r.status_code!=200: raise RuntimeError('Actual login failed: '+str(r.status_code)+' '+r.text)
    c.headers['Authorization']='Bearer '+r.json()['accessToken'];clients[role]=c
p=f['projectId'];d=f['detectionIds'][0]
r=clients['PM'].get(base+f'/api/v1/projects/{p}/ai-candidates/{d}',timeout=30);result['pm_candidate_read']=r.status_code
(root/'candidate-read.json').write_text(r.text)
if r.status_code!=200: raise RuntimeError('Candidate read failed '+str(r.status_code)+' '+r.text)
candidate=r.json()
r=clients['PM'].get(base+f'/api/v1/projects/{p}/ai-videos/{f["workId"]}/telemetry',timeout=30)
result['pm_telemetry_read']=r.status_code
if r.status_code!=200: raise RuntimeError('Telemetry read failed '+str(r.status_code)+' '+r.text)
result['telemetry_status']=r.json().get('status')
result['candidate_keys']=list(candidate)
body={'sourceKind':'AI_DETECTION','sourceId':d,'sourceVersion':candidate.get('sourceVersion'),'geometryVersion':candidate.get('geometryVersion'),'decision':'REJECT','classification':None,'reason':'Negative Crew role verification'}
r=clients['CREW'].post(base+f'/api/v1/projects/{p}/candidate-decisions',json=body,headers={'Idempotency-Key':str(uuid.uuid4())},timeout=30)
result['crew_candidate_decision']=r.status_code
result['crew_candidate_decision_code']=r.json().get('code')
if r.status_code!=403: raise RuntimeError('Crew decision must be forbidden')
result['detectionCount']=len(f['detectionIds']);result['chainageRange']=f['chainageRange'];result['health']=requests.get(base+'/health',timeout=10).status_code
result['origin_guard']=requests.get(base+'/health',headers={'Origin':'http://evil.invalid'},timeout=10).status_code
if result['health']!=200 or result['origin_guard']!=403: raise RuntimeError('Loopback health/origin guard failed')
(root/'http-smoke-evidence.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
