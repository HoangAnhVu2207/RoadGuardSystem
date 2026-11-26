import { createHash } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { readFile, realpath, stat } from 'node:fs/promises';
import { isAbsolute, relative, resolve, sep } from 'node:path';
import { Readable } from 'node:stream';
import { setTimeout as delay } from 'node:timers/promises';
import { etag, idempotency, safeCode, type RecordEvent, type Session } from './api.js';
import { byteRange, check, CheckError, guid, requireStorageUrl, telemetryEvidence, workProgress } from './contract.js';

type Clip = { key: string };
type Asset = { path: string; size: number; sha256: string; modifiedAt: number; kind: 'mp4' | 'srt' };
type Video = { clip: Clip; video: Asset; telemetry?: Asset };
type Band = { routeVersionId: string; segmentSetId: string; segmentIds: string[]; targetBand: string };
type Task = { id: string; projectId: string; operatorId: string; status: string; scope: Band[] };
type Target = { taskId: string; projectId: string; deviceId: string; recordedAt: string };
type Upload = { id: string; fileId: string; status: string; partSizeBytes: number };
type Work = { sourceVideoFileId: string; workId: string; jobId: string; currentAttemptId: string; state: string; errorCode?: string };

const repositoryRoot = resolve(import.meta.dirname, '../../..');

export function selectedClips(input: string | undefined): readonly Clip[] {
  const keys = (input ?? 'KENH_DONG,DUONG_SO_2').split(',').map(key => key.trim());
  check(keys.every(key => /^[A-Z][A-Z0-9_]{0,39}$/.test(key)) && new Set(keys).size === keys.length, 'invalid_clip_selection');
  return keys.map(key => ({ key }));
}

async function externalPath(source: string | undefined, code: string): Promise<string> {
  check(source && isAbsolute(source), code);
  const [root, candidate] = await Promise.all([realpath(repositoryRoot), realpath(source)])
    .catch(() => { throw new CheckError(code); });
  const fromRepository = relative(root, candidate);
  check(isAbsolute(fromRepository) || fromRepository.startsWith(`..${sep}`), code);
  return candidate;
}

export async function protectedProjectIdsFromEnv(): Promise<Set<string>> {
  const path = await externalPath(process.env.E2E_PROTECTED_PROJECT_IDS_FILE, 'missing_protected_project_file');
  const content = await readFile(path, 'utf8').catch(() => { throw new CheckError('protected_project_file_unreadable'); });
  const ids = new Set<string>();
  for (const [index, raw] of content.split(/\r?\n/).entries()) {
    const value = raw.replace(/#.*/, '').trim();
    if (!value) continue;
    check(/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value), `invalid_protected_project_id_${index + 1}`);
    ids.add(value.toLowerCase());
  }
  check(ids.size > 0, 'protected_project_file_empty');
  return ids;
}

export function assertWritableProject(projectId: string, protectedProjectIds: Set<string>): void {
  check(!protectedProjectIds.has(projectId.toLowerCase()), 'protected_project_write_blocked');
}

export function targetFromEnv(clip: Clip): Target {
  const prefix = `E2E_${clip.key}`;
  const recordedAt = process.env[`${prefix}_RECORDED_AT`];
  check(recordedAt && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$/.test(recordedAt) &&
    Number.isFinite(Date.parse(recordedAt)) && Date.parse(recordedAt) <= Date.now(), 'invalid_recorded_at');
  return {
    projectId: guid(process.env[`${prefix}_PROJECT_ID`]),
    taskId: guid(process.env[`${prefix}_TASK_ID`]),
    deviceId: guid(process.env[`${prefix}_DEVICE_ID`]),
    recordedAt,
  };
}

export async function inspectVideo(clip: Clip, record: RecordEvent): Promise<Video> {
  const videoPath = await externalPath(process.env[`E2E_${clip.key}_VIDEO`], 'missing_video_path');
  const video = await inspectAsset(videoPath, 'mp4', record);
  const paired = process.env[`E2E_${clip.key}_SRT`];
  const telemetry = paired === undefined ? undefined
    : await inspectAsset(await externalPath(paired, 'invalid_srt_path'), 'srt', record);
  return { clip, video, telemetry };
}

async function inspectAsset(path: string, kind: Asset['kind'], record: RecordEvent): Promise<Asset> {
  check(path.toLowerCase().endsWith(`.${kind}`), 'unexpected_asset_extension');
  try {
    const before = await stat(path);
    check(before.isFile() && before.size > 0, 'empty_or_invalid_video');
    const hash = createHash('sha256');
    let firstChunk = true;
    for await (const chunk of createReadStream(path)) {
      if (firstChunk && kind === 'mp4') {
        check(Buffer.isBuffer(chunk) && chunk.length >= 12 && chunk.toString('ascii', 4, 8) === 'ftyp',
          'unexpected_mp4_header');
        firstChunk = false;
      }
      hash.update(chunk);
    }
    const after = await stat(path);
    check(before.mtimeMs === after.mtimeMs && before.size === after.size, 'video_changed_during_hash');
    const sha256 = hash.digest('hex');
    record({ step: kind === 'mp4' ? 'local_video' : 'local_telemetry', bytes: after.size, checksumPrefix: sha256.slice(0, 12) });
    return { path, kind, size: after.size, sha256, modifiedAt: after.mtimeMs };
  } catch (error) {
    if (error instanceof CheckError) throw error;
    throw new CheckError('local_video_read_failed');
  }
}

const taskPath = (id: string) => `/api/v1/survey-tasks/${id}`;
const discovery = (projectId: string, datasetId: string) =>
  `/api/v1/projects/${projectId}/ai-videos?datasetId=${datasetId}`;

async function readTask(session: Session, target: Target, allowAssigned = false) {
  const { body, etag: version } = await session.call<Task>('survey_task', taskPath(target.taskId), 200);
  check(guid(body?.id) === target.taskId && guid(body.projectId) === target.projectId &&
    guid(body.operatorId) === session.actorId, 'survey_task_scope_mismatch');
  check(['ACCEPTED', 'IN_PROGRESS'].includes(body.status) || (allowAssigned && body.status === 'NEW_ASSIGNED'),
    'survey_task_not_submittable');
  check(Array.isArray(body.scope) && body.scope.length > 0, 'survey_task_scope_missing');
  const scope = body.scope.map(band => {
    check(Array.isArray(band.segmentIds) && band.segmentIds.length > 0 &&
      ['SURFACE', 'LEFT_EDGE', 'RIGHT_EDGE'].includes(band.targetBand), 'survey_band_scope_invalid');
    return {
      routeVersionId: guid(band.routeVersionId), segmentSetId: guid(band.segmentSetId),
      segmentIds: band.segmentIds.map(guid), targetBand: band.targetBand,
    };
  });
  return { scope, version: etag(version), status: body.status };
}

export async function preflightTarget(session: Session, target: Target) {
  await readTask(session, target, true);
  let cursor: string | undefined;
  let found = false;
  for (let page = 0; page < 20 && !found; page++) {
    const query = cursor ? `&cursor=${encodeURIComponent(cursor)}` : '';
    const { body } = await session.call<{ items: { id: string }[]; nextCursor?: string }>(
      'eligible_drones', `${taskPath(target.taskId)}/eligible-drones?limit=100${query}`, 200);
    check(Array.isArray(body?.items), 'eligible_drones_contract_mismatch');
    found = body.items.some(item => guid(item.id) === target.deviceId);
    cursor = body.nextCursor;
    if (!cursor) break;
  }
  check(found, 'configured_drone_not_eligible');
  // Guid.Empty reaches the adopted REAL route and fails validation before dataset lookup.
  const { body } = await session.call<{ code: string }>(
    'real_route', discovery(target.projectId, '00000000-0000-0000-0000-000000000000'), 422);
  check(body?.code === 'invalid_video_query', 'real_route_contract_mismatch');
}

async function poll<T>(timeoutMs: number, failure: string, read: () => Promise<T | undefined>) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const value = await read();
    if (value !== undefined) return value;
    if (Date.now() < deadline) await delay(Math.min(15_000, deadline - Date.now()));
  }
  throw new CheckError(failure);
}

async function putPart(video: Asset, partSize: number, part: number, url: string, timeoutMs: number) {
  const { start, end, length } = byteRange(video.size, partSize, part);
  const stream = createReadStream(video.path, { start, end });
  try {
    const options = {
      method: 'PUT', redirect: 'error', duplex: 'half',
      headers: { 'Content-Length': String(length) },
      body: Readable.toWeb(stream) as unknown as BodyInit,
      signal: AbortSignal.timeout(timeoutMs),
    } as const;
    const response = await fetch(url, options).catch(() => { throw new CheckError('storage_put_transport_error'); });
    try {
      check(response.status === 200, `storage_put_http_${response.status}`);
      return etag(response.headers.get('etag'));
    } finally {
      await response.body?.cancel();
    }
  } finally {
    stream.destroy();
  }
}

async function upload(session: Session, target: Target, video: Asset, origin: string, label: string, record: RecordEvent) {
  const before = await stat(video.path).catch(() => { throw new CheckError('video_unavailable_before_upload'); });
  check(before.size === video.size && before.mtimeMs === video.modifiedAt, 'video_changed_before_upload');
  const { body: created } = await session.call<Upload>('upload_create', '/api/v1/uploads', 201, {
    method: 'POST', headers: idempotency(),
    data: {
      purpose: video.kind === 'mp4' ? 'SURVEY_VIDEO' : 'TELEMETRY',
      projectId: target.projectId, targetId: target.taskId,
      fileName: `DEMO-${label}.${video.kind}`, mediaType: video.kind === 'mp4' ? 'video/mp4' : 'application/x-subrip',
      sizeBytes: video.size, checksumSha256: video.sha256,
    },
  });
  const uploadId = guid(created?.id);
  const fileId = guid(created?.fileId);
  record({ step: 'upload_started', uploadIdPrefix: uploadId.slice(0, 8), fileIdPrefix: fileId.slice(0, 8) });
  const partSize = created.partSizeBytes;
  check(Number.isSafeInteger(partSize) && partSize > 0 && Math.ceil(video.size / partSize) <= 10_000, 'invalid_multipart_size');
  const uploadPath = `/api/v1/uploads/${uploadId}`;
  const parts: { partNumber: number; eTag: string }[] = [];
  // A signed URL is minted immediately before each PUT so slow clients do not exhaust its TTL in a batch.
  const uploadDeadline = Date.now() + 25 * 60_000;
  const partCount = Math.ceil(video.size / partSize);
  for (let part = 1; part <= partCount; part++) {
    check(Date.now() < uploadDeadline, 'upload_deadline_reached');
    const { body } = await session.call<{ parts: { partNumber: number; url: string }[] }>(
      'part_url', `${uploadPath}/part-urls`, 200,
      { method: 'POST', headers: idempotency(), data: { partNumbers: [part] } });
    check(body?.parts?.length === 1 && body.parts[0]?.partNumber === part, 'part_url_response_mismatch');
    const url = requireStorageUrl(body.parts[0].url, origin);
    const remaining = Math.min(180_000, uploadDeadline - Date.now());
    check(remaining > 0, 'upload_deadline_reached');
    parts.push({ partNumber: part, eTag: await putPart(video, partSize, part, url, remaining) });
  }
  const uploaded = await stat(video.path).catch(() => { throw new CheckError('video_unavailable_after_upload'); });
  check(uploaded.size === video.size && uploaded.mtimeMs === video.modifiedAt, 'video_changed_during_upload');
  record({ step: 'storage_puts_completed', partCount });
  const fresh = await session.call<Upload>('upload_read', uploadPath, 200);
  await session.call('upload_complete', `${uploadPath}/complete`, 202, {
    method: 'POST', headers: { ...idempotency(), 'If-Match': etag(fresh.etag) },
    data: { parts, checksumSha256: video.sha256 },
  });
  await poll(4 * 60_000, 'upload_verification_timeout', async () => {
    const { body } = await session.call<Upload>('upload_verify', uploadPath, 200);
    check(guid(body?.id) === uploadId && guid(body.fileId) === fileId, 'upload_readback_identity_mismatch');
    if (body.status === 'VERIFIED') return true;
    check(body.status === 'VERIFYING', 'upload_verification_failed_or_unknown');
  });
  record({ step: 'upload_verified', status: 'VERIFIED', asset: video.kind });
  return fileId;
}

export async function runVideo(
  session: Session, target: Target, video: Video, origin: string, runId: string, record: RecordEvent,
) {
  const label = `${runId}-${video.clip.key}`;
  const task = await readTask(session, target, true);
  if (task.status === 'NEW_ASSIGNED') {
    await session.call('survey_task_accept', `${taskPath(target.taskId)}/accept`, 200, {
      method: 'POST', headers: { ...idempotency(), 'If-Match': task.version },
    });
    await readTask(session, target);
  }
  const fileId = await upload(session, target, video.video, origin, label, record);
  const telemetryId = video.telemetry && await upload(session, target, video.telemetry, origin, label, record);
  const { scope, version } = await readTask(session, target);
  const submit = {
    method: 'POST', headers: { ...idempotency(), 'If-Match': version },
    data: {
      videoFileIds: [fileId], telemetryFileIds: telemetryId ? [telemetryId] : [],
      ...(telemetryId ? { pairs: [{ videoFileId: fileId, telemetryFileId: telemetryId, timeOffsetMilliseconds: 0 }] } : {}),
      recordedAt: target.recordedAt, deviceId: target.deviceId, scope,
    },
  };
  const { body: dataset } = await session.call<{ id: string; surveyTaskId: string }>(
    'dataset_submit', `${taskPath(target.taskId)}/datasets`, 201, submit);
  const datasetId = guid(dataset?.id);
  check(guid(dataset.surveyTaskId) === target.taskId, 'dataset_task_mismatch');
  const { body: replay } = await session.call<{ id: string }>(
    'dataset_submit_replay', `${taskPath(target.taskId)}/datasets`, 201, submit);
  check(guid(replay?.id) === datasetId, 'dataset_replay_changed_identity');
  record({ step: 'dataset_created', datasetIdPrefix: datasetId.slice(0, 8) });
  const { body: detail } = await session.call<{
    id: string; surveyTaskId: string; projectId: string; sourceFiles: { fileId: string; checksumSha256: string }[];
    pairs?: { videoFileId: string; telemetryFileId: string; timeOffsetMilliseconds: number }[];
  }>('dataset_read', `/api/v1/datasets/${datasetId}`, 200);
  check(guid(detail?.id) === datasetId && guid(detail.surveyTaskId) === target.taskId &&
    guid(detail.projectId) === target.projectId &&
    detail.sourceFiles?.some(source => guid(source.fileId) === fileId && source.checksumSha256 === video.video.sha256),
  'dataset_readback_mismatch');
  if (telemetryId) {
    check(detail.sourceFiles.some(source => guid(source.fileId) === telemetryId && source.checksumSha256 === video.telemetry?.sha256) &&
      detail.pairs?.length === 1 && guid(detail.pairs[0].videoFileId) === fileId &&
      guid(detail.pairs[0].telemetryFileId) === telemetryId && detail.pairs[0].timeOffsetMilliseconds === 0,
    'telemetry_pair_readback_mismatch');
  }

  let previousState: string | undefined;
  const work = await poll(12 * 60_000, 'real_work_timeout', async () => {
    const { body } = await session.call<{ items: Work[] }>(
      'real_discovery', discovery(target.projectId, datasetId), 200);
    check(Array.isArray(body?.items), 'real_discovery_contract_mismatch');
    const matching = body.items.filter(item => guid(item.sourceVideoFileId) === fileId);
    check(matching.length <= 1, 'duplicate_real_video_work');
    const item = matching[0];
    if (!item) return;
    const progress = workProgress(item.state);
    if (item.state !== previousState) {
      record({
        step: 'real_work_state', workIdPrefix: guid(item.workId).slice(0, 8), jobIdPrefix: guid(item.jobId).slice(0, 8),
        attemptIdPrefix: guid(item.currentAttemptId).slice(0, 8),
        status: progress === 'failed' && !['RetryableFailed', 'TerminalFailed', 'Cancelled'].includes(item.state)
          ? 'UNKNOWN' : item.state, code: safeCode(item.errorCode),
      });
      previousState = item.state;
    }
    check(progress !== 'failed', 'real_work_failed');
    if (progress === 'done') return item;
  });
  const { body: job } = await session.call<{ id: string; status: string; resultId: string | null }>(
    'processing_job_read', `/api/v1/processing-jobs/${guid(work.jobId)}`, 200);
  check(guid(job?.id) === guid(work.jobId) && job.status === 'SUCCEEDED', 'processing_job_readback_mismatch');
  const { body: telemetry } = await session.call<{
    workId: string; attemptId: string; sourceVideoFileId: string; headRevisionId: string;
    measuredTelemetryStatus: string; positionMeaning: string; intervals: unknown[];
  }>('telemetry_read', `/api/v1/projects/${target.projectId}/ai-videos/${guid(work.workId)}/telemetry`, 200);
  check(guid(telemetry?.workId) === guid(work.workId) && guid(telemetry.attemptId) === guid(work.currentAttemptId) &&
    guid(telemetry.sourceVideoFileId) === fileId && Array.isArray(telemetry.intervals), 'telemetry_readback_mismatch');
  record({
    step: 'real_result', evidence: 'AI_WORKER_VERIFIED', ...telemetryEvidence(telemetry),
    legacyResultReference: job.resultId ? guid(job.resultId).slice(0, 8) : 'ABSENT',
  });
}
