export class CheckError extends Error {}

export function check(condition: unknown, code: string): asserts condition {
  if (!condition) throw new CheckError(code);
}

const emptyGuid = '00000000-0000-0000-0000-000000000000';

export function guid(value: unknown): string {
  check(typeof value === 'string' && /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value) &&
    value !== emptyGuid, 'expected_id');
  return value.toLowerCase();
}

function parseUrl(value: unknown): URL | undefined {
  if (typeof value !== 'string') return undefined;
  try {
    return new URL(value);
  } catch {
    return undefined;
  }
}

export function configuredOrigin(value: unknown, kind: 'api' | 'storage', disposable = false): string {
  const url = parseUrl(value);
  const allowed = disposable ? url?.protocol === 'http:' && url.hostname === '127.0.0.1' : url?.protocol === 'https:';
  check(url && allowed && !url.username && !url.password &&
    url.pathname === '/' && !url.search && !url.hash, `invalid_${kind}_origin`);
  return url.origin;
}

export function requireStorageUrl(value: unknown, origin: string): string {
  const url = parseUrl(value);
  check(typeof value === 'string' && url && url.origin === origin &&
    (url.protocol === 'https:' || (url.protocol === 'http:' && url.hostname === '127.0.0.1')) &&
    !url.username && !url.password && !url.hash && url.searchParams.has('X-Amz-Signature'),
  'storage_origin_or_signature_mismatch');
  return value;
}

export function assertMutation(value: string | undefined): void {
  check(value === 'true', 'mutation_not_enabled');
}

export function byteRange(size: number, partSize: number, part: number) {
  const start = (part - 1) * partSize;
  const length = Math.min(partSize, size - start);
  check([size, partSize, part, start].every(Number.isSafeInteger) &&
    start >= 0 && part >= 1 && length > 0, 'invalid_part_range');
  return { start, end: start + length - 1, length };
}

export function workProgress(state: string): 'done' | 'pending' | 'failed' {
  if (state === 'Ingested') return 'done';
  if (['Pending', 'Admitted', 'Running', 'ArtifactsReady'].includes(state)) return 'pending';
  return 'failed';
}

export function telemetryEvidence(view: {
  headRevisionId: string; measuredTelemetryStatus: string; positionMeaning: string; intervals: unknown[];
}) {
  check(view.positionMeaning === 'DRONE_REFERENCE_NOT_GROUND_LOCATION' &&
    Array.isArray(view.intervals) &&
    ['EXTERNAL_SRT', 'PAIRED_SRT', 'EMBEDDED_SRT', 'MISSING_TELEMETRY'].includes(view.measuredTelemetryStatus),
  'telemetry_contract_mismatch');
  return {
    revisionIdPrefix: view.headRevisionId === emptyGuid ? 'ABSENT' : guid(view.headRevisionId).slice(0, 8),
    measuredTelemetryStatus: view.measuredTelemetryStatus,
    positionIntervals: view.intervals.length,
  };
}
