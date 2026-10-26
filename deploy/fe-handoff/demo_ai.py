"""Owned development scenarios using existing BE routes; no external AI callback."""
import hashlib,pathlib,time,uuid
MODEL='72f84201-d033-4000-8000-000000000001'
DEVICE='5f1e9338-3f89-48f0-9d90-097b4ced4e62'

def initialize(j,tokens,request,upload,geometry_input,video_hash):
 media=pathlib.Path('/fixtures/synthetic-road-v1.mp4').read_bytes()
 if hashlib.sha256(media).hexdigest()!=video_hash:raise ValueError('Synthetic asset hash mismatch')
 def command(key,path,body,actor,etag=None):
  h={'Idempotency-Key':'handoff-ai-v1-'+key}
  if etag:h['If-Match']=etag
  saved=j.once('input-ai-'+key,lambda:{'body':body,'headers':h})
  return j.once('ai-'+key,lambda:list(request(path,'POST',saved['body'],tokens[actor],saved['headers'],metadata=True)))
 def version(reply):
  return next(v for k,v in reply[2].items() if k.lower()=='etag')
 def poll(path,actor,wanted):
  for _ in range(60):
   value=request(path,token=tokens[actor])[1]
   if value['status']==wanted:return value
   if value['status']=='FAILED':raise RuntimeError('Synthetic workflow worker failed')
   time.sleep(2)
  raise RuntimeError('Synthetic workflow worker readiness timeout')
 for suffix,pid in [('a','72f84201-c001-4000-8000-000000000001'),('b','72f84201-c001-4000-8000-000000000002')]:
  pm='pm-'+suffix;op='operator-'+suffix;root='/api/v1/projects/'+pid
  draft=command('draft-'+suffix,root+'/road-geometry-drafts',geometry_input(suffix),pm)
  road=command('confirm-'+suffix,root+'/road-geometry-drafts/'+draft[1]['id']+'/confirm',{'expectedCurrentVersionId':None,'effectiveFrom':'2026-10-02T00:00:00Z','reason':'Synthetic Development geometry'},'supervisor',version(draft))[1]
  route=road['routeVersionId'];section=road['roadSectionId'];sets=root+'/road-sections/'+section+'/versions/'+route+'/segment-sets'
  segment_set=command('set-'+suffix,sets,{'targetLengthMeters':100,'remainderMode':'KEEP'},pm);sid=segment_set[1]['id'];segments=[s['id'] for s in segment_set[1]['segments']]
  command('publish-'+suffix,sets+'/'+sid+'/publish',{'expectedPublishedSetId':None,'reason':'Synthetic Development geometry'},pm,version(segment_set))
  scope={'routeVersionId':route,'segmentSetId':sid,'segmentIds':segments,'targetBand':'SURFACE'}
  plan=command('plan-'+suffix,root+'/survey-plans',{'scope':[scope],'plannedAt':'2026-10-02T08:00:00Z','surveyType':'BASELINE'},pm)[1]
  actor_id='72f84201-a001-4000-8000-00000000000'+('6' if suffix=='a' else '7')
  task=command('task-'+suffix,root+'/survey-tasks',{'scope':[scope],'surveyType':'BASELINE','operatorId':actor_id,'dueAt':'2026-10-03T08:00:00Z','accessPoint':None,'planId':plan['id']},pm)
  taskroot='/api/v1/survey-tasks/'+task[1]['id']
  accepted=command('accept-'+suffix,taskroot+'/accept',None,op,version(task))
  u=command('upload-'+suffix,'/api/v1/uploads',{'purpose':'SURVEY_VIDEO','projectId':pid,'targetId':task[1]['id'],'fileName':'MOCK_AI_DEVELOPMENT_DATA.mp4','mediaType':'video/mp4','sizeBytes':len(media),'checksumSha256':video_hash},op)[1]
  uploadroot='/api/v1/uploads/'+u['id']
  if 'ai-complete-'+suffix not in j.data['steps']:
   if 'input-ai-complete-'+suffix not in j.data['steps']:
    part=request(uploadroot+'/part-urls','POST',{'partNumbers':[1]},tokens[op],{'Idempotency-Key':'handoff-ai-part-'+uuid.uuid4().hex})[1]['parts'][0]
    tag=upload(part['url'],media)
    read=request(uploadroot,token=tokens[op],metadata=True)
    command('complete-'+suffix,uploadroot+'/complete',{'parts':[{'partNumber':1,'eTag':tag}],'checksumSha256':video_hash},op,version(read))
   else:command('complete-'+suffix,uploadroot+'/complete',None,op)
  poll('/api/v1/files/'+u['fileId'],op,'VERIFIED')
  dataset=command('dataset-'+suffix,taskroot+'/datasets',{'videoFileIds':[u['fileId']],'telemetryFileIds':[],'recordedAt':'2026-10-02T08:00:00Z','deviceId':DEVICE,'scope':[scope],'pairs':[{'videoFileId':u['fileId'],'telemetryFileId':None,'timeOffsetMilliseconds':0}]},op,version(accepted))[1]
  package=request(root+'/geometry-package?routeVersionId='+route+'&segmentSetId='+sid,token=tokens[pm],metadata=True)
  for fixture in ['synthetic-road-v1','synthetic-empty-v1','synthetic-multiclass-v1']:
   run=command('run-'+suffix+'-'+fixture,root+'/ai-mock-runs',{'datasetId':dataset['id'],'scope':scope,'modelVersionId':MODEL,'preprocessingVersion':'fixture-preprocess.v1','configVersion':'fixture-config.v1','fixtureVersion':fixture,'stage':'VIDEO_ANALYSIS','expectedGeometryVersion':version(package).strip('"')},pm)[1]
   path=root+'/ai-mock-runs/'+run['id'];poll(path,pm,'SUCCEEDED')
   # Result contains immutable IDs/hashes, never capabilities or credentials.
   j.once('ai-result-'+suffix+'-'+fixture,lambda:request(path+'/result',token=tokens[pm])[1])
